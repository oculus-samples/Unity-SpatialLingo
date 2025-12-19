// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.Samples;
using SpatialLingo.AI;
using SpatialLingo.AppSystems;
using SpatialLingo.Audio;
using SpatialLingo.Characters;
using SpatialLingo.SceneObjects;
using SpatialLingo.SpeechAndText;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpatialLingo.Lessons
{
    /// <summary>
    /// Control Lessons: Correctness/wrongness of answers
    /// 
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class ExerciseManager : MonoBehaviour
    {
        // Show debug lessons rather than lessons from image/object tracking 
        private static bool s_debugLessonsShow = false;
        // Allow debug interactions to quickly iterate lessons
        private static bool s_debugOptionsEnabled = false;
        // Show transcription from STT system 
        private static bool s_debugTranscriptionFeedbackEnabled = true;

        // Required lessons to complete successfully
        private const int REQUIRED_COMPLETE_COUNT_TIER_1 = 3;
        private const int REQUIRED_COMPLETE_COUNT_TIER_2 = 2;
        private const int REQUIRED_COMPLETE_COUNT_TIER_3 = 1;

        // Goal lessons to display (if tracking is available)
        private const int GOAL_LESSON_COUNT_TIER_1 = 5;
        private const int GOAL_LESSON_COUNT_TIER_2 = 4;
        private const int GOAL_LESSON_COUNT_TIER_3 = 3;

        // Allowed number of lessons to complete (total needs to have room on tree)
        private const int MAX_LESSON_COMPLETE_COUNT_TIER_1 = 3;
        private const int MAX_LESSON_COMPLETE_COUNT_TIER_2 = 3;
        private const int MAX_LESSON_COMPLETE_COUNT_TIER_3 = 3;

        // Taxon processing 
        private const int LAYERS_PER_FRAME_TAXON_TIER_1 = 6;
        private const int LAYERS_PER_FRAME_TAXON_TIER_2 = 4;
        private const int LAYERS_PER_FRAME_TAXON_TIER_3 = 2;

        [MetaCodeSample("SpatialLingo")]
        public class ExerciseUpdateResult
        {
            public Lesson Lesson { get; }
            public int Attempts = 0;
            public ExerciseUpdateResult(Lesson lesson) => Lesson = lesson;
        }

        [SerializeField] private TranscribeFeedbackController m_feedbackController;

        private VoiceTranscriber m_transcriber;
        private LessonsManager m_lessonsManager;
        private LessonInteractionManager m_lessonManager;
        private Transform m_centerEyeAnchor;

        private string m_currentTranscriptionEvaluationID;

        private const float TIMEOUT_WAITING_TRACKING_FIRST = 2.0f; // not enough tracking elements
        private const float TIMEOUT_WAITING_TRACKING_OTHER = 8.0f;
        private const float TIMEOUT_EXPLORING_WORLD_FIRST = 2.0f; // outside of a lesson ~10-20s
        private const float TIMEOUT_EXPLORING_WORLD_OTHER = 8.0f;
        private const float TIMEOUT_DURING_LESSON_FIRST = 2.0f; // during a lesson ~10s
        private const float TIMEOUT_DURING_LESSON_OTHER = 8.0f;
        private const float TIMEOUT_BERRY_SQUEEZE_FIRST = 2.0f; // waiting for golden berry
        private const float TIMEOUT_BERRY_SQUEEZE_OTHER = 8.0f;

        private bool m_trackingNagStarted;
        private bool m_exploringNagStarted;
        private bool m_lessonNagStarted;
        private bool m_squeezeNagStarted;

        private DateTime m_previousInteractionTime;
        private bool m_isLessonInteractionEnabled;
        private int m_tutorialIndex;

        public delegate void ExperienceEvent(ExerciseManager manager);
        // All done with exercises/lessons
        public event ExperienceEvent AllTiersCompleted;

        private int m_completedInteractionCount;

        private Lesson3DInteractor m_activeLessonInteractor;
        private TreeController m_treeController;
        private GollyGoshInteractionManager m_gollyGoshManager;
        private AssistantAI m_assistantAI;

        private int m_goalLessonsDisplayMax;
        private int m_goalLessonsCompleteMax;
        private bool m_isSearchingForMoreLessons;
        private DateTime m_lastSearchTime;
        private float m_timeBetweenLessonSearches = 1.0f; // seconds

        private bool m_isWaitingForBerrySqueeze;
        private List<Lesson3DInteractor> m_activeUntrackedLessonList = new();

        public void Initialize(LessonsManager lessonsManager, LessonInteractionManager interactionManager, VoiceTranscriber transcriber, Transform centerEyeAnchor, AssistantAI assistantAI)
        {
            m_lessonsManager = lessonsManager;
            m_lessonManager = interactionManager;
            m_transcriber = transcriber;
            m_centerEyeAnchor = centerEyeAnchor;
            m_assistantAI = assistantAI;

            m_lessonManager.LessonActivated += OnLessonActivated;
            m_lessonManager.LessonDeactivated += OnLessonDeactivated;
            m_lessonManager.LessonCompletedSuccess += OnLessonCompleted;

            m_lessonsManager.ActivityAcquisitionFailed += OnActivityAcquisitionFailed;

            m_transcriber.VoiceTranscriptionUpdateComplete += OnVoiceTranscriptionUpdateComplete;
            m_transcriber.VoiceTranscriptionUpdateIncomplete += OnVoiceTranscriptionUpdateIncomplete;
            m_transcriber.VoiceTranscriptionVolumeUpdate += OnVoiceTranscriptionUpdateVolume;

            // Start following UI immediately for possible UI updates at any state
            m_feedbackController.StartFollowingTransform(m_centerEyeAnchor);
        }

        private void OnActivityAcquisitionFailed(LessonsManager.RequestStatusResult result)
        {
            _ = result != null ? result.Status : LessonsManager.RequestStatus.Unknown;
            if (result != null)
            {
                if (result.Status == LessonsManager.RequestStatus.FailWifi)
                {
                    m_feedbackController.ShowErrorWifi();
                }
                else
                {
                    m_feedbackController.ShowErrorServer();
                }
            }
        }

        private void SetGollyGoshFollowUser()
        {
            SetGollyGoshWatchUser();
            m_gollyGoshManager.Controller.FollowXZandY(m_centerEyeAnchor, 0.50f, 2.0f, 0.0f, 0.10f);
        }

        private void SetGollyGoshWatchUser()
        {
            m_gollyGoshManager.LookAt(m_centerEyeAnchor, 0.25f);
        }

        public void SetGollyGoshManager(GollyGoshInteractionManager gollyGoshManager)
        {
            m_gollyGoshManager = gollyGoshManager;
        }

        public void SetTreeManager(TreeController treeController)
        {
            m_treeController = treeController;
        }
        public void SetTargetLanguage(AppSessionData.Language targetLanguage)
        {
            m_transcriber.SetTargetLanguage(Language.Language.AppSessionToWitaiLanguage(targetLanguage));
            m_lessonManager.SetTargetLanguage(AppSessionData.TargetLanguageAI);
            m_completedInteractionCount = 0;
        }

        public void StartExperience()
        {
            m_isLessonInteractionEnabled = true;
            StartCurrentTier();
        }

        private void StartCurrentTier()
        {
            var tier = AppSessionData.Tier;
            StartTier(tier);
            m_trackingNagStarted = false;
            m_exploringNagStarted = false;
            m_lessonNagStarted = false;
            m_squeezeNagStarted = false;
            UpdateTimeoutInteraction();
        }

        public void ResetForReuse()
        {
            m_lessonManager.StopLessons();
            StopRequestBestLessons();
            ClearActiveLessonsList();
            m_isLessonInteractionEnabled = false;
        }

        private void StartTier(int tier)
        {
            m_tutorialIndex = 0;
            AppSessionData.Tier = tier;

            // Pause image processing for high CPU / GPU activities:
            SetLessonManagerLayersPerFrameNone();

            SetTreeToNextTier();

            if (tier == 3)
            {
                m_treeController.OpenPortal(true);
            }

            // Disable interactions while delaying
            m_lessonManager.DisallowActivationsOnProximity();
            m_isLessonInteractionEnabled = false;
            StopRequestBestLessons();
            ClearActiveLessonsList();

            // Delay starts
            _ = StartCoroutine(ContinueStartTier());
        }

        /// <summary>
        /// Adds short delay before earliest point berries (lessions) can be sent out 
        /// Used to wait for tree animation to keep focus and spread operations 
        /// </summary>
        /// <returns></returns>
        private IEnumerator ContinueStartTier()
        {
            var tier = AppSessionData.Tier;
            var lessonCountGoal = tier switch
            {
                2 => GOAL_LESSON_COUNT_TIER_2,
                3 => GOAL_LESSON_COUNT_TIER_3,
                // 1
                _ => GOAL_LESSON_COUNT_TIER_1,
            };

            var lessonCompleteMax = tier switch
            {
                2 => MAX_LESSON_COMPLETE_COUNT_TIER_2,
                3 => MAX_LESSON_COMPLETE_COUNT_TIER_3,
                // 1
                _ => MAX_LESSON_COMPLETE_COUNT_TIER_1,
            };

            // Wait short amount of time for tree animations to keep user's focus
            yield return new WaitForSeconds(0.5f);

            m_isLessonInteractionEnabled = true;
            SetLessonManagerLayersPerFrameFromTier();

            // Show top lessons when they become ready
            RequestBessLessonsUntilSatisfied(lessonCountGoal, lessonCompleteMax);
            m_lessonManager.AllowActivationsOnProximity();
            SetGollyGoshFollowUser();
        }

        private void SetLessonManagerLayersPerFrameFromTier()
        {
            var tier = AppSessionData.Tier;
            switch (tier)
            {
                case 3:
                    m_lessonManager.SetLayersPerFrame(LAYERS_PER_FRAME_TAXON_TIER_3);
                    break;
                case 2:
                    m_lessonManager.SetLayersPerFrame(LAYERS_PER_FRAME_TAXON_TIER_2);
                    break;
                //case 1:
                default:
                    m_lessonManager.SetLayersPerFrame(LAYERS_PER_FRAME_TAXON_TIER_1);
                    break;
            }
        }

        private void SetLessonManagerLayersPerFrameNone()
        {
            m_lessonManager.SetLayersPerFrame(0);
        }

        private void StopRequestBestLessons()
        {
            if (!m_isSearchingForMoreLessons)
            {
                return;
            }

            m_isSearchingForMoreLessons = false;
        }

        private void RequestBessLessonsUntilSatisfied(int lessonMaxGoal, int lessonMaxComplete)
        {
            if (m_isSearchingForMoreLessons)
            {
                return;
            }

            m_isSearchingForMoreLessons = true;
            ClearActiveLessonsList();
            m_goalLessonsDisplayMax = lessonMaxGoal;
            m_goalLessonsCompleteMax = lessonMaxComplete;

            // If debug bool is set: only show debug lessons and exit
            if (s_debugLessonsShow)
            {
                PresentDebugLessons(5);
                m_isSearchingForMoreLessons = false;
                return;
            }

            // Start search
            RequestBestLessonsIteration();
        }

        private void RequestBestLessonsIteration()
        {
            m_lastSearchTime = DateTime.Now;
            var existingLessons = m_activeUntrackedLessonList.Count;
            var remainingLessons = m_goalLessonsDisplayMax - existingLessons;

            var lessons = m_lessonManager.GetNextBestLessons(m_activeUntrackedLessonList, remainingLessons);
            if (lessons.Length > 0)
            {
                var maxNewLessons = Math.Min(remainingLessons, lessons.Length);
                for (var i = 0; i < maxNewLessons; i++)
                {
                    var lesson = lessons[i];
                    var untracked = m_lessonManager.CreateUntrackedCopy(lesson);
                    m_activeUntrackedLessonList.Add(untracked);

                    // Move from tree to lesson locations
                    untracked.SetDebugStatus(false);
                    untracked.UpdateFromLesson();
                    SendBerryToActivation(untracked);
                }
                // Play general berry trails start sound
                AppAudioController.Instance.PlaySound(SoundEffect.BerryTrailsStart, m_treeController.transform.position);

                // Prevent more lessons from being queried, have enough
                if (m_activeUntrackedLessonList.Count == m_goalLessonsDisplayMax)
                {
                    StopRequestBestLessons();
                }
            }
        }

        /// <summary>
        /// Show some randomly generated berries, used for debugging at runtime
        /// </summary>
        /// <param name="count">number of berries / lessons to generate</param>
        private void PresentDebugLessons(int count = 5)
        {
            var activity = new Activity
            {
                Classification = "DebugLessons",
                EnglishWord = "English Word",
                UserLanguageWord = "User Lang Word",
                TargetLanguageWord = "Target Lang Word",
                VerbsTargetLanguage = new string[] { "Verb Target A", "Verb Target B", "Verb Target C" },
                AdjectivesTargetLanguage = new string[] { "Adj Target A", "Adj Target B", "Adj Target C" },
                VerbsUserLanguage = new string[] { "Verb User A", "Verb User B", "Verb User C" },
                AdjectivesUserLanguage = new string[] { "Adj User A", "Adj User B", "Adj User C" }
            };

            for (var i = 0; i < count; i++)
            {
                var randX = Random.Range(-0.50f, 0.50f);
                var randY = Random.Range(-0.20f, 0.20f);
                var randZ = Random.Range(-0.50f, 0.50f);

                var pos = new Vector3(0.0f, 0.0f, 0.0f);
                pos += new Vector3(randX, randY, randZ);
                pos += m_centerEyeAnchor.position;
                var siz = new Vector3(0.3f, 0.4f, 0.2f);
                var lesson = new Lesson(activity, pos, siz);
                var interactor = m_lessonManager.CreateUntrackedInteractor(lesson);

                m_activeUntrackedLessonList.Add(interactor);

                // Move from tree to lesson locations
                interactor.SetDebugStatus(false);
                interactor.UpdateFromLesson();
                SendBerryToActivation(interactor);
            }
        }

        private void ClearActiveLessonsList(bool fadeOut = false)
        {
            foreach (var interactor in m_activeUntrackedLessonList)
            {
                m_lessonManager.RemoveLesson(interactor.Lesson, fadeOut);
            }
            m_activeUntrackedLessonList.Clear();
        }

        private void OnLessonActivated(Lesson3DInteractor interactor)
        {
            m_currentTranscriptionEvaluationID = null;
            UpdateTimeoutInteraction();
            m_tutorialIndex = 0;

            m_gollyGoshManager.StopSpeaking();
            RegisterActiveInteractor(interactor);
            m_transcriber.ClearTranscriptions();
            m_transcriber.SetTargetLanguage(Language.Language.AppSessionToWitaiLanguage(AppSessionData.TargetLanguage));
            m_transcriber.StartListening();
            m_feedbackController.ShowMicFeedback();
            m_feedbackController.ClearTextFeedback();
            m_feedbackController.ShowTextFeedback();
            m_gollyGoshManager.Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.ListenStart);
        }

        private void OnLessonDeactivated(Lesson3DInteractor interactor)
        {
            m_currentTranscriptionEvaluationID = null;
            UpdateTimeoutInteraction();
            m_gollyGoshManager.StopSpeaking();
            UnregisterActiveInteractor();
            m_feedbackController.HideMicFeedback();
            m_feedbackController.ClearTextFeedback();
            m_feedbackController.HideTextFeedback();
            m_transcriber.StopListening();
            m_lessonNagStarted = false;
            m_exploringNagStarted = false;
            UpdateTimeoutInteraction();
            SetGollyGoshFollowUser();
            var userLanguage = AppSessionData.UserLanguageAI;
            m_gollyGoshManager.Speak(userLanguage, Tutorial.ExitLessonPhrase());
            m_gollyGoshManager.Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.Celebrate);
        }

        private void OnLessonCompleted(Lesson3DInteractor interactor)
        {
            m_currentTranscriptionEvaluationID = null;
            m_feedbackController.HideMicFeedback();
            m_transcriber.StopListening();
            UpdateTimeoutInteraction();
            m_gollyGoshManager.StopSpeaking();

            UnregisterActiveInteractor();

            m_lessonNagStarted = false;
            m_exploringNagStarted = false;

            m_gollyGoshManager.CelebrateExplosion();

            // Move berry to tree
            SendBerryToTree(interactor);
            if (m_completedInteractionCount >= m_goalLessonsCompleteMax)
            {
                // Prevent any further lessons from being available
                ClearActiveLessonsList(true);
                m_lessonManager.DisallowActivationsOnProximity();
            }
        }
        private void RegisterActiveInteractor(Lesson3DInteractor interactor)
        {
            if (m_activeLessonInteractor != null)
            {
                Debug.LogWarning($"ExerciseManager - RegisterActiveInteractor - activeLessonInteractor was already set: {m_activeLessonInteractor}");
                UnregisterActiveInteractor();
            }
            m_activeLessonInteractor = interactor;
            m_activeLessonInteractor.PokeInteraction += OnLessonWordPoked;
            m_activeLessonInteractor.SqueezeInteraction += OnLessonWordSqueezed;
        }

        private void UnregisterActiveInteractor()
        {
            if (m_activeLessonInteractor != null)
            {
                m_activeLessonInteractor.PokeInteraction -= OnLessonWordPoked;
                m_activeLessonInteractor.SqueezeInteraction -= OnLessonWordSqueezed;
                m_activeLessonInteractor = null;
            }
        }

        private void OnLessonWordPoked(WordBar3D word)
        {
            UpdateTimeoutInteraction();
        }

        private void OnLessonWordSqueezed(WordBar3D word)
        {
            UpdateTimeoutInteraction();
        }

        private void OnVoiceTranscriptionUpdateVolume(float volume)
        {
            var isTalking = m_feedbackController.UpdateFromMicVolume(volume);
            if (isTalking)
            {
                UpdateTimeoutInteraction();
                m_gollyGoshManager.StopSpeaking();
            }
        }

        private void OnVoiceTranscriptionUpdateIncomplete(VoiceTranscriber.VoiceTranscriptionEvent result)
        {
            if (s_debugTranscriptionFeedbackEnabled)
            {
                m_feedbackController.SetTextFeedback(result.Transcription);
            }
            UpdateTimeoutInteraction();
            m_gollyGoshManager.StopSpeaking();
        }

        private void OnVoiceTranscriptionUpdateComplete(VoiceTranscriber.VoiceTranscriptionEvent result)
        {

            if (s_debugTranscriptionFeedbackEnabled)
            {
                m_feedbackController.SetTextFeedback(result.Transcription);
            }
            var mostRecent = result.Transcriber.Transcriptions.Last();
            UpdateTimeoutInteraction();
            m_gollyGoshManager.StopSpeaking();
            if (m_activeLessonInteractor != null)
            {
                var words = m_activeLessonInteractor.PresentedWords;
                CheckForTranscriptionResponse(words, mostRecent);
            }
            else
            {
                Debug.LogWarning($"ExerciseManager - got a transcription result, but no active lesson present");
            }
        }

        private async void CheckForTranscriptionResponse(Lesson3DInteractor.PresentedWordContext words, string transcription)
        {
            // Only want one of these evaluations at a time - if one is already running, need to ignore previous result:
            m_currentTranscriptionEvaluationID = AssistantAI.ContextReferenceOrDefault();
            var response = await m_assistantAI.EvaluateTranscriptionForWordCloud(
                AppSessionData.TargetLanguageAI, AppSessionData.UserLanguageAI, transcription, words.Nouns, words.Adjectives, words.Verbs,
                m_currentTranscriptionEvaluationID);

            // Check to see if the response is old, ignore if so
            if (response != null && m_currentTranscriptionEvaluationID == response.ContextID)
            {
                if (response.IsPassing && m_activeLessonInteractor != null)
                {
                    OnAttemptSuccessLesson();
                }
                else
                {
                    OnAttemptFailLesson(response.FailReason);
                }
            }
        }

        public void Dispose()
        {
            // 
        }

        private void SendBerryToActivation(Lesson3DInteractor interactor)
        {
            var lesson = interactor.Lesson;
            var start = m_treeController.transform.position;
            // Add height and randomization location
            start.y = 1.0f + Random.Range(0.0f, 0.5f);

            var ending = interactor.Lesson.Position;

            // Get new berry from a prefab
            var berry = interactor.CreateBerry();
            berry.TurnGoldenColor();
            // Animate a random berry from tree to activation
            berry.ShowPathEffects();
            berry.MoveToDestination(start, ending, interactor.BerryArrived);
            berry.PlayBerryTrailSound();
        }

        private void SendBerryToTree(Lesson3DInteractor interactor)
        {
            // Ignore all other berry squeezes & set to normal color - in case user is playing more lessons
            var currentBerries = m_treeController.CurrentBerries();
            foreach (var gameO in currentBerries)
            {
                var existingBerry = gameO.GetComponent<BerryController>();
                if (existingBerry != null)
                {
                    existingBerry.BerrySqueezeInteraction -= OnBerrySqueezeInteraction;
                    existingBerry.TurnBerryColor();
                    existingBerry.HideShimmer();
                }
            }

            var berry = interactor.TakeBerry();
            berry.ShowPathEffects();
            var sent = m_treeController.MoveBerryToIndex(berry.gameObject, BerryReachedTree);
            if (sent)
            {
                berry.PlayBerryTrailSound();
            }
            else
            {
                Debug.LogWarning("ExerciseManager - Berry did not move");
                BerryReachedTree(berry.gameObject);
            }
            // Only Wait for last berry
            if (HasCompletedAllLessonsForTier())
            {
                m_isWaitingForBerrySqueeze = true;
            }
            else
            {
                // Non-final berries turn fruit colored 
                berry.TurnBerryColor();
            }
            SendGGToBerry(berry.gameObject);
        }


        private void OnBerrySqueezeInteraction(BerryController berry)
        {
            berry.BerrySqueezeInteraction -= OnBerrySqueezeInteraction;
            StopRequestBestLessons();
            m_lessonManager.DisallowActivationsOnProximity();
            m_lessonManager.StopLessons();
            ClearActiveLessonsList(true);
            // Pause image processing for brief berry squeeze 
            SetLessonManagerLayersPerFrameNone();
            _ = StartCoroutine(SpaceOutActionsAfterBerrySqueeze(berry));
        }

        private IEnumerator SpaceOutActionsAfterBerrySqueeze(BerryController berry)
        {
            berry.TurnBerryColor();
            yield return new WaitForEndOfFrame();
            berry.HideShimmer();
            yield return new WaitForSeconds(0.50f);
            m_isWaitingForBerrySqueeze = false;
            SetLessonManagerLayersPerFrameFromTier();
            CheckGotoNextTier();
        }

        private void SendGGToBerry(GameObject berry)
        {
            UpdateTimeoutInteraction();
            m_gollyGoshManager.FollowBerryToTree(berry.gameObject);
        }

        private void BerryReachedTree(GameObject berry)
        {
            var controller = berry.GetComponent<BerryController>();
            // Pause image processing for various effects to take priority
            SetLessonManagerLayersPerFrameNone();
            _ = StartCoroutine(SpaceOutActionsAfterBerryReachedTree(controller));
        }
        private IEnumerator SpaceOutActionsAfterBerryReachedTree(BerryController controller)
        {
            controller.HidePathEffects(true);
            yield return new WaitForEndOfFrame();
            controller.StopBerryTrailSound();
            yield return new WaitForEndOfFrame();
            // Show shimmer only after the berry reaches the tree 
            if (controller.IsGolden)
            {
                controller.ShowShimmer();
                controller.BerrySqueezeInteraction += OnBerrySqueezeInteraction;
                yield return new WaitForEndOfFrame();
            }
            controller.EnableInteraction();
            m_gollyGoshManager.StopFollowing();
            yield return new WaitForEndOfFrame();
            SetGollyGoshWatchUser();
            SetLessonManagerLayersPerFrameFromTier();
        }

        private bool HasCompletedAllLessonsForTier()
        {
            var tier = AppSessionData.Tier;
            var requiredComplete = tier switch
            {
                2 => REQUIRED_COMPLETE_COUNT_TIER_2,
                3 => REQUIRED_COMPLETE_COUNT_TIER_3,
                // 1
                _ => REQUIRED_COMPLETE_COUNT_TIER_1,
            };
            return m_completedInteractionCount >= requiredComplete;
        }

        private void CheckGotoNextTier()
        {
            if (HasCompletedAllLessonsForTier())
            {
                if (!m_isWaitingForBerrySqueeze)
                {
                    var tier = AppSessionData.Tier;
                    m_completedInteractionCount = 0; // reset for next round

                    var nextTier = tier + 1;
                    if (nextTier > 3)
                    {
                        AllTiersCompleted?.Invoke(this);
                    }
                    else
                    {
                        m_gollyGoshManager.Speak(AppSessionData.UserLanguageAI, Tutorial.ReactToTreeGrow());
                        StartTier(nextTier);
                    }
                }
            }
        }

        private void SetTreeToNextTier()
        {
            var tier = AppSessionData.Tier;
            switch (tier)
            {
                case 2:
                case 3:
                    m_treeController.AnimateToTier(tier);
                    break;
            }
        }

        private void OnTimeoutWorld()
        {
            m_gollyGoshManager.StopSpeaking();
            // Get list of lessons:
            m_gollyGoshManager.TutorialGoToNearestLesson(m_activeUntrackedLessonList.ToArray());
            WaitingForLessonTimeout();
        }

        private void OnTimeoutTrackingLessons()
        {
            m_gollyGoshManager.StopSpeaking();
            var userLanguage = AppSessionData.UserLanguageAI;
            m_gollyGoshManager.Speak(userLanguage, Tutorial.TrackingWaitingPhrase());
        }

        private void WaitingForLessonTimeout()
        {
            var userLanguage = AppSessionData.UserLanguageAI;
            m_gollyGoshManager.Speak(userLanguage, Tutorial.LessonWaitingPhrase(AppSessionData.TargetLanguageName));
        }

        private void OnTimeoutLesson()
        {
            m_gollyGoshManager.StopSpeaking();
            PlayUserTutorial();
        }

        private void OnAttemptFailLesson(string failReason = null)
        {
            UpdateTimeoutInteraction();
            m_gollyGoshManager.StopSpeaking();
            var userLanguage = AppSessionData.UserLanguageAI;
            if (!string.IsNullOrWhiteSpace(failReason))
            {
                m_gollyGoshManager.Speak(userLanguage, failReason);
            }
            else
            {
                m_gollyGoshManager.Speak(userLanguage, Tutorial.IncompleteLessonPhrase());
            }
        }

        private void OnAttemptSuccessLesson()
        {
            if (m_activeLessonInteractor == null || m_activeLessonInteractor.Lesson.IsCompleted)
            {
                return;
            }
            UpdateTimeoutInteraction();
            m_gollyGoshManager.StopSpeaking();
            m_completedInteractionCount += 1;
            m_activeLessonInteractor.CompleteActivation(m_centerEyeAnchor);
            AppSessionData.AddCompletedLesson(m_activeLessonInteractor.Lesson.Classification);
            var userLanguage = AppSessionData.UserLanguageAI;
            m_gollyGoshManager.Speak(userLanguage, Tutorial.CompleteLessonPhrase());

            if (m_completedInteractionCount >= m_goalLessonsCompleteMax)
            {
                // Prevent any further lessons from being requested
                StopRequestBestLessons();
            }
        }

        private void PlayUserTutorial()
        {
            if (m_activeLessonInteractor == null)
            {
                return;
            }

            var tier = AppSessionData.Tier;
            var userLanguage = AppSessionData.UserLanguageAI;
            var targetLanguageName = AppSessionData.TargetLanguageName;
            var wordContext = m_activeLessonInteractor.PresentedWords;

            var targetNoun = wordContext.Nouns != null && wordContext.Nouns.Length > 0 ? wordContext.Nouns[0] : null;
            var targetPhrases = m_activeLessonInteractor.Lesson.Activity.ExamplePhrases;

            m_gollyGoshManager.LookAt(m_centerEyeAnchor, 0.5f);

            switch (tier)
            {
                case 1:
                    switch (m_tutorialIndex)
                    {
                        case 0:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier1TutorialA(targetLanguageName));
                            m_gollyGoshManager.TutorialPointToNearestWord(m_activeLessonInteractor);
                            break;

                        case 1:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier1TutorialB(targetLanguageName));
                            m_gollyGoshManager.TutorialPointToNearestWord(m_activeLessonInteractor);
                            break;

                        case 2:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier1TutorialC(targetLanguageName));
                            m_gollyGoshManager.TutorialTranslateNearestWord(m_activeLessonInteractor);
                            break;

                        case 3:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier1TutorialD(targetLanguageName));
                            m_gollyGoshManager.TutorialPlayAudioNearestWord(m_activeLessonInteractor);
                            break;

                        case 4:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier1TutorialE(targetPhrases[0]));
                            m_gollyGoshManager.Listen();
                            break;
                    }
                    break;
                case 2:
                    switch (m_tutorialIndex)
                    {
                        case 0:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier2TutorialA(targetLanguageName));
                            m_gollyGoshManager.TutorialPointToNearestWord(m_activeLessonInteractor);
                            break;

                        case 1:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier2TutorialB());
                            m_gollyGoshManager.TutorialPointToNearestWord(m_activeLessonInteractor);
                            break;

                        case 2:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier2TutorialC());
                            m_gollyGoshManager.TutorialTranslateNearestWord(m_activeLessonInteractor, TextCloudItem.WordType.adjective);
                            break;

                        case 3:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier2TutorialD());
                            m_gollyGoshManager.TutorialPlayAudioNearestWord(m_activeLessonInteractor, TextCloudItem.WordType.adjective);
                            break;

                        case 4:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier2TutorialE(targetPhrases[1]));
                            m_gollyGoshManager.Listen();
                            break;
                    }
                    break;
                case 3:
                    switch (m_tutorialIndex)
                    {
                        case 0:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier3TutorialA(targetLanguageName));
                            m_gollyGoshManager.TutorialPointToNearestWord(m_activeLessonInteractor);
                            break;

                        case 1:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier3TutorialB());
                            m_gollyGoshManager.TutorialPointToNearestWord(m_activeLessonInteractor);
                            break;

                        case 2:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier3TutorialC());
                            m_gollyGoshManager.TutorialTranslateNearestWord(m_activeLessonInteractor, TextCloudItem.WordType.verb);
                            break;

                        case 3:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier3TutorialD());
                            m_gollyGoshManager.TutorialPlayAudioNearestWord(m_activeLessonInteractor, TextCloudItem.WordType.verb);
                            break;

                        case 4:
                            m_gollyGoshManager.Speak(userLanguage, Tutorial.Tier3TutorialE(targetPhrases[2]));
                            m_gollyGoshManager.Listen();
                            break;
                    }
                    break;
            }

            // Continue loop at start
            m_tutorialIndex++;
            if (m_tutorialIndex >= 5)
            {
                m_tutorialIndex = 0;
            }
        }

        private void UpdateTimeoutInteraction()
        {
            m_previousInteractionTime = DateTime.UtcNow;
        }

        private void Update()
        {
            if (!m_isLessonInteractionEnabled)
            {
                return;
            }

            if (m_isSearchingForMoreLessons)
            {
                var diff = DateTime.Now - m_lastSearchTime;
                if (diff.TotalSeconds > m_timeBetweenLessonSearches)
                {
                    RequestBestLessonsIteration();
                }
            }

            // Debug Options for runtime debugging: 
            if (s_debugOptionsEnabled)
            {
                var isDownControllerButtonB = OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch);
                var isDownControllerTrigger = OVRInput.GetDown(
                    OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

                var attemptSkipLessonPass = false;
                var attemptSkipLessonFail = false;

                if (isDownControllerButtonB)
                {
                    attemptSkipLessonPass = true;
                }

                if (isDownControllerTrigger)
                {
                    attemptSkipLessonFail = true;
                }

                if (m_activeLessonInteractor != null)
                {
                    if (attemptSkipLessonPass)
                    {
                        OnAttemptSuccessLesson();
                    }
                    else if (attemptSkipLessonFail)
                    {
                        OnAttemptFailLesson();
                        UpdateTimeoutInteraction();
                    }
                }
            }

            // Timeout Checks
            var previousInteractionTimeDifference = DateTime.UtcNow - m_previousInteractionTime;
            if (m_activeLessonInteractor != null)
            {
                var timeoutLesson = m_lessonNagStarted ? TIMEOUT_DURING_LESSON_OTHER : TIMEOUT_DURING_LESSON_FIRST;
                if (previousInteractionTimeDifference.TotalSeconds > timeoutLesson)
                {
                    if (!m_isWaitingForBerrySqueeze)
                    {
                        m_lessonNagStarted = true;
                        OnTimeoutLesson();
                        UpdateTimeoutInteraction();
                    }
                }
            }
            else
            {
                var differenceCompletedExistingLessons = m_activeUntrackedLessonList.Count - m_completedInteractionCount;
                if (m_isWaitingForBerrySqueeze)
                {
                    var timeoutSqueeze = m_squeezeNagStarted ? TIMEOUT_BERRY_SQUEEZE_OTHER : TIMEOUT_BERRY_SQUEEZE_FIRST;
                    if (previousInteractionTimeDifference.TotalSeconds > timeoutSqueeze)
                    {
                        m_squeezeNagStarted = true;
                        UpdateTimeoutInteraction();
                        m_gollyGoshManager.Speak(AppSessionData.UserLanguageAI, Tutorial.WaitGoldenBerry());
                    }
                }
                else if (differenceCompletedExistingLessons > 0)
                {
                    var timeoutExploring = m_exploringNagStarted ? TIMEOUT_EXPLORING_WORLD_OTHER : TIMEOUT_EXPLORING_WORLD_FIRST;
                    if (previousInteractionTimeDifference.TotalSeconds > timeoutExploring)
                    {
                        m_exploringNagStarted = true;
                        OnTimeoutWorld();
                        UpdateTimeoutInteraction();
                    }
                }
                else
                {
                    var timeoutTracking = m_trackingNagStarted ? TIMEOUT_WAITING_TRACKING_OTHER : TIMEOUT_WAITING_TRACKING_FIRST;
                    if (previousInteractionTimeDifference.TotalSeconds > timeoutTracking)
                    {
                        m_trackingNagStarted = true;
                        OnTimeoutTrackingLessons();
                        UpdateTimeoutInteraction();
                    }
                }
            }
        }

        public void OnDestroy()
        {
            m_lessonManager.LessonActivated -= OnLessonActivated;
            m_lessonManager.LessonDeactivated -= OnLessonDeactivated;
            m_lessonManager.LessonCompletedSuccess -= OnLessonCompleted;

            m_lessonsManager.ActivityAcquisitionFailed += OnActivityAcquisitionFailed;

            m_transcriber.VoiceTranscriptionUpdateIncomplete -= OnVoiceTranscriptionUpdateIncomplete;
            m_transcriber.VoiceTranscriptionUpdateComplete -= OnVoiceTranscriptionUpdateComplete;
            m_transcriber.VoiceTranscriptionVolumeUpdate -= OnVoiceTranscriptionUpdateVolume;
        }
    }
}