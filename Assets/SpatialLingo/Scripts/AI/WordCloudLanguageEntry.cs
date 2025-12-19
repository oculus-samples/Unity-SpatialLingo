// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.Samples;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    [Serializable]
    public class WordCloudLanguageEntry
    {
        public string Language;
        public string[] Adjectives;
        public string[] Verbs;
        public string Word;

        public WordCloudLanguageEntry(string language, string word, string[] adjectives, string[] verbs)
        {
            Language = language;
            Word = word;
            Adjectives = adjectives;
            Verbs = verbs;
        }
    }
}