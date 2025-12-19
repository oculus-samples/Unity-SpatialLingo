// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.Utilities.CameraTaxonTracking;
using Meta.Utilities.ObjectClassifier;
using Meta.XR.Samples;
using SpatialLingo.AI;
using SpatialLingo.AppSystems;
using UnityEngine;

namespace SpatialLingo.Lessons
{
    [MetaCodeSample("SpatialLingo")]
    public class LessonUpdateResult
    {
        public Lesson Lesson { get; }

        public LessonUpdateResult(Lesson lesson) => Lesson = lesson;
    }

    [MetaCodeSample("SpatialLingo")]
    public class LessonsManager : IDisposable
    {
        public enum RequestStatus
        {
            Unknown,
            FailWifi,
            FailServer,
        }

        [MetaCodeSample("SpatialLingo")]
        public class RequestStatusResult
        {
            public RequestStatus Status { get; }

            public RequestStatusResult(RequestStatus status) => Status = status;
        }

        private const int RELATED_WORD_PARTS_OF_SPEECH_COUNT = 3;
        private const int WAIT_TIME_BETWEEN_REQUESTS_MS = 100;
        private const int ENDPOINT_MAX_RETRY_COUNT = 5;

        private CameraTaxonTracker m_taxonTracker;
        private AssistantAI m_assistant;

        private AssistantAI.SupportedLanguage m_userLanguage;
        private AssistantAI.SupportedLanguage m_targetLanguage;

        public delegate void LessonUpdatedEvent(LessonUpdateResult result);
        public event LessonUpdatedEvent LessonAdded; // When a lesson and underlaying activity are ready to be used
        public event LessonUpdatedEvent LessonUpdated; // When a lesson is updated (tracking only)
        public event LessonUpdatedEvent LessonRemoved; // When a lesson is removed, or underlaying activity is no longer ready

        public delegate void LessonTrackingEvent();
        public event LessonTrackingEvent LessonTrackingChanged; // Any time a lesson's tracking or activity status changes

        public delegate void ActivityAcquisitionEvent(RequestStatusResult result);
        public event ActivityAcquisitionEvent ActivityAcquisitionFailed;

        private Dictionary<CameraTrackedTaxon, Lesson> m_lessons = new();
        // Non-generic lessons need to map 1:1 to a lesson
        private Dictionary<Lesson, Activity> m_activities = new();

        private bool m_isProcessingActivity;
        private string m_processingRequestID;
        private Activity m_processingActivity;

        private bool m_didRequestResetActivities = false;
        private bool m_isTaxonTracking = false;
        private FaceDetection m_faceBlur;

        // Start is called once before the first execution of Update after the MonoBehaviour is created

        public LessonsManager(CameraTaxonTracker tracker, AssistantAI assistant, AssistantAI.SupportedLanguage userLanguage, AssistantAI.SupportedLanguage targetLanguage, FaceDetection faceBlur)
        {
            m_taxonTracker = tracker;
            m_assistant = assistant;
            m_userLanguage = userLanguage;
            m_targetLanguage = targetLanguage;
            m_faceBlur = faceBlur;

            m_taxonTracker.TaxonAdded += OnTaxonAddedEvent;
            m_taxonTracker.TaxonUpdated += OnTaxonUpdatedEvent;
            m_taxonTracker.TaxonRemoved += OnTaxonRemovedEvent;

            StartTracking();

        }

        public void SetLayersPerFrame(int layersPerFrame)
        {
            m_taxonTracker.SetLayersPerFrame(layersPerFrame);
        }

        public void SetTargetLanguage(AssistantAI.SupportedLanguage targetLanguage)
        {
            if (m_targetLanguage != targetLanguage)
            {
                m_targetLanguage = targetLanguage;
                if (m_isProcessingActivity)
                {
                    m_didRequestResetActivities = true;
                    ResetActivities();
                }
                else
                {
                    ResetActivities();
                    CheckGetActivityComponents();
                }
            }
        }

        public void Dispose()
        {
            if (m_taxonTracker != null)
            {
                m_taxonTracker.TaxonAdded -= OnTaxonAddedEvent;
                m_taxonTracker.TaxonUpdated -= OnTaxonUpdatedEvent;
                m_taxonTracker.TaxonRemoved -= OnTaxonRemovedEvent;
                m_taxonTracker = null;
            }
        }

