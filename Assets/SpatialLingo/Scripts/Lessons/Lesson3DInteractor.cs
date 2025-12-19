// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.Audio;
using SpatialLingo.Characters;
using SpatialLingo.SceneObjects;
using SpatialLingo.SpeechAndText;
using TMPro;
using UnityEngine;

namespace SpatialLingo.Lessons
{
    /// <summary>
    /// Presents the UI for interacting with a Lesson in 3D space
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class Lesson3DInteractor : MonoBehaviour
    {
        // Interaction related distances
        private const float DISTANCE_START_INTERACTION_METERS = 0.80f;
        private const float DISTANCE_STOP_INTERACTION_METERS = 1.50f;
        private const float DISTANCE_WORDS_RADIUS_METERS = 0.9f;
        private const float DISTANCE_WORDS_SPACING_METERS = 0.2f;
        private const float SPACING_WORDS_CIRCLE_DEGREES = 25.0f;

        // Offset from the actual content: helps with: positioning error, word cloud thickness 
        private const float DISTANCE_PADDING_OFFSET = 0.2f;

        private static bool s_showDebugFeatures = false;
        public static bool ShowDebugFeatures
        {
            get => s_showDebugFeatures;
            set
            {
                if (s_showDebugFeatures != value)
                {
                    s_showDebugFeatures = value;
                    DebugStatusChanged?.Invoke(s_showDebugFeatures);
                }
            }
        }

        public static bool TranscriptionPassesLesson(string transcription, PresentedWordContext presentedContext, int tier)
        {
            return tier switch
            {
                1 => AnyWordIsPresentInPhrase(transcription, presentedContext.Nouns),
                2 => AnyWordIsPresentInPhrase(transcription, presentedContext.Nouns)
                                           && AnyWordIsPresentInPhrase(transcription, presentedContext.Adjectives),
                3 => AnyWordIsPresentInPhrase(transcription, presentedContext.Nouns)
                                           && AnyWordIsPresentInPhrase(transcription, presentedContext.Adjectives)
                                           && AnyWordIsPresentInPhrase(transcription, presentedContext.Verbs),
                _ => false,
            };
        }

        private static bool AnyWordIsPresentInPhrase(string phrase, string[] words)
        {
            foreach (var word in words)
            {
                if (phrase.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                // Try removing spaces, and try hyphens
                if (word.Contains(" "))
                {
                    var nospaces = word.Replace(" ", "");
                    if (phrase.Contains(nospaces, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    var hyphens = word.Replace(" ", "-");
                    if (phrase.Contains(hyphens, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        [MetaCodeSample("SpatialLingo")]
        public class PresentedWordContext
        {
            public string[] Nouns { get; }
            public string[] Adjectives { get; }
            public string[] Verbs { get; }

            public PresentedWordContext()
            {
                Nouns = new string[0];
                Adjectives = new string[0];
                Verbs = new string[0];
            }
            public PresentedWordContext(string[] nouns, string[] adjectives, string[] verbs)
            {
                Nouns = nouns;
                Adjectives = adjectives;
                Verbs = verbs;
            }
        }

        public delegate void DebugStatusChangedEvent(bool currentStatus);
        public static event DebugStatusChangedEvent DebugStatusChanged;

        public event WordBar3D.InteractionEvent PokeInteraction;
        public event WordBar3D.InteractionEvent SqueezeInteraction;

        [SerializeField] private TextMeshPro m_debugText;
        [SerializeField] private GameObject m_debugShadow;
        [SerializeField] private GameObject m_debugCube;
        [SerializeField] private ParticleSystem m_completeEffect;
        [SerializeField] private WordBar3D m_wordCloudBarPrefab;
        [SerializeField] private GameObject m_berryControllerPrefab;

        [SerializeField] private GameObject m_vizEffectContainer;
        [SerializeField] private Animator m_vizEffectAnimator;
        [SerializeField] private MeshRenderer m_vizEffectMeshRenderer;

        [SerializeField] private GameObject m_prefabPoints;
        private List<GameObject> m_debugPoints = new();
        private bool m_isShowingVisualization = false;
        private bool m_shouldOrientVisualizationToHeadset = true;
        private Vector3 m_visualizationUp;

        private Coroutine m_vizEffectCoroutine;

        public delegate void LessonInteractorUpdatedEvent(Lesson3DInteractor interactor);
        public event LessonInteractorUpdatedEvent UserEnteredActivationArea;
        public event LessonInteractorUpdatedEvent UserExitedActivationArea;
        public event LessonInteractorUpdatedEvent UserCompletedSuccess;
        public event LessonInteractorUpdatedEvent UserTouchedBerry;

        public TextMeshPro DebugText => m_debugText;
        public GameObject DebugShadow => m_debugShadow;
        public GameObject DebugCube => m_debugCube;

        public PresentedWordContext PresentedWords
        {
            get;
            private set;
        } = new PresentedWordContext();

        public Lesson Lesson
        {
            get;
            private set;
        }

        public bool IsActiveRunning { get; private set; } = false;

        private Transform m_userHeadsetTransform;
        private int m_tier;
        private GameObject m_wordCloud;
        private BerryController m_berry;

        private bool m_isDebug = false;
        private bool m_isReadyForActivation = true;
        private bool m_isRunningCompleteSequence = false;

        private VoiceSpeaker m_speaker;

        private Action<Lesson3DInteractor> m_completeActivationCallback;

        private List<WordBar3D> m_listAllBars = new();

        private float m_previousDistance = -1.0f;

        private void Awake()
        {
            // Stop any effects
            HideVisualizationEffect();
        }

        // Dependency Injection for start
        public void Initialize(Lesson lesson, VoiceSpeaker speaker, Transform headset)
        {
            Lesson = lesson;
            m_speaker = speaker;
            m_userHeadsetTransform = headset;
            DebugStatusChanged += OnDebugStatusChanged;
        }

        public BerryController CreateBerry()
        {
            m_isReadyForActivation = false;
            var berry = Instantiate(m_berryControllerPrefab);
            var controller = berry.GetComponent<BerryController>();
            controller.DisplayRandomBerry();
            return controller;
        }

        /// <summary>
        /// Interactor takes ownership of the berry
        /// </summary>
        /// <param name="berryController"></param>
        public void GiveBerry(BerryController berryController)
        {
            if (m_berry != null)
            {
                // Unparent existing
                m_berry.BerrySqueezeInteraction -= OnBerrySqueezeInteraction;
                m_berry.transform.parent = null;
            }

            m_berry = berryController;
            m_berry.transform.parent = transform;

            m_isReadyForActivation = true;
            ResetDistanceCheck();
            m_berry.PlayEffectOnTouch = false;
            m_berry.EnableInteraction();
            m_berry.BerrySqueezeInteraction += OnBerrySqueezeInteraction;
        }

        private void OnBerrySqueezeInteraction(BerryController controller)
        {
            UserTouchedBerry?.Invoke(this);
        }

        /// <summary>
        /// Interaction releases control ov the berry
        /// </summary>
        /// <returns>Berry if present</returns>
        public BerryController TakeBerry()
        {
            var berry = m_berry;
            m_berry = null;
            if (berry != null)
            {
                berry.BerrySqueezeInteraction -= OnBerrySqueezeInteraction;
                berry.gameObject.SetActive(true);
                berry.transform.parent = null;
            }

            m_isReadyForActivation = false;
            return berry;
        }

        public void BerryArrived(BerryController berryController)
        {
            berryController.StopBerryTrailSound();
            berryController.HidePathEffects(true);
            GiveBerry(berryController);
        }

        public void ResetDistanceCheck()
        {
            m_previousDistance = float.MaxValue;
        }

        private void OnDebugStatusChanged(bool currentStatus)
        {
            UpdateFromLesson();
        }

        public void SetDebugStatus(bool isDebugOnly)
        {
            if (isDebugOnly)
            {
                // Should not have a berry as a debug lesson
                if (m_berry != null)
                {
                    Destroy(m_berry.gameObject);
                    m_berry = null;
                }
            }

            m_isDebug = isDebugOnly;
            UpdateFromLesson();
        }

        public void ShowLesson(int tier = 1)
        {
            m_tier = tier;
            UpdateFromLesson();
        }

        public void UpdateFromLesson()
        {
            if (Lesson != null && gameObject != null)
            {
                transform.position = Lesson.Position;
            }

            // Uncomment this to enable object visualization on lesson update 
            // ShowLessonVisualization();

            // Debug:
            if (s_showDebugFeatures)
            {
                m_debugText.gameObject.SetActive(true);
                m_debugCube.SetActive(true);
                m_debugCube.transform.localScale = Lesson.Extent;
                m_debugText.text = Lesson.Classification;
            }
            else
            {
                m_debugText.gameObject.SetActive(false);
                m_debugCube.SetActive(false);
            }
        }

        public void ShowLessonVisualization()
        {
            // Find the image most facing user:
            var bestImageContext = Lesson.BestImageForView(m_userHeadsetTransform);
            if (bestImageContext == null)
            {
                m_vizEffectContainer.SetActive(false);
                m_isShowingVisualization = false;
                return;
            }

            m_isShowingVisualization = true;
            m_visualizationUp = bestImageContext.Up;
            m_vizEffectContainer.SetActive(true);

            var scale2D = bestImageContext.Size;
            var cartoonScale = 1.5f; // 1.5 - 2.0
            var maxSizeAnyDimension = 2.0f; // Nothing larger than this ~ 6 feet
            var maxSizeAnyDimensionHalf = maxSizeAnyDimension * 0.5f;
            var scale3D = new Vector3(scale2D.x * cartoonScale, scale2D.y * cartoonScale, 1.0f);
            // Check limit x
            if (scale3D.x > maxSizeAnyDimensionHalf)
            {
                var reduction = maxSizeAnyDimensionHalf / scale3D.x;
                scale3D.x *= reduction;
                scale3D.y *= reduction;
            }
            // Check limit y
            if (scale3D.y > maxSizeAnyDimensionHalf)
            {
                var reduction = maxSizeAnyDimensionHalf / scale3D.y;
                scale3D.x *= reduction;
                scale3D.y *= reduction;
            }

            // Set size & position & orient in world
            m_vizEffectContainer.transform.localScale = scale3D;
            m_vizEffectContainer.transform.rotation = Quaternion.LookRotation(bestImageContext.Normal, bestImageContext.Up) * Quaternion.Euler(0.0f, 180.0f, 0.0f);

            // Show temporary effect
            ShowVisualizationEffect(bestImageContext.Image);
        }

        private void HideLessonVisualization()
        {
            m_vizEffectContainer.SetActive(false);
            m_isShowingVisualization = false;

        }

        private void ShowVisualizationEffect(Texture2D texture)
        {
            StopEffectWaitCoroutine();

            m_vizEffectContainer.gameObject.SetActive(true);

            m_vizEffectMeshRenderer.gameObject.SetActive(true);
            var block = new MaterialPropertyBlock();
            m_vizEffectMeshRenderer.GetPropertyBlock(block);
            block.SetTexture("_ColorTexture", texture);
            m_vizEffectMeshRenderer.SetPropertyBlock(block);

            // Play animator effect
            m_vizEffectAnimator.enabled = true;
            m_vizEffectAnimator.SetTrigger("RunTrigger");
            m_vizEffectCoroutine = StartCoroutine(WaitForAnimationEnd());
        }

        private void HideVisualizationEffect()
        {
            m_vizEffectAnimator.enabled = false;
            m_vizEffectMeshRenderer.gameObject.SetActive(false);
        }

        private IEnumerator WaitForAnimationEnd()
        {
            yield return null; // Wait for one frame for the state to update

            // Rough animation effect duration
            yield return new WaitForSeconds(3.0f);

            HideVisualizationEffect();
            HideLessonVisualization();
        }

        private void StopEffectWaitCoroutine()
        {
            if (m_vizEffectCoroutine != null)
            {
                StopCoroutine(m_vizEffectCoroutine);
                m_vizEffectCoroutine = null;
            }
        }

        private void ShowDebugPoints()
        {
            foreach (var point in m_debugPoints)
            {
                Destroy(point);
            }
            m_debugPoints.Clear();

            var samples = Lesson.SamplePoints;
            if (samples != null)
            {
                // SamplePoints
                foreach (var source in Lesson.SamplePoints)
                {
                    var go = Instantiate(m_prefabPoints);
                    go.transform.parent = transform;
                    go.transform.position = source.Item1;
                    go.SetActive(true);
                    m_debugPoints.Add(go);
                }
            }
        }

        // Start
        public void ActivateLesson()
        {
            if (IsActiveRunning)
            {
                return;
            }
            IsActiveRunning = true;

            ShowBars();
            ShowLessonVisualization();

            AppAudioController.Instance.PlaySound(SoundEffect.WordCloudAppear, Lesson.Position);
        }

        // Stop
        public void DeactivateLesson()
        {
            if (!IsActiveRunning)
            {
                return;
            }
            IsActiveRunning = false;
            if (m_berry != null)
            {
                m_berry.ResumeFromInteraction();
            }
            HideBars();
            HideLessonVisualization();

            AppAudioController.Instance.PlaySound(SoundEffect.WordCloudDisappear, Lesson.Position);
        }

        public void CompleteActivation(Transform lookAtTransform, Action<Lesson3DInteractor> callback = null)
        {
            if (m_isRunningCompleteSequence)
            {
                return;
            }
            m_isRunningCompleteSequence = true;
            m_completeActivationCallback = callback;
            m_berry.DisableInteraction();
            m_berry.PlayEffectOnTouch = true;
            CompleteLesson();
            var finalPosition = Lesson.Position;
            _ = StartCoroutine(CompleteBars(finalPosition, lookAtTransform));
        }

        private IEnumerator CompleteBars(Vector3 finalPosition, Transform lookAtTransform = null)
        {
            // Play sound for coalesce
            AppAudioController.Instance.PlaySound(SoundEffect.WordCloudCoalesce, finalPosition);

            foreach (var bar in m_listAllBars)
            {
                bar.RequestSpeak -= OnRequestSpeak;
                bar.AnimateCompleteDestroy(finalPosition);
            }
            m_listAllBars.Clear();

            yield return new WaitForSeconds(0.50f);

            // Play sparkle effect
            m_completeEffect.transform.position = finalPosition;
            yield return new WaitForEndOfFrame();
            m_completeEffect.gameObject.SetActive(true);
            m_completeEffect.time = 0.0f;
            m_completeEffect.Play();
            yield return new WaitForSeconds(0.10f);

            // Show berry
            if (m_berry != null)
            {
                m_berry.gameObject.SetActive(true);
                m_berry.ShowFace(lookAtTransform);
            }
            yield return new WaitForSeconds(0.20f);

            // Wiggle berry
            if (m_berry != null)
            {
                m_berry.PlayWiggleEffect();
            }
            yield return new WaitForSeconds(0.70f);

            // Done complete sequence
            m_isRunningCompleteSequence = false;
            m_completeEffect.gameObject.SetActive(false);
            UserCompletedSuccess?.Invoke(this);
            if (m_completeActivationCallback != null)
            {
                m_completeActivationCallback?.Invoke(this);
            }
        }

        public void FadeDestroy()
        {
            _ = StartCoroutine(FadeDestroyCoroutine());
        }

        private IEnumerator FadeDestroyCoroutine()
        {
            // Hide bars if present
            if (m_listAllBars.Count > 0)
            {
                HideBars();
            }
            // Fade out berry
            if (m_berry != null)
            {
                // Play exit effect
                var berry = TakeBerry();
                berry.FadeOutDestroy();
            }
            // Delay for bars if present 
            yield return new WaitForSeconds(2.0f);
            Destroy(gameObject);
        }

        public void ResetLesson()
        {
            Lesson.MarkIncomplete();
        }

        private void CompleteLesson()
        {
            Lesson.MarkComplete();
        }

        private void Update()
        {
            if (!m_userHeadsetTransform || m_isDebug || Lesson.IsCompleted)
            {
                return;
            }

            var maxAngleDegrees = 90.0f; // pointing roughtly towards the activation in the X-Z plane [60-90]

            // User also needs to be pointing in roughly the direction of the lesson
            var userForward = m_userHeadsetTransform.forward;
            userForward.y = 0.0f;
            userForward.Normalize();
            var userToCenter = Lesson.Position - m_userHeadsetTransform.position;
            userToCenter.y = 0.0f; // project onto xz plane
            var distance = userToCenter.magnitude;
            var angleDegrees = Vector3.Angle(userForward, userToCenter);
            if (!m_isRunningCompleteSequence)
            {
                if (IsActiveRunning)
                {
                    if (distance > DISTANCE_STOP_INTERACTION_METERS && m_previousDistance <= DISTANCE_STOP_INTERACTION_METERS)
                    {
                        UserExitedActivationArea?.Invoke(this);
                    }
                }
                else
                {
                    if (m_isReadyForActivation && angleDegrees < maxAngleDegrees)
                    {
                        if (distance < DISTANCE_START_INTERACTION_METERS && m_previousDistance >= DISTANCE_START_INTERACTION_METERS)
                        {
                            UserEnteredActivationArea?.Invoke(this);
                        }
                    }
                }
            }
            m_previousDistance = distance;
            if (m_isShowingVisualization && m_shouldOrientVisualizationToHeadset)
            {
                var reverse = m_userHeadsetTransform.forward;
                reverse.Scale(new Vector3(-1.0f, -1.0f, -1.0f));
                m_vizEffectContainer.transform.rotation = Quaternion.LookRotation(reverse, m_visualizationUp) * Quaternion.Euler(0.0f, 180.0f, 0.0f);
            }
        }

        private void ShowBars()
        {
            // Offset center a little more toward user
            var listRowTop = new List<WordBar3D>();
            var listRowMid = new List<WordBar3D>();
            var listRowBot = new List<WordBar3D>();
            var headsetCenter = m_userHeadsetTransform.position;
            var lessonCenter = Lesson.Position;
            var lessonCenterAtUserHeight = new Vector3(lessonCenter.x, headsetCenter.y, lessonCenter.z);
            var wordToHeadset = headsetCenter - lessonCenter;
            // Project to y at user's visor height
            var wordToHeadsetProjected = new Vector3(wordToHeadset.x, 0, wordToHeadset.z);
            wordToHeadsetProjected.Normalize();
            var paddingOffset = wordToHeadsetProjected;
            wordToHeadsetProjected.Scale(new Vector3(DISTANCE_WORDS_RADIUS_METERS, DISTANCE_WORDS_RADIUS_METERS, DISTANCE_WORDS_RADIUS_METERS));
            paddingOffset.Scale(new Vector3(DISTANCE_PADDING_OFFSET, DISTANCE_PADDING_OFFSET, DISTANCE_PADDING_OFFSET));
            var centerInteraction = lessonCenterAtUserHeight + wordToHeadsetProjected + paddingOffset;

            var centerStartDirection = -wordToHeadsetProjected;
            centerStartDirection.Normalize();

            // 3 vertical locations for rings:
            Vector3 centerInteractionBot;
            Vector3 centerInteractionMid;
            Vector3 centerInteractionTop;
            string[] adjs = { };
            string[] verbs = { };

            var activity = Lesson.Activity;
            string[] nouns;
            if (m_tier == 1)
            {
                // Single word
                listRowMid.Add(BarFromSettings(activity.TargetLanguageWord, activity.UserLanguageWord, TextCloudItem.WordType.noun, activity.TargetLanguageWord));
                nouns = new string[] { activity.TargetLanguageWord };
            }
            else if (m_tier == 2)
            {
                var adjA = activity.AdjectivesTargetLanguage[0];
                var adjB = activity.AdjectivesTargetLanguage[1];
                listRowMid.Add(BarFromSettings(activity.TargetLanguageWord, activity.UserLanguageWord, TextCloudItem.WordType.noun, activity.TargetLanguageWord));
                listRowBot.Add(BarFromSettings(adjA, activity.AdjectivesUserLanguage[0], TextCloudItem.WordType.adjective, adjA));
                listRowBot.Add(BarFromSettings(adjB, activity.AdjectivesUserLanguage[1], TextCloudItem.WordType.adjective, adjB));
                nouns = new string[] { activity.TargetLanguageWord };
                adjs = new string[] { adjA, adjB };
            }
            else // if (tier == 3)
            {
                var adjA = activity.AdjectivesTargetLanguage[0];
                var adjB = activity.AdjectivesTargetLanguage[1];
                var adjC = activity.AdjectivesTargetLanguage[2];
                var verbA = activity.VerbsTargetLanguage[0];
                var verbB = activity.VerbsTargetLanguage[1];
                // Word + 2 verbs + 3 adjs
                listRowTop.Add(BarFromSettings(activity.TargetLanguageWord, activity.UserLanguageWord, TextCloudItem.WordType.noun, activity.TargetLanguageWord));
                listRowMid.Add(BarFromSettings(verbA, activity.VerbsUserLanguage[0], TextCloudItem.WordType.verb, verbA));
                listRowMid.Add(BarFromSettings(verbB, activity.VerbsUserLanguage[1], TextCloudItem.WordType.verb, verbB));
                listRowBot.Add(BarFromSettings(adjA, activity.AdjectivesUserLanguage[0], TextCloudItem.WordType.adjective, adjA));
                listRowBot.Add(BarFromSettings(adjB, activity.AdjectivesUserLanguage[1], TextCloudItem.WordType.adjective, adjB));
                listRowBot.Add(BarFromSettings(adjC, activity.AdjectivesUserLanguage[2], TextCloudItem.WordType.adjective, adjC));
                nouns = new string[] { activity.TargetLanguageWord };
                verbs = new string[] { verbA, verbB };
                adjs = new string[] { adjA, adjB, adjC };
            }

            PresentedWords = new PresentedWordContext(nouns, adjs, verbs);

            centerInteractionMid = centerInteraction;
            centerInteractionTop = centerInteractionMid + new Vector3(0.0f, DISTANCE_WORDS_SPACING_METERS, 0.0f);
            centerInteractionBot = centerInteractionMid - new Vector3(0.0f, DISTANCE_WORDS_SPACING_METERS, 0.0f);

            PopulateCircleFromSettings(
                listRowTop, centerInteraction, centerInteractionTop, centerStartDirection, DISTANCE_WORDS_RADIUS_METERS,
                SPACING_WORDS_CIRCLE_DEGREES);
            PopulateCircleFromSettings(
                listRowMid, centerInteraction, centerInteractionMid, centerStartDirection, DISTANCE_WORDS_RADIUS_METERS,
                SPACING_WORDS_CIRCLE_DEGREES);
            PopulateCircleFromSettings(
                listRowBot, centerInteraction, centerInteractionBot, centerStartDirection, DISTANCE_WORDS_RADIUS_METERS,
                SPACING_WORDS_CIRCLE_DEGREES);

            foreach (var bar in m_listAllBars)
            {
                bar.AnimateInDelayed();
            }
        }

        private void HideBars()
        {
            foreach (var bar in m_listAllBars)
            {
                bar.RequestSpeak -= OnRequestSpeak;
                bar.AnimateOutDestroy();
            }
            m_listAllBars.Clear();
        }

        public WordBar3D[] Words()
        {
            return m_listAllBars.ToArray();
        }

        private void PopulateCircleFromSettings(List<WordBar3D> list, Vector3 focus, Vector3 center, Vector3 startDirection, float radius, float spacingDegrees)
        {
            var spacingRadians = spacingDegrees * Mathf.PI / 180.0f;

            var normalCircle = Vector3.up;
            var count = list.Count;
            var countMinusOne = count - 1;
            var index = 0;
            var offsetAngleRadians = spacingRadians * countMinusOne * 0.5f;
            foreach (var wordBar in list)
            {
                var location = new Vector3(startDirection.x, startDirection.y, startDirection.z);
                location.Scale(new Vector3(radius, radius, radius));
                var angle = spacingRadians * index - offsetAngleRadians;

                var rotate = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, normalCircle);
                location = rotate * location;
                location += center;

                var locationToFocus = focus - location;
                locationToFocus.Normalize();
                var wordRotation = Quaternion.LookRotation(locationToFocus, normalCircle);

                wordBar.transform.position = location;
                wordBar.transform.rotation = wordRotation;

                m_listAllBars.Add(wordBar);
                index += 1;
            }
        }

        private WordBar3D BarFromSettings(string primary, string secondary, TextCloudItem.WordType type, string phrase)
        {
            WordBar3D wordGO;
            wordGO = Instantiate(m_wordCloudBarPrefab);
            wordGO.Initialize(primary, secondary, type, m_userHeadsetTransform, phrase, true);
            wordGO.DisableSqueezeInteraction();
            wordGO.RequestSpeak += OnRequestSpeak;
            wordGO.SqueezeInteraction += OnSqueezeInteraction;
            wordGO.PokeInteraction += OnPokeInteraction;

            return wordGO;
        }

        private void OnSqueezeInteraction(WordBar3D wordGO)
        {
            SqueezeInteraction?.Invoke(wordGO);
        }

        private void OnPokeInteraction(WordBar3D wordGO)
        {
            PokeInteraction?.Invoke(wordGO);
        }

        private void OnRequestSpeak(string text)
        {
            m_speaker.SpeakAudioForText(Language.Language.AssistantAIToWitaiLanguage(AppSessionData.TargetLanguageAI), text, true);
        }

        private void OnDestroy()
        {
            DebugStatusChanged -= OnDebugStatusChanged;
        }
    }
}