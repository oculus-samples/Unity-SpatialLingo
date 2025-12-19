// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.SpeechAndText;
using Random = UnityEngine.Random;

namespace SpatialLingo.Lessons
{
    /// <summary>
    /// Lookup Table for various narrator sayings
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class Tutorial
    {
        private const string TARGET_LANGUAGE_STRING_KEY = "[Target Language]";
        private const string TARGET_WORD_STRING_KEY = "[Target Word]";
        private const string TARGET_PHRASE_STRING_KEY = "[Target Phrase]";

        private static string[] s_beckonList =
        {
            "Hey! Come help me outta here!",
            "Can you help me? I'm stuck",
            "Come grab my flipper!"
        };

        private static string[] s_languageSelectList =
        {
            "Learning new languages is fun! Pick a language to plant the seed.",
            "Learning new languages is fun! Choose the language you want to practice.",
        };

        private static string[] s_trackingList =
        {
            "Look carefully at your favorite things nearby to create lessons.",
            "See if you can create more lessons around the room by looking around your space.",
            "Look at interesting items around your room to create lessons.",
            "Let's look for more words to practice, by slowly looking around your space.",
            "There's gotta be some more lessons around here, look around your room carefully.",
            "Look around slowly to find more words to practice.",
        };

        private static string[] s_lessonWaitingList =
        {
            "There are words all around the room, approach a berry to start a lesson",
            "Follow me over here! Try this lesson.",
            "Hey! Come check out this berry I found!",
            "There's a language lesson over here, come give it a try!",
            "Hey! Over here!",
            "Come take a look at this word I found!",
        };

        private static string[] s_lessonSuccessList =
        {
            "You got it!",
            "Good job!",
            "Nice work!",
            "Excellent!",
            "Wonderful!",
            "Perfect!",
            "Amazing!",
            "That was great!",
            "Nicely done!",
            "Success!",
            "Great!",
            "Good work!",
            "Nice job!",
        };

        private static string[] s_lessonFailureList =
        {
            "That's not quite right",
            "Let's try that again",
            "Give that another shot",
            "Maybe I didn't hear you right, try again?",
        };

        private static string[] s_lessonExitList =
        {
            "Oh, you want to try something else?",
            "You don't want to do this lesson right now?",
            "Okay, let's find a different lesson to try",
            "There are more lessons around here, let's do one of those",
            "This lesson isn't finished, but there are more around here!",
            "I'm sure we can find a different lesson to try",
        };

        private static string[] s_goldenBerryWaitingList =
        {
            "This berry is golden! It must be special.",
            "Come give this berry a poke.",
            "You can touch this golden berry over here!",
            "This golden berry really wants to be poked."
        };

        private static string[] s_treeGrowList =
        {
            "Wow! Your language skills are growing!",
            "The language tree is getting bigger!",
            "Wow! Look at the language tree grow!",
            "The tree is growing!",
            "Grow language tree! Grow!"
        };

        private static string[] s_completeGameList =
        {
            "We did it! The language tree is fully grown",
            "Look at this beautiful language tree!",
            "The language tree is so big now! Thanks for your help!"
        };

        private static string[] s_waitRestartList =
        {
            "Do you want to start over? Or grow another language",
            "How about we grow another language tree!?",
            "Look at these other language trees we can grow",
            "Let's try another language, or we can regrow this one"
        };

        private static string RandomStringFromList(string[] list)
        {
            return list.Length == 0 ? null : list[Random.Range(0, list.Length)];
        }

        private static string FilterLanguageWord(string value, string targetLanguageName)
        {
            return value.Replace(TARGET_LANGUAGE_STRING_KEY, targetLanguageName);
        }

        private static string FilterDynamicWord(string value, string targetWord)
        {
            return value.Replace(TARGET_WORD_STRING_KEY, targetWord);
        }

        private static string FilterDynamicPhrase(string value, string targetPhrase)
        {
            return value.Replace(TARGET_PHRASE_STRING_KEY, targetPhrase);
        }

        public static string BeckonPhrase()
        {
            return RandomStringFromList(s_beckonList);
        }

        public static string ThanksPhrase()
        {
            return "Thanks so much!";
        }

        public static string LanguageSelectWaitPhrase()
        {
            return RandomStringFromList(s_languageSelectList);
        }

        public static string LanguageSelectComplete()
        {
            return "Nice choice!";
        }

        public static string GreetingPhrase()
        {
            return "Hi! My name is <emphasis level=\"strong\">Golly Gosh!</emphasis>";
        }

        public static string PresentSeedPhrase()
        {
            return "I found this language seed in the ground";
        }

        public static string SeedWaitPlantPhrase()
        {
            return "Can you help me plant it so it can grow big and strong?";
        }

        public static string SeedWaitDistancePhrase()
        {
            return "Hey! Come back and help me plant this seed!";
        }

        public static string PlantCompletePhrase()
        {
            return "Look the language tree is sprouting!";
        }

        public static string StartGameplayPhrase(string targetLanguageName)
        {
            var value = "Let's speak some [Target Language] to help it grow";
            return value.Replace(TARGET_LANGUAGE_STRING_KEY, targetLanguageName);
        }

        public static string TrackingWaitingPhrase()
        {
            return RandomStringFromList(s_trackingList);
        }

        public static string LessonWaitingPhrase(string targetLanguageName)
        {
            var value = RandomStringFromList(s_lessonWaitingList);
            return value.Replace(TARGET_LANGUAGE_STRING_KEY, targetLanguageName);
        }

        public static string CompleteLessonPhrase()
        {
            return RandomStringFromList(s_lessonSuccessList);
        }

        public static string IncompleteLessonPhrase()
        {
            return RandomStringFromList(s_lessonFailureList);
        }

        public static string ExitLessonPhrase()
        {
            return RandomStringFromList(s_lessonExitList);
        }

        // -------------------------------------------------------------------------- Tier 1
        public static string Tier1TutorialA(string targetLanguageName)
        {
            return FilterLanguageWord("Say this word in [Target Language]", targetLanguageName);
        }
        public static string Tier1TutorialB(string targetLanguageName)
        {
            return FilterLanguageWord("Can you say this word?", targetLanguageName);
        }
        public static string Tier1TutorialC(string targetLanguageName)
        {
            return FilterLanguageWord("Say this in [Target Language]", targetLanguageName);
        }
        public static string Tier1TutorialD(string targetLanguageName)
        {
            return FilterLanguageWord("Say this!", targetLanguageName);
        }
        public static string Tier1TutorialE(string targetNoun)
        {
            return FilterDynamicWord($"Repeat after me: [Target Word] {TargetLanguageHint()} ", targetNoun);
        }

        // -------------------------------------------------------------------------- Tier 2
        public static string Tier2TutorialA(string targetLanguageName)
        {
            return FilterLanguageWord("Describe this object in: [Target Language]", targetLanguageName);
        }
        public static string Tier2TutorialB()
        {
            return "Use one of these adjectives to describe this object";
        }
        public static string Tier2TutorialC()
        {
            return "For example, describe the object using this word";
        }
        public static string Tier2TutorialD()
        {
            return "Say this adjective along with the noun";
        }
        public static string Tier2TutorialE(string targetPhrase)
        {
            return $"Repeat after me, {TargetLanguageHint()} {targetPhrase}";
        }

        // -------------------------------------------------------------------------- Tier 3
        public static string Tier3TutorialA(string targetLanguageName)
        {
            return FilterLanguageWord("Form a sentence in [Target Language] using these words.", targetLanguageName);
        }
        public static string Tier3TutorialB()
        {
            return "Can you make a sentence about this object using an adjective and a verb?";
        }
        public static string Tier3TutorialC()
        {
            return "Make a sentence using this verb, a noun, and an adjective!";
        }
        public static string Tier3TutorialD()
        {
            return "Use a verb like this one, plus a noun and adjective to make a full sentence!";
        }
        public static string Tier3TutorialE(string targetPhrase)
        {
            return FilterDynamicPhrase($"Repeat after me: [Target Phrase] {TargetLanguageHint()}", targetPhrase);
        }

        public static string TargetLanguageHint()
        {
            return VoiceSpeaker.LanguageTextHintString(Language.Language.AssistantAIToWitaiLanguage(AppSessionData.TargetLanguageAI));
        }

        // -------------------------------------------------------------------------- Golden Berry
        public static string WaitGoldenBerry()
        {
            var value = RandomStringFromList(s_goldenBerryWaitingList);
            return value;
        }

        public static string ReactToTreeGrow()
        {
            var value = RandomStringFromList(s_treeGrowList);
            return value;
        }

        public static string ReactToAllTiersComplete()
        {
            var value = RandomStringFromList(s_completeGameList);
            return value;
        }

        public static string WaitGameRestart()
        {
            var value = RandomStringFromList(s_waitRestartList);
            return value;
        }
    }
}