        private void StartTracking()
        {
            if (m_isTaxonTracking)
            {
                return;
            }

            m_isTaxonTracking = true;
            CheckRunTaxonTracking();
        }

        private async void CheckRunTaxonTracking()
        {
            while (m_isTaxonTracking)
            {
                if (m_taxonTracker.IsIdle && InferenceEngineUtilities.IsLoaded)
                {
                    m_taxonTracker.StartPolling();
                }
                else
                {
                    await Awaitable.WaitForSecondsAsync(0.1f);
                }
            }
        }

        private void StopTracking()
        {
            m_isTaxonTracking = false;
        }

        // Tracking results
        private void OnTaxonAddedEvent(CameraTaxonTracker.TaxonUpdateResult result)
        {
            var taxon = result.Taxon;
            var classification = taxon.Taxa;

            // Activities are unique to locations
            // Want to be able to choose a best example image for activity, not just the first one available
            var context = taxon.ImageContext;
            var image = context.Image;
            var imageSize = context.Size;
            var activity = new Activity
            {
                Classification = classification,
                ContextImage = image,
                ContextImageExtent = imageSize
            };

            var lesson = new Lesson(activity, taxon);
            m_activities.Add(lesson, activity);
            m_lessons.Add(taxon, lesson);

            // Lesson may already be ready
            if (lesson.IsReady)
            {
                LessonAdded?.Invoke(new LessonUpdateResult(lesson));
                LessonTrackingChanged?.Invoke();
            }

            CheckGetActivityComponents();
        }

        private void OnTaxonUpdatedEvent(CameraTaxonTracker.TaxonUpdateResult result)
        {
            var taxon = result.Taxon;
            if (m_lessons.ContainsKey(taxon))
            {
                var lesson = m_lessons[taxon];
                LessonUpdated?.Invoke(new LessonUpdateResult(lesson));
                LessonTrackingChanged?.Invoke();
            }
            else
            {
                Debug.LogWarning("OnTaxonUpdatedEvent missing taxon key");
            }
        }

        private void OnTaxonRemovedEvent(CameraTaxonTracker.TaxonUpdateResult result)
        {
            var taxon = result.Taxon;
            // Remove lesson
            if (m_lessons.ContainsKey(taxon))
            {
                var lesson = m_lessons[taxon];

                // Remove activity
                if (m_activities.ContainsKey(lesson))
                {
                    var activity = m_activities[lesson];
                    _ = m_activities.Remove(lesson);

                    if (activity == m_processingActivity)
                    {
                        Debug.LogWarning($"LessonsManager - OnTaxonRemovedEvent - removing same activity that is in process: {m_processingActivity}");
                    }
                    else
                    {
                        activity.Clear();
                    }
                }
                else
                {
                    Debug.LogWarning("OnTaxonRemovedEvent missing lesson key");
                }

                _ = m_lessons.Remove(taxon);
                LessonRemoved?.Invoke(new LessonUpdateResult(lesson));
                LessonTrackingChanged?.Invoke();
            }
            else
            {
                Debug.LogWarning("OnTaxonRemovedEvent missing taxon key");
            }
        }

