// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
#if PLATFORM_ANDROID
#endif

namespace SpatialLingo.SpeechAndText
{
    public static class MicrophonePermissions
    {
        private const string MICROPHONE_PERMISSION = "android.permission.RECORD_AUDIO";

        /// <summary>
        /// Checks if the microphone permission is already granted.
        /// </summary>
        /// <returns>True if permission is granted, false otherwise.</returns>
        public static bool IsPermissionGranted()
        {
#if PLATFORM_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(MICROPHONE_PERMISSION);
#else
            // Always return true in the Editor or on non-Android platforms.
            return true;
#endif
        }

        /// <summary>
        /// Requests the microphone permission from the user.
        /// </summary>
        /// <param name="onPermissionResult">
        /// An action to be called with the result. 'true' if granted, 'false' if denied.
        /// </param>
        public static void Request(Action<bool> onPermissionResult)
        {
#if PLATFORM_ANDROID && !UNITY_EDITOR
        if (IsPermissionGranted())
        {
            // Permission already granted, invoke callback immediately.
            onPermissionResult?.Invoke(true);
            return;
        }

        // Permission not granted, so request it.
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += permissionName =>
        {
            onPermissionResult?.Invoke(true);
        };
        callbacks.PermissionDenied += permissionName =>
        {
            onPermissionResult?.Invoke(false);
        };
        callbacks.PermissionDeniedAndDontAskAgain += permissionName =>
        {
            onPermissionResult?.Invoke(false);
        };
        Permission.RequestUserPermission(MICROPHONE_PERMISSION, callbacks);
#else
            // In the Unity Editor or on other platforms, we assume permission is granted.
            onPermissionResult?.Invoke(true);
#endif
        }
    }
}