// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.UI
{
    [MetaCodeSample("SpatialLingo")]
    public class LookAtFocusBillboardBehaviour : MonoBehaviour
    {
        [SerializeField] private Transform m_focus;

        // -Z Look in the direction of focus object (how text default appears), only rotate in Y(up)
        private void Update()
        {
            if (m_focus != null)
            {
                var toPosition = m_focus.position - transform.position;
                var angle = Mathf.Atan2(-toPosition.z, toPosition.x) * Mathf.Rad2Deg - 90;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
            }
        }
    }
}