        /// <summary>
        /// Get the top K (ready) lessons at least N meters away from each other in the xz-plane
        /// Optionally allow lessons with same classification type
        /// </summary>
        /// <returns></returns>
        public Lesson[] NextLessonsInDistancePlaneXZ(List<Lesson> existingLessons, int kLessons, float minimumDistanceRestriction)
        {
            if (kLessons == 0)
            {
                return new Lesson[] { };
            }

            var startExistingCount = existingLessons.Count;
            var chosenList = new List<Lesson>();
            foreach (var lesson in existingLessons)
            {
                chosenList.Add(lesson);
            }

            var totalLessonsNeeded = startExistingCount + kLessons;

            var eligibleList = new List<(Lesson, float)>();

            // Sorted list based on score
            foreach (var pair in m_lessons)
            {
                var lesson = pair.Value;
                if (lesson.IsReady)
                {
                    var score = lesson.Confidence;
                    eligibleList.Add((lesson, score));
                }
            }

            var comparer = new MyTupleComparer();
            eligibleList.Sort(comparer);

            // Map to complete vs incomplete lessions, for primary and secondary searches
            var incompletedList = new List<Lesson>();
            var completedList = new List<Lesson>();
            var skippedConfidenceLow = 0;
            foreach (var option in eligibleList)
            {
                var lesson = option.Item1;
                // Do not include lessons with low confidence
                if (lesson.Confidence <= 0)
                {
                    skippedConfidenceLow += 1;
                    continue;
                }
                if (AppSessionData.HasLessonClassificationBeenCompleted(lesson.Classification))
                {
                    completedList.Add(lesson);
                }
                else
                {
                    incompletedList.Add(lesson);
                }
            }

            // Randomize the starting item in the list, if not already present
            if (chosenList.Count == 8)
            {
                if (incompletedList.Count > 0)
                {
                    var randomIndex = UnityEngine.Random.Range(0, incompletedList.Count);
                    var randomLesson = incompletedList[randomIndex];
                    chosenList.Add(randomLesson);
                    _ = incompletedList.Remove(randomLesson);
                }
                else if (completedList.Count > 0)
                {
                    var randomIndex = UnityEngine.Random.Range(0, completedList.Count);
                    var randomLesson = completedList[randomIndex];
                    chosenList.Add(randomLesson);
                    _ = completedList.Remove(randomLesson);
                }
            }

            while (chosenList.Count < totalLessonsNeeded)
            {
                // Try to get an incomplete lesson first
                var furthestLesson = FindFurthestLesson(chosenList, incompletedList, minimumDistanceRestriction);
                // Otherwise try to get a complete lesson
                if (furthestLesson == null)
                {
                    furthestLesson = FindFurthestLesson(chosenList, completedList, minimumDistanceRestriction);
                    if (furthestLesson == null)
                    {
                        // No more lessons satisfy constraints
                        break;
                    }
                }
                // Add lesson to chosen list, remove from putative lists
                chosenList.Add(furthestLesson);
                _ = completedList.Remove(furthestLesson);
                _ = incompletedList.Remove(furthestLesson);
            }

            // Remove original lessons from beginning
            for (var i = 0; i < startExistingCount; ++i)
            {
                chosenList.RemoveAt(0);
            }

            return chosenList.ToArray();
        }

        private Lesson FindFurthestLesson(List<Lesson> existing, List<Lesson> potential, float minimumDistanceRestriction)
        {
            // No searching necessary if only a single or no entry
            if (existing.Count == 0)
            {
                return potential.Count == 0 ? null : potential[0];
            }

            Lesson closestLesson = null;
            var closestDistanceMax = 0.0f;

            // Find the next lesson furthest away from all other lessons
            foreach (var lessonPotential in potential)
            {
                var minDistance = -1.0f;
                var shouldSkip = false;
                foreach (var lessonExisting in existing)
                {
                    // No repeated types
                    if (lessonPotential.Classification == lessonExisting.Classification)
                    {
                        shouldSkip = true;
                        break;
                    }
                    // Keep track of closest location
                    var distance = Vector3.Distance(lessonPotential.Position, lessonExisting.Position);
                    minDistance = minDistance < 0 ? distance : Mathf.Min(minDistance, distance);
                }

                if (!shouldSkip && minDistance > closestDistanceMax)
                {
                    closestLesson = lessonPotential;
                    closestDistanceMax = minDistance;
                }
            }
            // Enforce minimum distance restriction
            return closestDistanceMax < minimumDistanceRestriction ? null : closestLesson;
        }

        private class MyTupleComparer : IComparer<(Lesson, float)>
        {
            public int Compare((Lesson, float) x, (Lesson, float) y)
            {
                if (x.Item2 < y.Item2)
                {
                    return 1;
                }
                if (x.Item2 > y.Item2)
                {
                    return -1;
                }
                return 0;
            }
        }

        public Lesson[] ReadyLessons()
        {
            var ready = new List<Lesson>();
            foreach (var pair in m_lessons)
            {
                var lesson = pair.Value;
                if (lesson.IsReady)
                {
                    ready.Add(lesson);
                }
            }
            return ready.ToArray();
        }

