// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.BuildingBlocks;
using Meta.XR.Samples;
using UnityEngine;

[MetaCodeSample("SpatialLingo")]
public class PassthroughHighlighter : MonoBehaviour
{
    [Tooltip("The controller to cast the ray from.")]
    [SerializeField] private OVRInput.Controller m_controller = OVRInput.Controller.RTouch;

    private OVRCameraRig m_cameraRig;
    private Material m_highlightMaterial;
    private bool m_setupComplete = false;

    private void Start()
    {
        // Get the camera rig component.
        m_cameraRig = GetComponent<OVRCameraRig>();
        if (m_cameraRig == null)
        {
            Debug.LogError("OVRCameraRig not found!");
            return;
        }

        // Get the room mesh event handler.
        var roomMeshEventHandler = FindFirstObjectByType<RoomMeshEvent>();
        if (roomMeshEventHandler == null)
        {
            Debug.LogError("RoomMeshEvent handler not found!");
            return;
        }

        // Get the highlight material from the room mesh event handler.
        roomMeshEventHandler.OnRoomMeshLoadCompleted.AddListener(OnRoomMeshLoadCompleted);
    }

    private void OnRoomMeshLoadCompleted(MeshFilter meshFilter)
    {
        // Get the highlight material from the room mesh filter.
        m_highlightMaterial = meshFilter.GetComponent<Renderer>().material;
        if (m_highlightMaterial == null)
        {
            Debug.LogError("Highlight material not found on room mesh!");
            return;
        }

        m_setupComplete = true;
    }

    private void Update()
    {
        if (!m_setupComplete) return;

        // Get the position and orientation of the controller's pointer.
        var pointerPosition = OVRInput.GetLocalControllerPosition(m_controller);
        var pointerRotation = OVRInput.GetLocalControllerRotation(m_controller);
        var controllerTransform = OVRInput.IsControllerConnected(m_controller) ?
            m_controller == OVRInput.Controller.LTouch ? m_cameraRig.leftControllerAnchor : m_cameraRig.rightControllerAnchor :
            m_cameraRig.centerEyeAnchor;

        // Perform the raycast from the controller's position forward.
        if (Physics.Raycast(controllerTransform.TransformPoint(pointerPosition), controllerTransform.TransformDirection(pointerRotation * Vector3.forward), out var hit))
        {
            // If we hit something, send the world position of the hit to the shader.
            m_highlightMaterial.SetVector("_HighlightPosition", hit.point);
        }
    }
}