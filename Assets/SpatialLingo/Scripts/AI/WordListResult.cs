// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    public struct WordListResult
    {
        public string ContextID;
        public AssistantAI.SupportedLanguage FromLanguage { get; }
        public AssistantAI.SupportedLanguage ToLanguage { get; }
        public string[] WordsFrom { get; }
        public string[] WordsTo { get; }
        public WordListResult(AssistantAI.SupportedLanguage fromLanguage, AssistantAI.SupportedLanguage toLanguage, string[] wordsFrom, string[] wordsTo, string contextID)
        {
            FromLanguage = fromLanguage;
            ToLanguage = toLanguage;
            WordsFrom = wordsFrom;
            WordsTo = wordsTo;
            ContextID = contextID;
        }
    }
}