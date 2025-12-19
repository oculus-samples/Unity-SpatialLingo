# Meta Camera Tracking Utility

## Intro

This package provides interfaces for the Passthrough Camera API (PCA) for Meta Headset Unity projects. It offers interfaces to camera-related resources and operations.

For more information on PCA: [`Unity PCA Overview`](https://developers.meta.com/horizon/documentation/unity/unity-pca-overview/).

For more information on Android Camera2 API: [`Camera2 Overview`](https://developer.android.com/media/camera/camera2).

For more information on Passthrough: [`Unity Passthrough`](https://developers.meta.com/horizon/documentation/unity/unity-passthrough/).

## Installation

You can integrate this package into your own project by using the Package Manager to [add the following Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html):

```txt
https://github.com/oculus-samples/Unity-SpatialLingo.git?path=Packages/com.meta.utilities.cameratracking
```

## Features

This package utilizes several features related to passthrough camera:

- Requesting Permission for camera and scene data
- Camera Image access
- Camera Orientation access
- Depth Estimation

## Setup

The Android manifest for projects using this package has been updated to allow access for several features:

```xml
<uses-feature android:name="com.oculus.feature.PASSTHROUGH" android:required="true" />
<uses-permission android:name="com.oculus.permission.USE_SCENE" android:required="true" />
<uses-permission android:name="horizonos.permission.HEADSET_CAMERA" />
```

Permission for camera access is facilitated using the `PassthroughCameraPermissions` class, which encapsulates the specifics including requests and callbacks from the `Permission` Android API.

```csharp
// Check if permissions are already granted
PassthroughCameraPermissions.IsAllCameraPermissionsGranted()

// Register Callback
PassthroughCameraPermissions.AllCameraPermissionGranted += OnAllCameraPermissionGranted;

// Start Request process
PassthroughCameraPermissions.AskCameraPermissions();
```

## Camera Image

Access to the camera is facilitated using a `WebCamTextureManager`, which can be made accessible to code using:

```csharp
[SerializeReference] public WebCamTextureManager cameraTextureManager;
```

One of several different resolutions can be requested for the camera, from 320x240 to 1280x960:

```csharp
var resolution = new Vector2Int(320, 240); // Different sized can be requested or changed during runtime
cameraTextureManager.RequestedResolution = resolution;
CameraTextureManager.Eye = PassthroughCameraEye.Left; // Left and Right cameras available
```

The texture is accessible via `CameraTextureManager.WebCamTexture` and can then be directly referenced for a continuous feed, or copied for a single snapshot.

```csharp
CameraTextureManager.WebCamTexture // WebCamTexture : Texture
```

### Getting Camera Still Texture

```csharp
public WebCamTextureManager m_cameraTextureManager;

private void Start()
{
    GetCameraStillTexture2D(m_cameraTextureManager.WebCamTexture);
}

private Texture2D GetCameraStillTexture2D(WebCamTexture webCamTexture)
{
    var bufferSize = webCamTexture.width * webCamTexture.height;
    cameraImageBuffer = new Color32[bufferSize];
    var cameraImage = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

    _ = webCamTexture.GetPixels32(cameraImageBuffer);
    cameraImage.SetPixels32(cameraImageBuffer);
    cameraImage.Apply();
    return cameraImage;
}
```

## Camera Orientation

How the camera is oriented in 3D space, the `Pose`, is also available, using:

```csharp
var cachedCameraEye = PassthroughCameraEye.Left;
var cachedCameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(cachedCameraEye);
// cachedCameraPose is a Pose struct, with a .position and .rotation in world space
```

This value can be used to associate images or objects in an image to a location in 3D space.

## Camera Screenspace

Specific pixel-level accuracy mapped to 3D space can be achieved using the `ScreenPointToRayInCamera` method available through `PassthroughCameraUtils`. It can be useful to know specifics about where the user's peripheral gaze ends (image bounds), or locating items accurately on a sub-image-size basis.

```csharp
var ray = PassthroughCameraUtils.ScreenPointToRayInCamera(cameraEye, pixelCenter); // cameraEye is .Left or .Right
```

The passed pixel location should be with respect to the Camera's Screen frame of reference coordinate system, including the resolution scale:

```csharp
cameraResolution = PassthroughCameraUtils.GetCameraIntrinsics(m_cachedCameraEye).Resolution;
```

So for example, if the captured image resolution is 320x240 and the camera intrinsic resolution is 1280x960, points in the captured image will need to be scaled up by: `1280/320 & 960/240` (4) to have the expected scale in screen space.

## Depth Estimation

Rays obtained using the Camera API Screenspace function are used directly with the Depth API to get an estimate of the surface using:

```csharp
var didHit = raycastManager.Raycast(camearRay, out var hitInfo);
```

A `EnvironmentRaycastManager` is a `MonoBehaviour` that can be added to a gameobject in a unity scene, and referenced in code to access instance methods.

```csharp
[SerializedField] EnvironmentRaycastManager raycastManager
```

The Depth API needs to be checked for support before using any features: `EnvironmentDepthManager.IsSupported` .

For more information on the Depth API: [`Depth API Overview`](https://developers.meta.com/horizon/documentation/unity/unity-depthapi-overview/).

## Usage Example

A comprehensive example demonstrating how to request permission, set a target resolution, access the camera image as a texture, and display the image:

```csharp
// Reference the left or right eye camera
var cameraEye = PassthroughCameraEye.Left;

// Get camera pose = position & rotation
var cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(cameraEye);

// Get camera resolution
var cameraResolution = PassthroughCameraUtils.GetCameraIntrinsics(cameraEye).Resolution;

// Get the center of a pixel in camera image coordinates
int locX, locY;
// ...
var center = new Vector2Int(locX, locY);

// Get a ray through the screen into the world
var ray = PassthroughCameraUtils.ScreenPointToRayInCamera(cameraEye, center);

// Get permissions
PassthroughCameraPermissions.AskCameraPermissions();
```

## Example Scene

Camera image mesh placeholder in scene:

![scenePCA.png](Documentation~/Images/PCA/scenePCA.png)

Camera image placed onto mesh on headset in real-time:

![headsetPCA.jpg](Documentation~/Images/PCA/headsetPCA.jpg)
