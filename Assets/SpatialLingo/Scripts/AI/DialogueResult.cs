// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    public struct DialogueResult
    {
        public string ContextID;
        public AssistantAI.SupportedLanguage TargetLanguage { get; }
        public string Dialogue { get; }

        public DialogueResult(AssistantAI.SupportedLanguage targetLanguage, string dialogue, string contextID)
        {
            TargetLanguage = targetLanguage;
            Dialogue = dialogue;
            ContextID = contextID;
        }
    }
}