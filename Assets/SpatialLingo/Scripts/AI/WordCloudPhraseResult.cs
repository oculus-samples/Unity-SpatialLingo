// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    public struct WordCloudPhraseResult
    {
        public string ContextID;
        public readonly AssistantAI.SupportedLanguage TargetLanguage;
        public readonly WordCloudPhraseData Phrases;
        public WordCloudPhraseResult(AssistantAI.SupportedLanguage language, WordCloudPhraseData phrases, string contextID)
        {
            TargetLanguage = language;
            Phrases = phrases;
            ContextID = contextID;
        }
    }
}