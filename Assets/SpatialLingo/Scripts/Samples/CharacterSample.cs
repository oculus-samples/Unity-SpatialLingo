// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using SpatialLingo.Characters;
using SpatialLingo.Lessons;
using SpatialLingo.UI;
using TMPro;
using UnityEngine;
using static SpatialLingo.SceneObjects.TextCloudItem;

namespace SpatialLingo.Samples
{
    /// <summary>
    /// Test out Character
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class CharacterSample : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private GollyGoshController m_characterController;
        [SerializeField] private TreeController m_treeController;
        [SerializeField] private GameObject m_targetLocation;
        [SerializeField] private GameObject m_berryPrefab;
        [SerializeField] private GameObject m_buttonPrefab;
        [SerializeField] private BerryController m_berryController;
        [SerializeField] private FocusPointController m_moundController;
        [SerializeField] private WordBar3D m_wordBar;
        [SerializeField] private ConfettiController m_confettiController;
        [SerializeField] private TranscribeFeedbackController m_feedbackController;
        [SerializeField] private GameObject m_feedbackFollow;

        private void Start()
        {
            _ = StartCoroutine(TestBerry());

            SetupWordBar();

            SetupGGControls();

            SetupTreeControls();

            HandleTreeGoTo0();

            // Show an intial berry
            _ = StartCoroutine(BerryFlyTestInitial());

            // Start tree example
            _ = StartCoroutine(TreeTestInitial());

            m_characterController.ShowEyeEmotion(GollyGoshController.GollyGoshEyeEmotion.Excited);
            m_characterController.StartTalking();

            // Confetti example
            m_confettiController.ShowConfettiPresentation(m_confettiController.transform.position, Quaternion.identity);

            // Feedback example
            _ = StartCoroutine(TestFeedbackController());

            // Dirt mound example
            _ = StartCoroutine(TestMoundController());

            // Character gaze example
            _ = StartCoroutine(TestGollyGoshControllerGaze());
        }

        private IEnumerator TestGollyGoshControllerGaze()
        {
            m_characterController.GazeAt(new Vector3(0, 0, 0));
            yield return new WaitForSeconds(2.0f);

            m_characterController.GazeAtDelay(m_targetLocation.transform, 3.0f);
            yield return new WaitForSeconds(10.0f);

            m_characterController.GazeAtDelay(new Vector3(0, 5, 0), 3.0f);

            yield return new WaitForSeconds(5.0f);
            m_characterController.GazeAtDelay(new Vector3(0, 0, 5), 10.0f);
        }

        private IEnumerator TestMoundController()
        {
            m_moundController.ShowMound();
            yield return new WaitForSeconds(5.0f);
            for (var i = 0; i < 3; ++i)
            {
                yield return new WaitForSeconds(5.0f);
                m_moundController.ShowPopOutEffect();
            }

            for (var i = 0; i < 3; ++i)
            {
                yield return new WaitForSeconds(5.0f);
                m_moundController.ShowDiveInEffect();
            }
        }

        private IEnumerator TestFeedbackController()
        {
            m_feedbackController.StartFollowingTransform(m_feedbackFollow.transform);
            yield return new WaitForSeconds(5.0f);

            m_feedbackController.ShowErrorServer();
            yield return new WaitForSeconds(10.0f);

            m_feedbackController.ShowErrorServer();
            yield return new WaitForSeconds(3.0f);

            m_feedbackController.ShowErrorWifi();
            yield return new WaitForSeconds(3.0f);

            m_feedbackController.ShowErrorServer();
            yield return new WaitForSeconds(3.0f);

            m_feedbackController.ShowErrorWifi();
            yield return new WaitForSeconds(3.0f);
        }

        private void SetupWordBar()
        {
            // String primary, string secondary, TextCloudItem.wordType type, Transform focus, string speakingPhrase
            m_wordBar.Initialize("primary", "secondary", WordType.noun, null, "phrase");
            m_wordBar.AnimateInDelayed();
        }

