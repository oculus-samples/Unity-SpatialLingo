// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpatialLingo.Audio
{
    /// <summary>
    /// Manages all audio playback using a robust object pooling system for efficiency.
    /// Handles both 2D (UI, ambient) and 3D (world-space) sounds.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class AudioManager : MonoBehaviour
    {
        // A single, globally accessible instance of the AudioManager.
        private static AudioManager s_instance;
        public static AudioManager Instance
        {
            get
            {
                // If the instance doesn't exist, try to find it in the scene.
                if (s_instance == null)
                {
                    s_instance = FindFirstObjectByType<AudioManager>();
                }
                // If still not found, create a new GameObject for it. This makes the manager self-sufficient.
                if (s_instance == null)
                {
                    var singletonObject = new GameObject("AudioManager");
                    s_instance = singletonObject.AddComponent<AudioManager>();
                }
                Assert.IsNotNull(s_instance, "An AudioManager is needed in the scene, but it was not found.");
                return s_instance;
            }
        }

        [Header("Audio Prefabs")]
        [Tooltip("The AudioSource prefab for 3D positional sounds.")]
        [SerializeField] private AudioSource m_audioPrefab3D;

        [Tooltip("The AudioSource prefab for 2D UI and ambient sounds.")]
        [SerializeField] private AudioSource m_audioPrefab2D;

        [Header("Pooling Settings")]
        [Tooltip("Initial number of 3D sources to create. Will grow if more are needed.")]
        [SerializeField] private int m_initialPoolSize3D = 20;

        [Tooltip("Initial number of 2D sources to create.")]
        [SerializeField] private int m_initialPoolSize2D = 10;

        // Use queues for pooling (First-In, First-Out).
        private Queue<AudioSource> m_pool3D = new();
        private Queue<AudioSource> m_pool2D = new();

        // A parent transform to keep the pooled objects organized in the hierarchy.
        private Transform m_poolParent;

        private void Awake()
        {
            // Enforce the singleton pattern. If an instance already exists, destroy this new one.
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            // Keep the AudioManager alive when loading new scenes.
            DontDestroyOnLoad(gameObject);

            InitializePools();
        }

        /// <summary>
        /// Creates the initial pool of AudioSources to avoid instantiation during gameplay.
        /// </summary>
        private void InitializePools()
        {
            m_poolParent = new GameObject("AudioSourcePool").transform;
            m_poolParent.SetParent(transform);

            // Pre-warm the 3D audio source pool.
            for (var i = 0; i < m_initialPoolSize3D; i++)
            {
                var source = Instantiate(m_audioPrefab3D, m_poolParent);
                source.gameObject.SetActive(false);
                m_pool3D.Enqueue(source);
            }

            // Pre-warm the 2D audio source pool.
            for (var i = 0; i < m_initialPoolSize2D; i++)
            {
                var source = Instantiate(m_audioPrefab2D, m_poolParent);
                source.gameObject.SetActive(false);
                m_pool2D.Enqueue(source);
            }
        }

        /// <summary>
        /// Plays a "fire and forget" 2D sound. Perfect for UI clicks.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
        /// <param name="volumeScale">Volume multiplier (0.0 to 1.0).</param>
        public AudioSource PlayOneShot2D(AudioClip clip, float volumeScale = 1.0f)
        {
            if (clip == null) return null;

            var source = GetPooledSource2D();
            source.clip = clip;
            source.volume = volumeScale;
            source.loop = false;
            source.Play();

            // The source will be automatically returned to the pool when finished.
            _ = StartCoroutine(ReturnSourceWhenFinished(source));
            return source;
        }

        /// <summary>
        /// Plays a "fire and forget" 3D sound at a specific world position.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
        /// <param name="position">The world-space position to play the sound at.</param>
        /// <param name="volumeScale">Volume multiplier (0.0 to 1.0).</param>
        public AudioSource PlayOneShot3D(AudioClip clip, Vector3 position, float volumeScale = 1.0f)
        {
            if (clip == null) return null;

            var source = GetPooledSource3D();
            source.transform.position = position;
            source.clip = clip;
            source.volume = volumeScale;
            source.loop = false;
            source.Play();

            _ = StartCoroutine(ReturnSourceWhenFinished(source));
            return source;
        }

        /// <summary>
        /// Plays a sound that can be looped and attached to a transform. Returns the source for manual control.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
        /// <param name="parent">The transform to attach the sound to.</param>
        /// <param name="is3D">Whether the sound should be 3D or 2D.</param>
        /// <param name="loop">Whether the sound should loop.</param>
        /// <param name="volumeScale">Volume multiplier (0.0 to 1.0).</param>
        /// <returns>The AudioSource playing the sound, which can be used to stop it later.</returns>
        public AudioSource PlaySound(AudioClip clip, Transform parent, bool is3D = true, bool loop = false, float volumeScale = 1.0f)
        {
            if (clip == null) return null;

            var source = is3D ? GetPooledSource3D() : GetPooledSource2D();

            if (parent != null)
            {
                source.transform.SetParent(parent);
                source.transform.localPosition = Vector3.zero;
            }

            source.clip = clip;
            source.volume = volumeScale;
            source.loop = loop;
            source.Play();

            // If it's not looping, it should be automatically returned to the pool.
            if (!loop)
            {
                _ = StartCoroutine(ReturnSourceWhenFinished(source));
            }

            return source;
        }

        /// <summary>
        /// Manually stops a sound and returns its AudioSource to the pool.
        /// Essential for stopping looped sounds.
        /// </summary>
        /// <param name="sourceToStop">The AudioSource instance that was returned by PlaySound().</param>
        public void StopSound(AudioSource sourceToStop)
        {
            if (sourceToStop != null && sourceToStop.gameObject.activeInHierarchy)
            {
                sourceToStop.Stop();
                ReturnSourceToPool(sourceToStop);
            }
        }

        private AudioSource GetPooledSource3D()
        {
            if (m_pool3D.Count > 0)
            {
                var source = m_pool3D.Dequeue();
                source.gameObject.SetActive(true);
                return source;
            }
            else
            {
                Debug.LogWarning("3D audio pool exhausted. Instantiating a new source. Consider increasing the initial pool size.");
                return Instantiate(m_audioPrefab3D, m_poolParent);
            }
        }

        private AudioSource GetPooledSource2D()
        {
            if (m_pool2D.Count > 0)
            {
                var source = m_pool2D.Dequeue();
                source.gameObject.SetActive(true);
                return source;
            }
            else
            {
                Debug.LogWarning("2D audio pool exhausted. Instantiating a new source. Consider increasing the initial pool size.");
                return Instantiate(m_audioPrefab2D, m_poolParent);
            }
        }

        private void ReturnSourceToPool(AudioSource source)
        {
            // Don't return a source that is already in the pool.
            if (!source.gameObject.activeInHierarchy) return;

            source.Stop();
            source.transform.SetParent(m_poolParent);
            source.gameObject.SetActive(false);

            // Check the spatial blend to determine if it's a 3D or 2D source.
            if (source.spatialBlend > 0)
            {
                m_pool3D.Enqueue(source);
            }
            else
            {
                m_pool2D.Enqueue(source);
            }
        }

        /// <summary>
        /// A coroutine that waits for an AudioSource to finish playing and then returns it to the pool.
        /// </summary>
        private IEnumerator ReturnSourceWhenFinished(AudioSource source)
        {
            // Wait until the audio clip is no longer playing.
            yield return new WaitWhile(() => source.isPlaying);
            ReturnSourceToPool(source);
        }
    }
}