        public Lesson[] WaitingLessons()
        {
            var waiting = new List<Lesson>();
            foreach (var pair in m_lessons)
            {
                var lesson = pair.Value;
                if (!lesson.IsReady)
                {
                    waiting.Add(lesson);
                }
            }
            return waiting.ToArray();
        }

        public Activity[] Activities()
        {
            var list = new List<Activity>();
            foreach (var pair in m_activities)
            {
                var activity = pair.Value;
                list.Add(activity);
            }
            return list.ToArray();
        }

        public void AcquireActivityComponents()
        {
            CheckGetActivityComponents();
        }

        private void CheckGetActivityComponents()
        {
            if (m_isProcessingActivity)
            {
                return;
            }

            m_isProcessingActivity = true;
            Activity nextActivity = null;
            var bestActivityScore = -1.0f;

            foreach (var pair in m_activities)
            {
                var activity = pair.Value;

                // Get all the next best activities to start loading assets for
                // Could further adjust score for confidence and distance from user
                if (!activity.IsReady)
                {
                    var score = 0.0f;
                    if (!activity.HasCompleted)
                    {
                        score += 1.0f;
                    }

                    if (score > bestActivityScore)
                    {
                        bestActivityScore = score;
                        nextActivity = activity;
                    }
                }
            }

            if (nextActivity != null)
            {
                if (m_didRequestResetActivities)
                {
                    m_didRequestResetActivities = false;
                    ResetActivities();
                    ResetCheckActivityComponents();
                    CheckGetActivityComponents();
                }
                else
                {
                    GetActivityComponents(nextActivity);
                }
            }
            else
            {
                m_isProcessingActivity = false;
            }
        }

        private void ResetActivities()
        {
            var previousLessons = new List<Lesson>();
            foreach (var activityPair in m_activities)
            {
                var activity = activityPair.Value;
                if (!activity.IsReady)
                {
                    // Was not ready, don't need to invalidate
                    continue;
                }
                // Collect invalidated lessons
                foreach (var lessonPair in m_lessons)
                {
                    var lesson = lessonPair.Value;
                    if (lesson.Activity == m_processingActivity)
                    {
                        previousLessons.Add(lesson);
                    }
                }
                activity.Reset();
                foreach (var lesson in previousLessons)
                {
                    LessonRemoved?.Invoke(new LessonUpdateResult(lesson));
                    LessonTrackingChanged?.Invoke();
                }
                previousLessons.Clear();
            }
        }

        private string ActivitySummary()
        {
            var totalActivities = m_activities.Count;
            var readyActivities = 0;
            var activityClasses = new List<string>();
            foreach (var activityPair in m_activities)
            {
                var activity = activityPair.Value;
                if (activity.IsReady)
                {
                    readyActivities++;
                }
                activityClasses.Add(activity.Classification);
            }

            var totalLessons = m_lessons.Count;
            var readyLessons = 0;
            foreach (var lessonPair in m_lessons)
            {
                var lesson = lessonPair.Value;
                if (lesson.Activity.IsReady)
                {
                    readyLessons++;
                }
            }
            var classList = string.Join(",", activityClasses.ToArray());
            return $"LessonsManager - Activity Summary:\nLessons: {readyLessons}/{totalLessons}\nActivities: {readyActivities}/{totalActivities} - classifications:{classList}";
        }

        private void ProcessActivityComplete()
        {
            if (m_didRequestResetActivities)
            {
                m_processingActivity.Reset();
            }
            else if (!m_processingActivity.IsValidForUse())
            {
                m_processingActivity.Reset();
            }
            else if (m_processingActivity != null)
            {
                // Completed loading atomic assets
                m_processingActivity.IsAcquisitionComplete = true;
                foreach (var pair in m_lessons)
                {
                    var lesson = pair.Value;
                    if (lesson.Activity == m_processingActivity)
                    {
                        LessonAdded?.Invoke(new LessonUpdateResult(lesson));
                        LessonTrackingChanged?.Invoke();
                    }
                }
            }

            ResetCheckActivityComponents();
            CheckGetActivityComponents();
        }
        private void ResetCheckActivityComponents()
        {
            m_processingActivity = null;
            m_processingRequestID = null;
            m_isProcessingActivity = false;
        }

