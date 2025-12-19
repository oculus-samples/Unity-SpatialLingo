// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.Characters
{
    [MetaCodeSample("SpatialLingo")]
    public class MovementController : MonoBehaviour
    {
        public enum CurveType
        {
            Linear,
            EaseInOut,
            EaseIn,
            EaseOut
        }

        private enum TargetType
        {
            None,
            Position,
            Transform
        }

        private Action<MovementController, bool> m_moveCallback;
        private CurveType m_moveCurve;

        private TargetType m_targetType = TargetType.None;
        private Vector3 m_moveTargetPosition = Vector3.zero;
        private Transform m_moveTargetTransform;
        private float m_moveDuration;
        private float m_startTime;

        private bool m_isMoving;
        private Vector3 m_movePositionStart;
        private float m_lazyFollowRatio = 0.50f;

        public void SetPosition(Vector3 position)
        {
            m_targetType = TargetType.Position;
            m_moveTargetPosition = position;
            m_moveTargetTransform = null;
            transform.position = position;
        }

        public void SetTarget(Transform subject)
        {
            if (subject == null)
            {
                m_targetType = TargetType.None;
                m_moveTargetPosition = transform.position;
                m_moveTargetTransform = null;
            }
            else
            {
                m_targetType = TargetType.Transform;
                m_moveTargetPosition = transform.position;
                m_moveTargetTransform = subject;
            }
        }

        public void MoveTo(Vector3 targetPoint, float duration, CurveType curve, Action<MovementController, bool> callback = null)
        {
            SetupCallback(callback);
            m_targetType = TargetType.Position;
            m_moveTargetPosition = targetPoint;
            m_movePositionStart = transform.position;
            m_moveDuration = duration;
            m_moveCurve = curve;
            m_startTime = Time.time;
            m_isMoving = true;
        }

        public void MoveTo(Transform targetTransform, float duration, CurveType curve, Action<MovementController, bool> callback = null)
        {
            if (targetTransform == null)
            {
                SetTarget(null);
                return;
            }
            SetupCallback(callback);
            m_targetType = TargetType.Transform;
            m_moveTargetTransform = targetTransform;
            m_movePositionStart = transform.position;
            m_moveDuration = duration;
            m_moveCurve = curve;
            m_startTime = Time.time;
            m_isMoving = true;
        }

        private void SetupCallback(Action<MovementController, bool> callback = null)
        {
            PerformCallbackComplete(false);
            m_moveCallback = callback;
        }
        private void PerformCallbackComplete(bool result)
        {
            if (m_moveCallback != null)
            {
                m_moveCallback?.Invoke(this, result);
                m_moveCallback = null;
            }
        }

        private void Awake()
        {
            m_moveTargetPosition = transform.position;
        }

        private float CurveValueForRatio(float ratio01)
        {
            if (m_moveCurve == CurveType.EaseInOut)
            {
                return 0.5f * (1 - Mathf.Cos(ratio01 * Mathf.PI)); // half a period, flipped, shifted up 
            }

            return ratio01;
        }

        private void Update()
        {
            if (m_isMoving)
            {
                var diff = Time.time - m_startTime;
                var ratio = diff / m_moveDuration;
                if (ratio >= 1.0f)
                {
                    if (m_targetType == TargetType.Position)
                    {
                        transform.position = m_moveTargetPosition;
                    }
                    else if (m_targetType == TargetType.Transform)
                    {
                        transform.position = m_moveTargetTransform.position;
                    }
                    PerformCallbackComplete(true);
                    m_isMoving = false;
                }
                else
                {
                    var value = CurveValueForRatio(ratio);
                    if (m_targetType == TargetType.Position)
                    {
                        transform.position = Vector3.Lerp(m_movePositionStart, m_moveTargetPosition, value);
                    }
                    else if (m_targetType == TargetType.Transform)
                    {
                        transform.position = Vector3.Lerp(m_movePositionStart, m_moveTargetTransform.position, value);
                    }
                }
            }
            else
            {
                // Points don't need delayed following
                if (m_targetType == TargetType.Transform)
                {
                    transform.position = Vector3.Lerp(transform.position, m_moveTargetTransform.position, m_lazyFollowRatio);
                }
            }
        }
    }
}