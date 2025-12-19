// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SpatialLingo.SpeechAndText
{
    /// <summary>
    /// Emits an event when any keywords are found in a speech to text stream.
    /// </summary>
    public class STTTargetListener : MonoBehaviour
    {
        public UnityAction<string> TargetWordMatched;
        public List<string> TargetKeywords = new();
        private string m_lastTranscription = string.Empty;

        public void Reset()
        {
            m_lastTranscription = string.Empty;
        }

        public void OnTranscriptionUpdated(string transcription)
        {
            var newTranscriptionText = RemoveCommonWords(m_lastTranscription, transcription);
            var newTranscriptionWords = newTranscriptionText.Split(' ');
            foreach (var newWord in newTranscriptionWords)
            {
                var strippedWord = new string(newWord.Trim().Where(c => !char.IsPunctuation(c)).ToArray());
                if (TargetKeywords.Contains(strippedWord, StringComparer.OrdinalIgnoreCase))
                {
                    TargetWordMatched?.Invoke(strippedWord);
                }
            }
        }

        /// Because transcriptions may change according to context ("one two three" may later become "123",
        /// "farm" can be transcribed before becoming "farmhand"), each word in an updated transcription needs to be evaluated to
        /// see if anything has changed.
        private string RemoveCommonWords(string oldString, string newString)
        {
            var wordsOld = oldString.Split(' ');
            var wordsNew = newString.Split(' ');
            int i;
            for (i = 0; i < wordsOld.Length; i++)
            {
                if (!string.Equals(wordsOld[i], wordsNew[i], StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            return string.Join(' ', wordsNew, i, wordsNew.Length - i);
        }
    }
}