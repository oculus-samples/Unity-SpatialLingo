// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using Meta.Utilities.CameraTaxonTracking;
using Meta.Utilities.CameraTracking;
using Meta.Utilities.ObjectClassifier;
using Meta.XR;
using Meta.XR.EnvironmentDepth;
using Meta.XR.Samples;
using SpatialLingo.UI;
using TMPro;
using UnityEngine;

namespace SpatialLingo.Gym
{
    [MetaCodeSample("SpatialLingo")]
    public class GymObjectTrackingScene : MonoBehaviour
    {
        [Header("Object Tracking Dependencies")]
        [SerializeField] private WebCamTextureManager m_cameraTextureManager;
        [SerializeField] private Unity.InferenceEngine.ModelAsset m_objectClassifierModel;
        [SerializeField] private TextAsset m_objectClassifierClasses;

        [SerializeField] private Transform m_headsetLeftEyeTransform;

        [SerializeField] private EnvironmentRaycastManager m_environmentRaycastManager;
        [SerializeField] private EnvironmentDepthManager m_environmentDepthManager;

        [SerializeField] private ImageObjectClassifier m_classifier;

        [Header("Object Tracking Debugging Options")]
        [SerializeField] private TextMeshPro m_debugTextField;
        [SerializeField] private CanvasXRButton m_debugToggleDisplayButton;
        [SerializeField] private CanvasXRButton m_debugToggleTrackingButton;

        [Header("Object Tracking Debugging Visuals")]
        [SerializeField] private MeshRenderer m_debugTrackerRenderer;
        [SerializeField] private GameObject m_debugRayGO;
        [SerializeField] private CameraTrackedTaxonVisual m_taxonVisualPrefab;

        private CameraTaxonTracker m_tracker;
        private List<CameraTrackedTaxonVisual> m_taxonVisualList = new();
        private bool m_isTracking = false;
        private bool m_isDisplaying = true;

        private void OnSystemsBecameReadyEvent(GymScene gymScene)
        {
            StartInternals();
        }

        private void Start()
        {
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
            // Copy material:
            m_debugTrackerRenderer.material = Instantiate(m_debugTrackerRenderer.material);
            StartTrackingImageTaxon();
            m_debugToggleDisplayButton.ButtonWasSelected += OnDebugToggleDisplayButton;
            m_debugToggleTrackingButton.ButtonWasSelected += OnDebugToggleTrackingButton;
        }

        private void StartTrackingImageTaxon()
        {
            // Read in classes for model
            var classList = m_objectClassifierClasses.text.Split('\n');
            m_classifier.Initialize(m_objectClassifierModel, classList);

            var tracker = new CameraTaxonTracker(m_environmentRaycastManager, m_cameraTextureManager, m_classifier)
            {
                DebugRenderer = m_debugTrackerRenderer
            };
            tracker.TaxonAdded += OnTaxonAdded;
            tracker.TaxonUpdated += OnTaxonUpdated;
            tracker.TaxonRemoved += OnTaxonRemoved;
            m_tracker = tracker;

            if (!InferenceEngineUtilities.IsLoaded)
            {
                InferenceEngineUtilities.PreloadingComplete += OnPreloadingCompleteIE;
                _ = InferenceEngineUtilities.LoadAll();
            }

            m_classifier.SetLayersPerFrame(10);

            // Initial display values:
            m_debugRayGO.SetActive(true);
            m_debugTextField.text = "Tracked object counts\nwill display here.\n";
        }

        private void OnPreloadingCompleteIE()
        {
            // Inference Engine preload complete
        }

        public void OnDebugToggleDisplayButton(CanvasXRButton button)
        {
            ToggleDebugDisplay();
        }

        public void OnDebugToggleTrackingButton(CanvasXRButton button)
        {
            ToggleTracking();
        }

        private void Update()
        {
            if (m_isTracking && m_tracker != null && InferenceEngineUtilities.IsLoaded)
            {
                m_tracker.StartPolling();
            }
            if (m_isDisplaying && PassthroughCameraPermissions.IsAllCameraPermissionsGranted())
            {
                m_debugRayGO.transform.position = m_headsetLeftEyeTransform.position;
                m_debugRayGO.transform.rotation = m_headsetLeftEyeTransform.rotation;
            }
        }

        private void ToggleDebugDisplay()
        {
            if (m_isDisplaying)
            {
                var list = m_taxonVisualList.ToArray();
                m_taxonVisualList.Clear();
                foreach (var taxon in list)
                {
                    Destroy(taxon.gameObject);
                }
                m_debugRayGO.SetActive(false);
            }
            else
            {
                var list = m_tracker.TrackedTaxa;
                foreach (var taxon in list)
                {
                    AddVisualTaxonFromTaxon(taxon);
                }
                m_debugRayGO.SetActive(true);
            }
            m_isDisplaying = !m_isDisplaying;
        }

        private void ToggleTracking()
        {
            m_isTracking = !m_isTracking;
        }

        private void AddVisualTaxonFromTaxon(CameraTrackedTaxon taxon)
        {
            var taxonVisual = Instantiate(m_taxonVisualPrefab);
            taxonVisual.enabled = true;
            taxonVisual.SetTaxon(taxon);
            m_taxonVisualList.Add(taxonVisual);
        }

        private void UpdateDisplayText()
        {
            var list = m_tracker.TrackedTaxa;
            var counts = new Dictionary<string, int>();
            foreach (var taxa in list)
            {
                var name = taxa.Name;
                if (counts.ContainsKey(name))
                {
                    counts[name]++;
                }
                else
                {
                    counts[name] = 1;
                }
            }
            var feedback = "";
            foreach (var key in counts.Keys)
            {
                var value = counts[key];
                feedback += $"{key}: {value}\n";
            }
            feedback += $"Total: {list.Length}\n";

            m_debugTextField.text = feedback;
        }

        private void OnTaxonAdded(CameraTaxonTracker.TaxonUpdateResult result)
        {
            if (m_isDisplaying)
            {
                AddVisualTaxonFromTaxon(result.Taxon);
            }
            UpdateDisplayText();
        }

        private void OnTaxonUpdated(CameraTaxonTracker.TaxonUpdateResult result)
        {
            if (m_isDisplaying)
            {
                foreach (var visual in m_taxonVisualList)
                {
                    if (visual.Taxon == result.Taxon)
                    {
                        visual.UpdateVisuals();
                        break;
                    }
                }
            }
            UpdateDisplayText();
        }

        private void OnTaxonRemoved(CameraTaxonTracker.TaxonUpdateResult result)
        {
            if (m_isDisplaying)
            {
                CameraTrackedTaxonVisual foundTaxon = null;
                foreach (var visual in m_taxonVisualList)
                {
                    if (visual.Taxon == result.Taxon)
                    {
                        foundTaxon = visual;
                        break;
                    }
                }

                if (foundTaxon != null)
                {
                    _ = m_taxonVisualList.Remove(foundTaxon);
                    Destroy(foundTaxon.gameObject);
                }
            }
            UpdateDisplayText();
        }

        private void OnDestroy()
        {
            m_debugToggleDisplayButton.ButtonWasSelected -= OnDebugToggleDisplayButton;
            m_debugToggleTrackingButton.ButtonWasSelected -= OnDebugToggleTrackingButton;
        }
    }
}