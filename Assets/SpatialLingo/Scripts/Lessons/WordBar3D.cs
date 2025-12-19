// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using Oculus.Interaction;
using SpatialLingo.Animation;
using SpatialLingo.Audio;
using SpatialLingo.SceneObjects;
using SpatialLingo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using UnityEngine.Timeline;
using Random = UnityEngine.Random;

namespace SpatialLingo.Lessons
{
    [MetaCodeSample("SpatialLingo")]
    public class WordBar3D : MonoBehaviour
    {
        [MetaCodeSample("SpatialLingo")]
        public class AnimationTarget
        {
            public Vector3 StartPosition;
            public Vector3 StartScale;
            public Vector3 EndPosition;
            public Vector3 EndScale;
            public float Duration;
            public float StartTime;
            public TextCloudItem Target;
            public GameObject Feature;

            public Color StartColor;
            public Color EndColor;
            public Color StartBackerColor;
            public Color EndBackerColor;

            public bool ActiveColor = false;
            public bool ActivePosition = false;
            public bool ActiveScale = false;
            public bool ActiveWorldPosition = false;
            public bool ActiveBackerColor = false;

            public Vector2 StartBackerSize;
            public Vector2 EndBackerSize;
            public bool ActiveBacker = false;

            public AnimationCurve Curve;

            public Action CompleteAction;

            public bool Process()
            {
                var ratio = (Time.time - StartTime) / Duration;
                if (ratio >= 1)
                {
                    if (ActiveColor)
                    {
                        Target.SetTextColor(EndColor);
                    }
                    if (ActiveBackerColor)
                    {
                        Target.UpdateBlockColor(EndBackerColor);
                    }
                    if (ActivePosition)
                    {
                        Feature.transform.localPosition = EndPosition;
                    }
                    if (ActiveWorldPosition)
                    {
                        Feature.transform.position = EndPosition;
                    }
                    if (ActiveScale)
                    {
                        Feature.transform.localScale = EndScale;
                    }
                    if (ActiveBacker)
                    {
                        Target.SetBackerFromSize(EndBackerSize.x, EndBackerSize.y);
                    }
                    CompleteAction?.Invoke();

                    return true;
                }
                // Use a curve value
                var value = Curve.Evaluate(ratio);
                if (ActiveColor)
                {
                    Target.SetTextColor(Color.Lerp(StartColor, EndColor, ratio));
                }
                if (ActiveBackerColor)
                {
                    Target.UpdateBlockColor(Color.Lerp(StartBackerColor, EndBackerColor, ratio));
                }
                if (ActivePosition)
                {
                    Feature.transform.localPosition = Vector3.LerpUnclamped(StartPosition, EndPosition, value);
                }
                if (ActiveWorldPosition)
                {
                    Feature.transform.position = Vector3.LerpUnclamped(StartPosition, EndPosition, value);
                }
                if (ActiveScale)
                {
                    Feature.transform.localScale = Vector3.LerpUnclamped(StartScale, EndScale, value);
                }
                if (ActiveBacker)
                {
                    var size = Vector2.LerpUnclamped(StartBackerSize, EndBackerSize, value);
                    Target.SetBackerFromSize(size.x, size.y);
                }

                return false;
            }
        }

        [SerializeField] private TextCloudItem m_textNode;
        [SerializeField] private Animator m_animator;
        [SerializeField] private AudioSource m_audioSource;
        [FormerlySerializedAs("m_tMP")] [SerializeField] private TextMeshProUGUI m_tmp;
        [SerializeField] private AnimationCurve m_animationEasingCurve;
        [SerializeField] private PlayableDirector m_completionSequence;
        [SerializeField] private TouchHandGrabInteractable m_touchHandGrabInteractableComponent;

        public delegate void RequestSpeakEvent(string text);
        public event RequestSpeakEvent RequestSpeak;

        public delegate void InteractionEvent(WordBar3D wordGO);
        public event InteractionEvent PokeInteraction;
        public event InteractionEvent SqueezeInteraction;

        private string m_defaultText = "";
        private string m_squeezeText = "";

        private bool m_isSqueezeEnabled = true;
        private bool m_isPokeEnabled = true;
        private bool m_doubleFunctionPoke;

