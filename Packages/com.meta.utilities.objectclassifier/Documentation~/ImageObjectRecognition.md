# Image Object Recognition

Object recognition is a general term for identifying items in an image and particular data such as location and size.

## YOLO

YOLO is a specific type of object recognizer that uses a trained neural network to locate and identify any of 80 different object types. The YOLO model has been trained using one of the COCO image datasets: a large collection of images and related metadata. See the classes in the codebase for details:[`YOLO classes`](../../../Assets/SpatialLingo/Data/InferenceEngine/ObjectClassifier/classesYolo.txt).

For more information on YOLO, visit: [`Ultralytics YOLO Docs`](https://docs.ultralytics.com/models/yolo12/) and [`MIT Licensed YOLO`](https://github.com/MultimediaTechLab/YOLO).

For more information on the COCO dataset, visit: [`COCO Home`](https://cocodataset.org/#home)

## Unity Inference Engine

A general cross-platform format used for neural networks is the .onnx format. The YOLO model used by Spatial Lingo is optimized by unity, converted to a .sentis file (File located in codebase :[`YOLO model`](../../../Assets/SpatialLingo/Data/InferenceEngine/ObjectClassifier/yolov9onnx_min_0.2score_0.5iou.sentis).).

Unity's Inference Engine (IE) is able to pass in and receive out Tensor data. The process of running a model using IE can be performed all-at-once (`worker.Schedule`) or iteritively (`worker.ScheduleIterable`), to spread GPU and CPU work across time so as not to block UI or other threads.

For more information on the Unity Inference Engine (Sentis), visit: [`Unity Inference Engine Package`](https://docs.unity3d.com/Packages/com.unity.ai.inference@2.3/manual/index.html)

## Using the sample

The **ObjectRecognitionSample** scene demonstrates the code required to: load, run, and display the results of the YOLO onnx model using the Inference Engine package.

For a given image, the neural network outputs a list of: rectangles and classification (index inside 80 different classes).

![ExampleObjects.png](Images/YOLO/ExampleObjects.png)

The returned rectangle data is a vector of 4 floats, representing: x,y and width,height.

The returned classification integer is converted to a string using the `classesYolo.txt` as a lookup table.

The results are added to the scene and displayed in context.

## Using the Spatial Lingo app

The file [ImageObjectClassifier.cs](../Runtime/Scripts/ImageObjectClassifier.cs) encapsulates many of the details of the object recognition and classification process. It is initialized in the main app by passing the model to use, the classification list, and additional options to filter results.

The entry point for starting a run is: `ProcessImageForClassification(Texture2D ImageSource)`. When the process is complete, the event  `ImageProcessedComplete(ClassifiedImageResult result)` is called, returning a list of `ClassifiedImageObject` and the original Texture2D image used as reference.
