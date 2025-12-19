// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.Samples;
using SpatialLingo.Interactions;
using UnityEngine;

namespace SpatialLingo.Characters
{
    [MetaCodeSample("SpatialLingo")]
    public class GollyGoshController : MonoBehaviour
    {
        private const float TARGET_GOLLY_GOSH_MOVE_VELOCITY = 1.0f; // m/s 
        private const float TARGET_GOLLY_GOSH_MOVE_OFFSET = 1.0f; // meters

        private const float GOLLY_GOSH_MAX_FRAMES_EYES = 16f;
        private const float GOLLY_GOSH_MAX_FRAMES_MOUTH = 16f;
        private const float GOLLY_GOSH_MOUTH_FRAMERATE = 8f;

        private const int GOLLY_GOSH_BLINK_FRAMES_RANDOM_MIN = 50; // Time between blinks
        private const int GOLLY_GOSH_BLINK_FRAMES_RANDOM_MAX = 150;
        private const int GOLLY_GOSH_EYES_CLOSED_FRAMES_RANDOM_MIN = 10; // Time to keep eyes closed
        private const int GOLLY_GOSH_EYES_CLOSED_FRAMES_RANDOM_MAX = 15;

        private const int GOLLY_GOSH_EYES_CLOSED_FRAME_START = 4;
        private const int GOLLY_GOSH_EYES_CLOSED_FRAME_END = 7;

        public enum GollyGoshMovement
        {
            None = 0,
            EaseInOut = 1,
        }

        public enum GollyGoshAnimation
        {
            None = 0,
            Idle = 1,
            Flying = 2,
            Waving = 3,
            Celebrate = 4,
            PointForward = 5,
            PointBackward = 6,
            ListenStart = 7,
            ListenStop = 8,
            TutorialPoint = 9,
            TutorialSqueeze = 10,
            WavingContinuous = 11,
            PointForwardContinuous = 12,
            HelloWorld = 13,
            Beckon = 14,
        }

        public enum GollyGoshEyeEmotion
        {
            Neutral = 0,
            Surprised = 1,
            None = 99,
            Sad = 2,
            FrownyClosed = 4,
            SadClosed = 5,
            Confused = 6,
            Excited = 11,
            VerySad = 14,
        }

        public enum GollyGoshMouthEmotion
        {
            Happy = 0,
            Surprised = 1,
            Neutral = 5,
            Shocked = 6,
            Yell = 10,
            Smirk = 11,
            Smile = 12,
            Frowny = 13,
            Closed = 15,
        }

        public enum FollowLimits
        {
            XYZDistance,
            XZseperableY,
        }

        public delegate void UpdateEvent(GollyGoshController controller);
        public event Action UserTouched;

        [Header("Assets")]
        [SerializeField] private GameObject m_character;
        [SerializeField] private SkinnedMeshRenderer m_characterMeshRenderer;
        [SerializeField] private Animator m_animator;
        [SerializeField] private GameObject m_pathEffects;
        [SerializeField] private AnimationCurve m_curveEaseInOut;
        [SerializeField] private HandTrigger m_handTrigger;
        [SerializeField] private Transform m_mouthTransform;

        private bool m_isRotatingTo = false;
        private Quaternion m_rotatingFromRotation = Quaternion.identity;
        private Quaternion m_rotatingToRotation = Quaternion.identity;
        private float m_rotatingToTotalDuration;
        private float m_rotatingToPassedDuration;

        private int m_currentMouthFrame = 0;
        private int m_currentMouthSubFrame = 0;
        private float m_currentEyeFrame = 0.0f;
        private int m_endTalkingMouthFrame = 0;

        private bool m_isTalking = false;

        private bool m_isBlinking = false;
        private float m_endBlinkingEyeFrame = 0.0f;
        private int m_blinkCount = 0;
        private int m_blinkNextFrameCount = 0;

        private bool m_isMovingTo = false;
        private GollyGoshMovement m_movementType = GollyGoshMovement.None;
        private Vector3 m_movingFromPosition = new();
        private Vector3 m_movingControlPosition = new();
        private Vector3 m_movingToPosition = new();
        private float m_movingToTotalDuration;
        private float m_movingToPassedDuration;
        private Action<bool> m_movingToCompleteAction;

