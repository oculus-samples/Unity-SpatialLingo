// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Meta.XR.Samples;
using SpatialLingo.AI;
using SpatialLingo.AppSystems;
using SpatialLingo.SpeechAndText;
using UnityEngine;

namespace SpatialLingo.Lessons
{
    [MetaCodeSample("SpatialLingo")]
    public class LessonInteractionManager : IDisposable
    {
        public delegate void LessonActivionEvent(Lesson3DInteractor interactor);

        public event LessonActivionEvent LessonActivated;
        public event LessonActivionEvent LessonDeactivated;

        public event LessonActivionEvent LessonCompletedSuccess;

        private const float BEST_LESSON_DISTANCE_SEPARATION_GOAL = 0.20f; // for testing in small space, otherwise 0.5-1.5

        private Transform m_centerEyeAnchor;
        private Lesson3DInteractor m_lessonPrefab;
        private LessonsManager m_lessonsManager;
        private Dictionary<Lesson, Lesson3DInteractor> m_lessons = new();
        private Lesson3DInteractor m_activeInteractor;
        private VoiceSpeaker m_speaker;
        private bool m_allowActivationsProximity;

        public LessonInteractionManager(LessonsManager lessonsManager, Lesson3DInteractor lessonPrefab, Transform centerEyeAnchor, VoiceSpeaker speaker)
        {
            m_centerEyeAnchor = centerEyeAnchor;
            m_lessonPrefab = lessonPrefab;
            m_lessonsManager = lessonsManager;
            m_speaker = speaker;

            lessonsManager.LessonAdded += OnLessonAdded;
            lessonsManager.LessonUpdated += OnLessonUpdated;
            lessonsManager.LessonRemoved += OnLessonRemoved;
        }

        public void Dispose()
        {
            m_lessonsManager.LessonAdded -= OnLessonAdded;
            m_lessonsManager.LessonUpdated -= OnLessonUpdated;
            m_lessonsManager.LessonRemoved -= OnLessonRemoved;
            m_lessons.Clear();
        }

        public void SetLayersPerFrame(int layersPerFrame)
        {
            m_lessonsManager.SetLayersPerFrame(layersPerFrame);
        }

        public void AllowActivationsOnProximity()
        {
            m_allowActivationsProximity = true;
        }

        public void DisallowActivationsOnProximity()
        {
            m_allowActivationsProximity = false;
        }

        public void SetTargetLanguage(AssistantAI.SupportedLanguage targetLanguage)
        {
            m_lessonsManager.SetTargetLanguage(targetLanguage);
        }

        private void OnLessonAdded(LessonUpdateResult result)
        {
            if (m_lessons.TryGetValue(result.Lesson, out var interactor))
            {
                interactor.UpdateFromLesson();
            }
            else
            {
                _ = InstantiatePrefabForNewLesson(result.Lesson);
            }

            var readyLessons = 0;
            foreach (var pair in m_lessons)
            {
                var lesson = pair.Value;
                if (lesson.Lesson.IsReady)
                {
                    readyLessons++;
                }
            }
        }

        private void OnLessonUpdated(LessonUpdateResult result)
        {
            if (m_lessons.TryGetValue(result.Lesson, out var interactor))
            {
                interactor.UpdateFromLesson();
            }
            else
            {
                Debug.LogWarning("OnLessonUpdated called, but lesson not found");
            }
            var readyLessons = 0;
            foreach (var pair in m_lessons)
            {
                var lesson = pair.Value;
                if (lesson.Lesson.IsReady)
                {
                    readyLessons++;
                }
            }
        }

        public void RemoveLesson(Lesson lesson, bool fadeOut = false)
        {
            if (m_lessons.TryGetValue(lesson, out var interactor))
            {
                if (m_activeInteractor == interactor)
                {
                    m_activeInteractor.DeactivateLesson();
                    m_activeInteractor = null;
                }

                interactor.UserEnteredActivationArea -= OnUserEnteredActivationArea;
                interactor.UserExitedActivationArea -= OnUserExitedActivationArea;
                interactor.UserCompletedSuccess -= OnUserCompletedSuccess;
                interactor.UserTouchedBerry -= OnUserTouchedBerry;

                _ = m_lessons.Remove(lesson);

                if (fadeOut)
                {
                    interactor.FadeDestroy();
                }
                else
                {
                    UnityEngine.Object.Destroy(interactor.gameObject);
                }
            }
            else
            {
                Debug.LogWarning("OnLessonRemoved called, but lesson not found");
            }
        }

        private void OnLessonRemoved(LessonUpdateResult result)
        {
            RemoveLesson(result.Lesson);

            var readyLessons = 0;
            foreach (var pair in m_lessons)
            {
                var lesson = pair.Value;
                if (lesson.Lesson.IsReady)
                {
                    readyLessons++;
                }
            }
        }

