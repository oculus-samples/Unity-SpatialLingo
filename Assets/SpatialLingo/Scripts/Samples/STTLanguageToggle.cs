// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using SpatialLingo.SpeechAndText;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpatialLingo.Samples
{
    /// <summary>
    /// Changes the target language for speech to text.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class SttLanguageToggle : MonoBehaviour
    {
        private const string LABEL_TEXT_FORMAT = "Listening for:\n{0}";

        [SerializeField] private Button m_button;
        [SerializeField] private TMP_Text m_label;
        [SerializeField] private STTLanguageSwitch m_languageSwitch;

        private void Start()
        {
            m_label.text = string.Format(LABEL_TEXT_FORMAT, m_languageSwitch.CurrentLanguage);
            m_button.onClick.AddListener(ListenForNextLanguage);
        }

        private void OnDestroy()
        {
            m_button.onClick.RemoveListener(ListenForNextLanguage);
        }

        public void ListenForNextLanguage()
        {
            switch (m_languageSwitch.CurrentLanguage)
            {
                case WitaiSettingsHolder.Language.Spanish:
                    m_languageSwitch.CurrentLanguage = WitaiSettingsHolder.Language.English;
                    break;
                default:
                    m_languageSwitch.CurrentLanguage++;
                    break;
            }

            m_label.text = string.Format(LABEL_TEXT_FORMAT, m_languageSwitch.CurrentLanguage);
        }
    }
}