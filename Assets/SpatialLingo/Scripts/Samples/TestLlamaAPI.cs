// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.Utilities.LlamaAPI;
using Meta.XR.Samples;
using TMPro;
using UnityEngine;

namespace SpatialLingo.Llama
{
    [MetaCodeSample("SpatialLingo")]
    public class TestLlamaAPI : MonoBehaviour
    {
        private LlamaRestApi m_llamaAPI;

        // Image to test with, should be marked readable
        [Tooltip("Drag a image source here")]
        [SerializeReference]
        public Texture2D ImageSource;

        // Text field to output feedback results to
        [SerializeReference] public TextMeshPro TextField;

        // List of methods to test / evaluate
        private List<Action> EvaluationList = new();

        private void Start()
        {
            // Set up llama client
            // SpatialLingoSettings.LoadSettings();
            m_llamaAPI = new LlamaRestApi();

            // Set up a list of methods to test
            // Uncomment to test a particular chat sequence request & response
            // EvaluationList.Add(TestChatA);
            // EvaluationList.Add(TestChatB);
            // EvaluationList.Add(TestChatC);
            // EvaluationList.Add(TestChatD);
            EvaluationList.Add(TestChatE);

            // Start testing methods
            CheckNextEvaluation();
        }

        private async void TestChatA()
        {
            await TestChatConversation();
        }

        private async void TestChatB()
        {
            await TestChatWordCloud();
        }

        private async void TestChatC()
        {
            await TestChatTranslation();
        }

        private async void TestChatD()
        {
            await TestChatExampleSentence();
        }

        private async void TestChatE()
        {
            await TestChatImageUnderstanding();
        }

        // Continue iterating through methods for testing, until empty
        private void CheckNextEvaluation()
        {
            if (EvaluationList.Count == 0)
            {
                return;
            }

            var top = EvaluationList[0];
            EvaluationList.RemoveAt(0);
            top();
        }

        private void AppendResponse(string request, string response)
        {
            if (TextField != null)
            {
                var newline = string.IsNullOrEmpty(TextField.text) ? "" : "\n\n";
                TextField.text = TextField.text + newline + request + ": \n" + response;
            }
        }

        /// <summary>
        /// Test a conversation with an example taxon (classification)
        /// </summary>
        private async Task TestChatConversation()
        {
            var word = "Glass";
            var chat = m_llamaAPI.StartNewChat($"You are a helpful language assistant. You like to keep things positive and energized. You respond with short statements. You keep replies concise. You answer in quick remarks. Your remarks are funny and quirky.");
            var request = $"Ask me a very short, interesting, insightful, thoughtful question about \"{word}\". Keep your reply under 12 words.";
            var response = await m_llamaAPI.ContinueChat(chat, request);
            if (response != null)
            {
                AppendResponse(request, response.Message.Text);
            }
            else
            {
                AppendResponse(request, "<Error>");
            }

            CheckNextEvaluation();
        }

        /// <summary>
        /// Test a word cloud from word
        /// </summary>
        private async Task TestChatWordCloud()
        {
            var word = "Glass";
            var chat = m_llamaAPI.StartNewChat($"You are a language assistant. You keep answers concise and focused. Your responses are only comma separated lists. You don't add any other context to the response.");
            var request = $"Give a comma separated list of 10 words related to: \"{word}\". Only include nouns. Sort the results by most related. Return a formatted json array.";
            var response = await m_llamaAPI.ContinueChat(chat, request);
            if (response != null)
            {
                AppendResponse(request, response.Message.Text);
            }
            else
            {
                AppendResponse(request, "<Error>");
            }
            CheckNextEvaluation();
        }

        /// <summary>
        /// Test a sentence from word
        /// </summary>
        private async Task TestChatExampleSentence()
        {
            var word = "Glass";
            var chat = m_llamaAPI.StartNewChat($"You are a language assistant. You keep answers concise and focused.");
            var request = $"Give an example of using the word: \"{word}\" in a very short 4 to 6 word sentence.";
            var response = await m_llamaAPI.ContinueChat(chat, request);
            if (response != null)
            {
                AppendResponse(request, response.Message.Text);
            }
            else
            {
                AppendResponse(request, "<Error>");
            }
            CheckNextEvaluation();
        }

