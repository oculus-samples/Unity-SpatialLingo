// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.Characters;
using SpatialLingo.Lessons;
using SpatialLingo.SceneObjects;
using UnityEngine;

namespace SpatialLingo.States
{
    [MetaCodeSample("SpatialLingo")]
    public class LanguageSelectState : FlowState
    {
        public const float TIMEOUT_LIMIT_WAIT_SELECT = 12.0f;

        public new delegate void SendFlowSignalEvent();
        public new SendFlowSignalEvent SendFlowSignal;

        [SerializeField] private GameObject m_languageControllerPrefab;

        private LanguageSeedController m_seedController;
        private GollyGoshInteractionManager m_gollyGoshInteractionManager;
        private FindGollyGoshState m_state;
        private LanguageSelect m_languageSelect;
        private Transform m_headsetTransform;
        private float m_lastTimestamp = 0.0f;
        private bool m_hasSelectedLanguage = false;

        public void WillGetFocus(GollyGoshInteractionManager manager, Transform spawnTarget, Transform headsetTransform, LanguageSeedController seedController)
        {
            m_headsetTransform = headsetTransform;
            m_gollyGoshInteractionManager = manager;
            m_seedController = seedController;
            ResetLastTimestamp();
            AppSessionData.Reset();

            var languageDistanceBelowUserHeight = 0.20f;
            var userPosition = headsetTransform.position;
            var userDirection = headsetTransform.forward;
            userDirection.y = 0;
            userDirection.Normalize();
            var oppositeUserDirection = userDirection;
            oppositeUserDirection.Scale(new Vector3(-1.0f, -1.0f, -1.0f));
            var oppositeUserRotation = Quaternion.LookRotation(userDirection);

            // Place above dirt mound at user height
            var gameObjectLanguage = Instantiate(m_languageControllerPrefab);
            var moundLocation = spawnTarget.position;
            gameObjectLanguage.transform.position = new Vector3(moundLocation.x, userPosition.y - languageDistanceBelowUserHeight, moundLocation.z);
            gameObjectLanguage.transform.rotation = oppositeUserRotation;
            m_languageSelect = gameObjectLanguage.GetComponent<LanguageSelect>();
            m_languageSelect.LanguageSelected += OnLanguageSelected;
            var seedPosition = m_seedController.transform.position;
            m_languageSelect.AnimateIn(seedPosition);

            // Move GG behind and above seed
            var offsetGGBack = 0.20f;
            var offsetGGUp = 0.10f;
            var offsetGGDir = userDirection;
            offsetGGDir.x *= offsetGGBack;
            offsetGGDir.y = offsetGGUp;
            offsetGGDir.z *= offsetGGBack;
            var startGG = seedController.transform.position + offsetGGDir;
            m_gollyGoshInteractionManager.Controller.MoveTo(startGG, false, null, false, GollyGoshController.GollyGoshMovement.EaseInOut, 0.5f);
            m_gollyGoshInteractionManager.Speak(AppSessionData.UserLanguageAI, Tutorial.LanguageSelectWaitPhrase());

            // Presentation sequence
            _ = StartCoroutine(GollyGoshPresentSeedSequence());
        }

        private IEnumerator GollyGoshPresentSeedSequence()
        {
            var distanceLookSide = 0.20f;
            var seedPosition = m_seedController.transform.position;
            var ggToSeed = seedPosition - m_gollyGoshInteractionManager.Controller.GetPosition();
            var ggSide = ggToSeed;
            ggSide.y = 0;
            ggSide.Normalize();
            ggSide.Scale(new Vector3(distanceLookSide, distanceLookSide, distanceLookSide));
            ggSide = Quaternion.Euler(0.0f, 90.0f, 0.0f) * ggSide;
            var left = seedPosition + ggSide;
            var right = seedPosition - ggSide;

            // Look to side 1
            m_gollyGoshInteractionManager.LookAt(left, 0.25f);
            yield return new WaitForSeconds(0.50f);

            // Look to side 2
            m_gollyGoshInteractionManager.LookAt(right, 0.50f);
            yield return new WaitForSeconds(0.75f);

            // Look at user
            m_gollyGoshInteractionManager.LookAt(m_headsetTransform, 1.0f);
        }

        private void OnLanguageSelected(AppSessionData.Language language)
        {
            AppSessionData.TargetLanguage = language;
            m_hasSelectedLanguage = true;
            m_gollyGoshInteractionManager.StopSpeaking();
            _ = StartCoroutine(GollyGoshExcitedSequence());
        }

        private IEnumerator GollyGoshExcitedSequence()
        {
            // Fade language select away
            var seedPosition = m_seedController.transform.position;
            m_languageSelect.AnimateOut(seedPosition);
            m_languageSelect = null;

            // Wait for thanks to complete
            m_gollyGoshInteractionManager.Celebrate();
            m_gollyGoshInteractionManager.Speak(AppSessionData.TargetLanguageAI, Tutorial.LanguageSelectComplete());

            // Long range XZ & short range Y follow while waiting
            m_gollyGoshInteractionManager.Controller.FollowXZandY(m_headsetTransform, 0.0f, 10.0f, 0.0f, 0.20f);

            yield return m_gollyGoshInteractionManager.WaitForSpeechOrTimeout();
            yield return m_gollyGoshInteractionManager.WaitForSpeechPause();

            // Done
            SendFlowSignal?.Invoke();
        }

        public void WillLoseFocus()
        {
            Destroy(gameObject);
        }

        private void ResetLastTimestamp()
        {
            m_lastTimestamp = Time.time;
        }

        private void Update()
        {
            if (!m_hasSelectedLanguage)
            {
                var diff = Time.time - m_lastTimestamp;
                var phrase = Tutorial.LanguageSelectWaitPhrase();

                if (diff > TIMEOUT_LIMIT_WAIT_SELECT)
                {
                    ResetLastTimestamp();
                    var distance = 1.0f;
                    // If user is too far away
                    if (distance > 3.0f)
                    {
                        phrase = Tutorial.SeedWaitDistancePhrase();
                    }
                    m_gollyGoshInteractionManager.Speak(AppSessionData.UserLanguageAI, phrase);
                }
            }
        }
    }
}