// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using SpatialLingo.AI;
using SpatialLingo.AppSystems;
using SpatialLingo.SpeechAndText;

namespace SpatialLingo.Language
{
    [MetaCodeSample("SpatialLingo")]
    public static class Language
    {
        public static AssistantAI.SupportedLanguage AppSessionToAssistantLanguage(AppSessionData.Language language)
        {
            return language switch
            {
                AppSessionData.Language.English => AssistantAI.SupportedLanguage.English,
                AppSessionData.Language.Spanish => AssistantAI.SupportedLanguage.Spanish,
                _ => AssistantAI.SupportedLanguage.English,
            };
        }

        public static WitaiSettingsHolder.Language AppSessionToWitaiLanguage(AppSessionData.Language language)
        {
            return language switch
            {
                AppSessionData.Language.English => WitaiSettingsHolder.Language.English,
                AppSessionData.Language.Spanish => WitaiSettingsHolder.Language.Spanish,
                _ => WitaiSettingsHolder.Language.English,
            };
        }

        public static WitaiSettingsHolder.Language AssistantAIToWitaiLanguage(AssistantAI.SupportedLanguage language)
        {
            return language switch
            {
                AssistantAI.SupportedLanguage.English => WitaiSettingsHolder.Language.English,
                AssistantAI.SupportedLanguage.Spanish => WitaiSettingsHolder.Language.Spanish,
                _ => WitaiSettingsHolder.Language.English,
            };
        }
    }
}