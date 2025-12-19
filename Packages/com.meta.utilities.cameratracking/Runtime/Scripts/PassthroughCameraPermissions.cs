// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
#if UNITY_ANDROID
using System.Linq;
using UnityEngine;
using UnityEngine.Android;
using PCD = Meta.Utilities.CameraTracking.PassthroughCameraDebugger;
#endif

namespace Meta.Utilities.CameraTracking
{
    /// <summary>
    /// PLEASE NOTE: Unity doesn't support requesting multiple permissions at the same time with <see cref="Permission.RequestUserPermissions"/> on Android.
    /// This component is a sample and shouldn't be used simultaneously with other scripts that manage Android permissions.
    /// </summary>
    public class PassthroughCameraPermissions
    {
        public static List<string> PermissionRequestsOnStartup = new()
        {
#if UNITY_ANDROID
            OVRPermissionsRequester.ScenePermission,
#endif
        };

        public static event Action AllCameraPermissionGranted;

        public static readonly string[] CameraPermissions =
        {
            "android.permission.CAMERA",          // Required to use WebCamTexture object.
            "horizonos.permission.HEADSET_CAMERA" // Required to access the Passthrough Camera API in Horizon OS v74 and above.
        };

        public static bool? HasCameraPermission { get; private set; }
        private static bool s_askedOnce;

#if UNITY_ANDROID
        /// <summary>
        /// Request camera permission if the permission is not authorized by the user.
        /// </summary>
        public static void AskCameraPermissions()
        {
            if (s_askedOnce)
            {
                return;
            }
            s_askedOnce = true;
            if (IsAllCameraPermissionsGranted())
            {
                HasCameraPermission = true;
                PCD.DebugMessage(LogType.Log, "PCA: All camera permissions granted.");
                AllCameraPermissionGranted?.Invoke();
            }
            else
            {
                PCD.DebugMessage(LogType.Log, "PCA: Requesting camera permissions.");

                var callbacks = new PermissionCallbacks();
                callbacks.PermissionDenied += PermissionCallbacksPermissionDenied;
                callbacks.PermissionGranted += PermissionCallbacksPermissionGranted;

                // It's important to request all necessary permissions in one request because only one 'PermissionCallbacks' instance is supported at a time.
                var allPermissions = CameraPermissions.Concat(PermissionRequestsOnStartup).ToArray();
                Permission.RequestUserPermissions(allPermissions, callbacks);
            }
        }

        /// <summary>
        /// Permission Granted callback
        /// </summary>
        /// <param name="permissionName"></param>
        private static void PermissionCallbacksPermissionGranted(string permissionName)
        {
            PCD.DebugMessage(LogType.Log, $"PCA: Permission {permissionName} Granted");

            // Only initialize the WebCamTexture object if both permissions are granted
            if (IsAllCameraPermissionsGranted())
            {
                HasCameraPermission = true;
                AllCameraPermissionGranted?.Invoke();
            }
        }

        /// <summary>
        /// Permission Denied callback.
        /// </summary>
        /// <param name="permissionName"></param>
        private static void PermissionCallbacksPermissionDenied(string permissionName)
        {
            PCD.DebugMessage(LogType.Warning, $"PCA: Permission {permissionName} Denied");
            HasCameraPermission = false;
            s_askedOnce = false;
        }

        public static bool IsAllCameraPermissionsGranted() => CameraPermissions.All(Permission.HasUserAuthorizedPermission);
#else
        public static void AskCameraPermissions() { }
        public static bool IsAllCameraPermissionsGranted() => true;
#endif
    }
}