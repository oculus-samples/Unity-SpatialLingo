// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using SpatialLingo.AI;
using SpatialLingo.AppSystems;
using SpatialLingo.Audio;
using SpatialLingo.Lessons;
using UnityEngine;

namespace SpatialLingo.SceneObjects
{
    [MetaCodeSample("SpatialLingo")]
    public class LanguageSelect : MonoBehaviour
    {
        [SerializeField] private WordBar3D m_wordBarPrefab;
        [SerializeField] private ParticleSystem m_completeEffect;

        private List<WordBar3D> m_languageWordBars = new();

        public delegate void LanguageSelectedEvent(AppSessionData.Language language);
        public event LanguageSelectedEvent LanguageSelected;

        public void AnimateIn(Vector3 startPosition)
        {
            // Create WordBar3D instances for each language option
            PlayParticleEffect(startPosition);
            AppAudioController.Instance.PlaySound(SoundEffect.WordCloudAppear, startPosition);
            CreateLanguageOptions(startPosition);
            _ = StartCoroutine(WaitEnableOptions());
        }

        private IEnumerator WaitEnableOptions()
        {
            yield return new WaitForSeconds(1.5f);
            // Allow interaction
            foreach (var wordBar in m_languageWordBars)
            {
                wordBar.EnableSqueezeInteraction();
                wordBar.EnablePokeInteraction();
            }
        }

        public void AnimateOut(Vector3 finalPosition)
        {
            _ = StartCoroutine(AnimateOutDestroy(finalPosition));
        }

        private IEnumerator AnimateOutDestroy(Vector3 finalPosition)
        {
            // Disable any further interaction
            foreach (var wordBar in m_languageWordBars)
            {
                wordBar.DisableSqueezeInteraction();
                wordBar.DisablePokeInteraction();
            }
            yield return new WaitForSeconds(0.5f);

            foreach (var bar in m_languageWordBars)
            {
                bar.AnimateCompleteDestroy(finalPosition);
            }
            m_languageWordBars.Clear();
            AppAudioController.Instance.PlaySound(SoundEffect.WordCloudDisappear, transform.position);

            yield return new WaitForSeconds(0.20f);
            PlayParticleEffect(finalPosition);
            yield return new WaitForSeconds(0.80f);
            Destroy(gameObject);
        }

        private void PlayParticleEffect(Vector3 position)
        {
            _ = StartCoroutine(PlayParticleEffectCoroutine(position));
        }

        private IEnumerator PlayParticleEffectCoroutine(Vector3 position)
        {
            // Play sparkle effect
            m_completeEffect.transform.position = position;
            yield return new WaitForEndOfFrame();
            m_completeEffect.gameObject.SetActive(true);
            m_completeEffect.time = 0.0f;
            m_completeEffect.Play();
        }

        private void Awake()
        {
            m_completeEffect.Clear();
            m_completeEffect.Stop();
        }

        private void OnDestroy()
        {
            foreach (var langBar in m_languageWordBars)
            {
                if (langBar != null)
                {
                    Destroy(langBar.gameObject);
                }
            }
            m_languageWordBars.Clear();
        }

        private void CreateLanguageOptions(Vector3 startPosition)
        {
            var languageOptions = GetLanguageList();
            for (var i = 0; i < languageOptions.Length; i++)
            {
                var option = languageOptions[i];
                var language = option.Item1;
                var wordType = option.Item2;
                var langNative = AssistantAI.SupportedLanguageEnumToNativeName(language);
                var langEnglish = AssistantAI.SupportedLanguageEnumToEnglishName(language);
                var wordBar = Instantiate(m_wordBarPrefab, transform);
                wordBar.Initialize(
                    langNative,
                    langEnglish,
                    wordType,
                    Camera.main.transform,
                    langNative);
                wordBar.PokeInteraction += wordBar3D =>
                {
                    LanguageSelected?.Invoke((AppSessionData.Language)Enum.Parse(typeof(AppSessionData.Language), langEnglish));
                };
                // Start off disabled and non-interactable
                wordBar.gameObject.SetActive(false);
                wordBar.DisableSqueezeInteraction();
                wordBar.DisablePokeInteraction();
                m_languageWordBars.Add(wordBar);
            }
            PositionLanguageOptions(startPosition);
        }

        private void PositionLanguageOptions(Vector3 startPosition)
        {
            var radius = 0.35f; // Radius of the circle
            var count = m_languageWordBars.Count;
            for (var i = 0; i < count; i++)
            {
                var angle = i * Mathf.PI * 2 / count;
                var newPos = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                var languageWordBar = m_languageWordBars[i];
                languageWordBar.transform.localPosition = newPos;
                languageWordBar.transform.LookAt(Camera.main.transform);
                languageWordBar.transform.Rotate(0, 180, 0); // Face the camera
                languageWordBar.AnimateInPositionDelayed(startPosition);
            }
        }

        private (AssistantAI.SupportedLanguage, TextCloudItem.WordType)[] GetLanguageList()
        {
            var filteredArray = new[]
            {
                (AssistantAI.SupportedLanguage.English,TextCloudItem.WordType.verb),
                (AssistantAI.SupportedLanguage.Spanish,TextCloudItem.WordType.adjective)

            };
            return filteredArray;
        }
    }
}