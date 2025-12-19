// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections;
using System.Collections.Generic;
using Meta.Utilities.CameraTaxonTracking;
using Meta.Utilities.CameraTracking;
using Meta.Utilities.LlamaAPI;
using Meta.Utilities.ObjectClassifier;
using Meta.XR;
using Meta.XR.EnvironmentDepth;
using Meta.XR.Samples;
using SpatialLingo.AI;
using SpatialLingo.Audio;
using SpatialLingo.HeadsetTracking;
using SpatialLingo.Lessons;
using SpatialLingo.SpeechAndText;
using SpatialLingo.Utilities;
using TMPro;
using Unity.InferenceEngine;
using Unity.VisualScripting;
using UnityEngine;

namespace SpatialLingo.AppSystems
{
    /// <summary>
    /// The main application controller for the SpatialLingo app.
    /// Initializes and manages all core systems including tracking, voice, lessons, and UI flow.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class SpatialLingoApp : MonoBehaviour
    {
        [Header("Shaders")]
        [SerializeField] private ShaderVariantCollection[] m_prewarmShaderCollections;

        [Header("Headset Tracking Pieces")]
        [SerializeField] private Transform m_headsetLeftCameraTransform;
        [SerializeField] public Transform HeadsetEyeCenterTransform;

        [Header("Visual Systems")]
        [SerializeField] private WebCamTextureManager m_cameraTextureManager;
        [SerializeField] private EnvironmentRaycastManager m_environmentRaycastManager;
        [SerializeField] private EnvironmentDepthManager m_environmentDepthManager;
        [SerializeField] private FaceDetection m_faceBlur;

        [Header("Tracking Systems")]
        [SerializeField] private ImageObjectClassifier m_imageObjectClassifier;
        [SerializeField] private ModelAsset m_objectClassifierModel;
        [SerializeField] private TextAsset m_objectClassifierClasses;
        [SerializeField] private Lesson3DInteractor m_lessonInteractorPrefab;
        [SerializeField] public ExerciseManager ExerciseManager;

        [Header("Voice Systems")]
        [SerializeField] public VoiceSpeaker Speaker;
        [SerializeField] private VoiceTranscriber m_transcriber;

        [Header("Audio Controller System")]
        [SerializeField] public AppAudioController AudioController;

        [Header("App Flow")]
        [SerializeField] private GameObject m_appFlow;

        [Header("World Anchor Manager")]
        [SerializeField] private WorldAnchorManager m_worldAnchorManager;

        [Header("System UI")]
        [SerializeField] private TextMeshPro m_alertMessage;

        [Header("MRUK Wrapper")]
        [SerializeField] public RoomSense RoomSense;

        [Header("Debugging Items")]
        [SerializeField] private MeshRenderer m_debugRenderer;
        [SerializeField] private GameObject m_debugRay;

        private CameraTaxonTracker m_taxonTracker;
        private LessonsManager m_lessonManager;
        public LessonInteractionManager LessonInteractionManager;

        private bool m_isRetryingLessons = false;


        private void Start() => Init();

        public void Init()
        {
            // Tracing - Uncomment to run Graphics Collection Trace
            // GraphicsStateCollectionTracing.Instance.StartTracing(30.0f);

            // Warmup shaders
            WarmupShaderCollections();

            // Camera related permissions
            ContinueSystemsInit();
        }

        private async void WarmupShaderCollections()
        {
            await Awaitable.MainThreadAsync();

            foreach (var collection in m_prewarmShaderCollections)
            {
                while (!collection.WarmUpProgressively(10))
                {
                    await Awaitable.NextFrameAsync();
                }
            }
        }

        public void ContinueSystemsInit()
        {
            if (EnvironmentDepthManager.IsSupported)
            {
                m_environmentDepthManager.gameObject.SetActive(true);
                // Enable Depth API
                m_environmentDepthManager.enabled = true;
                // Enable ray casting using Depth API
                m_environmentRaycastManager.enabled = true;
            }
            else
            {
                Debug.LogError("Depth is not supported");
            }

            // Passthrough Camera settings
            var resolution = new Vector2Int(1280, 960); // 640x640 is res used for YOLO, highest res for cropped images
            m_cameraTextureManager.RequestedResolution = resolution;
            m_cameraTextureManager.Eye = PassthroughCameraEye.Left;

            // 3D Object Tracking
            var classList = new[]
            {
                "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light",
                "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow",
                "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee",
                "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard",
                "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple",
                "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa",
                "potted plant", "bed", "dining table", "toilet", "tv monitor", "laptop", "mouse", "remote", "keyboard",
                "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase",
                "scissors", "teddy bear", "hair drier", "toothbrush"
            };
            // Ignore list
            var options = new List<ImageObjectClassifier.ClassificationOption>
            {
                new("person", null, true), // hands or body part of user
                new("remote", null, true), // hand-held controllers
            };
            m_imageObjectClassifier.Initialize(m_objectClassifierModel, classList, options.ToArray());

            // Tracking
            m_taxonTracker = new CameraTaxonTracker(m_environmentRaycastManager, m_cameraTextureManager, m_imageObjectClassifier);

            // Llama & AI
            var llama = new LlamaRestApi();
            var assistant = new AssistantAI(llama);
            // LessonsManager
            m_lessonManager = new LessonsManager(m_taxonTracker, assistant, AssistantAI.SupportedLanguage.English, AssistantAI.SupportedLanguage.Spanish, m_faceBlur);
            // Lesson Interaction
            LessonInteractionManager = new LessonInteractionManager(m_lessonManager, m_lessonInteractorPrefab, HeadsetEyeCenterTransform, Speaker);
            // Exercise Manager
            ExerciseManager.Initialize(m_lessonManager, LessonInteractionManager, m_transcriber, HeadsetEyeCenterTransform, assistant);

            Variables.Application.Set(nameof(SpatialLingoApp), this);
            m_appFlow.SetActive(true);
            m_worldAnchorManager.Initialize();
            AudioController.Initialize();
        }

        private IEnumerator ShowSystemConnectionMessage(string message)
        {
            Debug.LogError($"SpatialLingoApp - Lesson Manager - ShowSystemMessage: {message}");
            if (m_isRetryingLessons)
            {
                yield break;
            }
            m_isRetryingLessons = true;
            m_alertMessage.gameObject.SetActive(true);
            m_alertMessage.text = message;
            yield return new WaitForSeconds(5.0f);
            m_alertMessage.gameObject.SetActive(false);
            m_lessonManager.AcquireActivityComponents(); // retry ad-infinitum
            m_isRetryingLessons = false;
        }
    }
}
