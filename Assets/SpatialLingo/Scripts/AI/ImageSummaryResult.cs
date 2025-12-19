// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    public struct ImageSummaryResult
    {
        public string ContextID;
        public AssistantAI.SupportedLanguage TargetLanguage { get; }
        public AssistantAI.ImageDescriptionScope Scope { get; }
        public string Summary { get; }
        public Texture2D ImageSource { get; }
        public string ImageString { get; }
        public ImageSummaryResult(AssistantAI.SupportedLanguage targetLanguage, AssistantAI.ImageDescriptionScope scope, Texture2D imageSource, string imageString, string summary, string contextID)
        {
            TargetLanguage = targetLanguage;
            Scope = scope;
            ImageSource = imageSource;
            ImageString = imageString;
            Summary = summary;
            ContextID = contextID;
        }
    }
}