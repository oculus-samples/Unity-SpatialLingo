// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.IO;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpatialLingo.UI
{
    [MetaCodeSample("SpatialLingo")]
    public class SceneMenu : MonoBehaviour
    {
        // Controller buttons don't register right away
        private const float WAIT_CHECK_DEBUG_TIME = 0.3f;

        [SerializeField] private GameObject m_displayUI;

        private void Start()
        {
            CreateMenu();
        }

        private void CreateMenu()
        {
            m_displayUI.SetActive(true);

            var sceneList = GetAllScenes();

            UIBuilder.Instance.AddDivider();
            foreach (var (sceneIndex, sceneName) in sceneList)
            {
                _ = UIBuilder.Instance.AddButton(
                    sceneName, sceneIndex,
                    () => LoadScene(sceneIndex)
                    );
            }
            UIBuilder.Instance.AddDivider();
        }

        private List<(int index, string name)> GetAllScenes()
        {
            var sceneList = new List<(int, string)>();
            var currIndex = SceneManager.GetActiveScene().buildIndex;

            // Exclude scene 0 (LoadingScene) from the list
            for (var i = 1; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                // Exclude current scene from the list
                if (i == currIndex)
                {
                    continue;
                }

                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                sceneList.Add((i, sceneName));
            }

            return sceneList;
        }

        private void LoadScene(int sceneIndex)
        {
            SceneManager.LoadScene(sceneIndex);
        }
    }
}