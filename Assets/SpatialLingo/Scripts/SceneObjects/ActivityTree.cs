// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using Meta.XR.Samples;
using SpatialLingo.VisualScriptingUnits;
using Unity.VisualScripting;
using UnityEngine;

namespace SpatialLingo.SceneObjects
{
    [MetaCodeSample("SpatialLingo")]
    public class ActivityTree : MonoBehaviour
    {
        [SerializeField] private List<GameObject> m_treeTiers;
        private int m_tier;

        private void Start()
        {
            EventBus.Register<EmptyEventArgs>(ScriptEventNames.LESSON_TIER_COMPLETE, OnTierComplete);
            m_treeTiers.ForEach(t => t.SetActive(false));
            m_treeTiers[m_tier].SetActive(true);
        }

        private void OnDestroy()
        {
            EventBus.Unregister(ScriptEventNames.LESSON_TIER_COMPLETE, (Action<EmptyEventArgs>)OnTierComplete);
        }

        private void OnTierComplete(EmptyEventArgs args)
        {
            m_treeTiers[m_tier].SetActive(false);
            m_tier++;
            m_tier = Mathf.Min(m_tier, m_treeTiers.Count - 1);
            m_treeTiers[m_tier].SetActive(true);
        }
    }
}