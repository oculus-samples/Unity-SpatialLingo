// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.WitAi.TTS.Utilities;
using UnityEngine;

namespace SpatialLingo.SpeechAndText
{
    /// <summary>
    /// Encapsulates Wit.ai TTS Service
    /// </summary>
    public class VoiceSpeaker : MonoBehaviour
    {
        public class SpeechProsodyVoice
        {
            public string Text;
            public string Pitch;
            public string Rate;
            public string Style;
            public string Volume;

            public SpeechProsodyVoice(string text, string pitch, string rate, string style, string volume)
            {
                Text = text;
                Pitch = pitch;
                Rate = rate;
                Style = style;
                Volume = volume;
            }

            public string ToProsidyXMLChunk()
            {
                var hasText = !string.IsNullOrWhiteSpace(Text);
                var hasPitch = !string.IsNullOrWhiteSpace(Pitch);
                var hasRate = !string.IsNullOrWhiteSpace(Rate);
                var hasStyle = !string.IsNullOrWhiteSpace(Style);
                var hasVolume = !string.IsNullOrWhiteSpace(Volume);

                // String concatenation:
                var chunk = "<prosody";
                if (hasPitch)
                {
                    chunk += $" pitch=\"{Pitch}\"";
                }
                if (hasRate)
                {
                    chunk += $" rate=\"{Rate}\"";
                }
                if (hasVolume)
                {
                    chunk += $" volume=\"{Volume}\"";
                }
                chunk += ">";

                if (hasStyle)
                {
                    chunk += $"<voice style=\"{Style}\">";
                }
                if (hasText)
                {
                    chunk += Text;
                }
                if (hasStyle)
                {
                    chunk += $"</voice>";
                }
                chunk += "</prosody>";
                return chunk;
            }
        }

        public delegate void VoiceSpeechEventDelegate();
        public event VoiceSpeechEventDelegate VoiceSpeechStarted;
        public event VoiceSpeechEventDelegate VoiceSpeechCompleted;

        [SerializeField] private TTSSpeaker m_speaker;
        [SerializeField] private string m_systemPitch;
        [SerializeField] private string m_systemRate;
        [SerializeField] private string m_systemStyle;
        [SerializeField] private string m_systemVolume;

        private void Awake()
        {
            m_speaker.Events.OnAudioClipPlaybackStart.AddListener(OnAudioClipPlaybackStart);
            m_speaker.Events.OnAudioClipPlaybackFinished.AddListener(OnAudioClipPlaybackFinished);
            m_speaker.Events.OnTextPlaybackStart.AddListener(OnTextPlaybackStart);
            m_speaker.Events.OnTextPlaybackFinished.AddListener(OnTextPlaybackFinished);
        }

        public static string LanguageTextHintString(WitaiSettingsHolder.Language language)
        {
            return LanguageTextHintChunk(language).ToProsidyXMLChunk();
        }

        private static SpeechProsodyVoice LanguageTextHintChunk(WitaiSettingsHolder.Language language)
        {
            var hint = LanguageTextHint(language);
            return new SpeechProsodyVoice(hint, null, "100", null, "-100dB");
        }

        public void SpeakAudioForText(WitaiSettingsHolder.Language language, string text, bool useLanguageHint = false)
        {
            var chunks = new List<SpeechProsodyVoice>
            {
                new(text, m_systemPitch, m_systemRate, m_systemStyle, m_systemVolume)
            };
            if (useLanguageHint)
            {
                chunks.Add(LanguageTextHintChunk(language));
            }

            var result = SpeechChunksToString(chunks.ToArray());
            result = "<speak>" + result + "</speak>";
            m_speaker.Speak(result);
        }

        public void StopAudioPlayback()
        {
            m_speaker.StopSpeaking();
        }

        private string SpeechChunksToString(SpeechProsodyVoice[] chunks)
        {
            var result = "";
            foreach (var chunk in chunks)
            {
                result += chunk.ToProsidyXMLChunk();
            }
            return result;
        }

        private static string LanguageTextHint(WitaiSettingsHolder.Language language)
        {
            // The phrase to speak is:
            var hint = language switch
            {
                WitaiSettingsHolder.Language.Spanish => "como la frase a decir",
                // Add more languages when supported
                // WitaiSettingsHolder.Language.Vietnamese => "cụm từ để nói là",
                // WitaiSettingsHolder.Language.French => "la phrase à parler est",
                // WitaiSettingsHolder.Language.German => "der Satz zu sprechen ist",
                // WitaiSettingsHolder.Language.Arabic => "العبارة للتحدث هي",
                // WitaiSettingsHolder.Language.Italian => "la frase per parlare è",
                // WitaiSettingsHolder.Language.Thai => "วลีที่จะพูดคือ",
                // WitaiSettingsHolder.Language.Hindi => "बोलने का मुहावरा है",
                // WitaiSettingsHolder.Language.Portuguese => "a frase para falar é",
                // WitaiSettingsHolder.Language.Indonesian => "Frasa untuk berbicara adalah",
                // WitaiSettingsHolder.Language.Tagalog => "ang pariralang sasabihin ay",
                // English
                _ => "the phrase to speak is",
            };
            return hint;
        }

        private void OnAudioClipPlaybackStart(AudioClip clip)
        {
            VoiceSpeechStarted?.Invoke();
        }

        private void OnAudioClipPlaybackFinished(AudioClip clip)
        {
            VoiceSpeechCompleted?.Invoke();
        }

        private void OnTextPlaybackStart(string text)
        {
            // Use event as needed
        }

        private void OnTextPlaybackFinished(string text)
        {
            // Use event as needed
        }
    }
}