        private MovementController m_gazeController;
        private float m_lookingAtDelay = 0.5f; // Additional lag for more fluid motion of rotation gaze

        private bool m_isFollowing = false;
        private FollowLimits m_followLimitType;
        private Transform m_followingTransform;
        private Vector2 m_followingMinLimits = new();
        private Vector2 m_followingMaxLimits = new();

        private float m_followingDelay = 0.1f;

        private void Awake()
        {
            m_pathEffects.SetActive(false);

            // Character gaze utilizes a movement controller's functionality 
            var gaze = new GameObject("Gaze");
            gaze.transform.parent = transform;
            m_gazeController = gaze.AddComponent<MovementController>();
            m_handTrigger.HandTriggered += OnHandTriggered;

            ShowPathEffects();
        }

        public void ShowPathEffects()
        {
            m_pathEffects.SetActive(true);
        }

        public void HidePathEffects()
        {
            m_pathEffects.SetActive(false);
        }

        public void StopTalking()
        {
            m_isTalking = false;
            // Set mouth frame to saved before talking frame
            m_currentMouthFrame = m_endTalkingMouthFrame;
            UpdateInternalEmotion();
        }

        public void StartTalking()
        {
            if (!m_isTalking)
            {
                m_endTalkingMouthFrame = m_currentMouthFrame;
                m_currentMouthSubFrame = 0;
            }

            m_isTalking = true;
        }

        private void StartBlinking()
        {
            m_endBlinkingEyeFrame = m_currentEyeFrame;
            m_currentEyeFrame = UnityEngine.Random.Range(GOLLY_GOSH_EYES_CLOSED_FRAME_START, GOLLY_GOSH_EYES_CLOSED_FRAME_END);
            UpdateInternalEmotion();
            m_blinkCount = 0;
            m_blinkNextFrameCount = UnityEngine.Random.Range(GOLLY_GOSH_EYES_CLOSED_FRAMES_RANDOM_MIN, GOLLY_GOSH_EYES_CLOSED_FRAMES_RANDOM_MAX);
            m_isBlinking = true;
        }

        private void StopBlinking()
        {
            m_isBlinking = false;
            m_currentEyeFrame = m_endBlinkingEyeFrame;
            UpdateInternalEmotion();
            m_blinkCount = 0;
            m_blinkNextFrameCount = UnityEngine.Random.Range(GOLLY_GOSH_BLINK_FRAMES_RANDOM_MIN, GOLLY_GOSH_BLINK_FRAMES_RANDOM_MAX);
        }

        /// <summary>
        /// Rotate the body (entirety of character)
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="duration"></param>
        public void RotateTo(Quaternion rotation, float duration)
        {
            if (duration == 0.0f)
            {
                m_character.transform.rotation = rotation;
                return;
            }
            m_rotatingFromRotation = m_character.transform.rotation;
            m_rotatingToRotation = rotation;
            m_rotatingToTotalDuration = duration;
            m_rotatingToPassedDuration = 0.0f;
        }

        /// <summary>
        /// Move the body (entirety of character).
        /// </summary>
        /// <see cref="RotateTo">See RotateTo() To look at target position, if needed.</see>
        /// <param name="position"></param>
        /// <param name="duration"></param>
        /// <param name="pathPoints"></param>
        public void MoveTo(Vector3 position, bool immediate, Action<bool> completeAction = null, bool controlPositionArc = true, GollyGoshMovement moveType = GollyGoshMovement.None, float duration = -1.0f)
        {
            m_movingToCompleteAction = completeAction;
            if (immediate)
            {
                m_isMovingTo = false;
                m_character.transform.position = position;
                if (m_movingToCompleteAction != null)
                {
                    m_movingToCompleteAction(true);
                    m_movingToCompleteAction = null;
                }
                return;
            }

            m_isMovingTo = true;
            m_movementType = moveType;
            m_movingFromPosition = m_character.transform.position;
            m_movingToPosition = position;
            m_movingControlPosition = controlPositionArc
                ? CharacterUtilities.ControlPositionForLinearArc(m_movingFromPosition, m_movingToPosition, TARGET_GOLLY_GOSH_MOVE_OFFSET)
                : Vector3.Lerp(m_movingFromPosition, m_movingToPosition, 0.50f);

            var distance = Vector3.Distance(m_movingFromPosition, m_movingToPosition);
            if (duration <= 0)
            {
                duration = distance / TARGET_GOLLY_GOSH_MOVE_VELOCITY;
            }

            m_movingToTotalDuration = duration;
            m_movingToPassedDuration = 0.0f;
        }

