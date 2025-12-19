// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.WitAi.Dictation;
using Meta.WitAi.Events.UnityEventListeners;
using Meta.WitAi.Json;
using Oculus.Voice.Dictation;
using UnityEngine;

namespace SpatialLingo.SpeechAndText
{
    /// <summary>
    /// Encapsulates Wit.ai STT Service
    /// </summary>
    public class VoiceTranscriber : MonoBehaviour
    {
        private const float SILENCE_MIC_WAIT_TIME_SECONDS = 1.1f;

        [SerializeField] private AppDictationExperience m_dictation;
        [SerializeField] private MultiRequestTranscription m_transcription;
        [SerializeField] private TranscriptionEventListener m_transcriptionEvents;
        [SerializeField] private STTLanguageSwitch m_languageSwitch;

        public List<string> Transcriptions { get; } = new();
        public bool IsListening { get; private set; }

        public delegate void VoiceTranscriptionEventDelegate(VoiceTranscriptionEvent result);
        public event VoiceTranscriptionEventDelegate VoiceTranscriptionUpdateComplete;
        public event VoiceTranscriptionEventDelegate VoiceTranscriptionUpdateIncomplete;

        public delegate void VoiceTranscriptionVolumeEventDelegate(float vollume);
        public event VoiceTranscriptionVolumeEventDelegate VoiceTranscriptionVolumeUpdate;

        private WitaiSettingsHolder.Language m_targetLanguage = WitaiSettingsHolder.Language.English;
        private int m_stopListeningCount = 0;
        private DateTime m_previousFullResponseTime;
        private string m_prependResponse = "";
        private bool m_hasFullResponse = true;

        public enum VoiceTranscriptionEventType
        {
            AddedContent
        }

        public class VoiceTranscriptionEvent
        {
            public VoiceTranscriptionEventType EventType { get; }
            public VoiceTranscriber Transcriber { get; }
            public string Transcription { get; }
            public VoiceTranscriptionEvent(VoiceTranscriber transcriber, string text)
            {
                Transcriber = transcriber;
                Transcription = text;
                EventType = VoiceTranscriptionEventType.AddedContent;
            }
        }

        private void Awake()
        {
            m_dictation.AudioEvents.OnMicAudioLevelChanged.AddListener(OnMicAudioLevelChanged);
            m_dictation.AudioEvents.OnMicStartedListening.AddListener(OnMicStartedListening);
            m_dictation.AudioEvents.OnMicStoppedListening.AddListener(OnMicStoppedListening);
            m_dictation.DictationEvents.OnPartialResponse.AddListener(OnPartialResponse);
            m_dictation.DictationEvents.OnFullTranscription.AddListener(OnFullResponse);
            m_transcriptionEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            m_previousFullResponseTime = DateTime.Now;
        }

        private void OnMicAudioLevelChanged(float volume)
        {
            VoiceTranscriptionVolumeUpdate?.Invoke(volume);
        }

        private void OnPartialTranscription(string text)
        {
            // Re-check appending if previous statement was ended too quickly by transcription system
            if (m_hasFullResponse)
            {
                var now = DateTime.Now;
                var diff = now - m_previousFullResponseTime;
                var diffSeconds = diff.TotalSeconds;

                if (diffSeconds < SILENCE_MIC_WAIT_TIME_SECONDS)
                {
                    if (Transcriptions.Count > 0)
                    {
                        m_prependResponse = Transcriptions.Last();
                    }
                }
                else
                {
                    m_prependResponse = "";
                }
            }
            m_hasFullResponse = false;
            var sendText = m_prependResponse + " " + text;
            VoiceTranscriptionUpdateIncomplete?.Invoke(new VoiceTranscriptionEvent(this, sendText));
        }

        private void OnMicStartedListening()
        {
            // Use event as needed
        }

        private void OnMicStoppedListening()
        {
            m_stopListeningCount += 1;
            if (IsListening)
            {
                if (m_stopListeningCount == 3)
                {
                    m_stopListeningCount = 0; // back to 0
                    // Re-Listen
                    _ = StartCoroutine(ReListenStart());
                }
            }
        }

        private IEnumerator ReListenStart()
        {
            yield return new WaitForSeconds(0.1f);
            // Check to see if still want to be listening
            if (IsListening)
            {
                m_dictation.ActivateImmediately();
            }
        }

        private void OnPartialResponse(WitResponseNode response)
        {
            // Use event as needed
        }

        private void OnFullResponse(string response)
        {
            m_hasFullResponse = true;

            // Keep any previous prepending needed: 
            var sendText = m_prependResponse + " " + response;
            m_prependResponse = "";
            Transcriptions.Add(sendText);

            VoiceTranscriptionUpdateComplete?.Invoke(new VoiceTranscriptionEvent(this, sendText));
            m_previousFullResponseTime = DateTime.Now;
        }

        public void ClearTranscriptions()
        {
            Transcriptions.Clear();
        }

        public void SetTargetLanguage(WitaiSettingsHolder.Language targetLanguage)
        {
            m_targetLanguage = targetLanguage;
        }

        public void StartListening()
        {
            m_languageSwitch.CurrentLanguage = m_targetLanguage;
            m_dictation.ActivateImmediately();
            m_stopListeningCount = 0;
            IsListening = true;
        }

        public void StopListening()
        {
            IsListening = false;
            m_dictation.Deactivate();
        }
    }
}