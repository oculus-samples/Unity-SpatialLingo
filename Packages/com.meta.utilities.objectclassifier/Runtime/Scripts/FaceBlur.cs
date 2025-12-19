// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.InferenceEngine;
using Unity.Mathematics;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Meta.Utilities.ObjectClassifier
{
    public class FaceBlur : ScriptableSettings<FaceBlur>
    {
        public Mesh QuadMesh;
        public Material UnlitTextureMaterial;

        private static IEnumerable<float4> GetBoxes(float[] results)
        {
            for (var i = 0; i != results.Length; i += 4)
                yield return new(results[i], results[i + 1], results[i + 2], results[i + 3]);
        }

        public static async Awaitable BlurFaces(Model model, BackendType backend, Texture inputTexture, RenderTexture targetTexture, bool useNMS, int blurResolution = 32)
        {
            Profiler.BeginSample("BlurFaces.ConvertTextureToTensor");
            using var inputTensor = new Tensor<float>(new TensorShape(1, 3, 640, 640), data: null);
            TextureConverter.ToTensor(inputTexture, inputTensor, new TextureTransform().SetChannelSwizzle(ChannelSwizzle.BGRA));
            Profiler.EndSample();

            using var cpuInputTensor = await inputTensor.ReadbackAndCloneAsync();

            Profiler.BeginSample("BlurFaces.CreateWorker");
            var worker = QuestCPUBackend.CreateWorker(model);
            Profiler.EndSample();

            var iterable = worker.ScheduleIterable(cpuInputTensor);
            var time = System.Diagnostics.Stopwatch.StartNew();
            while (iterable.MoveNext())
            {
                if (time.Elapsed.TotalMilliseconds > 1.0f)
                {
                    await Awaitable.NextFrameAsync();
                    time.Restart();
                }
            }

            var results = await GetResults(useNMS, worker);
            await Awaitable.MainThreadAsync();
            ExecuteBlurring(inputTexture, targetTexture, results, blurResolution);
        }

        private static void ExecuteBlurring(Texture inputTexture, RenderTexture targetTexture, float[] results, int blurResolution)
        {
            var cmd = new CommandBuffer
            {
                name = "FaceBlurring"
            };

            // First, copy the input texture to the target
            cmd.Blit(inputTexture, targetTexture);

            // Create a low-res version of the input texture
            var scale = (float)blurResolution / Mathf.Max(inputTexture.width, inputTexture.height);
            var lowResWidth = Mathf.RoundToInt(inputTexture.width * scale);
            var lowResHeight = Mathf.RoundToInt(inputTexture.height * scale);
            var lowResTexture = RenderTexture.GetTemporary(lowResWidth, lowResHeight, 0, targetTexture.format);
            lowResTexture.filterMode = FilterMode.Bilinear;
            cmd.Blit(inputTexture, lowResTexture);

            cmd.SetRenderTarget(targetTexture);

            // Set up orthographic projection for screen-space rendering
            var projectionMatrix = Matrix4x4.Ortho(0, targetTexture.width, 0, targetTexture.height, -1, 1);
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, projectionMatrix);

            var material = Instance.UnlitTextureMaterial;
            var quadMesh = Instance.QuadMesh;
            var block = new MaterialPropertyBlock();

            // For each detected face rect, draw the low-res region back to the target
            var boxes = GetBoxes(results);
            foreach (var box in boxes)
            {
                // Convert normalized coordinates to pixel coordinates
                var x1 = box.x * inputTexture.width;
                var y1 = (1 - box.w) * inputTexture.height;
                var x2 = box.z * inputTexture.width;
                var y2 = (1 - box.y) * inputTexture.height;

                var rectWidth = x2 - x1;
                var rectHeight = y2 - y1;
                var centerX = x1 + rectWidth / 2f;
                var centerY = y1 + rectHeight / 2f;

                // Create transformation matrix (quad is 1x1 centered at origin)
                var matrix = Matrix4x4.TRS(
                    new Vector3(centerX, centerY, 0),
                    Quaternion.identity,
                    new Vector3(rectWidth, rectHeight, 1)
                );

                // Use a MaterialPropertyBlock to set per-draw properties
                block.SetTexture("_MainTex", lowResTexture);

                // Set UV tiling and offset to sample the correct region
                var uvScale = new Vector2(box.z - box.x, box.w - box.y);
                var uvOffset = new Vector2(box.x, 1 - box.w);
                block.SetVector("_MainTex_ST", new Vector4(uvScale.x, uvScale.y, uvOffset.x, uvOffset.y));

                // Draw the quad mesh with the property block
                cmd.DrawMesh(quadMesh, matrix, material, 0, -1, block);
            }

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
            RenderTexture.ReleaseTemporary(lowResTexture);
        }

        private static async Task<float[]> GetResults(bool useNMS, Worker worker)
        {
            if (!useNMS)
            {
                // Get boxes and scores from model outputs
                var boxesTensor = worker.PeekOutput(0) as Tensor<float>;
                var scoresTensor = worker.PeekOutput(1) as Tensor<float>;

                using var boxesResult = await boxesTensor.ReadbackAndCloneAsync();
                using var scoresResult = await scoresTensor.ReadbackAndCloneAsync();

                var boxes = boxesResult.AsReadOnlyNativeArray().ToArray();
                var scores = scoresResult.AsReadOnlyNativeArray().ToArray();

                await Awaitable.MainThreadAsync();

                // Convert to ClassifiedImageObject list
                var classifiedObjects = GetBoxes(boxes).
                    Where((box, boxIndex) => scores[boxIndex] >= 0.01f).
                    Select(box =>
                    {
                        var x1 = box.x;
                        var y1 = box.y;
                        var x2 = box.z;
                        var y2 = box.w;

                        var centerX = (x1 + x2) / 2f;
                        var centerY = (y1 + y2) / 2f;
                        var width = x2 - x1;
                        var height = y2 - y1;

                        return new ImageObjectClassifier.ClassifiedImageObject(
                            centerX, centerY, width, height, 0, "face"
                        );
                    }).
                    ToList();

                // Process with ImageObjectClassifier to filter and merge overlapping detections
                var optionsDictionary = new Dictionary<string, ImageObjectClassifier.ClassificationOption>();
                var processedResult = await ImageObjectClassifier.ProcessClassificationTensors(
                    classifiedObjects, optionsDictionary, null
                );

                // Convert back to float4 boxes and limit to 32 results
                var processedBoxes = processedResult.ClassifiedObjects.Take(32).Select(obj =>
                {
                    var x1 = obj.CenterX - obj.Width / 2f;
                    var y1 = obj.CenterY - obj.Height / 2f;
                    var x2 = obj.CenterX + obj.Width / 2f;
                    var y2 = obj.CenterY + obj.Height / 2f;
                    return new float4(x1, y1, x2, y2);
                }).ToArray();

                // Flatten to results array
                var results = new float[processedBoxes.Length * 4];
                for (var i = 0; i < processedBoxes.Length; i++)
                {
                    results[i * 4] = processedBoxes[i].x;
                    results[i * 4 + 1] = processedBoxes[i].y;
                    results[i * 4 + 2] = processedBoxes[i].z;
                    results[i * 4 + 3] = processedBoxes[i].w;
                }
                return results;
            }
            else
            {
                // Use NMS (original method)
                var outputTensor = worker.PeekOutput(0) as Tensor<float>;
                using var resultTensor = await outputTensor.ReadbackAndCloneAsync();
                return resultTensor.DownloadToArray();
            }
        }
    }
}
