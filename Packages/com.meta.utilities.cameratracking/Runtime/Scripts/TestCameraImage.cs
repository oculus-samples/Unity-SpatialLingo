// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;

namespace Meta.Utilities.CameraTracking
{
    /// <summary>
    /// This script shows an example of getting image data from the Camera API
    /// </summary>
    public class TestCameraImage : MonoBehaviour
    {
        // Display for image
        [Tooltip("Drag a display mesh here")]
        [SerializeReference] public MeshRenderer DisplayMesh;

        // Reference to in-scene isntance of utility for accessing headset cameras
        [Tooltip("Drag camera manager here")]
        [SerializeReference] public WebCamTextureManager CameraTextureManager;

        // Local copy of the image data to display 
        private Texture2D m_cameraSnapshot;

        // Temporary buffer for read/write texture data
        private Color32[] m_pixelsBuffer;

        private void Start()
        {
            DisplayMesh.material.mainTexture = null;
            GetAllCameraPermissions();
        }

        private void GetAllCameraPermissions()
        {
            if (PassthroughCameraPermissions.IsAllCameraPermissionsGranted())
            {
                StartGetPassthrough();
                return;
            }
            PassthroughCameraPermissions.AllCameraPermissionGranted += OnAllCameraPermissionGranted;
            PassthroughCameraPermissions.AskCameraPermissions();
        }

        private void OnAllCameraPermissionGranted()
        {
            PassthroughCameraPermissions.AllCameraPermissionGranted -= OnAllCameraPermissionGranted;
            StartGetPassthrough();
        }

        public void OnDestroy()
        {
            PassthroughCameraPermissions.AllCameraPermissionGranted -= OnAllCameraPermissionGranted;
        }

        private void StartGetPassthrough()
        {
            // Example requested resolution
            var resolution = new Vector2Int(320, 240);
            CameraTextureManager.RequestedResolution = resolution;
            DisplayMesh.transform.localScale = new Vector3(resolution.x / (float)resolution.y, 1.0f, 1.0f);
            // Example requested eye:
            CameraTextureManager.Eye = PassthroughCameraEye.Left;
        }

        private void TakeSnapshot(WebCamTexture webCamTexture)
        {
            // Resize buffer for different resolutions
            var bufferSize = webCamTexture.width * webCamTexture.height;
            if (m_pixelsBuffer == null || m_pixelsBuffer.Length != bufferSize)
            {
                m_pixelsBuffer = new Color32[bufferSize];
                m_cameraSnapshot = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            }
            _ = webCamTexture.GetPixels32(m_pixelsBuffer);
            m_cameraSnapshot.SetPixels32(m_pixelsBuffer);
            m_cameraSnapshot.Apply();

            // Set the display texture
            DisplayMesh.material.mainTexture = m_cameraSnapshot;
        }

        private void Update()
        {
            // Wait for the camera texture to be available
            if (CameraTextureManager.WebCamTexture == null)
            {
                return;
            }

            // Display the live feed until an image is available 
            if (m_cameraSnapshot == null)
            {
                DisplayMesh.material.mainTexture = CameraTextureManager.WebCamTexture;
            }

            // Get and display a screenshot of the camera 
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                TakeSnapshot(CameraTextureManager.WebCamTexture);
            }
        }
    }
}