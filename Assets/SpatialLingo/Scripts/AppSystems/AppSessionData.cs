// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using System.Text;
using Meta.XR.Samples;
using SpatialLingo.AI;
using Unity.VisualScripting;
using LanguageHelper = SpatialLingo.Language.Language;

namespace SpatialLingo.AppSystems
{
    [MetaCodeSample("SpatialLingo")]
    [IncludeInSettings(true)]
    public static class AppSessionData
    {
        public enum Language
        {
            English,
            Spanish
        }

        public static int Tier { get; set; }

        public static List<string> CompletedLessonObjects { get; set; } = new();

        public static int CompletedLessonsInTier => CompletedLessonObjects.Count;

        public static bool ContentConsentSeen { get; set; }

        public static bool ContentConsentGranted { get; set; }

        public static Language TargetLanguage { get; set; }

        public static Language UserLanguage { get; set; }

        public static AssistantAI.SupportedLanguage UserLanguageAI => LanguageHelper.AppSessionToAssistantLanguage(UserLanguage);
        public static AssistantAI.SupportedLanguage TargetLanguageAI => LanguageHelper.AppSessionToAssistantLanguage(TargetLanguage);
        public static string TargetLanguageName => AssistantAI.SupportedLanguageEnumToEnglishName(TargetLanguageAI);

        public static void Reset()
        {
            Tier = 0;
            CompletedLessonObjects.Clear();
            TargetLanguage = Language.English;
            UserLanguage = Language.English;
        }


        public static void AddCompletedLesson(string classification)
        {
            // Add only unique entries
            if (!CompletedLessonObjects.Contains(classification))
            {
                CompletedLessonObjects.Add(classification);
            }
        }

        public static bool HasLessonClassificationBeenCompleted(string classification)
        {
            return CompletedLessonObjects.Contains(classification);
        }

        public static string GetDebugString()
        {
            StringBuilder debugString = new();

            _ = debugString.AppendLine("Session data");
            _ = debugString.AppendLine($"Tier: {Tier}");
            _ = debugString.AppendLine($"CompletedLessonObjects: {string.Join(" ", CompletedLessonObjects)}");
            _ = debugString.AppendLine($"CompletedLessonsInTier: {CompletedLessonsInTier}");
            _ = debugString.AppendLine($"ContentConsentSeen: {ContentConsentSeen}");
            _ = debugString.AppendLine($"ContentConsentGranted: {ContentConsentGranted}");
            _ = debugString.AppendLine($"TargetLanguage: {TargetLanguage}");

            return debugString.ToString();
        }

    }
}