// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.Samples;

namespace SpatialLingo.AI
{
    [MetaCodeSample("SpatialLingo")]
    [Serializable]
    public class WordCloudPhraseData
    {
        public string Phrase0;
        public string Phrase1;
        public string Phrase2;

        public WordCloudPhraseData(string phrase0, string phrase1, string phrase2)
        {
            Phrase0 = phrase0;
            Phrase1 = phrase1;
            Phrase2 = phrase2;
        }
    }
}