// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities.CameraTracking
{
    public static class PassthroughCameraDebugger
    {
        public enum DebugLevelEnum
        {
            ALL,
            NONE,
            ONLY_ERROR,
            ONLY_LOG,
            ONLY_WARNING
        }

        public static DebugLevelEnum DebugLevel = DebugLevelEnum.ALL;

        /// <summary>
        /// Send debug information to Unity console based on DebugType and DebugLevel
        /// </summary>
        /// <param name="mType"></param>
        /// <param name="message"></param>
        public static void DebugMessage(LogType mType, string message)
        {
            switch (mType)
            {
                case LogType.Error:
                    if (DebugLevel is DebugLevelEnum.ALL or DebugLevelEnum.ONLY_ERROR)
                    {
                        Debug.LogError(message);
                    }
                    break;
                case LogType.Log:
                    if (DebugLevel is DebugLevelEnum.ALL or DebugLevelEnum.ONLY_LOG)
                    {
                        Debug.Log(message);
                    }
                    break;
                case LogType.Warning:
                    if (DebugLevel is DebugLevelEnum.ALL or DebugLevelEnum.ONLY_WARNING)
                    {
                        Debug.LogWarning(message);
                    }
                    break;
            }
        }
    }
}
