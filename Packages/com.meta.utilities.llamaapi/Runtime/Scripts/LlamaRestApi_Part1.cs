// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;

namespace Meta.Utilities.LlamaAPI
{
    public partial class LlamaRestApi
    {
        #region "Exposed Data Objects"

        public class LlamaImageResponse
        {
            public string Message { get; }

            internal LlamaImageResponse(string message) => Message = message;
        }


        public class LlamaChat
        {
            private List<LlamaChatMessage> m_messages;

            internal LlamaChat() => m_messages = new List<LlamaChatMessage>();

            public int MessageCount => m_messages.Count;

            /// <summary>
            /// Add a message to the conversation list
            /// </summary>
            /// <param name="message">Message to add</param>
            /// <returns>New count of total messages in the conversation</returns>
            public int AddMessage(LlamaChatMessage message)
            {
                m_messages.Add(message);
                return m_messages.Count;
            }
            /// <summary>
            /// Get a specific message in the conversation
            /// </summary>
            /// <param name="index">Index of message requested</param>
            /// <returns>Message if found, null otherwise</returns>
            public LlamaChatMessage GetMessage(int index)
            {
                if (index < 0 || index >= m_messages.Count)
                {
                    return null;
                }

                return m_messages[index];
            }
        }

        public class LlamaChatMessage
        {
            public const string ROLE_SYSTEM = "system"; // system context
            public const string ROLE_USER = "user"; // content sent to llama
            public const string ROLE_ASSISTANT = "assistant"; // content sent from llama
            public const string ROLE_TOOL = "ipython"; // tool
            public string Role { get; }
            public string Text { get; }

            internal LlamaChatMessage(string role, string text)
            {
                Role = role;
                Text = text;
            }
        }

        public class LlamaChatResponse
        {
            public LlamaChat History { get; }
            public LlamaChatMessage Message { get; }

            internal LlamaChatResponse(LlamaChat history, LlamaChatMessage message)
            {
                History = history;
                Message = message;
            }
        }

        #endregion
    }
}