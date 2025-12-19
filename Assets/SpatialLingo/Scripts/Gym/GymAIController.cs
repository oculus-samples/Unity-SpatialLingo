// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using Meta.Utilities;
using Meta.Utilities.CameraTracking;
using Meta.Utilities.ImageUtilities;
using Meta.Utilities.LlamaAPI;
using Meta.Utilities.ObjectClassifier;
using Meta.XR.Samples;
using SpatialLingo.AI;
using SpatialLingo.UI;
using TMPro;
using UnityEngine;

namespace SpatialLingo.Gym
{
    [MetaCodeSample("SpatialLingo")]
    public class GymAIController : MonoBehaviour
    {
        [Header("AI Language Debug Options")]
        [SerializeField] private TextMeshPro m_debugDisplayLanguageA;
        [SerializeField] private TextMeshPro m_debugDisplayLanguageB;
        [SerializeField] private CanvasXRButton m_debugToggleLanguageButtonA;
        [SerializeField] private CanvasXRButton m_debugToggleLanguageButtonB;

        [Header("AI Translate Debug Options")]
        [SerializeField] private TMP_InputField m_debugInputLanguageA;
        [SerializeField] private TMP_InputField m_debugInputLanguageB;
        [SerializeField] private CanvasXRButton m_debugTranslateButton;

        [Header("AI Word Clouds Debug Options")]
        [SerializeField] private TMP_InputField m_debugInputSourceWord;
        [SerializeField] private TMP_InputField m_debugInputRelatedWords;
        [SerializeField] private CanvasXRButton m_debugWordCloudsButton;

        [Header("AI Image Analysis Debug Options")]
        [SerializeField] private WebCamTextureManager m_cameraTextureManager;
        [SerializeField] private CanvasXRButton m_debugImageAnalysisSourceButton;
        [SerializeField] private TextMeshPro m_debugImageAnalysisDuration;
        [SerializeField] private CanvasXRButton m_debugImageAnalysisDurationButton;
        [SerializeField] private CanvasXRButton m_debugImageAnalysisButtonGenerate;
        [SerializeField] private MeshRenderer m_debugImageAnalysisRenderer;
        [SerializeField] private MeshRenderer m_debugImageSendAnalysisRenderer;
        [SerializeField] private Texture2D m_debugImageAnalysisDefaultTexture;
        [SerializeField] private TMP_InputField m_debugInputImageAnalysis;
        [SerializeField] private TextMeshPro m_debugInputImageSubtext;
        [SerializeField] private CanvasXRButton m_debugImageAnalysisBlurFaceButton;

        [Header("AI Dialogue Debug Options")]
        [SerializeField] private CanvasXRButton m_debugButtonDialogueGenerate;
        [SerializeField] private TMP_InputField m_debugInputDialogue;

        [Header("AI Image Classification Debug Options")]
        [SerializeField] private ImageObjectClassifier m_imageClassifier;
        [AutoSet, SerializeField] private FaceDetection m_faceBlur;

        private AssistantAI m_assistant;

        private int m_selectedLanguageAIndex = 0;
        private int m_selectedLanguageBIndex = 1;
        private bool m_isTranslating = false;

        private bool m_isGettingRelated = false;

        private int m_selectedImageSummaryIndex = 0;
        private bool m_isImageAnalyzing = false;

        private bool m_isGeneratingDialogue = false;

        private Color32[] m_cameraImageBuffer = null;
        private Texture2D m_cameraImage;

        private void OnSystemsBecameReadyEvent(GymScene gymScene)
        {
            StartInternals();
        }

        private void Start()
        {
            if (GymScene.SystemReady)
            {
                StartInternals();
            }
            else
            {
                GymScene.SystemsBecameReady += OnSystemsBecameReadyEvent;
            }
        }

        private void StartInternals()
        {
            var llama = new LlamaRestApi();
            m_assistant = new AssistantAI(llama);

            m_assistant.TranslationComplete += OnTranslationComplete;
            m_assistant.FindRelatedWordsComplete += OnFindRelatedWordsComplete;
            m_assistant.CollectImageSummaryComplete += OnImageSummaryComplete;
            m_assistant.GenerateDialogueComplete += OnGenerateDialogueComplete;

            // Language
            m_debugToggleLanguageButtonA.ButtonWasSelected += OnDebugToggleLanguageButtonA;
            m_debugToggleLanguageButtonB.ButtonWasSelected += OnDebugToggleLanguageButtonB;
            // Translate
            m_debugTranslateButton.ButtonWasSelected += OnDebugTranslateButton;
            // Related
            m_debugWordCloudsButton.ButtonWasSelected += OnDebugRelatedWordsButton;
            // Image
            m_debugImageAnalysisSourceButton.ButtonWasSelected += OnDebugImageAnalyzeSourceButton;
            m_debugImageAnalysisDurationButton.ButtonWasSelected += OnDebugImageAnalyzeToggleDurationButton;
            m_debugImageAnalysisButtonGenerate.ButtonWasSelected += OnDebugImageAnalyzeButton;
            m_debugImageAnalysisBlurFaceButton.ButtonWasSelected += OnDebugImageBlurFaceButton;
            m_debugImageAnalysisRenderer.material = Instantiate(m_debugImageAnalysisRenderer.material);
            m_debugImageSendAnalysisRenderer.material = Instantiate(m_debugImageSendAnalysisRenderer.material);
            m_debugImageAnalysisRenderer.material.mainTexture = m_debugImageAnalysisDefaultTexture;
            // Dialogue
            m_debugButtonDialogueGenerate.ButtonWasSelected += OnDebugDialogueButton;

            // Default values:
            m_debugInputLanguageA.text = "Hello, how are you doing?";
            m_debugInputLanguageB.text = "";

            m_debugInputSourceWord.text = "Mouse";
            m_debugInputRelatedWords.text = "";

            m_debugInputImageAnalysis.text = "";

            m_debugInputImageSubtext.text = "";

            m_debugInputDialogue.text = "Generate an image description first, for dialogue context.";

            m_imageClassifier.ImageProcessedComplete += OnImageProcessedCompleteEvent;
            UpdateConstantDisplays();
        }

