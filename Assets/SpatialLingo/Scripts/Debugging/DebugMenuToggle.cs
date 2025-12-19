// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace SpatialLingo.Debugging
{
    [MetaCodeSample("SpatialLingo")]
    public class DebugMenuToggle : MonoBehaviour
    {
        [SerializeField] private GameObject m_debugCanvas;
        [SerializeField] private Grabbable m_grabbableUI;

        private bool m_readyToToggle;

        private bool m_isShowing = false;
        private bool IsShowing
        {
            get => m_isShowing;
            set
            {
                m_debugCanvas.SetActive(value);
                m_grabbableUI.enabled = value;
                m_isShowing = value;
            }
        }

        private void Start()
        {
            IsShowing = m_isShowing;
        }

        private void Update()
        {
            var thumbstickDown = OVRInput.Get(OVRInput.RawButton.LThumbstick) || OVRInput.Get(OVRInput.RawButton.RThumbstick);

            m_readyToToggle |= !OVRInput.Get(OVRInput.RawButton.LThumbstick) && !OVRInput.Get(OVRInput.RawButton.RThumbstick);

            if (thumbstickDown && m_readyToToggle)
            {
                IsShowing = !IsShowing;
                m_readyToToggle = false;
            }
        }
    }
}