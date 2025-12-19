// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    public struct DialogueContextExploreRoom
    {
        public AssistantAI.SupportedLanguage TargetLanguage; // language response should be in
        public string SceneDescription; // image understanding description of the world
        public DialogueContextExploreRoom(AssistantAI.SupportedLanguage targetLanguage, string sceneDescription)
        {
            TargetLanguage = targetLanguage;
            SceneDescription = sceneDescription;
        }
    }
}