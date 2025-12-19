// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections;
using Meta.XR.Samples;
using SpatialLingo.Audio;
using UnityEngine;

namespace SpatialLingo.Characters
{
    [MetaCodeSample("SpatialLingo")]
    public class FocusPointController : MonoBehaviour
    {
        [SerializeField] private GameObject m_mound;
        [SerializeField] private GameObject m_model;
        [SerializeField] private ParticleSystem m_shimmer;
        [SerializeField] private ParticleSystem m_dustGG;
        [SerializeField] private ParticleSystem m_dustSeed;

        private void Awake()
        {
            m_dustSeed.Stop();
            m_dustSeed.Clear();
        }

        public void SetOrientation(Vector3 position, Quaternion rotation)
        {
            gameObject.transform.position = position;
            gameObject.transform.rotation = rotation;
        }

        public void ShowMound()
        {
            m_mound.SetActive(true);
        }

        public void HideMound()
        {
            m_mound.SetActive(false);
        }

        public void ShowPopOutEffect()
        {
            m_dustGG.Stop();
            m_dustGG.Play();
        }

        public void ShowDiveInEffect()
        {
            m_dustSeed.Stop();
            m_dustSeed.Play();
        }

        public void ShowShimmer()
        {
            m_shimmer.Play();
            _ = AppAudioController.Instance.PlaySingletonSound(SoundEffect.DirtMoundAmbience, m_mound.transform);
        }

        public void HideShimmer()
        {
            m_shimmer.Stop();
            AppAudioController.Instance.StopSound(SoundEffect.DirtMoundAmbience);
        }

        public void FadeAway()
        {
            // Remove mound immediately
            m_model.SetActive(false);
            // Allow effects to hang around for duration 
            _ = StartCoroutine(DestroyAfterDelay(3.0f));
        }

        private IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }
    }
}