        /// <summary>
        /// Retry the task related to Activity Acquisition after a fail 
        /// If retry limit is reached, reset acquisition states and notify listeners
        /// Error could be underlying endpoint or WIFI in general 
        /// </summary>
        /// <param name="task">Task to perform</param>
        /// <returns>true if success, false otherwise</returns>
        private async Awaitable<bool> RetryActivitySubTaskForSuccess(Func<Awaitable<bool>> task)
        {
            var endpointRetryAttempts = 0;
            while (endpointRetryAttempts < ENDPOINT_MAX_RETRY_COUNT)
            {
                var resultSuccess = await ExecuteTask();
                if (!resultSuccess)
                {
                    Debug.LogWarning($"LessonsManager - RetryActivitySubTaskForSuccess unsuccessful task, will attempt retry:{endpointRetryAttempts}");
                    await Task.Delay(WAIT_TIME_BETWEEN_REQUESTS_MS);
                    ++endpointRetryAttempts;
                    continue;
                }

                return true;
            }

            Debug.LogWarning("LessonsManager - failed subtask, resetting activity");
            if (m_processingActivity != null)
            {
                m_processingActivity.Reset();
            }
            ResetCheckActivityComponents();
            CallbackActivityFail();
            return false;

            async Awaitable<bool> ExecuteTask()
            {
                try
                {
                    return await task();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return false;
                }
            }
        }

        private void CallbackActivityFail()
        {
            RequestStatusResult result;
            var reachability = Application.internetReachability;
            result = reachability == NetworkReachability.NotReachable
                ? new RequestStatusResult(RequestStatus.FailWifi)
                : new RequestStatusResult(RequestStatus.FailServer);
            ActivityAcquisitionFailed?.Invoke(result);
        }

        private async Awaitable<bool> ActivitySubTaskGetUserWord()
        {
            var translationResult = await GetActivityUserWord();
            var value = translationResult.StatementTo;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            m_processingActivity.UserLanguageWord = value;

            return true;
        }

        private async Awaitable<bool> ActivitySubTaskGetTargetWord()
        {
            var translationResult = await GetActivityTargetWord();
            var value = translationResult.StatementTo;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            m_processingActivity.TargetLanguageWord = value;

            return true;
        }

        private async Awaitable<bool> ActivitySubTaskGetPartsOfSpeech()
        {
            var relatedResult = await GetActivityRelatedWords();
            var adjectives = relatedResult.Adjectives;
            var verbs = relatedResult.Verbs;
            if (adjectives == null || verbs == null || adjectives.Length < RELATED_WORD_PARTS_OF_SPEECH_COUNT || verbs.Length < RELATED_WORD_PARTS_OF_SPEECH_COUNT)
            {
                return false;
            }
            m_processingActivity.AdjectivesTargetLanguage = adjectives;
            m_processingActivity.VerbsTargetLanguage = verbs;

            return true;
        }

        private async Awaitable<bool> ActivitySubTaskUserLanguageGetPartsOfSpeech()
        {
            if (m_processingActivity.AdjectivesTargetLanguage == null || m_processingActivity.VerbsTargetLanguage == null)
            {
                Debug.LogError("Null words, arrays were not populated. Check Llama API");
                return false;
            }

            // Marshall the list of words
            List<string> wordList = new();
            foreach (var adj in m_processingActivity.AdjectivesTargetLanguage)
            {
                wordList.Add(adj);
            }
            foreach (var verb in m_processingActivity.VerbsTargetLanguage)
            {
                wordList.Add(verb);
            }

            // Get direct translations
            var result = await m_assistant.TranslateWordList(m_targetLanguage, m_userLanguage, wordList.ToArray(), m_processingRequestID);
            var wordsTo = result.WordsTo;

            if (wordsTo == null || wordsTo.Length == 0 || wordsTo.Length != wordList.Count)
            {
                Debug.LogError("Empty or mismatch word counts");
                return false;
            }

            // Destinations:
            var resultAdjectives = new string[m_processingActivity.AdjectivesTargetLanguage.Length];
            var resultVerbs = new string[m_processingActivity.VerbsTargetLanguage.Length];

            for (var i = 0; i < wordsTo.Length; i++)
            {
                var wordTo = wordsTo[i];
                var index = i;
                if (index >= resultAdjectives.Length)
                {
                    index -= resultAdjectives.Length;
                    if (index >= resultVerbs.Length)
                    {
                        _ = resultVerbs.Length;
                        // N/A
                    }
                    else
                    {
                        resultVerbs[index] = wordTo;
                    }
                }
                else
                {
                    resultAdjectives[index] = wordTo;
                }
            }

            // Assign & done
            m_processingActivity.AdjectivesUserLanguage = resultAdjectives;
            m_processingActivity.VerbsUserLanguage = resultVerbs;

            return true;
        }

