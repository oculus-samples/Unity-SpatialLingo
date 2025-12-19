// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using SpatialLingo.AI;
using SpatialLingo.AppSystems;
using SpatialLingo.Audio;
using SpatialLingo.Lessons;
using SpatialLingo.SceneObjects;
using SpatialLingo.SpeechAndText;
using SpatialLingo.Utilities;
using UnityEngine;

namespace SpatialLingo.Characters
{
    [MetaCodeSample("SpatialLingo")]
    public class GollyGoshInteractionManager : MonoBehaviour
    {
        private const float GOLLY_GOSH_FOLLOW_HEADSET_MIN_DISTANCE = 0.5f; // meters
        private const float GOLLY_GOSH_FOLLOW_HEADSET_MAX_DISTANCE = 1.5f; // meters

        private const float GOLLY_GOSH_FOLLOW_BERRY_MIN_DISTANCE = 0.2f; // meters
        private const float GOLLY_GOSH_FOLLOW_BERRY_MAX_DISTANCE = 0.5f; // meters

        private const float TALKING_TIME_PAUSE = 0.60f;

        public delegate void GollyGoshStatusEvent();
        public event GollyGoshStatusEvent GollyGoshSpawned;
        public event GollyGoshStatusEvent GollyGoshFound;

        [Header("Assets")]
        [SerializeField] private GollyGoshController m_gollyGoshControllerPrefab;
        [SerializeField] private ConfettiController m_confettiController;

        public GollyGoshController Controller { get; private set; }

        private TreeController m_treeController;
        private LessonInteractionManager m_lessonManager;
        private VoiceSpeaker m_speaker;
        private Transform m_centerEyeAnchor;
        private bool m_waitingSpeech;

        // Used for animation purposes, to be independent of any supporting assets
        private GameObject m_tempLocation = null;

        public void Initialize(LessonInteractionManager lessonManager, Transform centerEyeAnchor, VoiceSpeaker speaker, RoomSense roomSense)
        {
            m_speaker = speaker;
            m_lessonManager = lessonManager;
            m_centerEyeAnchor = centerEyeAnchor;

            lessonManager.LessonActivated += OnLessonActivated;
            lessonManager.LessonDeactivated += OnLessonDeactivated;
            lessonManager.LessonCompletedSuccess += OnLessonCompletedSuccess;
            roomSense.FindSpawnPosition += OnFindSpawnPosition;
            roomSense.FindSpawnPositions();

            m_speaker.VoiceSpeechStarted += OnVoiceSpeechStarted;
            m_speaker.VoiceSpeechCompleted += OnVoiceSpeechCompleted;
        }

        private void OnFindSpawnPosition(RoomSense.SpawnPositionResult result)
        {
            var defaultPosition = GetDefaultSpawnPosition();

            Vector3 position;
            if (result == null)
            {
                Debug.LogWarning("GollyGoshInteractionManager - OnFindSpawnPostition: result is null. Defaulting to ground.");
                position = defaultPosition;
            }
            else
            {
                position = result.Position;
                if (position == Vector3.zero)
                {
                    Debug.LogWarning($"GollyGoshInteractionManager - position is origin. Defaulting to ground.");
                    position = defaultPosition;
                }
            }
            Controller = Instantiate(m_gollyGoshControllerPrefab);

            // Setup GollyGosh to ensure proper initial state
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.None);
            Controller.MoveTo(position, true);
            Controller.gameObject.SetActive(false);

            // Parent TTSSpeaker object and Audio Source to GG for spatial audio
            m_speaker.transform.SetParent(Controller.GetAudioTransform());

            // Mark GG as spawned
            GollyGoshSpawned?.Invoke();
        }

        public Vector3 HideInUserRoom()
        {
            // Cache spawn pos with slight height offset from ground
            var spawnPos = Controller.GetPosition();
            var ggSpawnPos = spawnPos + new Vector3(0, 0.15f, 0); // slight offset above, mostly buried

            // Show GG, update his pos and have him look at user
            Controller.gameObject.SetActive(true);
            Controller.MoveTo(ggSpawnPos, true);
            // look in direction of user but only rotating in Y:
            var userLocation = m_centerEyeAnchor.position;
            var lookLocationXZ = new Vector3(userLocation.x, ggSpawnPos.y, userLocation.z);
            Controller.GazeAt(lookLocationXZ);

            // Show initial emotions + animation + beckon
            ShowFaceFrown();
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.WavingContinuous);
            Speak(AppSessionData.TargetLanguageAI, Tutorial.BeckonPhrase());

