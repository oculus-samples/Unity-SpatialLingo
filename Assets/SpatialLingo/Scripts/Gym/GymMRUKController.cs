// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using SpatialLingo.UI;
using TMPro;
using UnityEngine;

namespace SpatialLingo.Gym
{
    [MetaCodeSample("SpatialLingo")]
    public class GymMRUKController : MonoBehaviour
    {
        [Header("MRUK Debugging Options")]
        [SerializeField] private TextMeshPro m_debugTextField;
        [SerializeField] private CanvasXRButton m_debugToggleDisplayButton;
        [SerializeField] private CanvasXRButton m_debugSelectSpaceSetupButton;

        [Header("MRUK Debugging Visuals")]
        [SerializeField] private EffectMesh m_mrukEffectMesh;

        private bool m_isDisplaying = true;

        private void OnSystemsBecameReadyEvent(GymScene gymScene)
        {
            StartInternals();
        }

        private void Start()
        {
            m_debugTextField.text = "MRUK Details: ...";
            if (GymScene.SystemReady)
            {
                StartInternals();
            }
            else
            {
                GymScene.SystemsBecameReady += OnSystemsBecameReadyEvent;
            }
        }

        private void StartInternals()
        {
            StartMruk();
            m_debugToggleDisplayButton.ButtonWasSelected += OnDebugToggleDisplayButton;
            m_debugSelectSpaceSetupButton.ButtonWasSelected += OnDebugSelectSpaceSetupButton;
        }

        public void OnDebugToggleDisplayButton(CanvasXRButton button)
        {
            ToggleDebugDisplay();
        }

        public void OnDebugSelectSpaceSetupButton(CanvasXRButton button)
        {
            _ = OVRScene.RequestSpaceSetup();
        }

        private void ToggleDebugDisplay()
        {
            m_mrukEffectMesh.HideMesh = m_isDisplaying;
            m_isDisplaying = !m_isDisplaying;
        }

        private void StartMruk()
        {
            m_debugTextField.text = $"MRUK start initialized: {MRUK.Instance.IsInitialized}";
            if (MRUK.Instance.IsInitialized)
            {
                ContinueMruk();
            }
            else
            {
                MRUK.Instance.SceneLoadedEvent.AddListener(OnSceneLoadedEvent);
            }

        }

        private void OnSceneLoadedEvent()
        {
            m_debugTextField.text = $"MRUK scene load initialized: {MRUK.Instance.IsInitialized}";
            ContinueMruk();
        }

        private void ContinueMruk()
        {
            var room = MRUK.Instance.GetCurrentRoom();

            var walls = room.WallAnchors;
            var floor = room.FloorAnchor;
            var ceiling = room.CeilingAnchor;
            var anchors = room.Anchors;
            var seats = room.SeatPoses;

            var feedback = $"Room objects [{room.name}]:\n";
            var floorValue = floor != null ? "exists" : "doesn't exist";
            var ceilingValue = ceiling != null ? "exists" : "doesn't exist";
            feedback += $"walls: {walls.Count}\n";
            feedback += $"floor: {floorValue}\n";
            feedback += $"ceiling: {ceilingValue}\n";
            feedback += $"seats: {seats.Count}\n";
            feedback += $"anchors: {anchors.Count}\n";

            var anchorTable = new Dictionary<string, int>();
            foreach (var anchor in anchors)
            {
                var labelString = anchor.Label.ToString();
                if (anchorTable.ContainsKey(labelString))
                {
                    anchorTable[labelString]++;
                }
                else
                {
                    anchorTable[labelString] = 1;
                }
            }

            var anchorFeedback = "";
            foreach (var key in anchorTable.Keys)
            {
                var value = anchorTable[key];
                anchorFeedback += $"{key}: {value}\n";
            }

            feedback += $"{anchorFeedback}";

            m_debugTextField.text = feedback;
        }

        private void OnDestroy()
        {
            m_debugToggleDisplayButton.ButtonWasSelected -= OnDebugToggleDisplayButton;
            m_debugSelectSpaceSetupButton.ButtonWasSelected -= OnDebugSelectSpaceSetupButton;
        }
    }
}