        private async void OnDebugImageBlurFaceButton(CanvasXRButton button)
        {
            var texture = (Texture2D)m_debugImageAnalysisRenderer.sharedMaterial.mainTexture;
            m_faceBlur.InputTexture = texture;
            var renderTexture = await m_faceBlur.RunBlurring();
            m_debugImageSendAnalysisRenderer.material.mainTexture = renderTexture;
        }

        private void OnImageProcessedCompleteEvent(ImageObjectClassifier.ClassifiedImageObject.ClassifiedImageResult result)
        {
            if (result.ClassifiedObjects.Length > 0)
            {
                // Pick a random single object:
                var item = result.ClassifiedObjects[0];

                var width = result.Source.width;
                var height = result.Source.height;
                // Convert from normalized center coordinates to image absolute scale coordinates
                var locX = Mathf.RoundToInt(width * 0.5f + width * item.CenterX - item.Width * width * 0.5f);
                var locY = Mathf.RoundToInt(height * 0.5f - height * item.CenterY - item.Height * height * 0.5f);
                var wid = Mathf.RoundToInt(item.Width * width);
                var hei = Mathf.RoundToInt(item.Height * height);

                var rect = new Rect(locX, locY, wid, hei);
                rect.x = Mathf.Clamp(rect.x, 0, width - 1);
                rect.y = Mathf.Clamp(rect.y, 0, height - 1);
                var cropped = ImageOperations.CropTexture(result.Source, rect);
                m_debugInputImageSubtext.text = item.ClassName;
                m_debugImageAnalysisRenderer.material.mainTexture = cropped;
            }
            else
            {
                // Set back to default
                m_debugImageAnalysisRenderer.material.mainTexture = m_debugImageAnalysisDefaultTexture;
                m_debugInputImageSubtext.text = "";
            }
        }

        private void UpdateConstantDisplays()
        {
            var languageNameA = AssistantAI.LanguageNativeName[m_selectedLanguageAIndex];
            var languageNameB = AssistantAI.LanguageNativeName[m_selectedLanguageBIndex];
            m_debugDisplayLanguageA.text = $"Language A\n({languageNameA})";
            m_debugDisplayLanguageB.text = $"Language B\n({languageNameB})";
            var duration = "";
            var scope = (AssistantAI.ImageDescriptionScope)m_selectedImageSummaryIndex;
            switch (scope)
            {
                case AssistantAI.ImageDescriptionScope.Short:
                    duration = "short";
                    break;
                case AssistantAI.ImageDescriptionScope.Medium:
                    duration = "medium";
                    break;
                case AssistantAI.ImageDescriptionScope.Long:
                    duration = "long";
                    break;
            }
            m_debugImageAnalysisDuration.text = $"Duration\n({duration})";
        }

        private void OnDebugToggleLanguageButtonA(CanvasXRButton button)
        {
            m_selectedLanguageAIndex += 1;
            var enumCount = Enum.GetNames(typeof(AssistantAI.SupportedLanguage)).Length;
            if (m_selectedLanguageAIndex >= enumCount)
            {
                m_selectedLanguageAIndex = 0;
            }
            UpdateConstantDisplays();
        }

        private void OnDebugToggleLanguageButtonB(CanvasXRButton button)
        {
            m_selectedLanguageBIndex += 1;
            if (m_selectedLanguageBIndex >= AssistantAI.LanguageNativeName.Length)
            {
                m_selectedLanguageBIndex = 0;
            }
            UpdateConstantDisplays();
        }

        private async void OnDebugTranslateButton(CanvasXRButton button)
        {
            if (m_isTranslating)
            {
                return;
            }

            m_isTranslating = true;
            var fromLanguage = (AssistantAI.SupportedLanguage)m_selectedLanguageAIndex;
            var toLanguage = (AssistantAI.SupportedLanguage)m_selectedLanguageBIndex;
            var inputText = m_debugInputLanguageA.text;
            _ = await m_assistant.Translate(fromLanguage, toLanguage, inputText);
        }

