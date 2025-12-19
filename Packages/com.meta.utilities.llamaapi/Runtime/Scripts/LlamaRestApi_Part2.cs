// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006

namespace Meta.Utilities.LlamaAPI
{
    public partial class LlamaRestApi
    {
        #region "Internal Data Transfer Objects"  

        [DataContract, Serializable]
        private class ChatTextRequestDTO
        {
            [DataMember(Name = "model")] public string model;
            [DataMember(Name = "messages")] public ChatTextRequestMessageDTO[] messages;
            [DataMember(Name = "max_tokens")] public int max_tokens;

            public ChatTextRequestDTO(string model, ChatTextRequestMessageDTO[] messages, int max_tokens = 256)
            {
                this.model = model;
                this.messages = messages;
                this.max_tokens = max_tokens;
            }
        }

        [DataContract, Serializable]
        private class ChatTextRequestMessageDTO
        {
            [DataMember(Name = "role")] public string role;
            [DataMember(Name = "content")] public string content;

            public ChatTextRequestMessageDTO(string role, string content)
            {
                this.role = role;
                this.content = content;
            }

            public static ChatTextRequestMessageDTO[] FromChat(LlamaChat chat)
            {
                var list = new List<ChatTextRequestMessageDTO>();
                var messageCount = chat.MessageCount;
                for (var i = 0; i < messageCount; ++i)
                {
                    var message = chat.GetMessage(i);
                    var requestMessage = new ChatTextRequestMessageDTO(message.Role, message.Text);
                    list.Add(requestMessage);
                }

                return list.ToArray();
            }
        }

        [DataContract, Serializable]
        private class ChatImageRequestDTO
        {
            [DataMember(Name = "model")]
            public string model;
            [DataMember(Name = "messages")]
            public ChatImageRequestMessageDTO[] messages;
            [DataMember(Name = "max_tokens")]
            public int max_tokens;

            public ChatImageRequestDTO(string model, ChatImageRequestMessageDTO[] messages, int max_tokens)
            {
                this.model = model;
                this.messages = messages;
                this.max_tokens = max_tokens;
            }
        }

        [DataContract, Serializable]
        private class ChatImageRequestMessageDTO
        {
            [DataMember(Name = "role")]
            public string role;
            [DataMember(Name = "content")]
            public ChatImageRequestContentDTO[] content;

            public ChatImageRequestMessageDTO(string role, ChatImageRequestContentDTO[] content)
            {
                this.role = role;
                this.content = content;
            }
        }

        [DataContract, Serializable]
        private class ChatImageRequestContentDTO
        {
            [DataMember(Name = "type")]
            public string type;
            [DataMember(Name = "text")]
            public string text;
            [DataMember(Name = "image_url")]
            public ChatImageRequestContentImageUrlDTO image_url;

            public ChatImageRequestContentDTO(string type, string text, ChatImageRequestContentImageUrlDTO image_url)
            {
                this.type = type;
                this.text = text;
                this.image_url = image_url;
            }
        }

        [DataContract, Serializable]
        private class ChatImageRequestContentImageUrlDTO
        {
            [DataMember(Name = "url")] public string url;

            public ChatImageRequestContentImageUrlDTO(string url) => this.url = url;
        }

        [DataContract, Serializable]
        private class ChatImageResponseDTO
        {
            [DataMember(Name = "id")] public string id;

            [DataMember(Name = "completion_message")]
            public ChatImageResponseCompletionMessageDTO completion_message;

            [DataMember(Name = "metrics")] public ChatImageResponseMetricDTO[] metrics;
        }

        [DataContract, Serializable]
        private class ChatImageResponseCompletionMessageDTO
        {
            [DataMember(Name = "role")] public string role;
            [DataMember(Name = "stop_reason")] public string stop_reason;
            [DataMember(Name = "content")] public ChatImageResponseMessageContentDTO content;
        }

        [DataContract, Serializable]
        private class ChatImageResponseMetricDTO
        {
            [DataMember(Name = "metric")] public string metric;
            [DataMember(Name = "value")] public int value;
            [DataMember(Name = "unit")] public string unit;
        }

        [DataContract, Serializable]
        private class ChatImageResponseMessageContentDTO
        {
            [DataMember(Name = "type")] public string type;
            [DataMember(Name = "text")] public string text;
        }

        #endregion
    }
}

#pragma warning restore IDE1006
