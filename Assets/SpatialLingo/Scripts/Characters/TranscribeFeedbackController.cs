// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using TMPro;
using UnityEngine;

namespace SpatialLingo.Characters
{
    [MetaCodeSample("SpatialLingo")]
    public class TranscribeFeedbackController : MonoBehaviour
    {
        private const float LAZY_LAG_RATIO_TRANSLATION = 0.1f;
        private const float LAZY_LAG_RATIO_ROTATION = 0.5f;
        private const float FADE_DURATION = 0.70f;
        private const int MIC_FREQUENCY_RECORD = 24000;
        private const int MAX_LENGTH_TEXT_STRING = 100;

        [SerializeField] private GameObject m_containerUI;
        [SerializeField] private GameObject m_iconRecording;
        [SerializeField] private GameObject m_iconInactive;

        [SerializeField] private MeshRenderer m_rendererRecording;
        [SerializeField] private MeshRenderer m_rendererInactive;

        [SerializeField] private MeshRenderer m_rendererErrorWifi;
        [SerializeField] private MeshRenderer m_rendererErrorServer;

        [SerializeField] private Transform m_targetOffset;

        [SerializeField] private TextMeshPro m_textField;

        [SerializeField] private bool m_useMicAsVolumeSource = false;
        [SerializeField] private float m_audioThresholdListening = 0.05f;

        private Coroutine m_transientCoroutine;

        private Transform m_followTarget;
        private bool m_isFollowing = false;
        private Coroutine m_animateCoroutine;
        private bool m_isInControlMic = false;

        private AudioClip m_micRecordingClip;
        private Coroutine m_micRecordingCoroutine;
        private bool m_isListeningMic = false;
        private float[] m_micSamplesValues;

        private MeshRenderer m_currentErrorDisplayingRenderer;

        private void Awake()
        {
            m_rendererRecording.gameObject.SetActive(false);
            m_rendererInactive.gameObject.SetActive(false);
            m_rendererErrorWifi.gameObject.SetActive(false);
            m_rendererErrorServer.gameObject.SetActive(false);
            m_textField.gameObject.SetActive(false);
            m_textField.text = string.Empty;
        }

        public void ShowMicFeedback()
        {
            StartMicListening();
            StopAnimationCoroutine();
            m_animateCoroutine = StartCoroutine(AnimateCoroutine(0.0f, 1.0f, true));
            SetToGoalOrientation();
        }

        public void HideMicFeedback()
        {
            StopAnimationCoroutine();
            m_animateCoroutine = StartCoroutine(AnimateCoroutine(1.0f, 0.0f, false));
            StopMicListening();
        }

        public void ShowTextFeedback()
        {
            m_textField.gameObject.SetActive(true);
        }

        public void HideTextFeedback()
        {
            m_textField.gameObject.SetActive(false);
        }

        public void SetTextFeedback(string value)
        {
            var length = value.Length;
            if (length > MAX_LENGTH_TEXT_STRING)
            {
                var start = length - MAX_LENGTH_TEXT_STRING;
                value = "..." + value.Substring(start, MAX_LENGTH_TEXT_STRING);
            }
            m_textField.text = value;
        }

        public void ClearTextFeedback()
        {
            m_textField.text = string.Empty;
        }

        public void ShowErrorServer()
        {
            if (m_currentErrorDisplayingRenderer == m_rendererErrorServer)
            {
                return;
            }
            StopTransientCoroutine();
            m_currentErrorDisplayingRenderer = m_rendererErrorServer;
            m_transientCoroutine = StartCoroutine(ShowImageTransientCoroutine(m_rendererErrorServer.gameObject, m_rendererErrorServer));
        }

        public void ShowErrorWifi()
        {
            if (m_currentErrorDisplayingRenderer == m_rendererErrorWifi)
            {
                return;
            }
            StopTransientCoroutine();
            m_currentErrorDisplayingRenderer = m_rendererErrorWifi;
            m_transientCoroutine = StartCoroutine(ShowImageTransientCoroutine(m_rendererErrorWifi.gameObject, m_rendererErrorWifi));
        }

        private void StopTransientCoroutine(bool forceStop = true)
        {
            if (m_transientCoroutine != null)
            {
                m_rendererErrorServer.gameObject.SetActive(false);
                m_rendererErrorWifi.gameObject.SetActive(false);
                m_transientCoroutine = null;
                m_currentErrorDisplayingRenderer = null;
                if (forceStop)
                {
                    StopCoroutine(m_transientCoroutine);
                }
            }
        }

        private IEnumerator ShowImageTransientCoroutine(GameObject item, MeshRenderer renderer)
        {
            var list = new MeshRenderer[] { renderer };
            item.gameObject.SetActive(true);
            item.SetActive(true);
            yield return AnimateAlphaCoroutine(list, 0.0f, 1.0f);
            yield return new WaitForSeconds(5.0f);
            yield return AnimateAlphaCoroutine(list, 1.0f, 0.0f);
            item.SetActive(false);
            item.gameObject.SetActive(false);
            StopTransientCoroutine(false);
        }

        public void StartFollowingTransform(Transform targetTransform = null)
        {
            LazyFollowStart(targetTransform);
        }

        public void StopFollowingTransform()
        {
            LazyFollowStop();
        }

        public bool UpdateFromMicVolume(float volume)
        {
            var isAboveThreshold = volume >= m_audioThresholdListening;
            if (m_useMicAsVolumeSource)
            {
                return isAboveThreshold;
            }

            if (isAboveThreshold)
            {
                m_iconInactive.gameObject.SetActive(false);
                m_iconRecording.gameObject.SetActive(true);
            }
            else
            {
                m_iconInactive.gameObject.SetActive(true);
                m_iconRecording.gameObject.SetActive(false);
            }
            return isAboveThreshold;
        }

