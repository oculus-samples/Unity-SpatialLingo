// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.WitAi.Dictation;
using Meta.XR.Samples;
using SpatialLingo.SpeechAndText;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpatialLingo.Samples
{
    /// <summary>
    /// Resets the gym's STT state and UI elements.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class SttClearButton : MonoBehaviour
    {
        [SerializeField] private Button m_clearButton;
        [SerializeField] private MultiRequestTranscription m_transcription;
        [SerializeField] private STTTargetListener m_transcriptionListener;
        [SerializeField] private SttTargetWordUI m_englishTargetWord;
        [SerializeField] private SttTargetWordUI m_spanishTargetWord;
        [SerializeField] private TMP_Text m_transcriptionText;

        private void Start()
        {
            m_clearButton.onClick.AddListener(Clear);
        }

        private void OnDestroy()
        {
            m_clearButton.onClick.RemoveListener(Clear);
        }

        private void Clear()
        {
            m_transcription.Clear();
            m_transcriptionListener.Reset();
            m_englishTargetWord.Reset();
            m_spanishTargetWord.Reset();
            m_transcriptionText.text = string.Empty;
        }
    }
}