        public TextCloudItem TextNode => m_textNode;
        public TextCloudItem.WordType WordType => m_textNode.WordUsage;

        public string SpeakingPhrase
        {
            private set;
            get;
        }

        public Vector2 Size
        {
            get
            {
                var local = m_textNode.transform.localScale;
                var size = m_textNode.Size;
                size.x *= local.x;
                size.y *= local.y;
                return size;
            }
        }

        private Vector2 m_backerSizeDefault;
        private Vector2 m_backerSizeSqueeze;

        private Color m_finalCompleteColor = new(1.0f, 1.0f, 1.0f);
        private Color m_inactiveTextColor = new(0.831f, 0.831f, 0.831f, 1.0f);
        private Color m_activeTextColor = new(0.875f, 0.545f, 0.996f, 1.0f);

        private Vector3 m_inactiveLocalPosition = new(0.0f, 0.0f, 0.0f);
        private Vector3 m_activeLocalPosition = new(0.0f, 0.0f, 0.04f);

        private Vector3 m_visibleLocalScale = new(1.0f, 1.0f, 1.0f); // large/normal size
        private Vector3 m_invisibleLocalScale = new(0.01f, 0.01f, 0.01f); // tiny

        private bool m_autoCompleteInteraction = false;

        private List<AnimationTarget> m_textAnimations = new();
        private InteractionState m_interactionState = InteractionState.Inactive;
        private enum InteractionState
        {
            Inactive,
            Pressed,
            Squeezed,
            Completed
        }

        private void OnEnable()
        {
            m_textAnimations.Clear();
        }

        public void Initialize(string primary, string secondary, TextCloudItem.WordType type, Transform focus, string speakingPhrase, bool doubleFunctionPoke = false)
        {
            m_defaultText = primary;
            m_squeezeText = secondary;
            SpeakingPhrase = speakingPhrase;
            m_textNode.WordUsage = type;

            m_backerSizeDefault = m_textNode.BackerSizeForDisplayText(m_defaultText);
            m_backerSizeSqueeze = m_textNode.BackerSizeForDisplayText(m_squeezeText);
            if (TryGetComponent<LookAtFocusBehaviour>(out var lookAt))
            {
                lookAt.Focus = focus;
                if (focus == null)
                {
                    lookAt.enabled = false;
                }
            }
            m_textNode.SetTextColor(m_inactiveTextColor);
            UpdateTextDisplayWithText(m_defaultText);
            m_doubleFunctionPoke = doubleFunctionPoke;
        }

        public void EnableSqueezeInteraction()
        {
            m_isSqueezeEnabled = true;
        }

        public void DisableSqueezeInteraction()
        {
            m_isSqueezeEnabled = false;
        }

        public void EnablePokeInteraction()
        {
            m_isPokeEnabled = true;
        }

        public void DisablePokeInteraction()
        {
            m_isPokeEnabled = false;
        }

        private void UpdateTextDisplayWithText(string text)
        {
            m_textNode.DisplayWord = text;
            m_textNode.UpdateAllDisplayFromInternals();
        }

        public void PokeSelectAutocomplete(bool playAudio = true)
        {
            if (m_interactionState != InteractionState.Inactive)
            {
                return;
            }
            m_autoCompleteInteraction = true;
            OnPokeSelected(playAudio);
        }

        private void OnPokeSelected()
        {
            if (!m_isPokeEnabled)
            {
                return;
            }
            OnPokeSelected(true);
        }

        private void OnPokeSelected(bool playAudio)
        {
            if (m_interactionState != InteractionState.Inactive)
            {
                return;
            }
            PokeInteraction?.Invoke(this);
            m_interactionState = InteractionState.Pressed;
            ActivateText();
            if (playAudio)
            {
                RequestSpeak?.Invoke(SpeakingPhrase);
            }

            AppAudioController.Instance.PlaySound(SoundEffect.WordCloudTapped, transform.position);
        }

        private void OnPokeUnselected()
        {
            if (m_doubleFunctionPoke)
            {
                return;
            }
            if (m_interactionState != InteractionState.Pressed)
            {
                return;
            }
            DeactivateText();
        }