        private IEnumerator TestBerry()
        {
            if (m_berryController == null)
            {
                yield break;
            }
            m_berryController.DisplayRandomBerry();
            yield return new WaitForSeconds(1.0f);
            m_berryController.PlaySqueezeEffect();
            yield return new WaitForSeconds(2.0f);
            m_berryController.TurnGoldenColor();
            yield return new WaitForSeconds(2.0f);
            m_berryController.PlaySqueezeEffect();
            yield return new WaitForSeconds(2.0f);
            m_berryController.TurnBerryColor();
            yield return new WaitForSeconds(2.0f);
        }

        private IEnumerator BerryFlyTestInitial()
        {
            yield return new WaitForSeconds(1.0f);
            var time = 0.25f;
            for (var i = 0; i < 10; ++i)
            {
                yield return new WaitForSeconds(time);
                HandleMoveBerry();
            }
        }

        private IEnumerator TreeTestInitial()
        {
            yield return new WaitForSeconds(1.0f);
            HandleTreeAnimate0To1();

            yield return new WaitForSeconds(5.0f);
            HandleTreeAnimate1To2();

            yield return new WaitForSeconds(5.0f);
            HandleTreeAnimate2To3();
        }

        private IEnumerator DestroyGameObjectAfterTime(GameObject item, float time = 2.0f)
        {
            yield return new WaitForSeconds(time);
            Destroy(item);
        }

        private void SetupTreeControls()
        {
            var buttonNames = new List<(string, Action)>(){
                ("Animate 0->1",HandleTreeAnimate0To1),
                ("Animate 1->2",HandleTreeAnimate1To2),
                ("Animate 2->3",HandleTreeAnimate2To3),
                ("Goto 1",HandleTreeGoTo1),
                ("Goto 2",HandleTreeGoTo2),
                ("Goto 3",HandleTreeGoTo3),
                ("MoveTo",HandleTreeMoveTo),
                ("AnimateBerry",HandleMoveBerry),
            };

            var columnCount = 4;

            var index = 0;
            var row = 0;
            var col = 0;
            foreach (var tuple in buttonNames)
            {
                var buttonName = tuple.Item1;
                var buttonAction = tuple.Item2;
                var button = Instantiate(m_buttonPrefab);
                var text = button.GetComponentInChildren<TextMeshPro>();
                text.text = buttonName;
                var canvasButton = button.GetComponentInChildren<CanvasXRButton>();
                canvasButton.ButtonWasSelected += CanvasWasSelectedEvent;
                m_canvasButtons.Add((canvasButton, buttonAction));
                var position = new Vector3(0.50f, 1.0f - row * 0.07f, -0.20f + 0.35f * (col / (columnCount - 1.0f)));
                button.transform.position = position;
                button.transform.rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);

                col += 1;
                index += 1;

                if (index % columnCount == 0)
                {
                    row += 1;
                    col = 0;
                }
            }

