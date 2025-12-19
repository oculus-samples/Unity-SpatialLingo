// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialLingo.UI
{
    [MetaCodeSample("SpatialLingo")]
    public class LookAtFocusBehaviour : MonoBehaviour
    {
        [SerializeField] private Transform m_focus;

        public Transform Focus
        {
            get => m_focus;
            set => m_focus = value;
        }

        private void Start()
        {
            Assert.IsNotNull(m_focus);
        }

        private void Update()
        {
            var toPosition = m_focus.position - transform.position;
            transform.rotation = Quaternion.LookRotation(toPosition, Vector3.up);
        }
    }
}