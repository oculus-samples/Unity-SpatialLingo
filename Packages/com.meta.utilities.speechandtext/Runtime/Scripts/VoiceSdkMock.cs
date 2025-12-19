// Copyright (c) Meta Platforms, Inc. and affiliates.


#if !VOICE_SDK_INSTALLED
using UnityEngine;
using System;
using Meta.WitAi.Data.Configuration;
using UnityEngine.Events;
using Meta.WitAi.Json;

#pragma warning disable IDE1006 // Naming Styles
namespace Meta.WitAi.TTS.Data
{
    public enum TTSDiskCacheLocation
    {
        Stream,
        Preload,
        Persistent,
        Temporary
    }

    [Serializable]
    public class TTSDiskCacheSettings
    {
        public TTSDiskCacheLocation DiskCacheLocation = TTSDiskCacheLocation.Stream;

        public bool StreamFromDisk = false;
        public float StreamBufferLength = 5f;
    }

    [Serializable]
    public class TTSClipData
    {
        public class ClipStream
        {
            public int SampleRate { get; internal set; }

            public event Action<float[], int, int> OnAddSamples;
        }

        internal ClipStream clipStream;
        internal AudioClip clip;
    }
}

namespace Meta.WitAi.TTS.Utilities
{
    public class TTSSpeaker : MonoBehaviour
    {
        public (UnityEvent<AudioClip> OnAudioClipPlaybackStart, UnityEvent<AudioClip> OnAudioClipPlaybackFinished, UnityEvent<string> OnTextPlaybackStart, UnityEvent<string> OnTextPlaybackFinished) Events => (new(), new(), new(), new());

        public void Speak(string result) { }
        public void StopSpeaking() { }
    }
}

namespace Meta.WitAi.Data.Configuration
{
    public class WitConfiguration : ScriptableObject
    {
        public WitConfiguration witConfiguration;
    }
}


namespace Meta.WitAi.TTS.Integrations
{
    public class TTSWit : MonoBehaviour
    {
        public WitConfiguration Configuration { get; set; }

        public Data.TTSClipData GetClipData(string text, TTSWitVoiceSettings m_voiceSettings, Data.TTSDiskCacheSettings m_diskCacheSettings) => throw new NotImplementedException();
        public Awaitable<object> LoadAsync(Data.TTSClipData clipData, Action<Data.TTSClipData> onLoadStreamReady, Action<Data.TTSClipData, string> onLoadStreamComplete) => throw new NotImplementedException();
    }

    [Serializable]
    public class TTSWitVoiceSettings
    {

    }
}

namespace Oculus.Voice.Dictation
{
    public class AppDictationExperience
    {
        public WitConfiguration RuntimeConfiguration { get; set; }

        public (UnityEvent<float> OnMicAudioLevelChanged, UnityEvent OnMicStartedListening, UnityEvent OnMicStoppedListening) AudioEvents => (new(), new(), new());
        public (UnityEvent<WitResponseNode> OnPartialResponse, UnityEvent<string> OnFullTranscription) DictationEvents => (new(), new());

        public void ActivateImmediately() { }
        public void Deactivate() { }
    }
}

namespace Meta.WitAi.Dictation
{
    public class MultiRequestTranscription : MonoBehaviour
    {
        public void Clear() => throw new NotImplementedException();
    }
}

namespace Meta.WitAi.Events.UnityEventListeners
{
    public class TranscriptionEventListener : MonoBehaviour
    {
        public UnityEvent<string> OnPartialTranscription => new();
    }
}

namespace Meta.WitAi.Json
{
    public class WitResponseNode { }
}

#pragma warning restore IDE1006 // Naming Styles
#endif
