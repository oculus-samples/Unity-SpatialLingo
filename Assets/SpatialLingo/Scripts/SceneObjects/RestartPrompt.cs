// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using SpatialLingo.Audio;
using SpatialLingo.Lessons;
using Unity.VisualScripting;
using UnityEngine;

namespace SpatialLingo.SceneObjects
{
    [MetaCodeSample("SpatialLingo")]
    public class RestartPrompt
    {
        public event Action<RestartOption> SelectedOption;

        private readonly WordBar3D m_wordbarPrefab;
        private readonly Vector3 m_position;
        private readonly Quaternion m_rotation;

        private List<WordBar3D> m_restartOptions = new();
        private GameObject m_parentObj;

        public RestartPrompt(
            WordBar3D wordbarPrefab,
            Vector3 position,
            Quaternion rotation)
        {
            m_wordbarPrefab = wordbarPrefab;
            m_position = position;
            m_rotation = rotation;
        }

        public void AnimateIn()
        {
            if (m_restartOptions.Count == 0)
            {
                m_parentObj = new GameObject("RestartOptions") { transform = { position = m_position, rotation = m_rotation } };
                var distanceFromCenter = .33f;

                var restartOptionLocalPosition = Vector3.left * distanceFromCenter;
                var restartOption = SpawnRestartOption(
                    "Restart Lesson",
                    TextCloudItem.WordType.adjective,
                    restartOptionLocalPosition,
                    RestartOption.Lesson);
                m_restartOptions.Add(restartOption);

                var languageOptionLocalPosition = Vector3.right * distanceFromCenter;
                var languageOption = SpawnRestartOption(
                    "Select Language",
                    TextCloudItem.WordType.noun,
                    languageOptionLocalPosition,
                    RestartOption.Language);
                m_restartOptions.Add(languageOption);
            }

            m_restartOptions.ForEach(option => option.AnimateIn());

            AppAudioController.Instance.PlaySound(SoundEffect.WordCloudAppear, m_position);
        }

        public void AnimateOut(bool autoDestroy = true)
        {
            AppAudioController.Instance.PlaySound(SoundEffect.WordCloudAppear, m_position);

            if (autoDestroy)
            {
                _ = CoroutineRunner.instance.StartCoroutine(DestroySequence());
            }
            else
            {
                m_restartOptions.ForEach(option => option.AnimateOut());
            }
        }

        private IEnumerator DestroySequence()
        {
            m_restartOptions.ForEach(option => option.AnimateOutDestroy());
            yield return new WaitUntil(() => m_restartOptions.TrueForAll(option => option == null));

            m_restartOptions.Clear();
            UnityEngine.Object.Destroy(m_parentObj);
            m_parentObj = null;
        }

        private WordBar3D SpawnRestartOption(
            string optionName,
            TextCloudItem.WordType wordType,
            Vector3 localPosition,
            RestartOption restartOption)
        {
            var restartObject = UnityEngine.Object.Instantiate(m_wordbarPrefab, m_parentObj.transform);
            restartObject.Initialize(
                optionName,
                string.Empty,
                wordType,
                Camera.main.transform,
                string.Empty);
            restartObject.DisableSqueezeInteraction();
            restartObject.PokeInteraction += _ => SelectedOption?.Invoke(restartOption);
            restartObject.transform.localPosition += localPosition;
            restartObject.transform.LookAt(Camera.main.transform);
            restartObject.transform.Rotate(0, 180, 0);
            return restartObject;
        }

        public enum RestartOption
        {
            Lesson,
            Language
        }
    }
}