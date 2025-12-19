// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.Utilities.CameraTracking;
using Meta.Utilities.ObjectClassifier;
using Meta.XR.Samples;
using SpatialLingo.PSO;
using SpatialLingo.SpeechAndText;
using SpatialLingo.UI;
using UnityEngine;
#if UNITY_ANDROID
using System;
using Meta.XR.MRUtilityKit;
using UnityEngine.Android;
#endif

namespace SpatialLingo.States
{
    [MetaCodeSample("SpatialLingo")]
    public class AppLoadingState : FlowState
    {
        private const string CONSENT_KEY = "UserHasConsented";
        private const float DISPLAY_FIXED_DISTANCE = 1.5f;

        public new delegate void SendFlowSignalEvent();
        public new SendFlowSignalEvent SendFlowSignal;

        private static bool HasSessionConsent { get; set; }

        [SerializeField] private ConsentUI m_consentUI;
        [SerializeField] private bool m_perSessionConsent = true;
        [SerializeField] private GameObject m_fixedDisplay;
        [SerializeField] private string m_collectionStreamingAssetsLoadPath;
        [SerializeField] private GraphicsStateCollectionLoader m_graphicsLoader;
        [SerializeField] private ShaderVariantCollection m_shaderVariantCollection;

        private bool m_isInitialized = false;
        private Transform m_headsetTransform;

        private void Awake()
        {
            m_fixedDisplay.SetActive(false);
            m_consentUI.gameObject.SetActive(false);
        }

        public void WillGetFocus(Transform headsetTransform)
        {
            m_headsetTransform = headsetTransform;
            m_isInitialized = true;

            RequestCameraPermissionsIfNeeded();
        }

        private void CheckConsentAfterPermissions()
        {
            if (HasConsent())
            {
                m_consentUI.gameObject.SetActive(false);
                // Continue the preload process
                ContinuePreload();
            }
            else
            {
                _ = StartCoroutine(DisplayConsentUI());
            }
        }

        private bool HasConsent() => m_perSessionConsent ? HasSessionConsent : PlayerPrefs.HasKey(CONSENT_KEY);

        private void SaveConsent()
        {
            HasSessionConsent = true;

            PlayerPrefs.SetInt(CONSENT_KEY, 1);
            PlayerPrefs.Save();
        }


        private IEnumerator DisplayConsentUI()
        {
            yield return new WaitForSeconds(0.1f); // brief delay to ensure smooth transition
            UpdateTransform(m_consentUI.transform, false, false);
            m_consentUI.gameObject.SetActive(true);
            m_consentUI.OnConsentGiven += OnConsentGiven;
        }

        private void OnConsentGiven()
        {
            // Cache the consent
            SaveConsent();
            // Continue the preload process
            ContinuePreload();
        }

        private void ContinuePreload()
        {
            m_fixedDisplay.SetActive(true);
            // Inference Engine: preload computer shaders concurrently
            if (!InferenceEngineUtilities.IsLoaded)
            {
                LoadComputeShaders();
            }
            else
            {
                LoadGraphicsStateCollection();
            }
        }

        private void RequestCameraPermissionsIfNeeded()
        {
            if (PassthroughCameraPermissions.IsAllCameraPermissionsGranted())
            {
                RequestMicPermissionsIfNeeded();
                return;
            }
            PassthroughCameraPermissions.AllCameraPermissionGranted += OnAllCameraPermissionGranted;
            PassthroughCameraPermissions.AskCameraPermissions();
        }

        private void RequestMicPermissionsIfNeeded()
        {
            if (MicrophonePermissions.IsPermissionGranted())
            {
                CheckConsentAfterPermissions();
                return;
            }
            MicrophonePermissions.Request(OnMicPermissionResult);
        }

        private void OnMicPermissionResult(bool granted)
        {
            if (!granted)
            {
                Debug.LogWarning("AppLoadingState - Microphone permission denied. Some features may be disabled.");
            }
            CheckConsentAfterPermissions();
        }

        private void OnAllCameraPermissionGranted()
        {
            PassthroughCameraPermissions.AllCameraPermissionGranted -= OnAllCameraPermissionGranted;
            RequestMicPermissionsIfNeeded();
        }

        private void LoadComputeShaders()
        {
            InferenceEngineUtilities.PreloadingComplete += OnComputeShadersLoadComplete;
            InferenceEngineUtilities.LoadFastest = true;
            _ = InferenceEngineUtilities.LoadAll();
        }

