// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using SpatialLingo.Characters;
using UnityEngine;

namespace SpatialLingo.Debugging
{
    [MetaCodeSample("SpatialLingo")]
    public class SetTreeTier : MonoBehaviour
    {
        [SerializeField] private TreeController m_treeController;
        [SerializeField] private int m_tier;

        private void Start()
        {
            m_treeController.SetTier(m_tier);
        }
    }
}