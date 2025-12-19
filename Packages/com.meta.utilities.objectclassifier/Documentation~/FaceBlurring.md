# Face Detection and Blurring

Face blurring provides privacy protection by automatically detecting and anonymizing faces in camera images. This feature uses a neural network to locate faces and applies a pixelation blur effect to obscure them. This is used in the Spatial Lingo demo to protect user privacy when submitting an image to Llama API for analysis.

## RetinaFace Model

The face detection system uses RetinaFace, a robust single-stage face detector based on the ResNet50 architecture. RetinaFace performs multi-scale face detection using anchor boxes at different feature pyramid levels.

For more information on RetinaFace, visit: [`RetinaFace Whitepaper`](https://arxiv.org/abs/1905.00641).

The specific model used in this project is available under MIT license: [`OpenVINO RetinaFace Model`](https://storage.openvinotoolkit.org/repositories/open_model_zoo/public/2022.1/retinaface-resnet50-pytorch/).

### Model Architecture

The model expects a 640x640 pixel input image with BGR channel ordering. Pixel values are normalized by scaling to 0-255 range and subtracting channel means `[104, 117, 123]` (standard values expected by the model).

The model uses anchor-based detection with three feature pyramid levels:

- **Level 1**: Anchor sizes [16, 32] with step 8 — detects small faces
- **Level 2**: Anchor sizes [64, 128] with step 16 — detects medium faces
- **Level 3**: Anchor sizes [256, 512] with step 32 — detects large faces

For each anchor, the model outputs bounding box coordinates and confidence scores, which are decoded using variance values `[0.1, 0.2]` to produce final face rectangles.

## Unity Inference Engine

The RetinaFace model is loaded as a `.sentis` file and executed using Unity's Inference Engine. The model can be pre-compiled in the editor using the context menu option "Compile and save model" on the `FaceDetection` component, or compiled at runtime from the source `.onnx` model.

For more information on the Unity Inference Engine (Sentis), visit: [`Unity Inference Engine Package`](https://docs.unity3d.com/Packages/com.unity.ai.inference@2.3/manual/index.html).

### Model Compilation

The `FaceDetection.LoadAndCompileModel()` method constructs a functional graph that:

1. Adjusts input pixels (scales by 255, subtracts BGR means)
2. Runs the source model forward pass
3. Decodes bounding boxes from anchor-relative coordinates
4. Optionally applies Non-Maximum Suppression (NMS)

Model weights can be quantized to Float16 or Uint8 to reduce memory usage.

## Post-Processing

Two post-processing strategies are available, controlled by the `UseNMS` flag:

### Non-Maximum Suppression (UseNMS = true)

The model’s NMS layer can run on GPU or CPU depending on the active Sentis backend. While it yields more accurate results, it can introduce stalls or higher latency due to the extra post-processing workload.

### CPU-based Filtering (UseNMS = false, default)

The default approach outputs raw boxes and scores without NMS, then processes them on the CPU using a lighter-weight heuristic:

1. All detections with scores above 0.01 are accepted (intentionally permissive threshold)
2. Detections are converted to `ClassifiedImageObject` format
3. `ImageObjectClassifier.ProcessClassificationTensors` filters and merges overlapping detections
4. Results are limited to 32 faces maximum

*tl;dr* Choosing UseNMS=false prioritizes performance over precision; enabling NMS improves accuracy at the cost of potential frame-time spikes depending on backend and workload.

## Quest CPU Backend

The `QuestCPUBackend` provides a custom inference backend optimized for Quest hardware to prevent frame stalling.

### The Problem: PhysX Thread Starvation

Unity's Burst job system is used by both the Inference Engine and PhysX (physics simulation). On Quest 3, the system is configured with 4 Burst worker threads (set via [`SpatialLingoActivity`](../../../Assets/SpatialLingo/Activity/SpatialLingoActivity.java)).

Large convolution operations in the neural network can occupy all available worker threads. Since PhysX synchronously waits on the main thread for its Burst jobs to complete, this causes frame stalls.

### The Solution: Thread Limiting

The `QuestCPUBackendImpl` overrides the convolution operation to limit parallelization:

```csharp
private const int MAX_THREADS = 2;

void IBackend.Conv(...)
{
    // Split work into batches that use at most 2 threads
    job.Schedule(arrayLength, arrayLength / MAX_THREADS + 1, fenceBeforeJobStart);
}
```

By limiting convolution jobs to 2 threads, at least 2 worker threads remain available for physics, preventing main thread stalls.

### Why CPU Instead of GPU?

The convolution layers in RetinaFace are too large to complete within the frame budget on Quest GPU. Alternative solutions include:

- Reducing input image resolution (requires model modification)
- Using Vulkan async compute API (not implemented by Unity Inference Engine)

CPU inference with thread management provides the most practical solution for real-time performance.

## Blurring Process

The `FaceBlur.BlurFaces` method orchestrates the complete detection and blurring pipeline:

```csharp
public static async Awaitable BlurFaces(
    Model model,
    BackendType backend,
    Texture inputTexture,
    RenderTexture targetTexture,
    bool useNMS,
    int blurResolution = 32)
```

### Pipeline Steps

1. **Texture to Tensor**: Input texture is converted to a tensor with BGRA channel swizzle (required by model)

2. **Tensor Readback**: The tensor is copied to CPU memory for inference

3. **Iterative Inference**: Work is spread across frames using `ScheduleIterable`, yielding after each millisecond to prevent blocking:

   ```csharp
   while (iterable.MoveNext())
   {
       if (time.Elapsed.TotalMilliseconds > 1.0f)
       {
           await Awaitable.NextFrameAsync();
           time.Restart();
       }
   }
   ```

4. **Result Processing**: Face rectangles are extracted and filtered

5. **Blur Rendering**: Detected faces are obscured in the output texture

### Blur Effect

The blur is implemented as a bilinear pixelation effect with a default resolution of 32 pixels:

1. Create a low-resolution copy of the input (scaled so the longest dimension is 32 pixels)
2. Copy the original image to the target render texture
3. For each detected face, draw a quad sampling from the low-res texture

This produces a recognizable pixelation that effectively anonymizes faces while being computationally inexpensive.

```csharp
var scale = (float)blurResolution / Mathf.Max(inputTexture.width, inputTexture.height);
var lowResTexture = RenderTexture.GetTemporary(lowResWidth, lowResHeight, 0, targetTexture.format);
lowResTexture.filterMode = FilterMode.Bilinear;
```

The rendering uses a `CommandBuffer` to efficiently batch all face blur operations into a single GPU submission.

## Key Classes

| Class | Description |
|-------|-------------|
| `FaceDetection` | MonoBehaviour that manages model loading and provides the `RunBlurring()` entry point |
| `FaceBlur` | Static utility class containing the `BlurFaces` pipeline and blur rendering logic |
| `QuestCPUBackend` | Custom inference backend with thread-limited convolution for Quest hardware |

## Usage Example

```csharp
// Get reference to the FaceDetection component
[SerializeField] private FaceDetection faceDetection;

// Assign the input texture
faceDetection.InputTexture = capturedImage;

// Run the blurring pipeline (async)
RenderTexture blurredResult = await faceDetection.RunBlurring();

// Use the blurred result
outputDisplay.texture = blurredResult;
```