        private void ActivateText()
        {
            m_textAnimations.Clear();
            var anim = new AnimationTarget
            {
                Target = m_textNode,
                Feature = m_tmp.gameObject,
                StartColor = m_inactiveTextColor,
                EndColor = m_activeTextColor,
                StartPosition = m_inactiveLocalPosition,
                EndPosition = m_activeLocalPosition,
                Duration = 0.30f,
                ActivePosition = true,
                ActiveColor = true,
                CompleteAction = ActivateTextCompleted,
                Curve = m_animationEasingCurve,
                StartTime = Time.time
            };
            m_textAnimations.Add(anim);
        }

        private void ActivateTextCompleted()
        {
            if (m_autoCompleteInteraction || m_doubleFunctionPoke)
            {
                DeactivateText();
                m_autoCompleteInteraction = false;
            }
        }

        private void DeactivateText()
        {
            // Out-tro might conflict with in-tro if not completed yet
            if (!m_autoCompleteInteraction && !m_doubleFunctionPoke)
            {
                m_textAnimations.Clear();
            }

            var anim = new AnimationTarget
            {
                Target = m_textNode,
                Feature = m_tmp.gameObject,
                StartColor = m_activeTextColor,
                EndColor = m_inactiveTextColor,
                StartPosition = m_activeLocalPosition,
                EndPosition = m_inactiveLocalPosition,
                Duration = 0.25f,
                ActivePosition = true,
                ActiveColor = true,
                CompleteAction = DeactivateTextCompleted,
                Curve = m_animationEasingCurve,
                StartTime = Time.time
            };
            m_textAnimations.Add(anim);
        }

        private void DeactivateTextCompleted()
        {
            // Continue thru squeeze interaction
            if (m_doubleFunctionPoke)
            {
                TransferDefaultTextOut();
                return;
            }
            if (m_interactionState == InteractionState.Pressed)
            {
                m_interactionState = InteractionState.Inactive;
            }
            else
            {
                Debug.LogWarning($"Completed poking animation but found state: {m_interactionState}");
            }
        }

        // Squeeze start
        private void RunSqueezeSequence()
        {
            m_textAnimations.Clear();
            TransferDefaultTextOut();
        }