        private void OnComputeShadersLoadComplete()
        {
            InferenceEngineUtilities.PreloadingComplete -= OnComputeShadersLoadComplete;
            LoadGraphicsStateCollection();
        }

        private void LoadGraphicsStateCollection()
        {
            m_graphicsLoader.WarmUpCompleted += OnGraphicsStateCollectionLoadComplete;
            m_graphicsLoader.WarmUp(m_collectionStreamingAssetsLoadPath);
        }

        private void OnGraphicsStateCollectionLoadComplete()
        {
            LoadShaderVariants();
        }

        private void LoadShaderVariants()
        {
            m_shaderVariantCollection.WarmUp();
            _ = StartCoroutine(CheckForMRUK());
        }

        private bool m_hasMrukPermissions = false;
        private IEnumerator CheckForMRUK()
        {
#if UNITY_ANDROID
            m_hasMrukPermissions = Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission);
#else 
            HasMrukPermissions = true;
#endif
            while (!m_hasMrukPermissions)
            {
                RequestPermissionForMRUK();
                yield return new WaitForSeconds(5.0f);
            }
            _ = StartCoroutine(WaitForMRUK());
        }

        private void RequestPermissionForMRUK()
        {
            if (!m_hasMrukPermissions)
            {
#if UNITY_ANDROID
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += PermissionForMrukGranted;
                callbacks.PermissionDenied += PermissionForMrukDenied;
                callbacks.PermissionRequestDismissed += PermissionForMrukDismissed;
                Permission.RequestUserPermission(OVRPermissionsRequester.ScenePermission, callbacks);
#endif
            }
        }

        private void PermissionForMrukGranted(string permission)
        {
#if UNITY_ANDROID
            m_hasMrukPermissions = Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission);
#endif
        }

        private void PermissionForMrukDenied(string permission)
        {
#if UNITY_ANDROID
            m_hasMrukPermissions = Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission);
#endif
        }

        private void PermissionForMrukDismissed(string permission)
        {
#if UNITY_ANDROID
            m_hasMrukPermissions = Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission);
#endif
        }

        private IEnumerator WaitForMRUK()
        {
#if UNITY_ANDROID
            // Wait for MRUK
            while (MRUK.Instance == null)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Wait for MRUK Room
            var timeoutWaitRoomSeconds = 5.0f;
            var timeStart = DateTime.Now;
            var hasAsked = false;
            while (MRUK.Instance.GetCurrentRoom() == null)
            {
                var timeNow = DateTime.Now;
                var diff = timeNow - timeStart;
                if (diff.TotalSeconds > timeoutWaitRoomSeconds)
                {
                    // Only ask once, then proceed.
                    if (hasAsked)
                    {
                        break;
                    }
                    hasAsked = true;
                    Debug.LogWarning($"AppLoadingState - No room available, requesting create or load");
                    // Load a scene, or create if one doesn't exist:
                    MRUK.Instance.LoadSceneFromDevice();
                }
                yield return new WaitForSeconds(0.1f);
            }
#else
            yield return null;
#endif

            // Continue loading process
            _ = StartCoroutine(ContinuePreloadExit());
        }

        private IEnumerator ContinuePreloadExit()
        {
            // Show loading screen animation momentarily before continuing
            yield return new WaitForSeconds(1.5f);
            SendFlowSignal?.Invoke();
        }

        public void WillLoseFocus()
        {
            Destroy(gameObject);
        }

        private void Update()
        {
            UpdateTransform(m_fixedDisplay.transform);
        }

        private void UpdateTransform(Transform target, bool useHeadsetForward = true, bool lookAtHeadset = true)
        {
            if (!m_isInitialized || m_headsetTransform == null || target == null)
            {
                return;
            }
            var headsetPosition = m_headsetTransform.position;
            var forward = useHeadsetForward ? m_headsetTransform.forward : Vector3.forward;
            target.position = headsetPosition + forward * DISPLAY_FIXED_DISTANCE;
            if (lookAtHeadset) target.transform.LookAt(headsetPosition, m_headsetTransform.up);
        }

        private void OnDestroy()
        {
            PassthroughCameraPermissions.AllCameraPermissionGranted -= OnAllCameraPermissionGranted;
            m_consentUI.OnConsentGiven -= OnConsentGiven;
            InferenceEngineUtilities.PreloadingComplete -= OnComputeShadersLoadComplete;
        }
    }
}