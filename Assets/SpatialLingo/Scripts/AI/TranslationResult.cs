// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    public struct TranslationResult
    {
        public string ContextID;
        public AssistantAI.SupportedLanguage LanguageFrom { get; }
        public AssistantAI.SupportedLanguage LanguageTo { get; }
        public string StatementFrom { get; }
        public string StatementTo { get; }
        public TranslationResult(AssistantAI.SupportedLanguage languageFrom, AssistantAI.SupportedLanguage languageTo, string statementFrom, string statementTo, string contextID)
        {
            LanguageFrom = languageFrom;
            LanguageTo = languageTo;
            StatementFrom = statementFrom;
            StatementTo = statementTo;
            ContextID = contextID;
        }
    }
}