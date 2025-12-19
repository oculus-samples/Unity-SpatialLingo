// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.Utilities.LlamaAPI;
using Meta.XR.Samples;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace SpatialLingo.Settings
{
    [MetaCodeSample("SpatialLingo")]
    [DefaultExecutionOrder(9999)]
    public class SpatialLingoSettings : ScriptableSettings<SpatialLingoSettings>
    {
        public string LlamaApiKey;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void SetUpLlamaApiKey()
        {
            if (!string.IsNullOrEmpty(Instance.LlamaApiKey) && LlamaRestApi.GetApiKeyAsync == null)
            {
#pragma warning disable CS1998
                LlamaRestApi.GetApiKeyAsync = async () =>
                {
                    Debug.Log($"LlamaRestApi set to use SpatialLingoSettings.LlamaApiKey");
                    return (Instance.LlamaApiKey, null);
                };
#pragma warning restore CS1998
            }
        }
    }
}
