# Meta Image Utilities

## Intro

This package contains helpful image functions

## Installation

You can integrate this package into your own project by using the Package Manager to [add the following Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html):

```txt
https://github.com/oculus-samples/Unity-SpatialLingo.git?path=Packages/com.meta.utilities.imageutilities
```

## Example use

```cs
using Meta.Utilities.ImageUtilities;

// Get a cropped image
Texture2D texture = ...
var rect = new Rect(50, 60, 70, 80);
var cropped = ImageOperations.CropTexture(texture, rect);

```
