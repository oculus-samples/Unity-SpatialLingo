// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.WitAi.TTS.Integrations;
using Oculus.Voice.Dictation;
using UnityEngine;

namespace SpatialLingo.SpeechAndText
{
    /// <summary>
    /// Switches language configurations for speech to text.
    /// </summary>
    public class STTLanguageSwitch : MonoBehaviour
    {
        [SerializeField] private AppDictationExperience m_witDictation;
        [SerializeField] private TTSWit m_wit;
        [SerializeField] private WitaiSettingsHolder m_witSettings;

        private bool m_useDictation = false;
        private bool m_useWit = false;
        private void OnEnable()
        {
            m_useWit = m_wit != null;
            m_useDictation = m_witDictation != null;
        }

        private WitaiSettingsHolder.Language m_currentLanguage = WitaiSettingsHolder.Language.English;
        public WitaiSettingsHolder.Language CurrentLanguage
        {
            get => m_currentLanguage;
            set
            {
                if (m_useDictation)
                {
                    m_witDictation.RuntimeConfiguration.witConfiguration = m_witSettings.GetSettingsForLanguage(value);
                }
                else if (m_useWit)
                {
                    m_wit.Configuration = m_witSettings.GetSettingsForLanguage(value);
                }
                m_currentLanguage = value;
            }
        }
    }
}