            m_treeController.BerryReachedDestination += OnBerryReachedDestination;
            m_treeController.SetOrientation(new Vector3(1.0f, 0.0f, 0.50f), Quaternion.Euler(0.0f, 90.0f, 0.0f));
            m_treeController.SetTier(1);
        }

        private void OnBerryReachedDestination(GameObject item)
        {
            // Do operation
        }

        private void HandleTreeMoveTo()
        {
            m_treeController.SetOrientation(transform.position, Quaternion.identity);
        }

        private void HandleMoveBerry()
        {
            if (!m_treeController.HasAvailableBerryLocations)
            {
                var removed = m_treeController.RemoveAllBerries();
                foreach (var item in removed)
                {
                    Destroy(item);
                }
            }

            var berry = Instantiate(m_berryPrefab);

            var toTransform = m_targetLocation.transform;
            m_berryPrefab.transform.position = toTransform.position;
            m_berryPrefab.transform.rotation = toTransform.rotation;

            _ = m_treeController.MoveBerryToIndex(berry.gameObject);
        }

        private void HandleTreeGoTo0()
        {
            m_treeController.SetTier(0);
        }

        private void HandleTreeGoTo1()
        {
            m_treeController.SetTier(1);
        }

        private void HandleTreeGoTo2()
        {
            m_treeController.SetTier(2);
        }

        private void HandleTreeGoTo3()
        {
            m_treeController.SetTier(3);
        }

        private void HandleTreeAnimate0To1()
        {
            m_treeController.AnimateToTier(1);
        }

        private void HandleTreeAnimate1To2()
        {
            m_treeController.AnimateToTier(2);
        }

        private void HandleTreeAnimate2To3()
        {
            m_treeController.AnimateToTier(3);
        }

        private void SetupGGControls()
        {
            var columnCount = 4;

            var buttonNames = new List<(string, Action)>(){
                ("MoveTo(fast)",HandleMoveToActionFast),
                ("MoveTo(slow)",HandleMoveToActionSlow),
                ("LookAt(once)",HandleLookAtOnceAction),
                ("LookAt(cont.)",HandleLookAtContAction),
                ("LookAt(stop)",HandleLookAtStopAction),
                ("PointAt(once)",HandlePointAtOnceAction),
                ("PointAt(cont.)",HandlePointAtContAction),
                ("PointAt(stop)",HandlePointAtStopAction),
                ("Follow(near)",HandleFollowCloseAction),
                ("Follow(far)",HandleFollowFarAction),
                ("Follow(stop)",HandleFollowStopAction),
                ("Anim(None)",HandleAnimationNoneAction),
                ("Anim(Idle)",HandleAnimationIdleAction),
                ("Anim(Fly)",HandleAnimationFlyingAction),
                ("Anim(Wave)",HandleAnimationWavingAction),
                ("Anim(Celebrate)",HandleAnimationCelebrateAction),
                ("Anim(Point Fwd)",HandleAnimationPointForwardAction),
                ("Anim(Point Rev)",HandleAnimationPointBackwardAction),
                ("Anim(Listen Start)",HandleAnimationListenStartAction),
                ("Anim(Listen Stop)",HandleAnimationListenStopAction),
                ("Emote(None)",HandleEmotionNoneAction),
                ("Emote(Happy)",HandleEmotionHappyAction),
                ("Emote(Sad)",HandleEmotionSadAction),
                ("Emote(Surpr.)",HandleEmotionSurprisedAction),
                ("Emote(Conf.)",HandleEmotionConfusedAction)
            };
            var index = 0;
            var row = 0;
            var col = 0;
            foreach (var tuple in buttonNames)
            {
                var buttonName = tuple.Item1;
                var buttonAction = tuple.Item2;
                var button = Instantiate(m_buttonPrefab);
                var text = button.GetComponentInChildren<TextMeshPro>();
                text.text = buttonName;
                var canvasButton = button.GetComponentInChildren<CanvasXRButton>();
                canvasButton.ButtonWasSelected += CanvasWasSelectedEvent;
                m_canvasButtons.Add((canvasButton, buttonAction));
                var position = new Vector3(-0.20f + 0.35f * (col / (columnCount - 1.0f)), 1.2f - row * 0.07f, 0.5f);
                button.transform.position = position;

                col += 1;
                index += 1;

                if (index % columnCount == 0)
                {
                    row += 1;
                    col = 0;
                }
            }

            // Initial
            m_characterController.RotateTo(Quaternion.Euler(0.0f, 180.0f, 0.0f), 0.0f);
            m_characterController.MoveTo(new Vector3(0.5f, 0.5f, 0.5f), true);
            m_characterController.PlayAnimation(GollyGoshController.GollyGoshAnimation.Idle);
        }

        private List<(CanvasXRButton, Action)> m_canvasButtons = new();

        private void CanvasWasSelectedEvent(CanvasXRButton button)
        {
            for (var i = 0; i < m_canvasButtons.Count; i++)
            {
                if (m_canvasButtons[i].Item1 == button)
                {
                    m_canvasButtons[i].Item2.Invoke();
                    break;
                }
            }
        }

        private void HandleMoveToActionSlow()
        {
            var toTransform = m_targetLocation.transform;
            m_characterController.MoveTo(toTransform.position, false, null);
        }

        private void HandleMoveToActionFast()
        {
            var toTransform = m_targetLocation.transform;
            m_characterController.MoveTo(toTransform.position, true, null);
        }

        private void HandleLookAtOnceAction()
        {
            var toTransform = m_targetLocation.transform;
            m_characterController.GazeAt(toTransform.position);
        }

        private void HandleLookAtContAction()
        {
            var toTransform = m_targetLocation.transform;
            m_characterController.GazeAt(toTransform);
        }

        private void HandleLookAtStopAction()
        {
            m_characterController.GazeStop();
        }

        private void HandlePointAtOnceAction()
        {
            var toTransform = m_targetLocation.transform;
            m_characterController.PointAt(toTransform, false);
        }

        private void HandlePointAtContAction()
        {
            var toTransform = m_targetLocation.transform;
            m_characterController.PointAt(toTransform, true);
        }

        private void HandlePointAtStopAction()
        {
            m_characterController.PointAt(null, false);
        }

        private void HandleFollowCloseAction()
        {
            var toTransform = m_targetLocation.transform;
            m_characterController.Follow(toTransform, 0.3f, 0.6f);
        }

        private void HandleFollowFarAction()
        {
            var toTransform = m_targetLocation.transform;
            m_characterController.Follow(toTransform, 0.5f, 1.25f);
        }

        private void HandleFollowStopAction()
        {
            m_characterController.Follow(null, 0.0f, 0.0f);
        }

        private void HandleAnimationNoneAction()
        {
            m_characterController.PlayAnimation(GollyGoshController.GollyGoshAnimation.None);
        }

        private void HandleAnimationIdleAction()
        {
            m_characterController.PlayAnimation(GollyGoshController.GollyGoshAnimation.Idle);
        }

        private void HandleAnimationFlyingAction()
        {
            m_characterController.PlayAnimation(GollyGoshController.GollyGoshAnimation.Flying);
        }

        private void HandleAnimationWavingAction()
        {
            m_characterController.PlayAnimation(GollyGoshController.GollyGoshAnimation.Waving);
        }

        private void HandleAnimationCelebrateAction()
        {
            m_characterController.PlayAnimation(GollyGoshController.GollyGoshAnimation.Celebrate);
        }

        private void HandleAnimationPointForwardAction()
        {
            m_characterController.PlayAnimation(GollyGoshController.GollyGoshAnimation.PointForward);
        }

        private void HandleAnimationPointBackwardAction()
        {
            m_characterController.PlayAnimation(GollyGoshController.GollyGoshAnimation.PointBackward);
        }

        private void HandleAnimationListenStartAction()
        {
            m_characterController.PlayAnimation(GollyGoshController.GollyGoshAnimation.ListenStart);
        }

        private void HandleAnimationListenStopAction()
        {
            m_characterController.PlayAnimation(GollyGoshController.GollyGoshAnimation.ListenStop);
        }

        private void HandleEmotionNoneAction()
        {
            m_characterController.ShowEyeEmotion(GollyGoshController.GollyGoshEyeEmotion.None);
        }

        private void HandleEmotionHappyAction()
        {
            m_characterController.ShowEyeEmotion(GollyGoshController.GollyGoshEyeEmotion.Neutral);
        }

        private void HandleEmotionSadAction()
        {
            m_characterController.ShowEyeEmotion(GollyGoshController.GollyGoshEyeEmotion.Sad);
        }

        private void HandleEmotionSurprisedAction()
        {
            m_characterController.ShowEyeEmotion(GollyGoshController.GollyGoshEyeEmotion.Surprised);
        }

        private void HandleEmotionConfusedAction()
        {
            m_characterController.ShowEyeEmotion(GollyGoshController.GollyGoshEyeEmotion.Confused);
        }
    }
}