// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Meta.WitAi.Data.Configuration;
using UnityEngine;

namespace SpatialLingo.SpeechAndText
{
    /// <summary>
    /// Provides access to multiple wit.ai configurations by language.
    /// </summary>
    public class WitaiSettingsHolder : ScriptableObject
    {
        public enum Language
        {
            English,
            Spanish
        }

        [SerializeField] private List<LanguageSettings> m_languageSettings;
        private Dictionary<Language, WitConfiguration> m_langSettingsDictionary;

        /// <summary>
        /// Gets wit.at settings for a particular language.
        /// </summary>
        /// <param name="language">Language to fetch settings for.</param>
        /// <returns>Matching WitConfiguration.</returns>
        public WitConfiguration GetSettingsForLanguage(Language language)
        {
            m_langSettingsDictionary ??= SerializeDictionary();
            return m_langSettingsDictionary[language];
        }

        private Dictionary<Language, WitConfiguration> SerializeDictionary()
        {
            Dictionary<Language, WitConfiguration> settingsDictionary = new();
            m_languageSettings.ForEach(settings => settingsDictionary.Add(settings.Language, settings.WitConfiguration));
            return settingsDictionary;
        }

        [Serializable]
        private struct LanguageSettings
        {
            public WitConfiguration WitConfiguration;
            public Language Language;
        }
    }
}