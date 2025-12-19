// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Meta.Utilities.CameraTaxonTracking;
using Meta.XR.Samples;
using SpatialLingo.Characters;
using SpatialLingo.Lessons;
using SpatialLingo.SceneObjects;
using SpatialLingo.SpeechAndText;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpatialLingo.Samples
{
    /// <summary>
    /// Test out Character
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class WordCloudSample : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private Lesson3DInteractor m_lessonPrefab;
        [SerializeField] private GameObject m_buttonPrefab;
        [SerializeField] private WordBar3D m_wordBar;
        [SerializeField] private VoiceSpeaker m_speaker;
        [SerializeField] private Transform m_centerEyeAnchor;
        [SerializeField] private BerryController m_berryPrefab;

        private List<Lesson3DInteractor> m_allInteractors = new();
        private Lesson3DInteractor m_activeInteractor;

        private void Start()
        {
            // generate faked data
            Activity activity;
            Lesson lesson;
            CameraTrackedTaxon taxon;

            activity = new Activity
            {
                EnglishWord = "plant",
                UserLanguageWord = "Plant",
                TargetLanguageWord = "Plant",
                VerbsTargetLanguage = new string[] { "Grow", "Water", "Nurture", "Fruit", "Respire" },
                AdjectivesTargetLanguage = new string[] { "Botanical", "Floral", "Green", "Lush", "Vibrant" },
                VerbsUserLanguage = new string[] { "Grow-B", "Water-B", "Nurture-B", "Fruit-B", "Respire-B" },
                AdjectivesUserLanguage = new string[] { "Botanical-B", "Floral-B", "Green-B", "Lush-B", "Vibrant-B" }
            };

            var lessonLocations = new Vector3[]
            {
                new(-0.9f,0.1f,0.50f),
                new(-0.6f,0.4f,0.50f),
                new(-0.3f,0.7f,0.50f),
                new(0.0f, 1.0f,0.50f),
                new(0.3f, 1.3f,0.50f),
                new(0.6f, 1.6f,0.50f),
                new(0.9f, 1.9f,0.50f),
            };

            var size = 0.10f;
            foreach (var location in lessonLocations)
            {
                // Make fake taxon samples for points (to get sizes)
                var samples = new List<TrackSample>();
                for (var i = 0; i < 5; ++i)
                {
                    var center = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
                    center.Normalize();

                    var normal = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
                    normal.Normalize();

                    var up = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
                    up.Normalize();

                    var point = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
                    point.Normalize();
                    point.Scale(new Vector3(size, size, size));
                    point += location;
                    var sample = new TrackSample(normal, Vector3.up, new Vector3[] { point }, new bool[] { false }, null, center);
                    samples.Add(sample);
                }
                taxon = new CameraTrackedTaxon("plant", samples.ToArray());
                lesson = new Lesson(activity, taxon);
                var interactor = Instantiate(m_lessonPrefab);
                interactor.Initialize(lesson, m_speaker, m_centerEyeAnchor);
                // Show most words:
                interactor.ShowLesson(3);

                var b = interactor.CreateBerry();
                var start = new Vector3(0, 2, 0);
                interactor.GiveBerry(b);
                b.MoveToDestination(start, interactor.Lesson.Position);


                // TOOD: Put berry at the lesson location

                interactor.UserEnteredActivationArea += OnUserEnteredActivationArea;
                interactor.UserExitedActivationArea += OnUserExitedActivationArea;
                interactor.UserCompletedSuccess += OnUserCompletedSuccess;
                m_allInteractors.Add(interactor);
            }

            // Word bar individual test:
            if (m_wordBar != null)
            {
                m_wordBar.Initialize("Test Word", "Long Translation Here", TextCloudItem.WordType.noun, m_centerEyeAnchor, null);
                m_wordBar.OnSqueezeStartEvent();
            }

            // In editor only test:
            // TestInteractionComplete();
        }

        private void TestInteractionComplete()
        {
            _ = StartCoroutine(TestCoroutine());
        }

        private IEnumerator TestCoroutine()
        {
            yield return new WaitForSeconds(1.0f);

            var interactor = m_activeInteractor;
            if (interactor == null)
            {
                interactor = m_allInteractors[0];
            }

            if (!interactor.IsActiveRunning)
            {
                interactor.ActivateLesson();
                yield return new WaitForSeconds(2.0f);
            }

            yield return new WaitForSeconds(3.0f);

            interactor.CompleteActivation(null, OnLessonCompletedEvent);
        }

        private void OnLessonCompletedEvent(Lesson3DInteractor interactor)
        {
            var berry = interactor.TakeBerry();
            berry.MoveToDestination(berry.transform.position, berry.transform.position + new Vector3(0, 2, 0));

            _ = StartCoroutine(DestroyAfter(berry.gameObject, 3.0f));

            interactor.ResetLesson();
            var b = interactor.CreateBerry();
            interactor.GiveBerry(b);
            b.MoveTo(interactor.Lesson.Position);

            // LOOP:
            TestInteractionComplete();
        }

        private IEnumerator DestroyAfter(GameObject item, float time)
        {
            yield return new WaitForSeconds(time);
            Destroy(item);
        }

        private void OnUserEnteredActivationArea(Lesson3DInteractor interactor)
        {
            if (m_activeInteractor == null)
            {
                m_activeInteractor = interactor;
                m_activeInteractor.ActivateLesson();
            }
        }

        private void OnUserExitedActivationArea(Lesson3DInteractor interactor)
        {
            if (m_activeInteractor == interactor)
            {
                m_activeInteractor.DeactivateLesson();
                m_activeInteractor = null;
            }
        }

        private void OnUserCompletedSuccess(Lesson3DInteractor interactor)
        {
            if (m_activeInteractor == interactor)
            {
                var berry = interactor.TakeBerry();
                berry.MoveToDestination(berry.transform.position, berry.transform.position + new Vector3(0, 2, 0));
                _ = StartCoroutine(DestroyAfter(berry.gameObject, 3.0f));

                interactor.ResetLesson();
                var b = interactor.CreateBerry();
                interactor.GiveBerry(b);
                b.MoveTo(interactor.Lesson.Position);
                m_activeInteractor = null;
            }
        }

        private void Update()
        {
            // Hand: seems to be triggered by a 'grab' as well?
            var isDownControllerTrigger = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            if (isDownControllerTrigger)
            {
                if (m_activeInteractor != null)
                {
                    m_activeInteractor.CompleteActivation(null, OnActiveLessonCompletedEvent);
                }
            }
        }

        private void OnActiveLessonCompletedEvent(Lesson3DInteractor interactor)
        {
            // Do operation
        }
    }
}