        public void SetTouchInteraction(bool enable)
        {
            m_handTrigger.gameObject.SetActive(enable);
        }

        public Transform GetAudioTransform()
        {
            return m_mouthTransform.transform;
        }

        public Vector3 GetPosition()
        {
            return m_character.transform.position;
        }

        public Quaternion GetRotation()
        {
            return m_character.transform.rotation;
        }

        /// <summary>
        /// Rotate head to point toward direction (internals might have limits)
        /// </summary>
        /// <param name="target">transform to follow</param>
        public void GazeAt(Transform target)
        {
            m_gazeController.SetTarget(target);
        }

        public void GazeAtDelay(Transform target, float duration, Action<MovementController, bool> callback = null)
        {
            m_gazeController.MoveTo(target, duration, MovementController.CurveType.EaseInOut, callback);
        }

        /// <summary>
        /// Rotate head to point toward direction (internals might have limits)
        /// </summary>
        /// <param name="position">Vector position to look towards</param>
        public void GazeAt(Vector3 position)
        {
            m_gazeController.SetPosition(position);
        }

        public void GazeAtDelay(Vector3 position, float duration, Action<MovementController, bool> callback = null)
        {
            m_gazeController.MoveTo(position, duration, MovementController.CurveType.EaseInOut, callback);
        }

        public void GazeStop()
        {
            m_gazeController.SetTarget(null);
        }

        public void StopFollowing()
        {
            Follow(null, 0, 0);
        }

        /// <summary>
        /// Move body (entirety of character) to be within some thresholded distance
        /// </summary>
        /// <param name="position"></param>
        /// <param name="minDistance"></param>
        /// <param name="maxDistance"></param>
        public void Follow(Transform position, float minDistance, float maxDistance)
        {
            if (position == null)
            {
                m_isFollowing = false;
                m_followingTransform = null;
                return;
            }
            m_followLimitType = FollowLimits.XYZDistance;
            m_isFollowing = true;
            m_followingTransform = position;
            m_followingMinLimits.x = minDistance;
            m_followingMinLimits.y = -1.0f;
            m_followingMaxLimits.x = maxDistance;
            m_followingMaxLimits.y = -1.0f;
        }

        public void FollowXZandY(Transform position, float minDistanceXZ, float maxDistanceXZ, float minDistanceY, float maxDistanceY)
        {
            if (position == null)
            {
                m_isFollowing = false;
                m_followingTransform = null;
                return;
            }

            m_followLimitType = FollowLimits.XZseperableY;
            m_isFollowing = true;
            m_followingTransform = position;
            m_followingMinLimits.x = minDistanceXZ;
            m_followingMinLimits.y = minDistanceY;
            m_followingMaxLimits.x = maxDistanceXZ;
            m_followingMaxLimits.y = maxDistanceY;
        }

        public void PlayAnimation(GollyGoshAnimation animationType)
        {
            switch (animationType)
            {
                case GollyGoshAnimation.None:
                    m_animator.SetTrigger("NoneTrigger");
                    break;
                case GollyGoshAnimation.Idle:
                    m_animator.SetTrigger("IdleTrigger");
                    break;
                case GollyGoshAnimation.Flying:
                    m_animator.SetTrigger("CelebrateTrigger"); // placeholder for flying
                    break;
                case GollyGoshAnimation.Waving:
                    m_animator.SetTrigger("WaveTrigger");
                    break;
                case GollyGoshAnimation.Celebrate:
                    m_animator.SetTrigger("CelebrateTrigger");
                    break;
                case GollyGoshAnimation.PointForward:
                    m_animator.SetTrigger("PointForwardTrigger");
                    break;
                case GollyGoshAnimation.PointBackward:
                    m_animator.SetTrigger("PointBackwardTrigger");
                    break;
                case GollyGoshAnimation.ListenStart:
                    m_animator.SetTrigger("ListenStartTrigger");
                    break;
                case GollyGoshAnimation.ListenStop:
                    m_animator.SetTrigger("ListenStopTrigger");
                    break;
                case GollyGoshAnimation.TutorialPoint:
                    m_animator.SetTrigger("PointTrigger");
                    break;
                case GollyGoshAnimation.TutorialSqueeze:
                    m_animator.SetTrigger("SquishTrigger");
                    break;
                case GollyGoshAnimation.WavingContinuous:
                    m_animator.SetTrigger("WaveContinuousTrigger");
                    break;
                case GollyGoshAnimation.PointForwardContinuous:
                    m_animator.SetTrigger("PointForwardContinuousTrigger");
                    break;
                case GollyGoshAnimation.HelloWorld:
                    m_animator.SetTrigger("HelloWorldTrigger");
                    break;
                case GollyGoshAnimation.Beckon:
                    m_animator.SetTrigger("BeckonTrigger");
                    break;
            }
        }