            // Enable grab interaction to allow user to pick him up
            Controller.SetTouchInteraction(true);
            Controller.UserTouched += OnUserTouched;

            // Return current floor position
            return spawnPos;
        }

        private void OnUserTouched()
        {
            Controller.SetTouchInteraction(false);
            Controller.UserTouched -= OnUserTouched;
            GollyGoshFound?.Invoke();
        }

        public void CelebrateExplosion()
        {
            var position = Controller.GetPosition();
            var direction = m_centerEyeAnchor.position - position;
            var rotation = Quaternion.LookRotation(direction, Vector3.up);
            m_confettiController.ShowConfettiPresentation(position, rotation, 1, 0, 0);
            AppAudioController.Instance.PlaySound(SoundEffect.CelebratoryStinger, position);
        }

        public void LookAt(Transform target, float duration = 0.0f, Action<MovementController, bool> callback = null)
        {
            if (duration <= 0)
            {
                Controller.GazeAt(target);
            }
            else
            {
                Controller.GazeAtDelay(target, duration, callback);
            }
        }

        public void LookAt(Vector3 position, float duration = 0.0f, Action<MovementController, bool> callback = null)
        {
            if (duration <= 0)
            {
                Controller.GazeAt(position);
            }
            else
            {
                Controller.GazeAtDelay(position, duration, callback);
            }
        }

        public void Celebrate()
        {
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.Celebrate);
        }

        public void Listen()
        {
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.ListenStart);
        }

        public void Idle()
        {
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.Idle);
        }

        public void ShowFaceNeutral()
        {
            Controller.ShowEyeEmotion(GollyGoshController.GollyGoshEyeEmotion.Neutral);
            Controller.ShowMouthEmotion(GollyGoshController.GollyGoshMouthEmotion.Neutral);
        }

        public void ShowFaceFrown()
        {
            Controller.ShowEyeEmotion(GollyGoshController.GollyGoshEyeEmotion.FrownyClosed);
            Controller.ShowMouthEmotion(GollyGoshController.GollyGoshMouthEmotion.Frowny);
        }

        public void ShowFaceHappy()
        {
            Controller.ShowEyeEmotion(GollyGoshController.GollyGoshEyeEmotion.Neutral);
            Controller.ShowMouthEmotion(GollyGoshController.GollyGoshMouthEmotion.Happy);
        }

        public void ShowFaceSurprised()
        {
            Controller.ShowEyeEmotion(GollyGoshController.GollyGoshEyeEmotion.Surprised);
            Controller.ShowMouthEmotion(GollyGoshController.GollyGoshMouthEmotion.Surprised);
        }

        private void OnLessonActivated(Lesson3DInteractor interactor)
        {
            // GG's offset direction could be updated to be more in view of the user,
            // instead of always off to the left of the lesson
            var sideDistance = 0.75f; // m
            var positionInteraction = interactor.transform.position;
            var positionUser = m_centerEyeAnchor.position;
            var interactionToUser = positionUser - positionInteraction;
            interactionToUser.y = 0.0f; // ignore up/down
            var unitItoU = interactionToUser.normalized;
            var halfTtoU = interactionToUser;
            halfTtoU.Scale(new Vector3(0.5f, 0.5f, 0.5f));
            var toRight = Quaternion.Euler(0.0f, 90.0f, 0.0f) * unitItoU;
            toRight.Scale(new Vector3(sideDistance, sideDistance, sideDistance));
            var right = positionInteraction + halfTtoU + toRight;
            var left = positionInteraction + halfTtoU - toRight;

            var ggLocation = Controller.GetPosition();
            var distanceRight = Vector3.Distance(ggLocation, right);
            var distanceLeft = Vector3.Distance(ggLocation, left);
            var targetLocation = distanceRight < distanceLeft ? right : left;

            // set GG to user's eye level
            targetLocation.y = positionUser.y;
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.Flying);
            Controller.MoveTo(targetLocation, false, OnMoveToActivationComplete);
            Controller.GazeAt(m_centerEyeAnchor);
        }

        private void OnMoveToActivationComplete(bool success)
        {
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.Idle);
            Controller.Follow(m_centerEyeAnchor, GOLLY_GOSH_FOLLOW_HEADSET_MIN_DISTANCE, GOLLY_GOSH_FOLLOW_HEADSET_MAX_DISTANCE);
        }

        public void FollowBerryToTree(GameObject berry)
        {
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.Flying);
            Controller.Follow(berry.transform, GOLLY_GOSH_FOLLOW_BERRY_MIN_DISTANCE, GOLLY_GOSH_FOLLOW_BERRY_MAX_DISTANCE);
            LookAt(berry.transform, 0.5f);
        }

        public void StopFollowing()
        {
            Controller.Follow(null, 0, 0);
        }

        private void OnLessonDeactivated(Lesson3DInteractor interactor)
        {
            // Handle event as needed
        }

        private void OnLessonCompletedSuccess(Lesson3DInteractor interactor)
        {
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.Celebrate);
        }

        public void SetTreeController(TreeController treeController)
        {
            m_treeController = treeController;
        }

        public void OnUserCompletedAllTiers()
        {
            var targetLocation = m_treeController.transform.position;
            // Move to tree, between user
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.Flying);
            Controller.MoveTo(targetLocation, false, OnMoveToTreeComplete);
            Controller.GazeAt(m_centerEyeAnchor);
        }

        private void OnMoveToTreeComplete(bool success)
        {
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.Celebrate);
            Controller.Follow(m_centerEyeAnchor, GOLLY_GOSH_FOLLOW_HEADSET_MIN_DISTANCE, GOLLY_GOSH_FOLLOW_HEADSET_MAX_DISTANCE);
        }

        public void TutorialGoToNearestLesson(Lesson3DInteractor[] lessons)
        {
            var ggPosition = Controller.GetPosition();

            Lesson3DInteractor closestLesson = null;
            var closestDistance = 0.0f;
            foreach (var lesson in lessons)
            {
                // ignore completed lessons
                if (lesson.Lesson.IsCompleted)
                {
                    continue;
                }
                var distance = Vector3.Distance(ggPosition, lesson.gameObject.transform.position);
                if (distance < closestDistance || closestLesson == null)
                {
                    closestDistance = distance;
                    closestLesson = lesson;
                }
            }
            if (closestLesson != null)
            {
                var distanceFromLesson = 0.25f;
                var tooCloseLimit = 0.5f;
                var lessonPosition = closestLesson.transform.position;
                var userPosition = m_centerEyeAnchor.position;
                var lessonToUser = userPosition - lessonPosition;
                var lessonToGG = ggPosition - lessonPosition;
                var distanceToLesson = Vector3.Distance(ggPosition, lessonToGG);
                if (distanceToLesson < tooCloseLimit)
                {
                    _ = StartCoroutine(ReactToNearbyLesson(closestLesson.transform));
                }
                else
                {
                    lessonToUser.y = 0.0f;
                    lessonToGG.y = 0.0f;
                    var lessonOffsetDirection = CharacterUtilities.PerpendicularComponent(lessonToUser, lessonToGG);
                    lessonOffsetDirection.Normalize();
                    lessonOffsetDirection.Scale(new Vector3(distanceFromLesson, distanceFromLesson, distanceFromLesson));
                    var finalLocation = lessonPosition + lessonOffsetDirection;
                    // Go to the LEFT/RIGHT of the point, face the user, do a point animation
                    m_tempLocation = new GameObject();
                    m_tempLocation.transform.position = lessonPosition;
                    Controller.GazeAt(lessonPosition);
                    Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.Flying);
                    Controller.MoveTo(finalLocation, false, GGArrivedAtLesson);
                }
            }
        }

        private IEnumerator ReactToNearbyLesson(Transform targetLesson)
        {
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.Celebrate);
            yield return new WaitForSeconds(1f);
            Controller.PointAt(targetLesson.transform, false);
            Controller.GazeAt(m_centerEyeAnchor);
        }

        private void GGArrivedAtLesson(bool success)
        {
            Controller.PointAt(m_tempLocation.transform, false);
            Controller.GazeAt(m_centerEyeAnchor);
            Destroy(m_tempLocation);
        }

        private WordBar3D NearestWordFromInteractor(Lesson3DInteractor lesson, TextCloudItem.WordType type = TextCloudItem.WordType.none)
        {
            var ggPosition = Controller.GetPosition();
            var words = lesson.Words();
            WordBar3D closestBar = null;
            var closestDistance = 0.0f;
            foreach (var word in words)
            {
                var distance = Vector3.Distance(ggPosition, lesson.gameObject.transform.position);
                if (distance < closestDistance || closestBar == null)
                {
                    if (type == TextCloudItem.WordType.none || type == word.WordType)
                    {
                        closestDistance = distance;
                        closestBar = word;
                    }
                }
            }

            return closestBar;
        }

        public void TutorialPointToNearestWord(Lesson3DInteractor interactor, TextCloudItem.WordType type = TextCloudItem.WordType.none)
        {
            var nearest = NearestWordFromInteractor(interactor, type);
            if (nearest == null)
            {
                return;
            }
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.PointForward);
            MoveToWordPresentation(nearest);
        }

        private void TutorialArriveAtWord(bool completed)
        {
            LookAt(m_centerEyeAnchor, 0.25f);
        }

        public void TutorialTranslateNearestWord(Lesson3DInteractor interactor, TextCloudItem.WordType type = TextCloudItem.WordType.none)
        {
            var nearest = NearestWordFromInteractor(interactor, type);
            if (nearest == null)
            {
                return;
            }
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.TutorialSqueeze);
            MoveToWordPresentation(nearest);
            nearest.OnSqueezeStartEvent();
        }

        public void TutorialPlayAudioNearestWord(Lesson3DInteractor interactor, TextCloudItem.WordType type = TextCloudItem.WordType.none)
        {
            var nearest = NearestWordFromInteractor(interactor, type);
            if (nearest == null)
            {
                return;
            }
            Controller.PlayAnimation(GollyGoshController.GollyGoshAnimation.TutorialPoint);
            MoveToWordPresentation(nearest);
            nearest.PokeSelectAutocomplete(false);
        }

        private void MoveToWordPresentation(WordBar3D wordBar)
        {
            var wordPosition = wordBar.transform.position;
            var wordWidth = wordBar.Size.x;
            var charPosition = Controller.GetPosition();
            var distanceSideWord = 0.25f + wordWidth * 0.5f; // slightly away
            var distanceTowardUser = 0.15f; // slightly in-front of word
            var wordToChar = wordPosition - charPosition;
            var wordSide = wordBar.transform.right;
            var wordForward = wordBar.transform.forward;
            wordSide.Scale(new Vector3(distanceSideWord, distanceSideWord, distanceSideWord));
            wordForward.Scale(new Vector3(distanceTowardUser, distanceTowardUser, distanceTowardUser));
            var targetPosition = wordPosition + wordSide + wordForward;
            LookAt(wordBar.transform, 0.50f);
            Controller.MoveTo(targetPosition, false, TutorialArriveAtWord);
        }

        public void Speak(AssistantAI.SupportedLanguage language, string phrase)
        {
            MarkWaitingForSpeechComplete();
            m_speaker.SpeakAudioForText(Language.Language.AssistantAIToWitaiLanguage(language), phrase);
        }

        public void StopSpeaking()
        {
            m_speaker.StopAudioPlayback();
            Controller.StopTalking();
        }

        public void Dispose()
        {
            m_lessonManager.LessonActivated -= OnLessonActivated;
            m_lessonManager.LessonDeactivated -= OnLessonDeactivated;
            m_lessonManager.LessonCompletedSuccess -= OnLessonCompletedSuccess;
            m_speaker.VoiceSpeechCompleted -= OnVoiceSpeechCompleted;
            m_speaker.VoiceSpeechStarted -= OnVoiceSpeechStarted;
        }

        private void OnVoiceSpeechStarted()
        {
            Controller.StartTalking();
        }

        private void OnVoiceSpeechCompleted()
        {
            m_waitingSpeech = false;
            Controller.StopTalking();
        }

        private void MarkWaitingForSpeechComplete()
        {
            m_waitingSpeech = true;
        }

        public IEnumerator WaitForSpeechOrTimeout(float timeoutTime = 3.0f)
        {
            var startTime = Time.time;
            while (m_waitingSpeech)
            {
                yield return new WaitForSeconds(0.1f);
                var diff = Time.time - startTime;
                if (diff > timeoutTime)
                {
                    break;
                }
            }
        }

        public IEnumerator WaitForSpeechPause()
        {
            yield return new WaitForSeconds(TALKING_TIME_PAUSE);
        }

        /// <summary>
        /// Find a location on the floor near user if possible
        /// </summary>
        /// <returns>best guess default location for spawning</returns>
        private Vector3 GetDefaultSpawnPosition()
        {
            var position = new Vector3();
            var mruk = MRUK.Instance;
            if (mruk != null)
            {
                var room = mruk.GetCurrentRoom();
                if (room != null)
                {
                    var floor = room.FloorAnchor;
                    if (floor == null)
                    {
                        floor = room.FindLargestSurface(MRUKAnchor.SceneLabels.FLOOR);
                    }

                    if (floor != null)
                    {
                        // Default to floor location 
                        var userHead = m_centerEyeAnchor.position;
                        _ = floor.GetClosestSurfacePosition(userHead, out var closestFloorPoint, out _, MRUKAnchor.ComponentType.All);
                        position = closestFloorPoint;
                    }
                }
            }

            return position;
        }
    }
}