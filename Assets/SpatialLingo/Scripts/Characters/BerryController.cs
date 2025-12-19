// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections;
using Meta.XR.Samples;
using SpatialLingo.Audio;
using SpatialLingo.Interactions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpatialLingo.Characters
{
    [MetaCodeSample("SpatialLingo")]
    public class BerryController : MonoBehaviour
    {
        private const float TARGET_BERRY_MOVEMENT_VELOCITY = 1.0f;
        private const float TARGET_BERRY_PATH_OFFSET_METERS = 1.5f;

        private const float BERRY_GOLDEN_COLOR_VALUE_OFF = 0.0f;
        private const float BERRY_GOLDEN_COLOR_VALUE_MAX = 1.0f;
        private const float BERRY_GOLDEN_COLOR_VALUE_MIN = 0.6f;
        private const float BERRY_GOLDEN_COLOR_INCREMENT_START = 0.01f;
        private const float BERRY_GOLDEN_COLOR_INCREMENT_HOVER = 0.001f;

        private const int BERRY_SQUEAK_MIN_INTERVAL = 3;
        private const int BERRY_SQUEAK_MAX_INTERVAL = 10;

        private const float LOOK_RAND_MIN_SECONDS = 5.0f;
        private const float LOOK_RAND_MAX_SECONDS = 10.0f;
        private const float LOOK_AT_DECAY_RATE = 0.10f;

        private static Color s_startColorBerryA = new(0.8f, 0.2f, 0.2f, 0.8f);
        private static Color s_startColorBerryB = new(0.8f, 0.2f, 0.5f, 0.8f);
        private static Color s_startColorBerryC = new(0.2f, 0.3f, 0.8f, 0.8f);
        private static Color s_startColorBerryD = new(0.6f, 0.1f, 1.0f, 0.8f);
        private static Color s_startColorGolden = new(1.0f, 0.8f, 0.0f, 0.8f);
        private bool m_isPlayingSqueezeEffect = false;
        private BerryDisplayVersion m_berryVersion;
        private Coroutine m_pathDelayCoroutine;
        private Coroutine m_soundAmbientDelayCoroutine;

        public enum BerryDisplayVersion
        {
            BerryA = 0, // Strawberry
            BerryB = 1, // Raspberry
            BerryC = 2, // Blueberry
            BerryD = 3, // Grapeberry
        }

        [Header("Hierarchy")]
        [SerializeField] private GameObject m_berryContainer;
        [SerializeField] private Transform m_berryTargetTransform;
        [SerializeField] private HandTrigger m_handTrigger;
        [SerializeField] private GameObject m_pathEffects;
        [SerializeField] private ParticleSystem m_pathParticleSystem;
        [SerializeField] private ParticleSystem m_squishEffects;
        [SerializeField] private ParticleSystem m_shimmerEffects;

        [Header("Assets")]
        [SerializeField] private Animator m_animator;
        [SerializeField] private GameObject m_berryPrefabA;
        [SerializeField] private GameObject m_berryPrefabB;
        [SerializeField] private GameObject m_berryPrefabC;
        [SerializeField] private GameObject m_berryPrefabD;

        public delegate void BerrySqueezeInteractionEvent(BerryController controller);
        public event BerrySqueezeInteractionEvent BerrySqueezeInteraction;

        public bool IsGolden => m_isGolden;
        public bool PlayEffectOnTouch { get; set; }

        private GameObject m_currentBerry;

        private float m_startTime;
        private float m_moveDuration;
        private bool m_isMoving = false;
        private Vector3 m_startPosition;
        private Vector3 m_controlPosition;
        private Vector3 m_endPosition;
        private Action<BerryController> m_moveCallback;
        private bool m_isGolden = false;
        private bool m_isShimmering = false;
        private float m_currentGoldenValue;
        private float m_currentGoldenIncrement = 0.0f;
        private MeshRenderer m_berryMeshRenderer;
        private AudioSource m_ambientAudioSource;
        private AudioSource m_trailAudioSource;
        private Coroutine m_playRandomSqueaksCoroutine;

        private bool m_isLooking;
        private bool m_isActivelyLooking;
        private Transform m_lookAtTransform;
        private float m_nextLookDuration;
        private float m_lastLookTime;

        private void Awake()
        {
            m_isShimmering = false;
            m_shimmerEffects.Stop();
            m_shimmerEffects.Clear();
            DisableInteraction();
            m_pathEffects.SetActive(false);
            HideSqueezeEffect();
            m_currentGoldenValue = BERRY_GOLDEN_COLOR_VALUE_OFF;
        }

        private void OnEnable()
        {
            PlayRandomSqueaks();
            m_handTrigger.HandTriggered += OnUserInteract;
        }

        private void OnDisable()
        {
            if (m_playRandomSqueaksCoroutine != null)
            {
                StopCoroutine(m_playRandomSqueaksCoroutine);
                m_playRandomSqueaksCoroutine = null;
            }

            m_handTrigger.HandTriggered -= OnUserInteract;
        }

        public void ShowPathEffects()
        {
            StopDelayPathHideCoroutine();
            m_pathEffects.SetActive(true);
        }

        public void HidePathEffects(bool delay = false)
        {
            StopDelayPathHideCoroutine();
            if (delay)
            {
                m_pathDelayCoroutine = StartCoroutine(DelayHidePath());
            }
            else
            {
                m_pathEffects.SetActive(false);
            }
        }

        private IEnumerator DelayHidePath()
        {
            yield return new WaitForSeconds(1.5f);
            m_pathEffects.SetActive(false);
            m_pathDelayCoroutine = null;
        }

        private void StopDelayPathHideCoroutine()
        {
            if (m_pathDelayCoroutine != null)
            {
                StopCoroutine(m_pathDelayCoroutine);
                m_pathDelayCoroutine = null;
            }
        }

        public void ShowFace(Transform lookAtTransform = null)
        {
            if (m_berryMeshRenderer != null)
            {
                var block = new MaterialPropertyBlock();
                m_berryMeshRenderer.GetPropertyBlock(block);
                block.SetFloat("_Enable_Face_Sprites", 1.0f);
                m_berryMeshRenderer.SetPropertyBlock(block);
            }

            if (lookAtTransform != null)
            {
                m_lookAtTransform = lookAtTransform;
                m_isLooking = true;
                m_isActivelyLooking = true;
            }
        }

        public void HideFace()
        {
            if (m_berryMeshRenderer != null)
            {
                var block = new MaterialPropertyBlock();
                m_berryMeshRenderer.GetPropertyBlock(block);
                block.SetFloat("_Enable_Face_Sprites", 0.0f);
                m_berryMeshRenderer.SetPropertyBlock(block);
            }

            m_isLooking = false;
            m_isActivelyLooking = false;
            m_lookAtTransform = null;
        }

        public void ResumeFromInteraction()
        {
            gameObject.SetActive(true);
            if (m_isShimmering)
            {
                ShowShimmer();
            }
            else
            {
                HideShimmer();
            }
        }

        public void ShowShimmer()
        {
            if (m_isShimmering)
            {
                return;
            }
            m_isShimmering = true;
            m_shimmerEffects.Play();
            PlayAmbientSound();
        }

        public void HideShimmer()
        {
            if (!m_isShimmering)
            {
                return;
            }
            m_isShimmering = false;
            m_shimmerEffects.Stop();
            StopAmbientSound();
        }

        private void PlayRandomSqueaks()
        {
            var delay = Random.Range(BERRY_SQUEAK_MIN_INTERVAL, BERRY_SQUEAK_MAX_INTERVAL);
            if (m_playRandomSqueaksCoroutine != null)
            {
                StopCoroutine(m_playRandomSqueaksCoroutine);
                m_playRandomSqueaksCoroutine = null;
            }
            m_playRandomSqueaksCoroutine = StartCoroutine(PlayRandomSqueaksCoroutine(delay));
        }

        private IEnumerator PlayRandomSqueaksCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            // Don't start a squeak if we're moving
            if (!m_isMoving)
            {
                AppAudioController.Instance.PlaySound(SoundEffect.BerrySqueaks, transform.position);
            }
            PlayRandomSqueaks();
        }

        private void ShowSqueezeEffect()
        {
            m_squishEffects.gameObject.SetActive(true);
            m_squishEffects.time = 0;
            m_squishEffects.Play();
        }

        private void HideSqueezeEffect()
        {
            m_squishEffects.time = 0;
            m_squishEffects.Pause();
            m_squishEffects.gameObject.SetActive(false);
        }

        public void PlaySqueezeEffect()
        {
            if (m_isPlayingSqueezeEffect)
            {
                return;
            }

            AppAudioController.Instance.PlaySound(m_isGolden ? SoundEffect.BerrySqueezeGolden : SoundEffect.BerrySqueeze, transform.position);

            m_isPlayingSqueezeEffect = true;
            _ = StartCoroutine(SqueezeEffectCoroutine());
        }

        public void PlayWiggleEffect()
        {
            m_animator.SetTrigger("WiggleTrigger");
        }

        public void PlayExitEffect()
        {
            m_animator.SetTrigger("DeactivateTrigger");
        }

        private IEnumerator SqueezeEffectCoroutine()
        {
            if (m_squishEffects != null)
            {
                ShowSqueezeEffect();
                yield return new WaitForSeconds(2.0f);
                if (m_squishEffects != null)
                {
                    HideSqueezeEffect();
                }
            }

            m_isPlayingSqueezeEffect = false;
        }

        private void PlayAmbientSound()
        {
            StopAmbientSound();
            m_soundAmbientDelayCoroutine = StartCoroutine(PlayAmbientSoundDelayed());
        }

        private IEnumerator PlayAmbientSoundDelayed()
        {
            yield return new WaitForEndOfFrame();
            m_ambientAudioSource = AppAudioController.Instance.PlaySound(SoundEffect.GoldenBerryProximity, transform, true);
            m_soundAmbientDelayCoroutine = null;
        }

        private void StopSoundAmbientDelayCoroutine()
        {
            if (m_soundAmbientDelayCoroutine != null)
            {
                StopCoroutine(m_soundAmbientDelayCoroutine);
                m_soundAmbientDelayCoroutine = null;
            }
        }

        private void StopAmbientSound()
        {
            StopSoundAmbientDelayCoroutine();
            if (m_ambientAudioSource != null)
            {
                AppAudioController.Instance.StopSound(m_ambientAudioSource);
                m_ambientAudioSource = null;
            }
        }

        public void PlayBerryTrailSound()
        {
            if (m_trailAudioSource != null)
            {
                StopBerryTrailSound();
            }
            m_trailAudioSource = AppAudioController.Instance.PlaySound(SoundEffect.BerryTrailLoop, transform, true);
        }

        public void StopBerryTrailSound()
        {
            AppAudioController.Instance.StopSound(m_trailAudioSource);
            m_trailAudioSource = null;
        }

        public void MoveTo(Vector3 endPosition)
        {
            transform.position = endPosition;
        }

        public void MoveToDestination(Vector3 startPosition, Vector3 endPosition, Action<BerryController> callback = null)
        {
            var startToEnd = endPosition - startPosition;
            var distance = startToEnd.magnitude;
            m_moveDuration = distance / TARGET_BERRY_MOVEMENT_VELOCITY;
            m_startTime = Time.time;
            m_startPosition = startPosition;
            m_endPosition = endPosition;
            m_controlPosition = CharacterUtilities.ControlPositionForLinearArc(startPosition, endPosition, TARGET_BERRY_PATH_OFFSET_METERS);
            m_moveCallback = callback;
            m_isMoving = true;
        }

        public void TurnGoldenColor()
        {
            m_isGolden = true;
            m_currentGoldenIncrement = BERRY_GOLDEN_COLOR_INCREMENT_START;
            SetGoldenMaterialColorValue(m_currentGoldenValue);
            SetParticleStartColor();
        }

        public void TurnBerryColor()
        {
            m_isGolden = false;
            m_currentGoldenIncrement = -BERRY_GOLDEN_COLOR_INCREMENT_START;
            SetParticleStartColor();
        }

        private void AnimateGoldenColor()
        {
            m_currentGoldenValue += m_currentGoldenIncrement;
            if (m_currentGoldenIncrement > 0)
            {
                if (m_currentGoldenValue > BERRY_GOLDEN_COLOR_VALUE_MAX)
                {
                    m_currentGoldenIncrement = -BERRY_GOLDEN_COLOR_INCREMENT_HOVER;
                    m_currentGoldenValue = BERRY_GOLDEN_COLOR_VALUE_MAX;
                }
            }
            else
            {
                if (m_currentGoldenValue < BERRY_GOLDEN_COLOR_VALUE_MIN)
                {
                    m_currentGoldenIncrement = BERRY_GOLDEN_COLOR_INCREMENT_HOVER;
                    m_currentGoldenValue = BERRY_GOLDEN_COLOR_VALUE_MIN;
                }
            }

            SetGoldenMaterialColorValue(m_currentGoldenValue);
        }

        private void AnimateBerryColor()
        {
            m_currentGoldenValue += m_currentGoldenIncrement;
            if (m_currentGoldenValue < BERRY_GOLDEN_COLOR_VALUE_OFF)
            {
                m_currentGoldenValue = BERRY_GOLDEN_COLOR_VALUE_OFF;
                m_currentGoldenIncrement = 0.0f;
            }
            SetGoldenMaterialColorValue(m_currentGoldenValue);
        }

        private void SetGoldenMaterialColorValue(float value)
        {
            if (m_berryMeshRenderer != null)
            {
                var block = new MaterialPropertyBlock();
                m_berryMeshRenderer.GetPropertyBlock(block);
                block.SetFloat("_Golden_FX_Amount", value);
                m_berryMeshRenderer.SetPropertyBlock(block);
            }
        }

        public void DisplayRandomBerry()
        {
            var number = Random.Range(0, 3);
            switch (number)
            {
                case 0:
                    DisplayBerry(BerryDisplayVersion.BerryA);
                    break;
                case 1:
                    DisplayBerry(BerryDisplayVersion.BerryB);
                    break;
                case 2:
                    DisplayBerry(BerryDisplayVersion.BerryC);
                    break;
                default: // 3
                    DisplayBerry(BerryDisplayVersion.BerryD);
                    break;
            }
        }

        public void DisplayBerry(BerryDisplayVersion version)
        {
            if (m_currentBerry != null)
            {
                m_berryMeshRenderer = null;
                Destroy(m_currentBerry);
            }

            m_berryVersion = version;
            var prefab = version switch
            {
                BerryDisplayVersion.BerryA => m_berryPrefabA,
                BerryDisplayVersion.BerryB => m_berryPrefabB,
                BerryDisplayVersion.BerryC => m_berryPrefabC,
                BerryDisplayVersion.BerryD => m_berryPrefabD,
                _ => m_berryPrefabA,
            };
            m_currentBerry = Instantiate(prefab, m_berryContainer.transform);
            m_berryMeshRenderer = m_currentBerry.GetComponentInChildren<MeshRenderer>();
            SetGoldenMaterialColorValue(m_currentGoldenValue);
            HideFace();
            SetParticleStartColor();
        }

        private void SetParticleStartColor()
        {
            var main = m_pathParticleSystem.main;
            main.startColor = m_isGolden
                ? (ParticleSystem.MinMaxGradient)s_startColorGolden
                : m_berryVersion switch
                {
                    BerryDisplayVersion.BerryA => (ParticleSystem.MinMaxGradient)s_startColorBerryA,
                    BerryDisplayVersion.BerryB => (ParticleSystem.MinMaxGradient)s_startColorBerryB,
                    BerryDisplayVersion.BerryC => (ParticleSystem.MinMaxGradient)s_startColorBerryC,
                    BerryDisplayVersion.BerryD => (ParticleSystem.MinMaxGradient)s_startColorBerryD,
                    _ => (ParticleSystem.MinMaxGradient)s_startColorBerryA,
                };
        }

        public void OnUserInteract()
        {
            BerrySqueezeInteraction?.Invoke(this);
            if (PlayEffectOnTouch)
            {
                PlaySqueezeEffect();
            }

            // Stop & allow re-start of user interaction
            DisableInteraction();
            EnableInteraction();
        }
        public void EnableInteraction()
        {
            m_handTrigger.gameObject.SetActive(true);
        }

        public void DisableInteraction()
        {
            m_handTrigger.gameObject.SetActive(false);
        }

        public void FadeOutDestroy()
        {
            _ = StartCoroutine(FadeOutCoroutine());
        }

        private IEnumerator FadeOutCoroutine()
        {
            // Play animate out effect
            PlayExitEffect();

            // Wait for animation to finish
            yield return new WaitForSeconds(1.0f);
            // Stop any coroutines
            if (m_playRandomSqueaksCoroutine != null)
            {
                StopCoroutine(m_playRandomSqueaksCoroutine);
                m_playRandomSqueaksCoroutine = null;
            }
            // Stop any cached sounds
            AppAudioController.Instance.StopSound(m_ambientAudioSource);
            AppAudioController.Instance.StopSound(m_trailAudioSource);
            // Finally, destroy self
            Destroy(gameObject);
        }

        private void Update()
        {
            if (m_isMoving)
            {
                var diff = Time.time - m_startTime;
                var ratio = diff / m_moveDuration;
                if (ratio >= 1.0f)
                {
                    transform.position = m_endPosition;
                    if (m_moveCallback != null)
                    {
                        m_moveCallback(this);
                        m_moveCallback = null;
                    }
                    m_isMoving = false;
                }
                else
                {
                    transform.position = CharacterUtilities.BezierQuadraticAtT(m_startPosition, m_controlPosition, m_endPosition, ratio);
                }
            }

            if (m_isGolden)
            {
                AnimateGoldenColor();
            }
            else if (m_currentGoldenIncrement < 0.0f)
            {
                AnimateBerryColor();
            }

            if (m_isLooking)
            {
                if (m_isActivelyLooking)
                {
                    var forward = m_lookAtTransform.position - m_berryTargetTransform.position;
                    forward.y = 0; // Ignore any rotation up/down
                    forward.Normalize();
                    var upward = Vector3.up;
                    var currentRotation = m_berryTargetTransform.rotation;
                    var goalRotation = Quaternion.LookRotation(forward, upward);
                    var nextRotation = Quaternion.Lerp(currentRotation, goalRotation, LOOK_AT_DECAY_RATE);
                    var diff = Quaternion.Angle(nextRotation, goalRotation);
                    m_berryTargetTransform.rotation = nextRotation;

                    // Close enough to desired direction, stop and look again later
                    if (diff < 1.0f)
                    {
                        m_isActivelyLooking = false;
                        m_lastLookTime = Time.time;
                        m_nextLookDuration = Random.Range(LOOK_RAND_MIN_SECONDS, LOOK_RAND_MAX_SECONDS);
                    }
                }
                else
                {
                    // Waited long enough, start looking in destired direction;
                    var diff = Time.time - m_lastLookTime;
                    if (diff > m_nextLookDuration)
                    {
                        m_isActivelyLooking = true;
                    }
                }

            }
        }
    }
}