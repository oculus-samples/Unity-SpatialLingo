// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.Utilities.LlamaAPI
{
    public partial class LlamaRestApi
    {
        private HttpClient m_client;
        private string m_model = MODEL_NAME_LLAMA4_MAVERICK;

        // Declare the client version
        private const string CLIENT_NAME = "LlamaRestApiClient";
        private const string CLIENT_VERSION = "0.0.1";
        // Sample models to try
        private const string MODEL_NAME_LLAMA4_MAVERICK = "Llama-4-Maverick-17B-128E-Instruct-FP8";
        private const string MODEL_NAME_LLAMA4_SCOUT = "Llama-4-Scout-17B-16E-Instruct-FP8";
        private const string MODEL_NAME_LLAMA3_3_INSTRUCT = "Llama-3.3-70B-Instruct";

        public static Func<Awaitable<(string apiKey, string endpointOverride)>> GetApiKeyAsync = null;
        private ValueTask m_makeClientTask;


        public LlamaRestApi()
        {
            m_makeClientTask = MakeClientAsync();

            async ValueTask MakeClientAsync()
            {
                Debug.Assert(GetApiKeyAsync != null, $"{nameof(GetApiKeyAsync)} must be set to use LlamaRestApi.");

                var (apiKey, endpointOverride) = await GetApiKeyAsync();
                m_client = MakeClientWithKey(apiKey, endpointOverride);
            }
        }

        private static HttpClient MakeClientWithKey(string apiKey, string baseEndpointOverride = null)
        {
            const string PUBLIC_LLAMA_API = "https://api.llama.com/v1/";

#if HAS_META_UTILITIES
            if (AndroidHelpers.GetStringIntentExtra("LlamaApiEndpoint") is { } adbOverride)
            {
                Debug.Log($"ADB String LlamaApiEndpoint set to: {adbOverride}");
                baseEndpointOverride = adbOverride;
            }
#endif

            Assert.IsNotNull(apiKey, "apiKey");
            Assert.IsTrue(apiKey.Length > 2, "apiKey.Length > 2");

            var client = new HttpClient
            {
                BaseAddress = new Uri(baseEndpointOverride ?? PUBLIC_LLAMA_API),
            };
            Debug.Log($"Llama API Endpoint = {client.BaseAddress}");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(CLIENT_NAME, CLIENT_VERSION));
            client.Timeout = TimeSpan.FromSeconds(20.0f);
            return client;
        }

        private async Awaitable<HttpClient> GetHttpClient()
        {
            await m_makeClientTask;
            return m_client;
        }


        /// <summary>
        /// Chat endpoint. Begin a conversation with AI.
        /// </summary>
        /// <param name="prompt">System prompt for context or behavior</param>
        /// <returns>Chat Response</returns>
        public LlamaChat StartNewChat(string prompt)
        {
            var chat = new LlamaChat();
            var system = new LlamaChatMessage(LlamaChatMessage.ROLE_SYSTEM, prompt);
            _ = chat.AddMessage(system);
            return chat;
        }

        /// <summary>
        /// Chat endpoint. Continue conversation with AI.
        /// </summary>
        /// <param name="history">Previous conversation history</param>
        /// <param name="text">Message sent from or in service of user</param>
        /// <returns>Chat Response</returns>
        public async Awaitable<LlamaChatResponse> ContinueChat(LlamaChat history, string text)
        {
            var user = new LlamaChatMessage(LlamaChatMessage.ROLE_USER, text);
            _ = history.AddMessage(user);

            var messages = ChatTextRequestMessageDTO.FromChat(history);
            var response = await ChatInternal(messages);
            if (response == null)
            {
                Debug.LogError($"Received a null result for {nameof(ContinueChat)}");
                return null;
            }
            var role = response.completion_message.role;
            var message = response.completion_message.content.text;
            var assistant = new LlamaChatMessage(role, message);
            _ = history.AddMessage(assistant);

            var chatResponse = new LlamaChatResponse(history, assistant);
            return chatResponse;
        }

        public bool AddChatHistoryAsUser(LlamaChat chat, string text)
        {
            return AddChatHistory(chat, LlamaChatMessage.ROLE_USER, text);
        }

        public bool AddChatHistoryAsAI(LlamaChat chat, string text)
        {
            return AddChatHistory(chat, LlamaChatMessage.ROLE_ASSISTANT, text);
        }

        private bool AddChatHistory(LlamaChat chat, string role, string text)
        {
            var oldCount = chat.MessageCount;
            var message = new LlamaChatMessage(role, text);
            var newCount = chat.AddMessage(message);
            return newCount > oldCount;
        }

        private async Awaitable<ChatImageResponseDTO> ChatInternal(ChatTextRequestMessageDTO[] messages, int maxTokens = 1024)
        {
            m_client ??= await GetHttpClient();

            var endpoint = "chat/completions";
            var request = new ChatTextRequestDTO(m_model, messages, maxTokens);
            var requestJSON = JsonUtility.ToJson(request);
            var content = new StringContent(requestJSON, Encoding.UTF8, "application/json");
            try
            {
                var response = await m_client.PostAsync(endpoint, content);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Debug.LogError(
                        $"Expected response code: {HttpStatusCode.OK}, got: {response.StatusCode} [{(int)response.StatusCode}]\n" +
                        $"Request Data:\n{requestJSON}");
                    Debug.LogError($"Server Reason:\n{response.ReasonPhrase}\nServer Content:\n{response.Content}");
                    return null;
                }

                if (response.Content == null)
                {
                    Debug.LogError($"Expected response content, found null");
                    return null;
                }

                var value = await response.Content.ReadAsStringAsync();
                var responseObject = JsonUtility.FromJson<ChatImageResponseDTO>(value);
                return responseObject;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LlamaRestApi - ChatInternal - client.PostAsync failed: {e.Message}");
            }

            return null;
        }

        /// <summary>
        /// Image Understanding endpoint. Get information about passed images
        /// </summary>
        /// <param name="text">Message to pass along with image(s)</param>
        /// <param name="images">Images are base64 w/ prefix or web URLs</param>
        /// <returns>Image Undestanding Response</returns>
        public async Awaitable<LlamaImageResponse> ImageUnderstanding(string systemText, string userText, string[] images)
        {
            var response = await ImageUnderstandingInternal(systemText, userText, images);
            if (response == null)
            {
                Debug.LogError($"Received a null result for {nameof(ImageUnderstanding)}");
                return null;
            }
            var imageResponse = new LlamaImageResponse(response.completion_message.content.text);
            return imageResponse;
        }

        private async Awaitable<ChatImageResponseDTO> ImageUnderstandingInternal(string systemText, string userText, string[] images)
        {
            m_client ??= await GetHttpClient();

            var endpoint = "chat/completions";
            ChatImageRequestMessageDTO systemMessage = null;
            if (systemText != null)
            {
                var systemContentList = new List<ChatImageRequestContentDTO>();
                var systemContentText = new ChatImageRequestContentDTO("text", systemText, null);
                systemContentList.Add(systemContentText);
                var systemContents = systemContentList.ToArray();
                systemMessage = new ChatImageRequestMessageDTO(LlamaChatMessage.ROLE_SYSTEM, systemContents);
            }

            var contentList = new List<ChatImageRequestContentDTO>();
            var contentText = new ChatImageRequestContentDTO("text", userText, null);
            contentList.Add(contentText);

            foreach (var image in images)
            {
                var contentImageURL = new ChatImageRequestContentImageUrlDTO(image);
                var contentImage = new ChatImageRequestContentDTO("image_url", null, contentImageURL);
                contentList.Add(contentImage);
            }

            var contents = contentList.ToArray();
            var message = new ChatImageRequestMessageDTO(LlamaChatMessage.ROLE_USER, contents);
            var messages = systemMessage != null
                ? (new ChatImageRequestMessageDTO[] { systemMessage, message })
                : (new ChatImageRequestMessageDTO[] { message });
            var request = new ChatImageRequestDTO(m_model, messages, 1024);
            var requestJSON = JsonUtility.ToJson(request);

            var content = new StringContent(requestJSON, Encoding.UTF8, "application/json");
            try
            {
                var response = await m_client.PostAsync(endpoint, content);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Debug.LogError(
                        $"Expected response code: {HttpStatusCode.OK}, got: {response.StatusCode} [{(int)response.StatusCode}]\n" +
                        $"Request Data:\n{requestJSON}");
                    Debug.LogError($"Server Reason:\n{response.ReasonPhrase}\nServer Content:\n{response.Content}");
                    return null;
                }

                if (response.Content == null)
                {
                    Debug.LogError($"Expected response content, found null");
                    return null;
                }

                var value = await response.Content.ReadAsStringAsync();
                var responseObject = JsonUtility.FromJson<ChatImageResponseDTO>(value);
                return responseObject;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LlamaRestApi - ImageUnderstandingInternal - client.PostAsync failed: {e.Message}");
            }

            return null;
        }

    }
}
