// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Integrations;
using UnityEngine;

namespace SpatialLingo.SpeechAndText
{
    /// <summary>
    /// Encapsulates Wit.ai TTS Service
    /// </summary>
    public class VoiceSynthesizer : MonoBehaviour
    {
        [SerializeField] private TTSWit m_wit; // Wit is a TTSService
        [SerializeField] private TTSDiskCacheSettings m_diskCacheSettings;
        [SerializeField] private TTSWitVoiceSettings m_voiceSettings;

        private class AudioDataContainer
        {
            public TTSClipData ClipData;
            public float[] Samples;

            public void OnAddSamples(float[] samples, int offset, int length)
            {
                Samples = samples;
            }

            public void OnLoadStreamReady(TTSClipData data)
            {
            }

            public void OnLoadStreamComplete(TTSClipData data, string name)
            {
                // Use data
            }
        }

        private Dictionary<string, AudioClip> m_audioCache = new();
        public async Task<AudioClip> SythesizeAudioForText(string text)
        {
            if (text == null)
            {
                return null;
            }
            if (m_audioCache.TryGetValue(text, out var clip))
            {
                return clip;
            }

            // Create a clip container (this may need caching)
            var clipData = m_wit.GetClipData(text, m_voiceSettings, m_diskCacheSettings);
            var container = new AudioDataContainer
            {
                ClipData = clipData
            };
            if (clipData.clipStream != null)
            {
                clipData.clipStream.OnAddSamples += container.OnAddSamples;
            }
            // Start the load
            try
            {
                var task = m_wit.LoadAsync(clipData, container.OnLoadStreamReady, container.OnLoadStreamComplete);
                _ = await task;
            }
            catch (Exception e)
            {
                Debug.LogError($"SythesizeAudioForText: LoadAsync Exception: {e.Message}");
            }
            clip = clipData.clip;
            if (clip == null)
            {
                if (container.Samples != null)
                {
                    var sampleRate = 16000;
                    if (clipData.clipStream != null)
                    {
                        sampleRate = clipData.clipStream.SampleRate;
                    }
                    clip = AudioClip.Create(name, container.Samples.Length, 1, sampleRate, false);
                    _ = clip.SetData(container.Samples, 0);
                }
                else
                {
                    Debug.LogWarning($"data has been cached - write a whole separate system to incorporate IAudioClipStream");
                }
            }
            if (clip != null)
            {
                m_audioCache[text] = clip;
            }
            return clip;
        }
    }
}