        private async Awaitable<bool> ActivitySubTaskTargetLanguageSentence()
        {
            var dialogueResult = await GetActivityExamplePhrase();
            var examplePhrase = dialogueResult.Dialogue;

            if (string.IsNullOrWhiteSpace(examplePhrase))
            {
                return false;
            }
            m_processingActivity.ExamplePhrases = new string[] { examplePhrase };

            return true;
        }

        private async Awaitable<bool> ActivitySubTaskGetWordCloudData()
        {
            string imageString = null;
            var image = m_processingActivity.ContextImage;
            if (image != null)
            {
                imageString = await AssistantAI.GetNetworkSafeImageString(image, m_faceBlur);
            }
            var result = await m_assistant.GenerateWordCloudData(m_userLanguage, m_targetLanguage, m_processingActivity.Classification, imageString, m_processingRequestID);
            if (result.Wordcloud == null || result.Wordcloud.Wordclouds == null || result.Wordcloud.Wordclouds.Length == 0)
            {
                Debug.LogWarning($"ActivitySubTaskGetWordCloudData - result fail: {result}");
                return false;
            }

            var wordClouds = result.Wordcloud;
            var wordCloudUser = wordClouds.WordCloudForLanguage(m_userLanguage);
            var wordCloudTarget = wordClouds.WordCloudForLanguage(m_targetLanguage);

            if (wordCloudUser == null || wordCloudTarget == null)
            {
                Debug.LogWarning($"ActivitySubTaskGetWordCloudData - wordCloudUser ({wordCloudUser}) or wordCloudTarget ({wordCloudTarget}) null");
                return false;
            }

            m_processingActivity.UserLanguageWord = wordCloudUser.Word;
            m_processingActivity.VerbsUserLanguage = wordCloudUser.Verbs;
            m_processingActivity.AdjectivesUserLanguage = wordCloudUser.Adjectives;

            m_processingActivity.TargetLanguageWord = wordCloudTarget.Word;
            m_processingActivity.VerbsTargetLanguage = wordCloudTarget.Verbs;
            m_processingActivity.AdjectivesTargetLanguage = wordCloudTarget.Adjectives;

            return true;
        }

