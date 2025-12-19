// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.WitAi.TTS.Utilities;
using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.Samples
{
    /// <summary>
    /// Manages the UI for the TTS sample.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class TtsSampleRoot : MonoBehaviour
    {
        [SerializeField][TextArea] private string m_textToSpeak = "This is a test for text to speech.";
        [SerializeField] private TTSSpeaker m_speaker;

        private void Start()
        {
            m_speaker.Speak(m_textToSpeak);
        }
    }
}