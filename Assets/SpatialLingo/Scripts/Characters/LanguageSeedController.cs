// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.Characters
{
    [MetaCodeSample("SpatialLingo")]
    public class LanguageSeedController : MonoBehaviour
    {
        private Quaternion m_rotationPerFrame = Quaternion.Euler(0.5f, 1.0f, 0.25f);
        private const float TARGET_VELOCITY = 2.5f;

        [Header("Assets")]
        [SerializeField] private GameObject m_seed;
        [SerializeField] private GameObject m_grabbable;
        [SerializeField] private Animator m_animator;

        private bool m_isRotating = true;
        private bool m_isMoving = true;
        private Vector3 m_moveStartPosition;
        private Vector3 m_moveEndPosition;
        private float m_moveDuration;
        private float m_moveStartTime;

        public delegate void SeedWasInteractedEvent();
        public event SeedWasInteractedEvent SeedWasInteracted;

        public void MoveTo(Vector3 position, bool immediate = true, float duration = 0.0f)
        {
            if (!immediate)
            {
                m_moveStartPosition = transform.position;
                m_moveEndPosition = position;
                var distance = Vector3.Distance(m_moveStartPosition, m_moveEndPosition);
                m_moveDuration = 0.0f;
                if (distance > 0)
                {
                    m_moveDuration = distance / TARGET_VELOCITY;
                }

                if (duration > 0.0f)
                {
                    m_moveDuration = duration;
                }
                m_moveStartTime = Time.time;
                m_isMoving = true;
            }
            else
            {
                transform.position = position;
            }
        }

        public void FadeIn()
        {
            m_seed.SetActive(true);
            m_animator.SetTrigger("ActivateTrigger");
        }

        public void PlayRumble()
        {
            m_animator.SetTrigger("CompleteTrigger");
        }

        public void FadeOutDestroy()
        {
            m_animator.SetTrigger("DeactivateTrigger");
            _ = StartCoroutine(DelayedDestroy());
        }

        private IEnumerator DelayedDestroy()
        {
            yield return new WaitForSeconds(0.50f);
            Destroy(gameObject);
        }

        public void OnGrabbableSelect()
        {
            SeedWasInteracted?.Invoke();
        }

        public void EnableGrabInteraction()
        {
            m_grabbable.SetActive(true);
        }

        public void DisableGrabInteraction()
        {
            m_grabbable.SetActive(false);
        }

        private void Update()
        {
            if (m_isRotating)
            {
                m_seed.transform.rotation *= m_rotationPerFrame;
            }

            if (m_isMoving)
            {
                var diff = Time.time - m_moveStartTime;
                var ratio = diff / m_moveDuration;
                if (ratio > 1.0f)
                {
                    transform.position = m_moveEndPosition;
                    m_isMoving = false;
                }
                else
                {
                    transform.position = Vector3.Lerp(m_moveStartPosition, m_moveEndPosition, ratio);
                }
            }
        }
    }
}