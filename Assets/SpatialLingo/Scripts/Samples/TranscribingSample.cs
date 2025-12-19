// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using SpatialLingo.SpeechAndText;
using UnityEngine;

namespace SpatialLingo.Samples
{
    /// <summary>
    /// Resets the gym's STT state and UI elements.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class TranscribingSample : MonoBehaviour
    {
        [SerializeField] private VoiceTranscriber m_transcriber;

        private void Start()
        {
            m_transcriber.VoiceTranscriptionUpdateComplete += OnVoiceTranscriptionUpdateComplete;
            m_transcriber.StartListening();
        }

        private void OnVoiceTranscriptionUpdateComplete(VoiceTranscriber.VoiceTranscriptionEvent result)
        {
            if (result.Transcriber.Transcriptions.Count > 3)
            {
                result.Transcriber.StopListening();
            }
        }

    }
}