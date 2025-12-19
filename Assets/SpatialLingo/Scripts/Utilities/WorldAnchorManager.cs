// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Threading.Tasks;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.HeadsetTracking
{
    [MetaCodeSample("SpatialLingo")]
    public class WorldAnchorManager : MonoBehaviour
    {
        private enum AnchorCacheMode
        {
            PositionAndRotation,
            PositionAndForwardRotation,
            PositionOnly,
        }

        [Tooltip("Drag your OVRCameraRig from the hierarchy here.")]
        [SerializeField] private Transform m_cameraRig;
        [SerializeField] private Transform m_centerEyeAnchor;
        [SerializeField] private AnchorCacheMode m_anchorCacheMode = AnchorCacheMode.PositionAndRotation;

        private Vector3 m_initialAnchorPosition;
        private Quaternion m_initialAnchorRotation;

        private OVRSpatialAnchor m_worldAnchor;
        private bool m_anchorCreated;

        public async void Initialize()
        {
            if (m_cameraRig == null || m_centerEyeAnchor == null)
            {
                Debug.LogError("Necessary reference not assigned!");
                return;
            }

            // Clear previous anchoring:
            m_anchorCreated = false;
            if (m_worldAnchor)
            {
                Destroy(m_worldAnchor);
                m_worldAnchor = null;
            }

            // Ensure OVR is initialized and tracking is enabled
            while (!OVRPlugin.initialized || OVRManager.instance == null || !OVRManager.tracker.isEnabled)
            {
                await Task.Yield();
            }

            // Ensure MRUK is ready
            while (MRUK.Instance == null || MRUK.Instance.GetCurrentRoom() == null)
            {
                await Task.Yield();
            }

            await Task.Delay(200); // Small delay to ensure tracking is stable

            // Create a new GameObject to hold the spatial anchor
            var worldAnchorGO = new GameObject("[WorldAnchor]");
            worldAnchorGO.transform.position = m_centerEyeAnchor.position;
            var targetRot = m_centerEyeAnchor.rotation;
            switch (m_anchorCacheMode)
            {
                case AnchorCacheMode.PositionAndForwardRotation:
                    targetRot = Quaternion.LookRotation(m_centerEyeAnchor.forward, Vector3.up);
                    break;

                case AnchorCacheMode.PositionOnly:
                    targetRot = Quaternion.identity;
                    break;
            }
            worldAnchorGO.transform.rotation = targetRot;
            m_worldAnchor = worldAnchorGO.AddComponent<OVRSpatialAnchor>();

            // Store the initial position and rotation of the anchor
            m_initialAnchorPosition = m_worldAnchor.transform.position;
            m_initialAnchorRotation = m_worldAnchor.transform.rotation;

            // Ensure the anchor is valid and localized before saving
            while (!m_worldAnchor.Created || !m_worldAnchor.Localized)
            {
                await Task.Yield();
            }

            // Await the asynchronous save operation
            bool success = await m_worldAnchor.SaveAnchorAsync();
            if (success)
            {
                m_anchorCreated = true;
            }
            else
            {
                Debug.LogError("Failed to create world anchor.");
            }
        }

        private void LateUpdate()
        {
            if (!m_anchorCreated) return;

            // Calculate drift since initial placement
            var positionDifference = m_worldAnchor.transform.position - m_initialAnchorPosition;
            var rotationDifference = m_worldAnchor.transform.rotation * Quaternion.Inverse(m_initialAnchorRotation);

            // Apply the INVERSE of the drift to the camera rig to counteract it
            m_cameraRig.position -= positionDifference;
            m_cameraRig.rotation = Quaternion.Inverse(rotationDifference) * m_cameraRig.rotation;
        }
    }
}