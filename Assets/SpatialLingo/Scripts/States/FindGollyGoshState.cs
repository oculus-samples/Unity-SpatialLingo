// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.Audio;
using SpatialLingo.Characters;
using SpatialLingo.Lessons;
using Unity.VisualScripting;
using UnityEngine;

namespace SpatialLingo.States
{
    [MetaCodeSample("SpatialLingo")]
    public class FindGollyGoshState : FlowState
    {
        private const int LAYERS_PER_FRAME_TAXON = 10;
        private const float TIMEOUT_LIMIT_WAIT_FIND_GG = 8.0f;
        private const float TIMEOUT_LIMIT_WAIT_SELECT = 8.0f;
        private const float MAX_DISTANCE_STILL_ENGAGED = 2.0f;

        public new delegate void SendFlowSignalEvent(LanguageSeedController seedController, FocusPointController focusController);
        public new SendFlowSignalEvent SendFlowSignal;

        [SerializeField] private FocusPointController m_moundPrefab;
        [SerializeField] private GameObject m_seedControllerPrefab;

        private GollyGoshInteractionManager m_gollyGoshInteractionManager;
        private LanguageSeedController m_seedController;
        private FocusPointController m_mound;
        private AppAudioController m_audioController;
        private Transform m_headsetTransform;
        private float m_lastTimestamp = 0.0f;
        private bool m_isGollyGoshHidden = false;
        private bool m_isRunningIntro = false;
        private bool m_hasSelectedSeed = false;

        public void WillGetFocus(GollyGoshInteractionManager manager, Transform headsetTransform, AppAudioController audioController)
        {
            m_gollyGoshInteractionManager = manager;
            m_headsetTransform = headsetTransform;
            m_audioController = audioController;

            // High starting search:
            var app = Variables.Application.Get<SpatialLingoApp>(nameof(SpatialLingoApp));
            app.LessonInteractionManager.SetLayersPerFrame(LAYERS_PER_FRAME_TAXON);

            var moundObject = Instantiate(m_moundPrefab);
            m_mound = moundObject.GetComponent<FocusPointController>();
            Variables.Application.Set(nameof(FocusPointController), m_mound);

            m_gollyGoshInteractionManager.GollyGoshFound += OnGollyGoshFound;
            m_isGollyGoshHidden = true;
            ResetLastTimestamp();
            var position = m_gollyGoshInteractionManager.HideInUserRoom();

            m_mound.SetOrientation(position, Quaternion.identity);
            m_mound.ShowShimmer();
            var seedGameObject = Instantiate(m_seedControllerPrefab);
            m_seedController = seedGameObject.GetComponent<LanguageSeedController>();
            Variables.Application.Set(nameof(LanguageSeedController), m_seedController);
            m_seedController.DisableGrabInteraction();
            m_seedController.gameObject.SetActive(false);
        }

        public void WillLoseFocus()
        {
            Destroy(gameObject);
        }

        private void OnGollyGoshFound()
        {
            // Stop any remaining nags
            ResetLastTimestamp();
            m_gollyGoshInteractionManager.StopSpeaking();

            m_audioController.PlaySound(SoundEffect.GGPulledFromGround, m_gollyGoshInteractionManager.Controller.GetPosition());

            m_gollyGoshInteractionManager.LookAt(m_headsetTransform);

            m_mound.HideShimmer();
            m_mound.ShowPopOutEffect();

            // Move straight up
            var startGG = m_mound.transform.position + new Vector3(0.0f, 1.5f, 0.0f);
            m_gollyGoshInteractionManager.ShowFaceSurprised();
            m_gollyGoshInteractionManager.Speak(AppSessionData.TargetLanguageAI, Tutorial.ThanksPhrase());
            m_gollyGoshInteractionManager.Celebrate();

            m_gollyGoshInteractionManager.Controller.MoveTo(startGG, false, GollyGoshMovedToReadyStart, false);
            // Look at user
            m_gollyGoshInteractionManager.LookAt(m_headsetTransform);
        }

        private void GollyGoshMovedToReadyStart(bool completed)
        {
            m_isGollyGoshHidden = false;
            m_isRunningIntro = true;
            m_gollyGoshInteractionManager.GollyGoshFound -= OnGollyGoshFound;
            m_gollyGoshInteractionManager.Controller.FollowXZandY(m_headsetTransform, 0.50f, 0.90f, 0.0f, 0.10f);

            // Start seed presentation
            m_seedController.SeedWasInteracted += OnSeedWasInteracted;
            _ = StartCoroutine(GollyGoshPresentSeed());
        }

