// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using Meta.XR.Samples;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Events;

namespace SpatialLingo.Interactions
{
    [MetaCodeSample("SpatialLingo")]
    public class SqueezableHandInteraction : MonoBehaviour
    {
        // Events to listen for 
        [SerializeField] private UnityEvent m_onSelectAction;
        [SerializeField] private UnityEvent m_onUnselectAction;

        // Transform offsets
        [SerializeField] private Transform m_originTransform;
        [SerializeField] private Transform m_rotateTransform;
        [SerializeField] private Transform m_squeezeTransform;
        [SerializeField] private Transform m_inverseRotateTransform;

        // Find at start
        private TouchHandGrabInteractor m_systemTouchHandGrabInteractorLeft;
        private GrabInteractor m_systemGrabInteractorLeft;
        private GrabInteractor m_systemGrabInteractorRight;
        private TouchHandGrabInteractable m_touchHandGrabInteractableReference;
        private OVRHand m_rightHand;
        private OVRHand m_leftHand;

        // State / Caches:
        private bool m_isSelected;
        private float m_startSelectDistance;
        private OVRHand m_selectingHand;
        private TouchHandGrabInteractor m_interactor;
        private Vector3 m_directionSqueeze;

        private List<HandVisual> m_handVisuals = new();
        public bool ShouldHideHandOnInteraction;

        private void Start()
        {
            m_touchHandGrabInteractableReference = GetComponent<TouchHandGrabInteractable>();

            var touchHandInteractors = FindObjectsByType<TouchHandGrabInteractor>(FindObjectsSortMode.InstanceID);
            foreach (var interactor in touchHandInteractors)
            {
                var handRef = interactor.GetComponent<HandRef>();
                if (handRef.Handedness == Handedness.Left)
                {
                    m_systemTouchHandGrabInteractorLeft = interactor;
                }
            }

            var sceneHandVisuals = FindObjectsByType<HandVisual>(FindObjectsSortMode.InstanceID);
            foreach (var hand in sceneHandVisuals)
            {
                m_handVisuals.Add(hand);
            }

            var hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.InstanceID);
            foreach (var hand in hands)
            {
                if (hand.GetHand() == OVRPlugin.Hand.HandLeft)
                {
                    m_leftHand = hand;
                }
                else
                {
                    m_rightHand = hand;
                }
            }

            if (m_touchHandGrabInteractableReference != null)
            {
                m_touchHandGrabInteractableReference.WhenPointerEventRaised += OnWhenPointerEventRaised;
            }
        }

        private void OnDestroy()
        {
            if (m_touchHandGrabInteractableReference != null)
            {
                m_touchHandGrabInteractableReference.WhenPointerEventRaised -= OnWhenPointerEventRaised;
            }
        }

        private float AverageDistanceFingersCenter(OVRHand hand)
        {
            var handState = new OVRPlugin.HandState();
            var handType = hand.GetHand();
            var step = OVRPlugin.Step.Render;
            var gotHand = OVRPlugin.GetHandState(step, handType, ref handState);
            if (!gotHand)
            {
                return -1.0f;
            }

            var points = FingerActivePositions(handState);
            m_directionSqueeze = points[0] - points[2]; // thumb - middle
            m_directionSqueeze.Normalize();

            var center = new Vector3();
            foreach (var point in points)
            {
                center += point;
            }

            center /= points.Length;
            var averageDistance = 0.0f;
            foreach (var point in points)
            {
                averageDistance += Vector3.Distance(center, point);
            }

            averageDistance /= points.Length;
            return averageDistance;
        }

        private Vector3[] FingerActivePositions(OVRPlugin.HandState handState)
        {
            var positions = handState.BonePositions;

            var thumb = positions[(int)OVRPlugin.BoneId.XRHand_ThumbTip]; // 5
            var index = positions[(int)OVRPlugin.BoneId.XRHand_IndexTip]; // 10
            var middle = positions[(int)OVRPlugin.BoneId.XRHand_MiddleTip]; // 15
            var ring = positions[(int)OVRPlugin.BoneId.XRHand_RingTip]; // 20
            var pinky = positions[(int)OVRPlugin.BoneId.XRHand_LittleTip]; // 25
            var a = new Vector3(thumb.x, thumb.y, thumb.z);
            var b = new Vector3(index.x, index.y, index.z);
            var c = new Vector3(middle.x, middle.y, middle.z);
            var d = new Vector3(ring.x, ring.y, ring.z);
            var e = new Vector3(pinky.x, pinky.y, pinky.z);
            Vector3[] points = { a, b, c, d, e };
            return points;
        }

        private void Update()
        {
            if (m_isSelected)
            {
                var currentDistance = AverageDistanceFingersCenter(m_selectingHand);
                var ratio = currentDistance / m_startSelectDistance;
                if (currentDistance == 0 || m_startSelectDistance == 0)
                {
                    Debug.LogWarning($"Found Zero Ratio: {ratio} = {currentDistance} / {m_startSelectDistance}");
                }
                else
                {
                    // A more realistic squeeze effect could scale along the direction of the squeeze
                    var directionScale = Math.Clamp(ratio, 0.1f, 1.0f);
                    m_squeezeTransform.localScale = new Vector3(directionScale, directionScale, directionScale);
                }
            }
        }

        private void OnWhenPointerEventRaised(PointerEvent evt)
        {
            if (evt.Type == PointerEventType.Select)
            {
                var touchGrab = (TouchHandGrabInteractor)evt.Data;
                if (touchGrab != null)
                {
                    m_interactor = touchGrab;
                }

                m_selectingHand = m_systemTouchHandGrabInteractorLeft == m_interactor ? m_leftHand : m_rightHand;
                m_isSelected = true;
                m_startSelectDistance = AverageDistanceFingersCenter(m_selectingHand);
                CheckHideHands();
                m_onSelectAction.Invoke();
            }
            else if (evt.Type == PointerEventType.Unselect)
            {
                CheckShowHands();
                var useScale = 1.0f;
                m_squeezeTransform.localScale = new Vector3(useScale, useScale, useScale);
                m_isSelected = false;
                m_interactor = null;
                m_onUnselectAction.Invoke();
            }
        }

        private void CheckHideHands()
        {
            if (ShouldHideHandOnInteraction)
            {
                foreach (var hand in m_handVisuals)
                {
                    hand.gameObject.SetActive(false);
                }
            }
        }

        private void CheckShowHands()
        {
            if (ShouldHideHandOnInteraction)
            {
                foreach (var hand in m_handVisuals)
                {
                    hand.gameObject.SetActive(true);
                }
            }
        }
    }
}