        /// <summary>
        /// Test a question about an image from image+prompt
        /// </summary>
        private async Task TestChatImageUnderstanding()
        {
            var image = GetSourceImageAsString(ImageSource);
            var system = "You area a helpful assistant who provides concise answers.";
            var request = "Say one interesting thing about this image. Please keep the answer short, only about one sentence long.";
            var images = new string[] { image };
            var response = await m_llamaAPI.ImageUnderstanding(system, request, images);
            if (response != null)
            {
                AppendResponse(request, response.Message);
            }
            else
            {
                AppendResponse(request, "<Error>");
            }
            CheckNextEvaluation();
        }

        /// <summary>
        /// Test a translation from a sentence
        /// </summary>
        private async Task TestChatTranslation()
        {
            var languageFrom = "English";
            var languageTo = "Vietnamese";
            var sentenceFrom = "What are you doing today?";
            var request = $"Translate this sentence from \"{languageFrom}\" to \"{languageTo}\": \"{sentenceFrom}\" ";
            var chat = m_llamaAPI.StartNewChat($"You are a translation assistant. You translate sentences from \"{languageFrom}\" to \"{languageTo}\".");
            var response = await m_llamaAPI.ContinueChat(chat, request);
            if (response != null)
            {
                AppendResponse(request, response.Message.Text);
            }
            else
            {
                AppendResponse(request, "<Error>");
            }
            CheckNextEvaluation();
        }

        /// <summary>
        /// Convert the image to a base64 string.
        /// Scale the image down where necessary to limit data sent on network
        /// </summary>
        /// <param name="original">source image</param>
        /// <returns>image as a base64 string, in a network-useable resolution</returns>
        private string GetSourceImageAsString(Texture2D original)
        {
            // Minimum image size to still have enough resolution for image understanding (4:3 ratio)
            var desiredTextureSize = new Vector2Int(300, 225);
            var desiredPixels = desiredTextureSize.x * desiredTextureSize.y;
            // Create a te
            var tempTexture = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
            var renderTexture = RenderTexture.GetTemporary(original.width, original.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(original, renderTexture);
            RenderTexture.active = renderTexture;
            tempTexture.ReadPixels(new Rect(0, 0, original.width, original.height), 0, 0);
            tempTexture.Apply();
            RenderTexture.ReleaseTemporary(renderTexture);
            // Determine the output size, rounded to nearest dimensions.
            var inputSize = new Vector2Int(tempTexture.width, tempTexture.height);
            var inputWidthToHeightRatio = inputSize.x / inputSize.y;
            var outputHeight = (int)Mathf.Round(Mathf.Sqrt(desiredPixels / inputWidthToHeightRatio));
            var outputWidth = (int)Mathf.Round(inputWidthToHeightRatio * outputHeight);
            // Only scale down, not up.
            if (outputWidth > original.width)
            {
                outputWidth = original.width;
                outputHeight = original.height;
            }

            _ = outputWidth * outputHeight;
            tempTexture = Resize(tempTexture, outputWidth, outputHeight);

            // Get the image as a jpeg string, with corresponding prefix
            var base64Data = tempTexture.EncodeToJPG();
            var base64String = Convert.ToBase64String(base64Data);
            var prefix = "data:image/jpeg;base64,";

            var url = $"{prefix}{base64String}";
            return url;
        }

        /// <summary>
        /// Resize in image to target dimensions
        /// </summary>
        /// <param name="texture2D">source image</param>
        /// <param name="targetWidth">desired width</param>
        /// <param name="targetHeight">desired height</param>
        /// <returns></returns>
        private Texture2D Resize(Texture2D texture2D, int targetWidth, int targetHeight)
        {
            // Create a temporary RenderTexture with the target size
            // RenderTexture rt = new RenderTexture(targetX, targetY, 24);
            var rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0,
                RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            // Set the active RenderTexture to the temporary texture
            RenderTexture.active = rt;
            // Copy the source texture to the destination RenderTexture using the GPU
            Graphics.Blit(texture2D, rt);
            // Create a new Texture2D to store the resized result
            var result = new Texture2D(targetWidth, targetHeight);
            // Read the pixel values from the RenderTexture to the new Texture2D
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            // Upload the changed pixels to the graphics card
            result.Apply();
            // Release the temporary RenderTexture
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }
    }
}
