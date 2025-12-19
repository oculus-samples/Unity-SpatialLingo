// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    public struct RelatedWordResult
    {
        public string ContextID;
        public AssistantAI.SupportedLanguage TargetLanguage { get; }
        public string Word { get; }
        public string[] Nouns { get; }
        public string[] Adjectives { get; }
        public string[] Verbs { get; }
        public RelatedWordResult(AssistantAI.SupportedLanguage targetLanguage, string word, string[] nouns, string[] adjectives, string[] verbs, string contextID)
        {
            TargetLanguage = targetLanguage;
            Word = word;
            Nouns = nouns;
            Adjectives = adjectives;
            Verbs = verbs;
            ContextID = contextID;
        }
    }
}