        private async Awaitable<bool> ActivitySubTaskTargetLanguagePhrases()
        {
            var word = m_processingActivity.TargetLanguageWord;
            var verbs = m_processingActivity.VerbsTargetLanguage;
            var adjectives = m_processingActivity.AdjectivesTargetLanguage;
            var result = await m_assistant.GenerateSpeechExamplesForWordCloud(m_targetLanguage, word, verbs, adjectives, m_processingRequestID);

            var phrases = result.Phrases;

            if (phrases == null)
            {
                Debug.LogWarning($"ActivitySubTaskTargetLanguagePhrases - phrases was null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(phrases.Phrase0) ||
                string.IsNullOrWhiteSpace(phrases.Phrase1) ||
                string.IsNullOrWhiteSpace(phrases.Phrase2))
            {
                Debug.LogWarning($"ActivitySubTaskTargetLanguagePhrases - a phrases was empty or null");
                return false;
            }

            m_processingActivity.ExamplePhrases = new string[] { phrases.Phrase0, phrases.Phrase1, phrases.Phrase2 };

            return true;
        }

        private async void GetActivityComponents(Activity activity)
        {
            _ = Time.time;
            m_processingActivity = activity;
            m_processingRequestID = Guid.NewGuid().ToString();
            m_processingActivity.EnglishWord = m_processingActivity.Classification;
            var classification = m_processingActivity.Classification;

            if (!await RetryActivitySubTaskForSuccess(ActivitySubTaskGetWordCloudData))
            {
                Debug.LogWarning($"LessonsManager - failed subtask ActivitySubTaskGetPartsOfSpeech for: {classification}");
                return;
            }

            if (!await RetryActivitySubTaskForSuccess(ActivitySubTaskTargetLanguagePhrases))
            {
                Debug.LogWarning($"LessonsManager - failed subtask ActivitySubTaskTargetLanguageSentence for: {classification}");
                return;
            }

            ProcessActivityComplete();
        }

        private async Awaitable<TranslationResult> GetActivityUserWord()
        {
            return await m_assistant.Translate(AssistantAI.SupportedLanguage.English, m_userLanguage, m_processingActivity.EnglishWord, m_processingRequestID);
        }

        private async Awaitable<TranslationResult> GetActivityTargetWord()
        {
            return await m_assistant.Translate(AssistantAI.SupportedLanguage.English, m_targetLanguage, m_processingActivity.EnglishWord, m_processingRequestID);
        }

        private async Awaitable<RelatedWordResult> GetActivityRelatedWords()
        {
            return await m_assistant.FindRelatedWords(m_targetLanguage, m_processingActivity.EnglishWord, RELATED_WORD_PARTS_OF_SPEECH_COUNT, m_processingRequestID);
        }

        private async Awaitable<DialogueResult> GetActivityExamplePhrase()
        {
            var relatedWords = new List<string>();
            if (m_processingActivity.VerbsTargetLanguage != null && m_processingActivity.VerbsTargetLanguage.Length > 0)
            {
                relatedWords.Add(m_processingActivity.VerbsTargetLanguage[0]);
            }
            if (m_processingActivity.AdjectivesTargetLanguage != null && m_processingActivity.AdjectivesTargetLanguage.Length > 0)
            {
                relatedWords.Add(m_processingActivity.AdjectivesTargetLanguage[0]);
            }
            var dialogue = new DialogueContextFocusWord(m_targetLanguage, m_processingActivity.EnglishWord, relatedWords.ToArray(), 0.5f);
            return await m_assistant.GenerateExampleSentenceDialogue(dialogue, m_processingRequestID);
        }
    }

    /// <summary>
    /// Object in the 3D world
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class Lesson
    {
        public Activity Activity { get; }
        public string Classification => Activity.Classification;
        public bool IsCompleted { get; private set; } = false;
        public bool IsReady => Activity.IsReady;

        // Tracked data source
        private CameraTrackedTaxon m_taxon;
        // Default values if no underlying tracker 
        private Vector3 m_position;
        private Vector3 m_extent;

        public Lesson(Activity activity, CameraTrackedTaxon taxon)
        {
            Activity = activity;
            m_taxon = taxon;
        }

        public Lesson(Activity activity, Vector3 position, Vector3 extent)
        {
            Activity = activity;
            m_position = position;
            m_extent = extent;
        }

        public List<(Vector3, bool)> SamplePoints => m_taxon != null ? m_taxon.SamplePoints : null;

        public List<Vector3> SampleNormals => m_taxon != null ? m_taxon.SampleNormals : null;

        public Vector3 Position => m_taxon != null ? m_taxon.Center : m_position;

        public Vector3 Extent => m_taxon != null ? m_taxon.Extent : m_extent;

        public float Confidence => m_taxon != null ? m_taxon.Reliability : 1.0f;

        public ImageSampleContext BestImageForView(Transform transform)
        {
            if (m_taxon != null)
            {
                return m_taxon.GetRepresentativeImage(transform);
            }
            return null;
        }

        public void MarkComplete()
        {
            IsCompleted = true;
        }

        public void MarkIncomplete()
        {
            IsCompleted = false;
        }

        public Lesson UntrackedCopy()
        {
            var untrackedActivity = Activity.UntrackedCopy();
            if (m_taxon != null)
            {
                var taxon = m_taxon.UntrackedCopy();
                return new Lesson(untrackedActivity, taxon);
            }
            return new Lesson(untrackedActivity, Position, Extent);
        }
    }

