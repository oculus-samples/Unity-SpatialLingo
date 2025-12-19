# Meta Taxon Tracking Utilities

## Intro

This package allows for tracking 3D objects using output from 2D image object detection.

The CameraTaxonTracker class uses an: image classifier, depth ray cast manager, and webcam texture manager to track objects in 3D.

## Installation

You can integrate this package into your own project by using the Package Manager to [add the following Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html):

```txt
https://github.com/oculus-samples/Unity-SpatialLingo.git?path=Packages/com.meta.utilities.taxontracking
```

## Example use

```cs
// Dependencies
[SerializeField] private WebCamTextureManager cameraTextureManager;
[SerializeField] private EnvironmentRaycastManager environmentRaycastManager;
[SerializeField] private ImageObjectClassifier imageObjectClassifier;

// Instantiation
var taxonTracker = new CameraTaxonTracker(environmentRaycastManager, cameraTextureManager, imageObjectClassifier);

// Listening for lifecycle events
taxonTracker.TaxonAdded += OnTaxonAddedEvent;
taxonTracker.TaxonUpdated += OnTaxonUpdatedEvent;
taxonTracker.TaxonRemoved += OnTaxonRemovedEvent

// Example event handler:
private void OnTaxonAddedEvent(CameraTaxonTracker.TaxonUpdateResult result)
{
  var taxon = result.Taxon;
  // use the tracked taxa data (CameraTrackedTaxon)
  var classification = taxon.Taxa;
  var position = taxon.Center
  var size = taxon.Extent

  // ...
}


// Manage the object classifier, eg: adjust layers per frame:
taxonTracker.SetLayersPerFrame(layersPerFrame);

// Eg: sequentially process camera images
taxonTracker.StartPolling();

```