        private void OnTranslationComplete(TranslationResult result)
        {
            m_isTranslating = false;
            m_debugInputLanguageB.text = result.StatementTo;
        }

        private async void OnDebugRelatedWordsButton(CanvasXRButton button)
        {
            if (m_isGettingRelated)
            {
                return;
            }

            m_isGettingRelated = true;
            var toLanguage = (AssistantAI.SupportedLanguage)m_selectedLanguageBIndex;
            var inputText = m_debugInputSourceWord.text;
            _ = await m_assistant.FindRelatedWords(toLanguage, inputText);
        }
        private void OnFindRelatedWordsComplete(RelatedWordResult result)
        {
            m_isGettingRelated = false;
            var nouns = string.Join(", ", result.Nouns);
            var adjs = string.Join(", ", result.Adjectives);
            var verbs = string.Join(", ", result.Verbs);
            m_debugInputRelatedWords.text = $"nouns: {nouns}\nadjs: {adjs}\nverbs: {verbs}";
        }

        private void OnDebugImageAnalyzeSourceButton(CanvasXRButton button)
        {
            Debug.Assert(m_cameraTextureManager != null);
            var webTexture = m_cameraTextureManager.WebCamTexture;
            Debug.Assert(webTexture != null);
            var texture2D = GetCameraStillTexture2D(webTexture);
            m_debugImageAnalysisRenderer.material.mainTexture = texture2D;
        }

        private void OnDebugImageAnalyzeToggleDurationButton(CanvasXRButton button)
        {
            m_selectedImageSummaryIndex += 1;
            var enumCount = Enum.GetNames(typeof(AssistantAI.ImageDescriptionScope)).Length;
            if (m_selectedImageSummaryIndex >= enumCount)
            {
                m_selectedImageSummaryIndex = 0;
            }
            UpdateConstantDisplays();
        }

        private async void OnDebugImageAnalyzeButton(CanvasXRButton button)
        {
            if (m_isImageAnalyzing)
            {
                return;
            }
            m_isImageAnalyzing = true;
            var texture = (Texture2D)m_debugImageAnalysisRenderer.material.mainTexture;
            if (texture != null)
            {
                var toLanguage = (AssistantAI.SupportedLanguage)m_selectedLanguageBIndex;
                var duration = (AssistantAI.ImageDescriptionScope)m_selectedImageSummaryIndex;
                _ = await m_assistant.AcquireImageSummary(toLanguage, duration, texture, m_faceBlur);
            }
            else
            {
                m_isImageAnalyzing = false;
            }
        }

        private void OnImageSummaryComplete(ImageSummaryResult result)
        {
            m_isImageAnalyzing = false;
            m_debugImageSendAnalysisRenderer.material.mainTexture = result.ImageSource != null ? result.ImageSource : (Texture)null;

            m_debugInputImageAnalysis.text = result.Summary;
        }

        private async void OnDebugDialogueButton(CanvasXRButton button)
        {
            if (m_isGeneratingDialogue)
            {
                return;
            }
            m_isGeneratingDialogue = true;
            var toLanguage = (AssistantAI.SupportedLanguage)m_selectedLanguageBIndex;
            var sceneDescription = m_debugInputImageAnalysis.text;
            var context = new DialogueContextExploreRoom(toLanguage, sceneDescription);
            _ = await m_assistant.GenerateExploreRoomDialogue(context);
        }

        private void OnGenerateDialogueComplete(DialogueResult result)
        {
            m_isGeneratingDialogue = false;
            m_debugInputDialogue.text = result.Dialogue;
        }

        private void OnDestroy()
        {
            m_debugToggleLanguageButtonA.ButtonWasSelected -= OnDebugToggleLanguageButtonA;
            m_debugToggleLanguageButtonB.ButtonWasSelected -= OnDebugToggleLanguageButtonB;
            m_debugTranslateButton.ButtonWasSelected -= OnDebugTranslateButton;
            m_debugWordCloudsButton.ButtonWasSelected -= OnDebugRelatedWordsButton;
            m_debugImageAnalysisSourceButton.ButtonWasSelected -= OnDebugImageAnalyzeSourceButton;
            m_debugImageAnalysisDurationButton.ButtonWasSelected -= OnDebugImageAnalyzeToggleDurationButton;
            m_debugImageAnalysisButtonGenerate.ButtonWasSelected -= OnDebugImageAnalyzeButton;
            m_debugButtonDialogueGenerate.ButtonWasSelected -= OnDebugDialogueButton;
            m_imageClassifier.ImageProcessedComplete -= OnImageProcessedCompleteEvent;
        }

        private Texture2D GetCameraStillTexture2D(WebCamTexture webCamTexture)
        {
            var bufferSize = webCamTexture.width * webCamTexture.height;
            if (m_cameraImageBuffer == null || m_cameraImageBuffer.Length != bufferSize)
            {
                m_cameraImageBuffer = new Color32[bufferSize];
                m_cameraImage = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            }
            _ = webCamTexture.GetPixels32(m_cameraImageBuffer);
            m_cameraImage.SetPixels32(m_cameraImageBuffer);
            m_cameraImage.Apply();
            return m_cameraImage;
        }
    }
}
