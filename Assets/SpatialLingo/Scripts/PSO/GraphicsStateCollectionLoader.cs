// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.IO;
using Meta.XR.Samples;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking;

namespace SpatialLingo.PSO
{
    [MetaCodeSample("SpatialLingo")]
    public class GraphicsStateCollectionLoader : MonoBehaviour
    {
        private struct PostWarmUpJob : IJob
        {
            public event Action Complete;
            public void Execute()
            {
                Complete?.Invoke();
            }
        }

        public event Action WarmUpCompleted;
        private const string TEMP_FILE_NAME = "graphicsCollectionTempScene.graphicsstate";
        private GraphicsStateCollection m_loadGraphicsStateCollection;

        private void OnJobComplete()
        {
            WarmUpCompleted?.Invoke();
        }

        public void WarmUp(string streamingAssetsLoadPath)
        {
            var absoluteSourcePath = Path.Combine(Application.streamingAssetsPath, streamingAssetsLoadPath);
            var absoluteDestinationPath = Path.Combine(Application.persistentDataPath, TEMP_FILE_NAME);
            m_loadGraphicsStateCollection = new GraphicsStateCollection();
            CopyFileFromStreamingAssetsToPersistentData(absoluteSourcePath, absoluteDestinationPath, OnPersistentFileWrite);
        }

        public void OnPersistentFileWrite(string persistentFilePath)
        {
            if (persistentFilePath != null)
            {
                try
                {
                    var exists = File.Exists(persistentFilePath);
                    _ = m_loadGraphicsStateCollection.LoadFromFile(persistentFilePath);
                    if (m_loadGraphicsStateCollection.isWarmedUp)
                    {
                        OnJobComplete();
                    }
                    else
                    {
                        var handle = m_loadGraphicsStateCollection.WarmUp();
                        handle.Complete();
                        OnJobComplete();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"GraphicsStateCollectionLoader - loadGraphicsStateCollection Load or Warmup failed: {e.Message}");
                    OnJobComplete();
                }
            }
            else
            {
                OnJobComplete();
            }
        }

        private void CopyFileFromStreamingAssetsToPersistentData(string sourcePath, string destinationPath, Action<string> onComplete)
        {
            _ = StartCoroutine(CopyFileFromStreamingAssets(sourcePath, destinationPath, onComplete));
        }

        private IEnumerator CopyFileFromStreamingAssets(string sourcePath, string destinationPath, Action<string> onComplete)
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            var www = UnityWebRequest.Get(sourcePath);
            yield return www.SendWebRequest();

            bool success;
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load file from StreamingAssets: {www.error}");
                success = false;
            }
            else
            {
                try
                {
                    File.WriteAllBytes(destinationPath, www.downloadHandler.data);
                    success = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to write file to Persistent Data Path: {e.Message}");
                    success = false;
                }
            }
            if (onComplete != null)
            {
                onComplete?.Invoke(success ? destinationPath : null);
            }
        }
    }
}