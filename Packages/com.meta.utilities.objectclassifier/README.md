# Meta Object Classifier

## Intro

This package allows for encapsulating the details of using a unity sentis model classifier abstracted to a C# interface.

See also:

- [Image Object Recognition](./Documentation~/ImageObjectRecognition.md) for YOLO-based object detection
- [Face Detection and Blurring](./Documentation~/FaceBlurring.md) for face detection and privacy protection features

## Installation

You can integrate this package into your own project by using the Package Manager to [add the following Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html):

```txt
https://github.com/oculus-samples/Unity-SpatialLingo.git?path=Packages/com.meta.utilities.objectclassifier
```

## Example use

Setup a game object in a scene or prefab using the `ImageObjectClassifier` Monobehaviour.

```cs
using Meta.Utilities.ObjectClassifier;

// ...

[SerializeField] private ImageObjectClassifier classifier;
[SerializeField] private ModelAsset model;
[SerializeField] private Texture2D texture2D;

// ... After Awake has been called:

// List of classification labels, eg from YOLO.txt
string[] classList = {"person","bicycle","..."};

// List of options to pass, here: ignoring these two object types
var options = List<ClassificationOption>
{
    new("person", null, true), // hands or body part of user
    new("remote", null, true), // hand-held controllers
};

// Initialize the classifier
classifier.Initialize(model, classList, options.ToArray());

// Listen for events
classifier.ImageProcessedComplete += OnImageProcessedComplete;

// Precise control of layers to use per frame
classifier.SetLayersPerFrame(5);

// Preload all Inference Engine Compute Shaders (not required, but localizes the loading hitches)
_ = InferenceEngineUtilities.LoadAll();

// Start processing an image
classifier.ProcessImageForClassification(texture2D);

// ...

// Process Completed handler
private void OnImageProcessedComplete(ImageObjectClassifier.ClassifiedImageResult result)
{
  // Process results
}


```
