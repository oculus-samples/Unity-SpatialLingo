// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections;
using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.UI
{
    [MetaCodeSample("SpatialLingo")]
    public class PermissionUI : MonoBehaviour
    {
        public Action OnContinue;
        public Func<bool> CheckAllPermissions;

        [SerializeField] private GameObject m_buttonOK;
        [SerializeField] private GameObject m_buttonSettings;

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show(bool withSettingsButton)
        {
            m_buttonOK.SetActive(!withSettingsButton);
            m_buttonSettings.SetActive(withSettingsButton);
            gameObject.SetActive(true);
        }

        public void OnOKButtonPressed()
        {
            OnContinue?.Invoke();
            gameObject.SetActive(false);
        }

        public void OnOpenSettingsPressed()
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager");
            var intent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", "com.oculus.vrshell");
            _ = intent.Call<AndroidJavaObject>("putExtra", "intent_data", "systemux://settings");
            _ = intent.Call<AndroidJavaObject>("putExtra", "uri", "applications?package=" + Application.identifier);
            currentActivity.Call("startActivity", intent);

            _ = StartCoroutine(CheckPermissions());
        }

        private IEnumerator CheckPermissions()
        {
            if (CheckAllPermissions == null)
            {
                yield break;
            }

            while (!CheckAllPermissions.Invoke())
            {
                yield return new WaitForSeconds(1);
            }

            OnContinue?.Invoke();
            Hide();
        }
    }
}
