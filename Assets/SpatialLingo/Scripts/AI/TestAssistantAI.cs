// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.Utilities.LlamaAPI;
using Meta.Utilities.ObjectClassifier;
using Meta.XR.Samples;
using TMPro;
using UnityEngine;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    public class TestAssistantAI : MonoBehaviour
    {
        [SerializeField] private FaceDetection m_faceBlur;

        // Text field to display the translations
        [Tooltip("Drag an input text field here for translation output")]
        [SerializeField] private TMP_InputField m_inputTextFieldTranslation;

        // Text field to display the related words
        [Tooltip("Drag an input text field here for related words output")]
        [SerializeField] private TMP_InputField m_inputTextFieldRelatedWords;

        // Text field to display the image summary
        [Tooltip("Drag an input text field here for image understanding output")]
        [SerializeField] private TMP_InputField m_inputTextFieldImageUnderstanding;

        // Text field to display the conversation
        [Tooltip("Drag an input text field here for conversation output")]
        [SerializeField] private TMP_InputField m_inputTextFieldConversation;

        // Input texture for image understanding
        [Tooltip("Drag a texture source here for image understanding input")]
        [SerializeField] private Texture2D m_inputTextureSource;

        // Text field to display word list translation
        [Tooltip("Drag an input text field here for word list output")]
        [SerializeField] private TMP_InputField m_inputTextFieldWordList;

        // Input texture for word cloud generation
        [Tooltip("Example Image & Classification LLAMA Image Sensing")]
        [SerializeField] private Texture2D[] m_inputExampleTrackedImages;
        [SerializeField] private string[] m_inputExampleTrackedClassifications;

        private AssistantAI m_assistantAI;

        // Start is called once before the first execution of Update after the MonoBehaviour is created

        private void Start()
        {
            // SpatialLingoSettings.LoadSettings();

            var llamaAPI = new LlamaRestApi();
            m_assistantAI = new AssistantAI(llamaAPI);

            // Clear Text Fields
            m_inputTextFieldTranslation.text = "";
            m_inputTextFieldRelatedWords.text = "";
            m_inputTextFieldImageUnderstanding.text = "";
            m_inputTextFieldConversation.text = "";
            m_inputTextFieldWordList.text = "";

            // Uncomment to test out different scenarios
            // TestWordCloudRequests();
            TestWordCloudEvaluation();
        }

        private async void TestWordCloudEvaluation()
        {
            string[] nouns, adjs, verbs;
            string word, speech, line;
            WordCloudTranscriptPhraseResult result;
            AssistantAI.SupportedLanguage targetLanguage;

            // English example - cup
            word = "Cup";
            nouns = new string[] { word };
            adjs = new string[] { "Tall", "Coffee", "Glass" };
            verbs = new string[] { "Fill", "Pour" };

            targetLanguage = AssistantAI.SupportedLanguage.English;
            speech = "I will, fill the: glass cup. with- coffee!"; // punctuation
            //speech = "fill the glass cup with Coffee!"; // capitalization
            //speech = "This is a list of random words"; // fail
            result = await m_assistantAI.EvaluateTranscriptionForWordCloud(targetLanguage, AssistantAI.SupportedLanguage.English, speech, nouns, adjs, verbs);
            line = speech + " - " + result.IsPassing + " - " + result.FailReason;
            Debug.Log(line);
            m_inputTextFieldWordList.text += line + "\n";

            // English example - Television
            word = "Television";
            nouns = new string[] { word };
            adjs = new string[] { "Widescreen", "Large", "Black" };
            verbs = new string[] { "Watch", "Hang" };

            targetLanguage = AssistantAI.SupportedLanguage.English;
            speech = "I want to watch the wide screen TV"; // spacing / compound words
            //speech = "This is a list of random words"; // fail
            result = await m_assistantAI.EvaluateTranscriptionForWordCloud(targetLanguage, AssistantAI.SupportedLanguage.English, speech, nouns, adjs, verbs);
            line = speech + " - " + result.IsPassing + " - " + result.FailReason;
            Debug.Log(line);
            m_inputTextFieldWordList.text += line + "\n";

            // Spanish Example - Television
            word = "televisión";
            nouns = new string[] { word };
            adjs = new string[] { "Pantalla Ancha", "Grande", "Negra" };
            verbs = new string[] { "Ver", "Instalar" };

            targetLanguage = AssistantAI.SupportedLanguage.Spanish;
            speech = "Quiero instalar el televisor grande";
            result = await m_assistantAI.EvaluateTranscriptionForWordCloud(targetLanguage, AssistantAI.SupportedLanguage.English, speech, nouns, adjs, verbs);
            line = speech + " - " + result.IsPassing + " - " + result.FailReason;
            Debug.Log(line);
            m_inputTextFieldWordList.text += line + "\n";

            // Spanish Example - Silla
            word = "La Silla";
            nouns = new string[] { word };
            adjs = new string[] { "De oficina", "Portátil", "Marrón" };
            verbs = new string[] { "Empujar", "Sentarse" };

            targetLanguage = AssistantAI.SupportedLanguage.English;
            speech = "Quiero sentarse en la CIA marrón.";
            result = await m_assistantAI.EvaluateTranscriptionForWordCloud(targetLanguage, AssistantAI.SupportedLanguage.Spanish, speech, nouns, adjs, verbs);
            line = speech + " - " + result.IsPassing + " - " + result.FailReason;
            Debug.Log(line);
            m_inputTextFieldWordList.text += line + "\n";
        }

        private async void TestWordCloudRequests()
        {
            var userLanguage = AssistantAI.SupportedLanguage.English;
            var targetLanguage = AssistantAI.SupportedLanguage.Spanish;

            string speech;
            string[] nouns;
            string[] adjs;
            string[] verbs;
            WordCloudTranscriptPhraseResult passResult;

            var maxCount = Math.Min(m_inputExampleTrackedImages.Length, m_inputExampleTrackedClassifications.Length);
            for (var i = 0; i < maxCount; ++i)
            {
                var croppedImage = m_inputExampleTrackedImages[i];
                var classification = m_inputExampleTrackedClassifications[i];

                var imageString = await AssistantAI.GetNetworkSafeImageString(croppedImage, m_faceBlur);
                var wordCloudResult = await m_assistantAI.GenerateWordCloudData(userLanguage, targetLanguage, classification, imageString);
                var wordCloud = wordCloudResult.Wordcloud;

                var cloudDataUser = wordCloud.WordCloudForLanguage(userLanguage);
                var cloudDataTarget = wordCloud.WordCloudForLanguage(targetLanguage);

                m_inputTextFieldTranslation.text += cloudDataUser.Word + " - " + cloudDataTarget.Word + "\n";
                m_inputTextFieldTranslation.text += cloudDataUser.Adjectives[0] + " - " + cloudDataTarget.Adjectives[0] + "\n";
                m_inputTextFieldTranslation.text += cloudDataUser.Adjectives[1] + " - " + cloudDataTarget.Adjectives[1] + "\n";
                m_inputTextFieldTranslation.text += cloudDataUser.Adjectives[2] + " - " + cloudDataTarget.Adjectives[2] + "\n";
                m_inputTextFieldTranslation.text += cloudDataUser.Verbs[0] + " - " + cloudDataTarget.Verbs[0] + "\n";
                m_inputTextFieldTranslation.text += cloudDataUser.Verbs[1] + " - " + cloudDataTarget.Verbs[1] + "\n";

                var phraseResult = await m_assistantAI.GenerateSpeechExamplesForWordCloud(targetLanguage, cloudDataTarget.Word, cloudDataTarget.Verbs, cloudDataTarget.Adjectives);
                m_inputTextFieldRelatedWords.text += phraseResult.Phrases.Phrase0 + "\n";
                m_inputTextFieldRelatedWords.text += phraseResult.Phrases.Phrase1 + "\n";
                m_inputTextFieldRelatedWords.text += phraseResult.Phrases.Phrase2 + "\n";
                m_inputTextFieldRelatedWords.text += "\n";

                nouns = new string[] { cloudDataTarget.Word };
                verbs = cloudDataTarget.Verbs;
                adjs = cloudDataTarget.Adjectives;

                // A bad example
                speech = "Donde esta la biblioteca";
                passResult = await m_assistantAI.EvaluateTranscriptionForWordCloud(targetLanguage, AssistantAI.SupportedLanguage.English, speech, nouns, adjs, verbs);
                m_inputTextFieldWordList.text += speech + " = " + passResult.IsPassing + "\n";

                // A good example (maybe)
                speech = "Yo quiero " + verbs[0] + " " + nouns[0] + " " + adjs[0];
                passResult = await m_assistantAI.EvaluateTranscriptionForWordCloud(targetLanguage, AssistantAI.SupportedLanguage.English, speech, nouns, adjs, verbs);
                m_inputTextFieldWordList.text += speech + " = " + passResult.IsPassing + " - " + passResult.FailReason + "\n";
            }

            // Static example:
            var word = "La Silla";
            nouns = new string[] { word };
            adjs = new string[] { "Cómoda", "Ergonómica", "Robusta" };
            verbs = new string[] { "Sentarse", "Ocupar" };

            // A bad example
            speech = "La silla es grande.";
            passResult = await m_assistantAI.EvaluateTranscriptionForWordCloud(targetLanguage, AssistantAI.SupportedLanguage.English, speech, nouns, adjs, verbs);
            m_inputTextFieldWordList.text += speech + " - " + passResult.IsPassing + " - " + passResult.FailReason + "\n";

            // A good example
            speech = "Me siento en mi silla robusta";
            passResult = await m_assistantAI.EvaluateTranscriptionForWordCloud(targetLanguage, AssistantAI.SupportedLanguage.English, speech, nouns, adjs, verbs);

            m_inputTextFieldWordList.text += speech + " - " + passResult.IsPassing + " - " + passResult.FailReason + "\n";

        }

        private async void TestBasicRequests()
        {
            // Translation request tests:
            m_assistantAI.TranslationComplete += OnTranslationComplete;
            var phraseEnglish = "Hello, how old are you?";
            _ = await m_assistantAI.Translate(AssistantAI.SupportedLanguage.English, AssistantAI.SupportedLanguage.Spanish, phraseEnglish);
            _ = await m_assistantAI.Translate(AssistantAI.SupportedLanguage.English, AssistantAI.SupportedLanguage.Vietnamese, phraseEnglish);
            var phraseSpanish = "Hola, ¿cuántos años tienes?";
            _ = await m_assistantAI.Translate(AssistantAI.SupportedLanguage.Spanish, AssistantAI.SupportedLanguage.English, phraseSpanish);
            _ = await m_assistantAI.Translate(AssistantAI.SupportedLanguage.Spanish, AssistantAI.SupportedLanguage.Vietnamese, phraseSpanish);
            var phraseVietnamese = "Xin chào, bạn bao nhiêu tuổi?";
            _ = await m_assistantAI.Translate(AssistantAI.SupportedLanguage.Vietnamese, AssistantAI.SupportedLanguage.English, phraseVietnamese);
            _ = await m_assistantAI.Translate(AssistantAI.SupportedLanguage.Vietnamese, AssistantAI.SupportedLanguage.Spanish, phraseVietnamese);

            // Related words request tests:
            m_assistantAI.FindRelatedWordsComplete += OnFindRelatedWordsComplete;
            var wordEnglish = "Sunglasses";
            _ = await m_assistantAI.FindRelatedWords(AssistantAI.SupportedLanguage.English, wordEnglish);
            var spanishWord = "Gafas de sol";
            _ = await m_assistantAI.FindRelatedWords(AssistantAI.SupportedLanguage.Spanish, spanishWord);
            var vietnameseWord = "Kính râm";
            _ = await m_assistantAI.FindRelatedWords(AssistantAI.SupportedLanguage.Vietnamese, vietnameseWord);

            // Image understanding request tests:
            m_assistantAI.CollectImageSummaryComplete += OnCollectImageSummaryComplete;
            _ = await m_assistantAI.AcquireImageSummary(AssistantAI.SupportedLanguage.English, AssistantAI.ImageDescriptionScope.Short, m_inputTextureSource, m_faceBlur);
            _ = await m_assistantAI.AcquireImageSummary(AssistantAI.SupportedLanguage.Spanish, AssistantAI.ImageDescriptionScope.Short, m_inputTextureSource, m_faceBlur);
            _ = await m_assistantAI.AcquireImageSummary(AssistantAI.SupportedLanguage.Vietnamese, AssistantAI.ImageDescriptionScope.Short, m_inputTextureSource, m_faceBlur);
            _ = await m_assistantAI.AcquireImageSummary(AssistantAI.SupportedLanguage.English, AssistantAI.ImageDescriptionScope.Medium, m_inputTextureSource, m_faceBlur);
            _ = await m_assistantAI.AcquireImageSummary(AssistantAI.SupportedLanguage.English, AssistantAI.ImageDescriptionScope.Long, m_inputTextureSource, m_faceBlur);

            // Conversation request tests:
            m_assistantAI.GenerateDialogueComplete += OnGenerateDialogueComplete;
            var description = "This is a small room. There is a desk, window, computer, and backpack. This might be a person's bedroom or an office. It is a little messy and hard to make out smaller specific items.";
            var dialogue = new DialogueContextExploreRoom(AssistantAI.SupportedLanguage.English, description);
            _ = await m_assistantAI.GenerateExploreRoomDialogue(dialogue);

            // Test translating a set of different words
            string[] words = { "Kitty", "Cat", "Plant", "Stapler", "Laptop", "Monitor", "Book", "Cup", "Table", "Fruitful", "Botanical" };
            var result = await m_assistantAI.TranslateWordList(AssistantAI.SupportedLanguage.English, AssistantAI.SupportedLanguage.Spanish, words);
            m_inputTextFieldWordList.text = string.Join(",", result.WordsTo);
        }

        private void OnGenerateDialogueComplete(DialogueResult result)
        {
            m_inputTextFieldConversation.text += $"[{result.TargetLanguage}]: {result.Dialogue}\n\n";
        }

        private void OnCollectImageSummaryComplete(ImageSummaryResult result)
        {
            m_inputTextFieldImageUnderstanding.text += $"[{result.TargetLanguage}] {result.Summary}\n\n";
        }

        private void OnFindRelatedWordsComplete(RelatedWordResult result)
        {
            var list = string.Join(",", result.Nouns, result.Adjectives, result.Verbs);
            m_inputTextFieldRelatedWords.text += $"[{result.TargetLanguage}] {result.Word} :\n{list}\n\n";
        }

        private void OnTranslationComplete(TranslationResult result)
        {
            m_inputTextFieldTranslation.text += $"[{result.LanguageFrom}] {result.StatementFrom}\n=>\n[{result.LanguageTo}] {result.StatementTo}\n\n";
        }
    }
}
