// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using TMPro;
using UnityEngine;

namespace SpatialLingo.UI
{
    [MetaCodeSample("SpatialLingo")]
    public class DictationUI : MonoBehaviour
    {
        [SerializeField] private GameObject m_listeningIcon;
        [SerializeField] private TMP_Text m_dictationText;

        public void OnTranscriptionUpdated(string text)
        {
            m_dictationText.text = text;
        }

        public void OnStartedDictation()
        {
            m_listeningIcon.SetActive(true);
        }

        public void OnStoppedDictation()
        {
            m_listeningIcon.SetActive(false);
        }

        public void Dismiss()
        {
            m_listeningIcon.SetActive(false);
            m_dictationText.text = string.Empty;
        }
    }
}