        /// <summary>
        /// Facial expressions (eyes, may include mouth)
        /// </summary>
        public void ShowEyeEmotion(GollyGoshEyeEmotion emotionType)
        {
            if (m_isBlinking)
            {
                // Wait until blink is done
                m_endBlinkingEyeFrame = (int)emotionType;
            }
            else
            {
                m_currentEyeFrame = (int)emotionType;
            }
            UpdateInternalEmotion();
        }

        public void ShowMouthEmotion(GollyGoshMouthEmotion emotionType)
        {
            if (m_isTalking)
            {
                // Wait until talking is complete to update face
                m_endTalkingMouthFrame = (int)emotionType;
            }
            else
            {
                m_endTalkingMouthFrame = m_currentMouthFrame;
                m_currentMouthFrame = (int)emotionType;
                UpdateInternalEmotion();
            }
        }

        public void PointAt(Transform position, bool continuous)
        {
            m_animator.SetTrigger("PointTrigger");
        }

        private void Update()
        {
            if (m_isRotatingTo)
            {
                m_rotatingToPassedDuration += Time.deltaTime;
                var ratio = m_rotatingToPassedDuration / m_rotatingToTotalDuration;
                if (ratio >= 1.0f)
                {
                    m_character.transform.rotation = m_rotatingToRotation;
                    m_isRotatingTo = false;
                }
                else
                {
                    var rotation = Quaternion.Lerp(m_rotatingFromRotation, m_rotatingToRotation, ratio);
                    m_character.transform.rotation = rotation;
                }
            }

            if (m_isMovingTo)
            {
                m_movingToPassedDuration += Time.deltaTime;
                var ratio = m_movingToPassedDuration / m_movingToTotalDuration;
                if (ratio >= 1.0f)
                {
                    m_character.transform.position = m_movingToPosition;
                    m_isMovingTo = false;
                    m_movingToCompleteAction?.Invoke(true);
                }
                else
                {
                    var value = ratio;
                    if (m_movementType == GollyGoshMovement.EaseInOut)
                    {
                        value = m_curveEaseInOut.Evaluate(ratio);
                    }

                    m_character.transform.position = CharacterUtilities.BezierQuadraticAtT(m_movingFromPosition, m_movingControlPosition, m_movingToPosition, value);
                }
            }

            // Always following gaze:
            var gazeDirection = (m_gazeController.transform.position - m_character.transform.position).normalized;
            if (gazeDirection.magnitude > 0)
            {
                var gazeGoal = Quaternion.LookRotation(gazeDirection, Vector3.up);
                var charRotation = Quaternion.Lerp(m_character.transform.rotation, gazeGoal, m_lookingAtDelay);
                m_character.transform.rotation = charRotation;
            }

            if (m_isFollowing)
            {
                var direction = m_followingTransform.position - m_character.transform.position;

                if (m_followLimitType == FollowLimits.XYZDistance)
                {
                    var distance = direction.magnitude;
                    var minDistance = m_followingMinLimits.x;
                    var maxDistance = m_followingMaxLimits.x;
                    // Too close
                    if (distance < minDistance)
                    {
                        direction.Normalize();
                        direction.Scale(new Vector3(minDistance, minDistance, minDistance));
                        var goal = m_followingTransform.position - direction;
                        var position = Vector3.Lerp(m_character.transform.position, goal, m_followingDelay);
                        m_character.transform.position = position;
                    }
                    // Too far
                    else if (distance > maxDistance)
                    {
                        direction.Normalize();
                        direction.Scale(new Vector3(maxDistance, maxDistance, maxDistance));
                        var goal = m_followingTransform.position - direction;
                        var position = Vector3.Lerp(m_character.transform.position, goal, m_followingDelay);
                        m_character.transform.position = position;
                    }
                }
                else // FollowLimits.XZseperableY
                {
                    var direction2D = new Vector2(direction.x, direction.z);
                    var distanceXZ = direction2D.magnitude;
                    var directionY = direction.y;
                    var distanceY = Mathf.Abs(directionY);

                    var minDistanceXZ = m_followingMinLimits.x;
                    var maxDistanceXZ = m_followingMaxLimits.x;
                    var minDistanceY = m_followingMinLimits.y;
                    var maxDistanceY = m_followingMaxLimits.y;

                    // Y
                    if (distanceY < minDistanceY)
                    {
                        directionY = directionY < 0 ? -minDistanceY : minDistanceY;
                        var goalY = m_followingTransform.position.y - directionY;
                        var position = m_character.transform.position;
                        position.y = Mathf.Lerp(position.y, goalY, m_followingDelay);
                        m_character.transform.position = position;
                    }
                    else if (distanceY > maxDistanceY)
                    {
                        directionY = directionY < 0 ? -maxDistanceY : maxDistanceY;
                        var goalY = m_followingTransform.position.y - directionY;
                        var position = m_character.transform.position;
                        position.y = Mathf.Lerp(position.y, goalY, m_followingDelay);
                        m_character.transform.position = position;
                    }

                    // XZ
                    if (distanceXZ < minDistanceXZ)
                    {
                        direction2D.Normalize();
                        direction2D.Scale(new Vector2(minDistanceXZ, minDistanceXZ));
                        var position = m_followingTransform.position;
                        var position2D = new Vector2(position.x, position.z);
                        var goal2D = position2D - direction2D;

                        position = m_character.transform.position;
                        position2D.x = position.x;
                        position2D.y = position.z;
                        position2D = Vector2.Lerp(position2D, goal2D, m_followingDelay);
                        position.x = position2D.x;
                        position.z = position2D.y;
                        m_character.transform.position = position;
                    }
                    else if (distanceXZ > maxDistanceXZ)
                    {
                        direction2D.Normalize();
                        direction2D.Scale(new Vector2(maxDistanceXZ, maxDistanceXZ));
                        var position = m_followingTransform.position;
                        var position2D = new Vector2(position.x, position.z);
                        var goal2D = position2D - direction2D;

                        position = m_character.transform.position;
                        position2D.x = position.x;
                        position2D.y = position.z;
                        position2D = Vector2.Lerp(position2D, goal2D, m_followingDelay);
                        position.x = position2D.x;
                        position.z = position2D.y;
                        m_character.transform.position = position;
                    }
                }
            }

            if (m_isTalking)
            {
                m_currentMouthSubFrame += 1;
                if (m_currentMouthSubFrame > GOLLY_GOSH_MOUTH_FRAMERATE)
                {
                    m_currentMouthSubFrame = 0;
                    m_currentMouthFrame += 1;
                    if (m_currentMouthFrame > GOLLY_GOSH_MAX_FRAMES_MOUTH)
                    {
                        m_currentMouthFrame = 0;
                    }

                    UpdateInternalEmotion();
                }
            }

            if (m_isBlinking)
            {
                m_blinkCount += 1;
                if (m_blinkCount > m_blinkNextFrameCount)
                {
                    StopBlinking();
                }

            } // Waiting to blink again:
            else
            {
                m_blinkCount += 1;
                if (m_blinkCount > m_blinkNextFrameCount)
                {
                    StartBlinking();
                }
            }
        }

        private void OnHandTriggered() => UserTouched?.Invoke();

        private void UpdateInternalEmotion()
        {
            var block = new MaterialPropertyBlock();
            m_characterMeshRenderer.GetPropertyBlock(block); // Get existing properties
            block.SetFloat("_Mouth_Frame_Number", m_currentMouthFrame);
            block.SetFloat("_Eye_Frame_Number", m_currentEyeFrame);
            m_characterMeshRenderer.SetPropertyBlock(block); // Apply the block
        }
    }
}