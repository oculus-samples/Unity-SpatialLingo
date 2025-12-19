// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using Meta.XR.Samples;
using SpatialLingo.SpeechAndText;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpatialLingo.Samples
{
    /// <summary>
    /// Updates a color when a target word is found in a STT transcription.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class SttTargetWordUI : MonoBehaviour
    {
        [SerializeField] private STTTargetListener m_targetListener;
        [SerializeField] private TMP_InputField m_targetInput;
        [SerializeField] private Color m_colorOnMatch;
        [SerializeField] private Graphic m_matchIndicator;
        private int m_wordIndex;
        private Color m_unmatchedColor;
        private string m_originalWord;

        public void Reset()
        {
            OnInputChanged(m_originalWord);
        }

        private void Start()
        {
            m_unmatchedColor = m_matchIndicator.color;
            m_wordIndex = m_targetListener.TargetKeywords.Count;
            m_originalWord = m_targetInput.text;
            m_targetListener.TargetKeywords.Add(m_targetInput.text);
            m_targetListener.TargetWordMatched += OnAnyWordMatched;
            m_targetInput.onValueChanged.AddListener(OnInputChanged);
        }

        private void OnDestroy()
        {
            m_targetListener.TargetWordMatched -= OnAnyWordMatched;
        }

        private void OnInputChanged(string newInput)
        {
            m_targetListener.TargetKeywords[m_wordIndex] = newInput;
            m_matchIndicator.color = m_unmatchedColor;
        }

        private void OnAnyWordMatched(string matchingWord)
        {
            if (string.Equals(matchingWord, m_targetInput.text, StringComparison.OrdinalIgnoreCase))
            {
                m_matchIndicator.color = m_colorOnMatch;
            }
        }
    }
}