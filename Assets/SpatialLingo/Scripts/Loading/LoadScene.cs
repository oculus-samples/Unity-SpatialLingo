// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using SpatialLingo.States;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpatialLingo.Loading
{
    [MetaCodeSample("SpatialLingo")]
    public class LoadScene : MonoBehaviour
    {
        private const string MAIN_SCENE_NAME = "MainScene";
        private const string SELECT_SCENE_NAME = "SelectScene";

        private enum SceneToLoad
        {
            None,
            SelectScene,
            MainScene
        }

        [SerializeField] private AppLoadingState m_appLoadingState;
        [SerializeField] private SceneToLoad m_sceneToLoad = SceneToLoad.MainScene;

        private void Start()
        {
            if (m_appLoadingState == null) return;
            m_appLoadingState.SendFlowSignal += OnSendFlowSignal;
            m_appLoadingState.WillGetFocus(Camera.main.transform);
        }

        private void OnSendFlowSignal()
        {
            m_appLoadingState.SendFlowSignal -= OnSendFlowSignal;
            m_appLoadingState.WillLoseFocus();

            LoadNextScene();
        }

        private void LoadNextScene()
        {
            switch (m_sceneToLoad)
            {
                case SceneToLoad.SelectScene:
                    SceneManager.LoadScene(SELECT_SCENE_NAME);
                    break;
                case SceneToLoad.MainScene:
                    SceneManager.LoadScene(MAIN_SCENE_NAME);
                    break;
                case SceneToLoad.None:
                default:
                    Debug.LogWarning("LoadScene - No scene selected to load");
                    break;
            }
        }
    }
}