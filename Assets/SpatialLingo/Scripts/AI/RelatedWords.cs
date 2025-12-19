// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    public struct RelatedWords
    {
        public string Word;
        public string[] Adjectives;
        public string[] Nouns;
        public string[] Verbs;

        public RelatedWords(string word, string[] adjectives, string[] nouns, string[] verbs)
        {
            Word = word;
            Adjectives = adjectives;
            Nouns = nouns;
            Verbs = verbs;
        }
    }
}