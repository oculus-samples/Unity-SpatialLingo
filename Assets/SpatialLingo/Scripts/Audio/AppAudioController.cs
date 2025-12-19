// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace SpatialLingo.Audio
{
    public enum SoundEffect
    {
        // List all sound effects here
        BackgroundMusic,
        GGPulledFromGround,
        BerrySqueeze,
        BerrySqueezeGolden,
        CelebratoryStinger,
        WordCloudTapped,
        WordCloudPinched,
        WordCloudAppear,
        WordCloudDisappear,
        WordCloudCoalesce,
        SkylightAmbience,
        GoldenBerryProximity,
        TreeGrowing,
        TreeRestartFull,
        TreeRestartHalf,
        TreeStingers,
        DirtMoundPull,
        DirtMoundHit,
        DirtMoundAmbience,
        BerryTrailsStart,
        BerryTrailLoop,
        BerrySqueaks,
        FoliageGrowth,
    }

    [MetaCodeSample("SpatialLingo")]
    [Serializable]
    public class SoundDefinition
    {
        public SoundEffect SoundEffect;
        public AudioClip[] Clips; // Use an array for variations

        /// <summary>
        /// Helper to get a clip. If there's more than one, pick a specific variation or one at random.
        /// </summary>
        /// <param name="variation">Index for specific clip type</param>
        /// <returns>Referenced audio clip if it exists</returns>
        public AudioClip GetClip(int variation = -1)
        {
            if (Clips == null || Clips.Length == 0)
            {
                return null;
            }
            else if (Clips.Length == 1)
            {
                return Clips[0];
            }
            else if (variation >= 0 && variation < Clips.Length)
            {
                return Clips[variation];
            }
            else
            {
                return Clips[Random.Range(0, Clips.Length)];
            }
        }
    }

    [MetaCodeSample("SpatialLingo")]
    public class AppAudioController : MonoBehaviour
    {
        private static AppAudioController s_instance;
        public static AppAudioController Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = FindFirstObjectByType<AppAudioController>();
                }
                if (s_instance == null)
                {
                    var singletonObject = new GameObject("AppAudioController");
                    s_instance = singletonObject.AddComponent<AppAudioController>();
                }
                Assert.IsNotNull(s_instance, "An AppAudioController is needed in the scene, but it was not found.");

                return s_instance;
            }
        }

        public AudioManager AudioManager => AudioManager.Instance;

        [Header("Sound Definitions")]
        [SerializeField]
        private List<SoundDefinition> m_soundDefinitions;
        private Dictionary<SoundEffect, SoundDefinition> m_soundDictionary;

        // This dictionary will ONLY track looping sounds started via PlaySingletonSound.
        private readonly Dictionary<SoundEffect, AudioSource> m_singletonLoopingSounds = new();

        private void Awake()
        {
            // Ensure sound definitions aren't null to avoid errors.
            m_soundDefinitions ??= new();

            // Convert the Inspector list into a dictionary for fast lookups at runtime.
            m_soundDictionary = m_soundDefinitions.ToDictionary(def => def.SoundEffect, def => def);
        }

        public void Initialize()
        {
            // Play background music
            _ = PlaySingletonSound(SoundEffect.BackgroundMusic, null, 0.4f);
        }

        /// <summary>
        /// Plays a 2D one-shot sound effect (e.g., UI click, notification).
        /// </summary>
        public void PlaySound(SoundEffect effect, float volume = 1.0f, int variation = -1)
        {
            if (TryGetClip(effect, variation, out var clip))
            {
                _ = AudioManager.PlayOneShot2D(clip, volume);
            }
        }

        /// <summary>
        /// Plays a 3D one-shot sound effect at a specific world position.
        /// </summary>
        public void PlaySound(SoundEffect effect, Vector3 position, float volume = 1.0f, int variation = -1)
        {
            if (TryGetClip(effect, variation, out var clip))
            {
                _ = AudioManager.PlayOneShot3D(clip, position, volume);
            }
        }

        /// <summary>
        /// Plays a sound and returns its AudioSource instance for individual control.
        /// Use this for effects that can have multiple instances playing at once.
        /// </summary>
        public AudioSource PlaySound(SoundEffect effect, Transform parent, bool loop = false, float volume = 1.0f, int variation = -1)
        {
            if (TryGetClip(effect, variation, out var clip))
            {
                return AudioManager.PlaySound(clip, parent, parent != null, loop, volume);
            }
            return null;
        }

        /// <summary>
        /// Stops a specific instance of a sound.
        /// </summary>
        public void StopSound(AudioSource sourceInstance)
        {
            if (sourceInstance != null)
            {
                AudioManager.StopSound(sourceInstance);
            }
        }

        /// <summary>
        /// Plays a looping sound that is tracked as a singleton. Any previous instance of
        /// the same SoundEffect will be stopped. Use this for music, ambience, etc.
        /// </summary>
        public AudioSource PlaySingletonSound(SoundEffect effect, Transform parent = null, float volume = 1.0f, int variation = -1)
        {
            // First, stop any existing tracked instance of this sound effect.
            StopSound(effect);

            if (TryGetClip(effect, variation, out var clip))
            {
                var source = AudioManager.PlaySound(clip, parent, parent != null, true, volume);
                if (source != null)
                {
                    // Add the new instance to our tracking dictionary.
                    m_singletonLoopingSounds[effect] = source;
                }
                return source;
            }
            return null;
        }

        /// <summary>
        /// Stops a tracked singleton sound effect using its ID.
        /// </summary>
        public void StopSound(SoundEffect effect)
        {
            if (m_singletonLoopingSounds.TryGetValue(effect, out var source) && source != null)
            {
                AudioManager.StopSound(source);
                _ = m_singletonLoopingSounds.Remove(effect);
            }
        }

        /// <summary>
        /// Helper function to reduce code duplication
        /// </summary>
        /// <param name="effect">Type of effect to find</param>
        /// <param name="variation">index of specific type</param>
        /// <param name="clip">Output AudioClip to set up with effect</param>
        /// <returns>true if audio clip was found and set up, otherwise false</returns>
        private bool TryGetClip(SoundEffect effect, int variation, out AudioClip clip)
        {
            clip = null;
            try
            {
                if (m_soundDictionary.TryGetValue(effect, out var def))
                {
                    clip = def.GetClip(variation);
                    if (clip != null)
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"AudioClip - TryGetClip failed: {e.Message}");
            }

            return false;
        }
    }
}