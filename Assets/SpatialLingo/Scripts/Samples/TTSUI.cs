// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.WitAi.TTS.Utilities;
using Meta.XR.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpatialLingo.Samples
{
    /// <summary>
    /// Hooks up TTS events to the gym scene's UI.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class Ttsui : MonoBehaviour
    {
        [SerializeField] private TMP_InputField m_ttsField;
        [SerializeField] private Button m_ttsButton;
        [SerializeField] private TTSSpeaker m_ttsSpeaker;

        private void Start()
        {
            m_ttsButton.onClick.AddListener(OnTTSButtonPressed);
        }

        private void OnDestroy()
        {
            m_ttsButton.onClick.RemoveListener(OnTTSButtonPressed);
        }

        private void OnTTSButtonPressed()
        {
            m_ttsSpeaker.Speak(m_ttsField.text);
        }
    }
}