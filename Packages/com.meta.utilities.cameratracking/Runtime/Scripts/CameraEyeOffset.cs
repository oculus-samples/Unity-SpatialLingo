// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;

namespace Meta.Utilities.CameraTracking
{
    public class CameraEyeOffset : MonoBehaviour
    {
        // Relative positions are with respect to the LeftEyeAnchor in the TrackingSpace game object
        private readonly Vector3 m_relativePositionQuest3 = new(-0.00091539894f, -0.018030539f, 0.063489795f);
        private readonly Quaternion m_relativeRotationQuest3 = Quaternion.Euler(new Vector3(11.026862f, 0.037174352f, 359.53418f));
        private readonly Vector3 m_relativePositionQuest3S = new(-0.00075580832f, -0.012620992f, 0.074757546f);
        private readonly Quaternion m_relativeRotationQuest3S = Quaternion.Euler(new Vector3(5.7593379f, 0.21119192f, 359.72345f));

        private void Start()
        {
            var headsetType = OVRPlugin.GetSystemHeadsetType();
            switch (headsetType)
            {
                case OVRPlugin.SystemHeadset.Meta_Quest_3:
                    transform.localPosition = m_relativePositionQuest3;
                    transform.localRotation = m_relativeRotationQuest3;
                    break;
                case OVRPlugin.SystemHeadset.Meta_Quest_3S:
                    transform.localPosition = m_relativePositionQuest3S;
                    transform.localRotation = m_relativeRotationQuest3S;
                    break;
                default:
                    Debug.LogWarning($"{nameof(CameraEyeOffset)}: Unhandled offset for headset type {headsetType}");
                    break;
            }
        }
    }
}