        private void ClearLessons()
        {
            // Reset lesson for re-use
            foreach (var pair in m_lessons)
            {
                var lesson = pair.Value;
                lesson.Lesson.MarkIncomplete();
            }

            if (m_activeInteractor != null)
            {
                Debug.LogWarning($"Best lessons cleared, but activeInteractor was set: {m_activeInteractor}");
                m_activeInteractor.DeactivateLesson();
                m_activeInteractor = null;
            }
        }

        public void StopLessons()
        {
            ClearLessons();
        }

        public Lesson3DInteractor[] GetNextBestLessons(List<Lesson3DInteractor> exisingInteractorList, int moreCount, bool setToAvailable = false)
        {
            var interactors = new List<Lesson3DInteractor>();
            var existingLessons = new List<Lesson>();
            foreach (var interactor in exisingInteractorList)
            {
                var lesson = interactor.Lesson;
                existingLessons.Add(lesson);
            }

            var foundLessons = m_lessonsManager.NextLessonsInDistancePlaneXZ(existingLessons, moreCount, BEST_LESSON_DISTANCE_SEPARATION_GOAL);

            foreach (var lesson in foundLessons)
            {
                if (m_lessons.TryGetValue(lesson, out var interactor))
                {
                    interactors.Add(interactor);
                    if (setToAvailable)
                    {
                        interactor.SetDebugStatus(false);
                        interactor.UpdateFromLesson();
                    }
                }
                else
                {
                    Debug.LogWarning($"lesson lookup didn't have an interactor for {lesson.Classification}");
                }
            }
            return interactors.ToArray();
        }

        private void OnUserEnteredActivationArea(Lesson3DInteractor interactor)
        {
            if (m_allowActivationsProximity && m_activeInteractor == null)
            {
                m_activeInteractor = interactor;
                m_activeInteractor.ShowLesson(AppSessionData.Tier);
                m_activeInteractor.ActivateLesson();
                LessonActivated?.Invoke(m_activeInteractor);
            }
        }

        private void OnUserExitedActivationArea(Lesson3DInteractor interactor)
        {
            if (m_activeInteractor == interactor)
            {
                m_activeInteractor.DeactivateLesson();
                LessonDeactivated?.Invoke(m_activeInteractor);
                m_activeInteractor = null;
            }
        }

        private void OnUserCompletedSuccess(Lesson3DInteractor interactor)
        {
            if (m_activeInteractor == interactor)
            {
                m_activeInteractor.DeactivateLesson();
                LessonCompletedSuccess?.Invoke(interactor);
                m_activeInteractor = null;
                foreach (var lesson in m_lessons.Values)
                {
                    lesson.ResetDistanceCheck();
                }
            }
            else
            {
                Debug.LogWarning($"LessonInteractionManager - OnUserCompletedSuccess - active interactor ({m_activeInteractor}) not match requested interactor ({interactor})");
            }
        }

        private void OnUserTouchedBerry(Lesson3DInteractor interactor)
        {
            if (!m_allowActivationsProximity)
            {
                return;
            }

            if (m_activeInteractor == interactor)
            {
                return;
            }

            if (m_activeInteractor != null)
            {
                m_activeInteractor.DeactivateLesson();
                LessonDeactivated?.Invoke(m_activeInteractor);
            }

            m_activeInteractor = interactor;
            m_activeInteractor.ShowLesson(AppSessionData.Tier);
            m_activeInteractor.ActivateLesson();
            LessonActivated?.Invoke(m_activeInteractor);
        }

        public Lesson3DInteractor CreateUntrackedCopy(Lesson3DInteractor original)
        {
            var newLesson = original.Lesson.UntrackedCopy();
            var newInteractor = InstantiatePrefabForNewLesson(newLesson);
            return newInteractor;
        }

        public Lesson3DInteractor CreateUntrackedInteractor(Lesson lesson)
        {
            var newInteractor = InstantiatePrefabForNewLesson(lesson);
            return newInteractor;
        }

        private Lesson3DInteractor InstantiatePrefabForNewLesson(Lesson lesson)
        {
            var interactor = UnityEngine.Object.Instantiate(m_lessonPrefab);
            interactor.Initialize(lesson, m_speaker, m_centerEyeAnchor);
            interactor.SetDebugStatus(true);
            interactor.ShowLesson(1);
            interactor.UserEnteredActivationArea += OnUserEnteredActivationArea;
            interactor.UserExitedActivationArea += OnUserExitedActivationArea;
            interactor.UserCompletedSuccess += OnUserCompletedSuccess;
            interactor.UserTouchedBerry += OnUserTouchedBerry;
            m_lessons[lesson] = interactor;
            return interactor;
        }
    }
}