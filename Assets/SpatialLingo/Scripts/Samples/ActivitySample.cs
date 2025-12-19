// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.Utilities.CameraTaxonTracking;
using Meta.Utilities.CameraTracking;
using Meta.Utilities.LlamaAPI;
using Meta.Utilities.ObjectClassifier;
using Meta.XR;
using Meta.XR.EnvironmentDepth;
using Meta.XR.Samples;
using SpatialLingo.AI;
using SpatialLingo.Lessons;
using SpatialLingo.SpeechAndText;
using TMPro;
using Unity.InferenceEngine;
using UnityEngine;

namespace SpatialLingo.Samples
{
    /// <summary>
    /// Sample script to demonstrate activity learning and interaction.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class ActivitySample : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private WebCamTextureManager m_cameraTextureManager;
        [SerializeField] private EnvironmentRaycastManager m_environmentRaycastManager;
        [SerializeField] private EnvironmentDepthManager m_environmentDepthManager;

        [Header("Dependencies")]
        [SerializeField] private ImageObjectClassifier m_classifier;
        [SerializeField] private Transform m_headsetLeftEyeTransform;
        [SerializeField] private VoiceSynthesizer m_synthesizer;
        [SerializeField] private VoiceSpeaker m_speaker;

        [Header("Lessons")]
        [SerializeField] private ModelAsset m_objectClassifierModel;
        [SerializeField] private TextAsset m_objectClassifierClasses;
        [SerializeField] private Lesson3DInteractor m_lessonPrefab;

        [Header("Debug")]
        [SerializeField] private TextMeshPro m_debugDisplayText;

        public static bool SystemReady
        {
            get;
            private set;
        }

        private LessonsManager m_lessonManager;
        private Dictionary<Lesson, Lesson3DInteractor> m_lessonLookup = new();

        private void Start()
        {
            // Set debug mode for lessons
            Lesson3DInteractor.ShowDebugFeatures = true;
            // IE preload
            _ = InferenceEngineUtilities.LoadAll();

            GetAllCameraPermissions();
        }

        // Start
        private void StartSystemInit()
        {
            if (EnvironmentDepthManager.IsSupported)
            {
                m_environmentDepthManager.gameObject.SetActive(true);

                // Enable depth
                m_environmentDepthManager.enabled = true;
                // Enable ray casting
                m_environmentRaycastManager.enabled = true;
            }
            else
            {
                Debug.LogWarning("Depth is not supported");
            }

            var resolution = new Vector2Int(800, 600); // 640x640 is res used for YOLO
            m_cameraTextureManager.RequestedResolution = resolution;
            m_cameraTextureManager.Eye = PassthroughCameraEye.Left;

            var classList = m_objectClassifierClasses.text.Split('\n');

            var options = new List<ImageObjectClassifier.ClassificationOption>
            {
                new("person", null, true)
            };

            m_classifier.Initialize(m_objectClassifierModel, classList, options.ToArray());
            m_classifier.SetLayersPerFrame(10);

            // Complete system loading
            SystemReady = true;

            var llama = new LlamaRestApi();
            var assistant = new AssistantAI(llama);

            var tracker = new CameraTaxonTracker(m_environmentRaycastManager, m_cameraTextureManager, m_classifier);

            m_lessonManager = new LessonsManager(tracker, assistant, AssistantAI.SupportedLanguage.English, AssistantAI.SupportedLanguage.Spanish, null);
            m_lessonManager.LessonAdded += OnLessonAdded;
            m_lessonManager.LessonUpdated += OnLessonUpdated;
            m_lessonManager.LessonRemoved += OnLessonRemoved;
            m_lessonManager.LessonTrackingChanged += OnLessonTrackingChanged;
        }

        private void OnLessonAdded(LessonUpdateResult result)
        {
            var lesson = result.Lesson;
            var interactor = Instantiate(m_lessonPrefab);
            interactor.Initialize(lesson, m_speaker, Camera.main.transform);
            interactor.SetDebugStatus(true);
            m_lessonLookup[lesson] = interactor;
            UpdateInteractorFromLookup(lesson, true);
        }

        private void OnLessonUpdated(LessonUpdateResult result)
        {
            UpdateInteractorFromLookup(result.Lesson);
        }

        private void OnLessonRemoved(LessonUpdateResult result)
        {
            var lesson = result.Lesson;
            if (m_lessonLookup.TryGetValue(lesson, out var interactor))
            {
                Destroy(interactor);
            }
            _ = m_lessonLookup.Remove(lesson);
        }

        private void OnLessonTrackingChanged()
        {
            UpdateDisplayText();
        }

        private void UpdateInteractorFromLookup(Lesson lesson, bool isStart = false)
        {
            if (m_lessonLookup.TryGetValue(lesson, out var interactor))
            {
                var activity = lesson.Activity;
                var size = lesson.Extent;
                var center = lesson.Position;
                interactor.transform.position = center;

                var adjs = string.Join(", ", activity.AdjectivesTargetLanguage);
                var verbs = string.Join(", ", activity.VerbsTargetLanguage);
                interactor.DebugText.text = $"{lesson.Activity.UserLanguageWord} - {activity.TargetLanguageWord}\n{adjs}\n{verbs}";
                interactor.DebugText.transform.localPosition = new Vector3(0.0f, size.y * 0.5f + 0.1f, 0.0f);

                interactor.DebugCube.transform.localScale = new Vector3(size.x, size.y, size.z);

                interactor.DebugShadow.transform.localScale = new Vector3(size.x, 1.0f, size.z);
                interactor.DebugShadow.transform.localPosition = new Vector3(0.0f, -size.y * 0.5f, 0.0f);

                // If isStart == false, linearly interpolate position and scale
            }
        }

        private void UpdateDisplayText()
        {
            var text = "";
            var ready = m_lessonManager.ReadyLessons();
            var waiting = m_lessonManager.WaitingLessons();
            var activities = m_lessonManager.Activities();
            var readyCount = 0;
            foreach (var activity in activities)
            {
                if (activity.IsReady)
                {
                    readyCount += 1;
                }
            }

            text += $"Lessons ({ready.Length + waiting.Length}) - Ready: {ready.Length} / Waiting: {waiting.Length}\n";
            text += $"Activities ({activities.Length}) - Ready: {readyCount} / Waiting: {activities.Length - readyCount}\n";

            foreach (var activity in activities)
            {
                if (activity.IsReady)
                {
                    text += $" {activity.Classification} - {activity.EnglishWord} - {activity.UserLanguageWord} <-> {activity.TargetLanguageWord} - - - - \n";
                    var adjs = string.Join(", ", activity.AdjectivesTargetLanguage);
                    var verbs = string.Join(", ", activity.VerbsTargetLanguage);
                    text += $"    {adjs}\n   {verbs}\n   {activity.ExamplePhrases[0]}\n";
                }
            }

            m_debugDisplayText.text = text;
        }

        // Permissions
        private void GetAllCameraPermissions()
        {
            if (PassthroughCameraPermissions.IsAllCameraPermissionsGranted())
            {
                StartSystemInit();
                return;
            }
            PassthroughCameraPermissions.AllCameraPermissionGranted += OnAllCameraPermissionGranted;
            PassthroughCameraPermissions.AskCameraPermissions();
        }

        private void OnAllCameraPermissionGranted()
        {
            PassthroughCameraPermissions.AllCameraPermissionGranted -= OnAllCameraPermissionGranted;
            StartSystemInit();
        }

        public void OnDestroy()
        {
            PassthroughCameraPermissions.AllCameraPermissionGranted -= OnAllCameraPermissionGranted;
        }
    }
}