        private void SetMoveTrackTargetTransform(Transform target)
        {
            var timeline = (TimelineAsset)m_completionSequence.playableAsset;
            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is MoveWordBar moveTrack)
                {
                    m_completionSequence.SetGenericBinding(moveTrack, target);
                    break;
                }
            }
        }

        private void TransferDefaultTextOut()
        {
            var anim = new AnimationTarget
            {
                Target = m_textNode,
                Feature = m_tmp.gameObject,
                StartScale = m_visibleLocalScale,
                EndScale = m_invisibleLocalScale,
                ActiveScale = true,
                CompleteAction = TransferDefaultTextOutCompleted,
                Duration = 0.25f,
                Curve = m_animationEasingCurve,
                StartTime = Time.time
            };
            m_textAnimations.Add(anim);
        }

        private void TransferDefaultTextOutCompleted()
        {
            m_textNode.SetTextDisplay(m_squeezeText);
            m_textNode.SetTextColor(m_activeTextColor);
            TransferSqueezeTextIn();
        }

        private void TransferSqueezeTextIn()
        {
            var anim = new AnimationTarget
            {
                Target = m_textNode,
                Feature = m_tmp.gameObject,
                StartScale = m_invisibleLocalScale,
                EndScale = m_visibleLocalScale,
                ActiveScale = true,
                StartBackerSize = m_backerSizeDefault,
                EndBackerSize = m_backerSizeSqueeze,
                ActiveBacker = true,
                CompleteAction = TransferSqueezeTextInCompleted,
                Duration = 0.25f,
                Curve = m_animationEasingCurve,
                StartTime = Time.time
            };
            m_textAnimations.Add(anim);
        }

        private void TransferSqueezeTextInCompleted()
        {
            _ = StartCoroutine(WaitForTranslationTime());
        }

        private IEnumerator WaitForTranslationTime()
        {
            yield return new WaitForSeconds(1.5f);
            TransferSqueezeTextOut();
        }

        private void TransferSqueezeTextOut()
        {
            var anim = new AnimationTarget
            {
                Target = m_textNode,
                Feature = m_tmp.gameObject,
                StartScale = m_visibleLocalScale,
                EndScale = m_invisibleLocalScale,
                ActiveScale = true,
                CompleteAction = TransferSqueezeTextOutCompleted,
                Duration = 0.25f,
                Curve = m_animationEasingCurve,
                StartTime = Time.time
            };
            m_textAnimations.Add(anim);
        }

        private void TransferSqueezeTextOutCompleted()
        {
            m_textNode.SetTextDisplay(m_defaultText);
            m_textNode.SetTextColor(m_inactiveTextColor);
            TransferDefaultTextIn();
        }

        private void TransferDefaultTextIn()
        {
            var anim = new AnimationTarget
            {
                Target = m_textNode,
                Feature = m_tmp.gameObject,
                StartTime = Time.time,
                StartScale = m_invisibleLocalScale,
                EndScale = m_visibleLocalScale,
                ActiveScale = true,
                StartBackerSize = m_backerSizeSqueeze,
                EndBackerSize = m_backerSizeDefault,
                ActiveBacker = true,
                CompleteAction = TransferDefaultTextInCompleted,
                Duration = 0.25f,
                Curve = m_animationEasingCurve
            };
            anim.StartTime = Time.time;
            m_textAnimations.Add(anim);

            // Play original word restored sound
            AppAudioController.Instance.PlaySound(SoundEffect.WordCloudPinched, transform.position, variation: 1);
        }

        private void TransferDefaultTextInCompleted()
        {
            if (m_doubleFunctionPoke)
            {
                if (m_interactionState == InteractionState.Pressed)
                {
                    m_interactionState = InteractionState.Inactive;
                }
                return;
            }
            if (m_interactionState == InteractionState.Squeezed)
            {
                m_interactionState = InteractionState.Inactive;
            }
            else
            {
                Debug.LogWarning($"Completed squeeze sequence, but found self in state: {m_interactionState}");
            }
        }

        public void OnSqueezeStartEvent()
        {
            if (m_doubleFunctionPoke)
            {
                return;
            }
            if (!m_isSqueezeEnabled)
            {
                return;
            }
            if (m_interactionState != InteractionState.Inactive)
            {
                return;
            }
            m_interactionState = InteractionState.Squeezed;
            // Cancel the grabbing interaction
            m_touchHandGrabInteractableComponent.Disable();
            m_touchHandGrabInteractableComponent.Enable();
            SqueezeInteraction?.Invoke(this);
            // Play world cloud pinched sound
            AppAudioController.Instance.PlaySound(SoundEffect.WordCloudPinched, transform.position, variation: 0);
            RunSqueezeSequence();
        }

        public void AnimateInDelayed()
        {
            m_animator.gameObject.SetActive(false);
            _ = StartCoroutine(DelayedAnimateInCoroutine());
        }

        private IEnumerator DelayedAnimateInCoroutine()
        {
            yield return DelayedAnimation();
            m_animator.gameObject.SetActive(true);
            AnimateIn();
        }

        public void AnimateInPositionDelayed(Vector3 startPosition)
        {
            gameObject.SetActive(true);
            m_animator.gameObject.SetActive(false);
            _ = StartCoroutine(DelayedAnimateInPositionCoroutine(startPosition));
        }

        private IEnumerator DelayedAnimateInPositionCoroutine(Vector3 startPosition)
        {
            yield return DelayedAnimation();
            m_animator.gameObject.SetActive(true);
            AnimateInPosition(startPosition);
        }

        private IEnumerator DelayedAnimation()
        {
            // Stagger animation start
            yield return new WaitForSeconds(Random.value * 0.2f + 0.05f);
        }

        public void AnimateOutDestroy()
        {
            // Clear listeners to avoid any further interaction
            RequestSpeak = null;
            PokeInteraction = null;
            SqueezeInteraction = null;

            // Set state to completed to avoid any further interaction
            m_interactionState = InteractionState.Completed;

            // And clear text animations
            m_textAnimations.Clear();

            // Now animate out and destroy self after a short delay
            _ = StartCoroutine(DelayedAnimateOutCoroutine());
        }

        private IEnumerator DelayedAnimateOutCoroutine()
        {
            AnimateOut();
            yield return new WaitForSeconds(0.5f);
            Destroy(gameObject);
        }

        public void AnimateIn()
        {
            m_animator.SetTrigger("ActivateTrigger");
        }
        public void AnimateInPosition(Vector3 startPosition)
        {
            StartMovement(startPosition);
        }

        private void StartMovement(Vector3 startPosition)
        {
            m_textAnimations.Clear();
            // Animation
            var anim = new AnimationTarget
            {
                Target = m_textNode,
                Feature = gameObject, // scale the word bar
                // Backer Color
                StartBackerColor = m_finalCompleteColor,
                EndBackerColor = m_textNode.BackerColor,
                ActiveBackerColor = true,
                // Text color
                StartColor = m_finalCompleteColor,
                EndColor = m_inactiveTextColor,
                ActiveColor = true,
                // Scale
                StartScale = m_invisibleLocalScale,
                EndScale = m_visibleLocalScale,
                ActiveScale = true,
                // Position
                StartPosition = startPosition,
                EndPosition = gameObject.transform.position,
                ActiveWorldPosition = true,
                // Start
                Curve = m_animationEasingCurve,
                CompleteAction = null,
                Duration = 0.60f,
                StartTime = Time.time
            };
            // Backer Color
            m_textNode.UpdateBlockColor(m_finalCompleteColor);
            // Text Color
            m_textNode.SetTextColor(m_finalCompleteColor);
            // Scale
            gameObject.transform.localScale = m_invisibleLocalScale;
            // Position
            gameObject.transform.position = startPosition;

            // Start
            m_textAnimations.Add(anim);
        }
        public void AnimateOut()
        {
            m_animator.SetTrigger("DeactivateTrigger");
        }

        private void AnimateComplete()
        {
            m_animator.SetTrigger("CompleteTrigger");
        }

        public void AnimateCompleteDestroy(Vector3 finalPosition)
        {
            // Clear listeners to avoid any further interaction
            RequestSpeak = null;
            PokeInteraction = null;
            SqueezeInteraction = null;

            // Set state to completed to avoid any further interaction
            m_interactionState = InteractionState.Completed;

            // And clear text animations
            m_textAnimations.Clear();

            // Now animate out and destroy self after a short delay
            _ = StartCoroutine(DelayedCompleteCoroutine(finalPosition));
        }
        private IEnumerator DelayedCompleteCoroutine(Vector3 finalPosition)
        {
            yield return new WaitForSeconds(Random.value * 0.2f);
            AnimateComplete();
            CompleteGlow(finalPosition);
        }

        private void CompleteGlow(Vector3 finalPosition)
        {
            m_textAnimations.Clear();
            // Animation
            var anim = new AnimationTarget
            {
                Target = m_textNode,
                Feature = gameObject, // scale the word bar
                // Backer Color
                StartBackerColor = m_textNode.BackerColor,
                EndBackerColor = m_finalCompleteColor,
                ActiveBackerColor = true,
                // Text color
                StartColor = m_inactiveTextColor,
                EndColor = m_finalCompleteColor,
                ActiveColor = true,
                // Scale
                StartScale = m_visibleLocalScale,
                EndScale = m_invisibleLocalScale,
                ActiveScale = true,
                // Position
                StartPosition = gameObject.transform.position,
                EndPosition = finalPosition,
                ActiveWorldPosition = true,
                // Start
                Curve = m_animationEasingCurve,
                CompleteAction = CompleteGlowCompleted,
                Duration = 0.60f,
                StartTime = Time.time
            };
            m_textAnimations.Add(anim);
        }

        private void CompleteGlowCompleted()
        {
            Destroy(gameObject);
        }

        private void Update()
        {
            if (m_textAnimations.Count > 0)
            {
                for (var i = 0; i < m_textAnimations.Count; i++)
                {
                    var anim = m_textAnimations[i];
                    if (anim.Process())
                    {
                        m_textAnimations.RemoveAt(i);
                        --i;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            m_textAnimations.Clear();
        }
    }
}