        private void StopAnimationCoroutine()
        {
            if (m_animateCoroutine != null)
            {
                StopCoroutine(m_animateCoroutine);
                m_animateCoroutine = null;
            }
        }

        private void StartMicListening()
        {
            if (!m_useMicAsVolumeSource)
            {
                return;
            }
            if (m_isListeningMic)
            {
                return;
            }
            m_isListeningMic = true;
            if (m_micRecordingCoroutine != null)
            {
                StopCoroutine(m_micRecordingCoroutine);
            }

            if (Microphone.devices.Length == 0)
            {
                m_isListeningMic = false;
                return;
            }
            var useDevice = Microphone.devices[0];
            var clip = Microphone.Start(useDevice, true, 1, MIC_FREQUENCY_RECORD);
            if (clip == null)
            {
                m_isListeningMic = false;
                return;
            }
            m_micRecordingClip = clip;
            _ = StartCoroutine(MicMonitoringLoop());
        }

        private IEnumerator MicMonitoringLoop()
        {
            while (m_isListeningMic)
            {
                yield return new WaitForSeconds(0.50f);
                var samplesCount = m_micRecordingClip.samples;
                if (m_micSamplesValues == null || m_micSamplesValues.Length < samplesCount)
                {
                    m_micSamplesValues = new float[samplesCount];
                }

                _ = m_micRecordingClip.GetData(m_micSamplesValues, 0);
                if (m_micSamplesValues.Length == 0)
                {
                    continue;
                }

                var aboveCount = 0;
                for (var i = 0; i < samplesCount; ++i)
                {
                    var sample = m_micSamplesValues[i];
                    if (sample > m_audioThresholdListening)
                    {
                        aboveCount += 1;
                        break;
                    }
                }

                if (aboveCount > 0)
                {
                    m_iconInactive.SetActive(false);
                    m_iconRecording.SetActive(true);
                }
                else
                {
                    m_iconInactive.SetActive(true);
                    m_iconRecording.SetActive(false);
                }
            }
        }

        private void StopMicListening()
        {
            if (!m_useMicAsVolumeSource)
            {
                return;
            }
            if (!m_isListeningMic)
            {
                return;
            }

            if (m_micRecordingCoroutine != null)
            {
                StopCoroutine(m_micRecordingCoroutine);
                m_micRecordingCoroutine = null;
            }
            m_micRecordingClip = null;

            if (m_isInControlMic)
            {
                if (Microphone.devices.Length > 0)
                {
                    Microphone.End(Microphone.devices[0]);
                }
            }

            m_isListeningMic = false;
        }


        private IEnumerator AnimateAlphaCoroutine(MeshRenderer[] renderers, float startA, float endA)
        {
            var duration = FADE_DURATION;
            var startTime = Time.time;
            var ratio = 0.0f;
            var valueA = startA;
            while (ratio < 1.0f)
            {
                SetAlpha(renderers, valueA);
                valueA = Mathf.Lerp(startA, endA, ratio);
                var nowTime = Time.time;
                var diffTime = nowTime - startTime;
                ratio = diffTime / duration;
                yield return null;
            }

            valueA = endA;
            SetAlpha(renderers, valueA);
        }

        private IEnumerator AnimateCoroutine(float startA, float endA, bool startActive)
        {
            if (startActive)
            {
                m_iconInactive.gameObject.SetActive(true);
                m_iconRecording.gameObject.SetActive(false);
                m_containerUI.SetActive(true);
            }

            var list = new MeshRenderer[] { m_rendererRecording, m_rendererInactive };
            yield return AnimateAlphaCoroutine(list, startA, endA);

            if (!startActive)
            {
                m_iconInactive.gameObject.SetActive(true);
                m_iconRecording.gameObject.SetActive(false);
                m_containerUI.SetActive(false);
            }

            m_animateCoroutine = null;
        }

        private void SetAlpha(MeshRenderer[] renderers, float value)
        {
            foreach (var r in renderers)
            {
                var material = r.material;
                var color = material.color;
                color.a = value;
                material.color = color;
                r.material = material;
            }
        }

        private void LazyFollowStart(Transform targetTransform)
        {
            m_followTarget = targetTransform;
            m_isFollowing = m_followTarget != null;
        }

        private void LazyFollowStop()
        {
            m_isFollowing = false;
            m_followTarget = null;
        }

        private void Update()
        {
            if (m_isFollowing)
            {
                var goalPosition = m_followTarget.position - m_followTarget.rotation * m_targetOffset.localPosition;
                var newPosition = Vector3.Lerp(transform.position, goalPosition, LAZY_LAG_RATIO_TRANSLATION);
                transform.position = newPosition;

                var toTarget = m_followTarget.position - transform.position;
                toTarget.Normalize();

                var goalRotation = Quaternion.LookRotation(-toTarget, m_followTarget.up);
                var newRotation = Quaternion.Lerp(transform.rotation, goalRotation, LAZY_LAG_RATIO_ROTATION);
                transform.rotation = newRotation;
            }
        }

        private void SetToGoalOrientation()
        {
            var goalPosition = m_followTarget.position - m_followTarget.rotation * m_targetOffset.localPosition;
            transform.position = goalPosition;

            var toTarget = m_followTarget.position - transform.position;
            toTarget.Normalize();

            var goalRotation = Quaternion.LookRotation(-toTarget, m_followTarget.up);
            transform.rotation = goalRotation;
        }
    }
}