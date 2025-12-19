// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;

namespace Meta.Utilities.ImageUtilities
{
    public class ImageOperations
    {

        public static Texture2D CropTexture(Texture2D sourceTexture, Rect cropRect)
        {
            cropRect.x = Mathf.Clamp(cropRect.x, 0, sourceTexture.width);
            cropRect.y = Mathf.Clamp(cropRect.y, 0, sourceTexture.height);
            cropRect.width = Mathf.Clamp(cropRect.width, 0, sourceTexture.width - cropRect.x);
            cropRect.height = Mathf.Clamp(cropRect.height, 0, sourceTexture.height - cropRect.y);
            // Get the pixels from the specified region
            var pixels = sourceTexture.GetPixels(
                (int)cropRect.x,
                (int)cropRect.y,
                (int)cropRect.width,
                (int)cropRect.height
            );
            var croppedTexture = new Texture2D((int)cropRect.width, (int)cropRect.height, TextureFormat.RGBA32, false);
            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply();
            return croppedTexture;
        }
    }
}