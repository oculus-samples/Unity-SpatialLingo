// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using SpatialLingo.Audio;
using UnityEngine;

namespace SpatialLingo.Samples
{
    /// <summary>
    /// Test out AudioManager
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class AudioSample : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private GameObject m_targetExampleSound3D;
        [SerializeField] private AudioClip m_soundClip;

        private void Start()
        {
            // Add a looping noise to the target + play it
            _ = AudioManager.Instance.PlaySound(m_soundClip, m_targetExampleSound3D.transform, true, true);
        }
    }
}