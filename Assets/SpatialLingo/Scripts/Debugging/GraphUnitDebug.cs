// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.Lessons;
using SpatialLingo.VisualScriptingUnits;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace SpatialLingo.Debugging
{
    /// <summary>
    /// Tracks the current flow state and sends debug events to the app flow.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class GraphUnitDebug : MonoBehaviour
    {
        private const string DEBUG_FMT = "Current state: {0}";
        [SerializeField] private TMP_Text m_debugText;
        [SerializeField] private Button m_skipButton;
        [SerializeField] private Button m_boundsButton;
        [SerializeField] private TMP_Text m_sessionText;

        private void Start()
        {
            SkippableUnit.UnitEntered += OnGraphUnitChanged;
            m_skipButton?.onClick.AddListener(NextState);
            m_boundsButton?.gameObject.SetActive(false);
            OnGraphUnitChanged(SkippableUnit.CurrentName);
        }

        private void OnDestroy()
        {
            if (m_skipButton != null)
            {
                m_skipButton.onClick.RemoveListener(NextState);
            }

            if (m_boundsButton != null)
            {
                m_boundsButton.onClick.RemoveListener(NextState);
            }

            SkippableUnit.UnitEntered -= OnGraphUnitChanged;
        }

        private void OnGraphUnitChanged(string unitName)
        {
            if (string.IsNullOrEmpty(unitName))
            {
                return;
            }

            if (m_boundsButton != null)
            {
                if (unitName.Contains(nameof(LessonWaitingState)))
                {
                    m_boundsButton.gameObject.SetActive(true);
                    if (m_boundsButton.onClick.GetPersistentEventCount() == 0)
                    {
                        m_boundsButton.onClick.AddListener(ToggleBounds);
                    }
                }
                else
                {
                    m_boundsButton.gameObject.SetActive(false);
                    m_boundsButton.onClick.RemoveListener(ToggleBounds);
                }
            }

            m_debugText.text = string.Format(DEBUG_FMT, unitName);
        }

        private void NextState()
        {
            EventBus.Trigger(ScriptEventNames.DEBUG_SKIP);
        }

        private void ToggleBounds()
        {
            Lesson3DInteractor.ShowDebugFeatures = !Lesson3DInteractor.ShowDebugFeatures;
        }

        private void Update()
        {
            m_sessionText.text = AppSessionData.GetDebugString();
        }
    }
}