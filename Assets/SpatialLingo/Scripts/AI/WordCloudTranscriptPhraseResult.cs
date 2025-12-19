// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    public class WordCloudTranscriptPhraseResult
    {
        public string ContextID;
        public readonly AssistantAI.SupportedLanguage TargetLanguage;
        public readonly string Transcript;
        public readonly string[] Nouns;
        public readonly string[] Adjectives;
        public readonly string[] Verbs;
        public readonly bool IsPassing;
        public readonly string FailReason;
        public WordCloudTranscriptPhraseResult(AssistantAI.SupportedLanguage targetLanguage, string transcript, string[] nouns, string[] adjectives, string[] verbs, bool isPassing, string failReason, string contextID)
        {
            TargetLanguage = targetLanguage;
            Transcript = transcript;
            Nouns = nouns;
            Adjectives = adjectives;
            Verbs = verbs;
            IsPassing = isPassing;
            FailReason = failReason;
            ContextID = contextID;
        }
    }
}