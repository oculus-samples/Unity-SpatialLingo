// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.Samples;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    [Serializable]
    public class WordCloudData
    {
        public WordCloudLanguageEntry[] Wordclouds;

        public WordCloudLanguageEntry WordCloudForLanguage(AssistantAI.SupportedLanguage language)
        {
            var languageName = AssistantAI.SupportedLanguageEnumToEnglishName(language);
            foreach (var cloud in Wordclouds)
            {
                if (cloud.Language == languageName)
                {
                    return cloud;
                }
            }

            return null;
        }
    }
}