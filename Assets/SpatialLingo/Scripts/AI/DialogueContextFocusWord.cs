// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    public struct DialogueContextFocusWord
    {
        public readonly AssistantAI.SupportedLanguage TargetLanguage;
        public readonly string Word;
        public readonly string[] RelatedWords;
        public readonly float SuccessRate;

        public DialogueContextFocusWord(AssistantAI.SupportedLanguage targetLanguage, string word, string[] relatedWords, float successRate)
        {
            TargetLanguage = targetLanguage;
            Word = word;
            RelatedWords = relatedWords;
            SuccessRate = successRate;
        }
    }
}