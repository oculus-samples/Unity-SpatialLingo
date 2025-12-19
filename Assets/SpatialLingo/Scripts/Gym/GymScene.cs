// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.Utilities.CameraTracking;
using Meta.Utilities.LlamaAPI;
using Meta.Utilities.ObjectClassifier;
using Meta.XR;
using Meta.XR.EnvironmentDepth;
using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.Gym
{
    [MetaCodeSample("SpatialLingo")]
    public class GymScene : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private WebCamTextureManager m_cameraTextureManager;
        [SerializeField] private EnvironmentRaycastManager m_environmentRaycastManager;
        [SerializeField] private EnvironmentDepthManager m_environmentDepthManager;

        public static bool SystemReady
        {
            get;
            private set;
        }

        public delegate void SystemsBecameReadyEvent(GymScene gymScene);
        public static event SystemsBecameReadyEvent SystemsBecameReady;

        private void Start()
        {
            // IE preload
            _ = InferenceEngineUtilities.LoadAll();

            GetAllCameraPermissions();
        }

        // Start
        private void StartSystemInit()
        {
            if (EnvironmentDepthManager.IsSupported)
            {
                m_environmentDepthManager.gameObject.SetActive(true);

                // Enable depth
                m_environmentDepthManager.enabled = true;
                // Enable ray casting
                m_environmentRaycastManager.enabled = true;
            }
            else
            {
                Debug.LogWarning("Depth is not supported");
            }

            var resolution = new Vector2Int(800, 600); // 640x640 is res used for YOLO
            m_cameraTextureManager.RequestedResolution = resolution;
            m_cameraTextureManager.Eye = PassthroughCameraEye.Left;

            // Complete system loading
            SystemReady = true;
            SystemsBecameReady?.Invoke(this);
        }

        // Permissions
        private void GetAllCameraPermissions()
        {
            if (PassthroughCameraPermissions.IsAllCameraPermissionsGranted())
            {
                StartSystemInit();
                return;
            }
            PassthroughCameraPermissions.AllCameraPermissionGranted += OnAllCameraPermissionGranted;
            PassthroughCameraPermissions.AskCameraPermissions();
        }

        private void OnAllCameraPermissionGranted()
        {
            PassthroughCameraPermissions.AllCameraPermissionGranted -= OnAllCameraPermissionGranted;
            StartSystemInit();
        }

        public void OnDestroy()
        {
            PassthroughCameraPermissions.AllCameraPermissionGranted -= OnAllCameraPermissionGranted;
        }
    }
}