    /// <summary>
    /// A single word
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class Activity
    {
        public bool IsReady => IsAcquisitionComplete;
        public string Classification;
        public string EnglishWord;
        public string[] ExamplePhrases;
        public bool HasCompleted = false;
        public bool IsAcquisitionComplete = false;

        // Target-language words
        public string TargetLanguageWord;
        public string[] VerbsTargetLanguage;
        public string[] AdjectivesTargetLanguage;

        // User-language words for assistance:
        public string UserLanguageWord;
        public string[] VerbsUserLanguage;
        public string[] AdjectivesUserLanguage;

        // Image Sensing data:
        public Texture2D ContextImage; // Some representative image from the list
        public Vector2 ContextImageExtent; // 3D approximation of image 2D size

        public Activity UntrackedCopy()
        {
            var activity = new Activity
            {
                Classification = Classification,
                EnglishWord = EnglishWord,
                ExamplePhrases = ExamplePhrases,
                HasCompleted = HasCompleted,
                IsAcquisitionComplete = IsAcquisitionComplete,

                TargetLanguageWord = TargetLanguageWord,
                VerbsTargetLanguage = VerbsTargetLanguage,
                AdjectivesTargetLanguage = AdjectivesTargetLanguage,

                UserLanguageWord = UserLanguageWord,
                VerbsUserLanguage = VerbsUserLanguage,
                AdjectivesUserLanguage = AdjectivesUserLanguage,

                ContextImage = ContextImage,
                ContextImageExtent = ContextImageExtent
            };

            return activity;
        }

        public string Description()
        {
            var result = $"[Activity: ({IsReady}), Classification:{Classification}, EnglishWord:{EnglishWord}, UserLanguageWord:{UserLanguageWord}, TargetLanguageWord:{TargetLanguageWord} IsAcquisitionComplete: {IsAcquisitionComplete}, ContextImage:{ContextImage} ]";
            return result;
        }

        public bool IsValidForUse()
        {
            if (string.IsNullOrEmpty(Classification))
            {
                Debug.LogWarning("Activity ValidForUse: Classification was empty");
                return false;
            }

            if (string.IsNullOrEmpty(EnglishWord))
            {
                Debug.LogWarning("Activity ValidForUse: EnglishWord was empty");
                return false;
            }

            if (ExamplePhrases == null || ExamplePhrases.Length != 3)
            {
                Debug.LogWarning("Activity ValidForUse: EnglishWord was empty or had incorrect count");
                return false;
            }

            // This is set after validation: IsAcquisitionComplete

            if (string.IsNullOrEmpty(TargetLanguageWord))
            {
                Debug.LogWarning("Activity ValidForUse: TargetLanguageWord was empty");
                return false;
            }

            if (VerbsTargetLanguage == null || VerbsTargetLanguage.Length != 2)
            {
                Debug.LogWarning("Activity ValidForUse: VerbsTargetLanguage was empty or had incorrect count");
                return false;
            }

            if (AdjectivesTargetLanguage == null || AdjectivesTargetLanguage.Length != 3)
            {
                Debug.LogWarning("Activity ValidForUse: AdjectivesTargetLanguage was empty or had incorrect count");
                return false;
            }

            if (string.IsNullOrEmpty(UserLanguageWord))
            {
                Debug.LogWarning("Activity ValidForUse: UserLanguageWord was empty");
                return false;
            }

            if (VerbsUserLanguage == null || VerbsUserLanguage.Length != 2)
            {
                Debug.LogWarning("Activity ValidForUse: VerbsTargetLanguage was empty or had incorrect count");
                return false;
            }

            if (AdjectivesUserLanguage == null || AdjectivesUserLanguage.Length != 3)
            {
                Debug.LogWarning("Activity ValidForUse: AdjectivesUserLanguage was empty or had incorrect count");
                return false;
            }

            if (ContextImage == null)
            {
                Debug.LogWarning("Activity ValidForUse: ContextImage was empty");
                return false;
            }

            return true;
        }

        // Set dynamic data to blank
        public void Reset()
        {
            UserLanguageWord = null;
            TargetLanguageWord = null;
            VerbsTargetLanguage = null;
            AdjectivesTargetLanguage = null;
            HasCompleted = false;
            ExamplePhrases = null;
            VerbsUserLanguage = null;
            AdjectivesUserLanguage = null;
            IsAcquisitionComplete = false;
        }

        // Set all data to blank
        public void Clear()
        {
            Reset();
            ContextImage = null;
            ContextImageExtent = new Vector2();
        }
    }
}