        private IEnumerator GollyGoshPresentSeed()
        {
            // Wait for thanks to complete
            m_gollyGoshInteractionManager.Celebrate();
            m_gollyGoshInteractionManager.ShowFaceHappy();
            m_gollyGoshInteractionManager.LookAt(m_headsetTransform);
            yield return m_gollyGoshInteractionManager.WaitForSpeechOrTimeout();

            // Move around & thanks for rescue 
            m_gollyGoshInteractionManager.Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.HelloWorld);
            m_gollyGoshInteractionManager.Speak(AppSessionData.UserLanguageAI, Tutorial.GreetingPhrase());
            m_gollyGoshInteractionManager.ShowFaceNeutral();
            yield return m_gollyGoshInteractionManager.WaitForSpeechOrTimeout();
            yield return m_gollyGoshInteractionManager.WaitForSpeechPause(); ;

            m_gollyGoshInteractionManager.Speak(AppSessionData.UserLanguageAI, Tutorial.PresentSeedPhrase());
            m_gollyGoshInteractionManager.ShowFaceHappy();
            // Record where the mound is
            var moundPosition = m_mound.gameObject.transform.position;
            // Move GG in direction opposite of user to mound
            var moundToUser = m_headsetTransform.position - moundPosition;
            // Ignore Y difference
            moundToUser.y = 0.0f;
            moundToUser.Normalize();
            var distanceFromMound = 0.45f;
            moundToUser.Scale(new Vector3(distanceFromMound, distanceFromMound, distanceFromMound));
            var ggWaitPosition = moundPosition - moundToUser;
            ggWaitPosition.y = m_headsetTransform.position.y;
            m_gollyGoshInteractionManager.Controller.StopFollowing();
            m_gollyGoshInteractionManager.Controller.MoveTo(ggWaitPosition, false, null, false);
            m_gollyGoshInteractionManager.Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.PointForwardContinuous);

            // Place Seed above mound, slightly lower than GG, slightly after GG moves back
            yield return new WaitForSeconds(0.50f);
            m_seedController.gameObject.SetActive(true);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            var aboveMound = moundPosition;
            aboveMound.y = ggWaitPosition.y - 0.20f;
            m_seedController.MoveTo(aboveMound);
            m_seedController.FadeIn();

            // Play seed pulled SFX
            m_audioController.PlaySound(SoundEffect.DirtMoundPull, moundPosition);

            // Transition from user to seed
            m_gollyGoshInteractionManager.LookAt(m_seedController.transform, 0.50f);

            // Waiting for present seed phrase to complete:
            yield return m_gollyGoshInteractionManager.WaitForSpeechOrTimeout();
            yield return m_gollyGoshInteractionManager.WaitForSpeechPause();

            m_gollyGoshInteractionManager.Speak(AppSessionData.UserLanguageAI, Tutorial.SeedWaitPlantPhrase());
            m_gollyGoshInteractionManager.ShowFaceHappy();
            m_gollyGoshInteractionManager.Celebrate();

            // Transition from seed to user
            m_gollyGoshInteractionManager.LookAt(m_headsetTransform, 0.50f);

            yield return m_gollyGoshInteractionManager.WaitForSpeechOrTimeout();
            yield return m_gollyGoshInteractionManager.WaitForSpeechPause();

            // Start seed interaction
            ResetLastTimestamp();
            m_isRunningIntro = false;
            // Go directly to grab seed:
            OnSeedWasInteracted();
        }

        private void OnSeedWasInteracted()
        {
            m_hasSelectedSeed = true;
            m_seedController.SeedWasInteracted -= OnSeedWasInteracted;

            SendFlowSignal?.Invoke(m_seedController, m_mound);
        }

        private void ResetLastTimestamp()
        {
            m_lastTimestamp = Time.time;
        }

        private void Update()
        {
            if (m_isGollyGoshHidden)
            {
                var diff = Time.time - m_lastTimestamp;
                if (diff > TIMEOUT_LIMIT_WAIT_FIND_GG)
                {
                    ResetLastTimestamp();
                    var phrase = Tutorial.BeckonPhrase();
                    m_gollyGoshInteractionManager.Speak(AppSessionData.UserLanguageAI, phrase);
                }
            }
            else if (!m_isRunningIntro && !m_hasSelectedSeed)
            {
                var diff = Time.time - m_lastTimestamp;
                if (diff > TIMEOUT_LIMIT_WAIT_SELECT)
                {
                    ResetLastTimestamp();
                    var phrase = Tutorial.SeedWaitPlantPhrase();
                    var distance = Vector3.Distance(m_headsetTransform.position, m_mound.gameObject.transform.position);
                    if (distance > MAX_DISTANCE_STILL_ENGAGED)
                    {
                        phrase = Tutorial.SeedWaitDistancePhrase();
                    }
                    m_gollyGoshInteractionManager.Speak(AppSessionData.UserLanguageAI, phrase);
                }
            }
        }
    }
}