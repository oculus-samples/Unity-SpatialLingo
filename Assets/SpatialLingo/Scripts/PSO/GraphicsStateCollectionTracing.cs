// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking.PlayerConnection;

namespace SpatialLingo.PSO
{
    [MetaCodeSample("SpatialLingo")]
    public class GraphicsStateCollectionTracing : MonoBehaviour
    {
        private GraphicsStateCollection m_graphicsStateCollection;

        private string m_saveFilePath = "Scene.graphicsstate";
        private string m_loadFilePath = "SceneLoad.graphicsstate";
        private DateTime m_traceStart;
        private float m_traceDuration;
        private bool m_isTracing;

        private static GraphicsStateCollectionTracing s_instance;
        public static GraphicsStateCollectionTracing Instance
        {
            get
            {
                if (s_instance == null)
                {
                    var go = new GameObject("GraphicsStateCollectionTracing");
                    s_instance = go.AddComponent<GraphicsStateCollectionTracing>();
                }
                return s_instance;
            }
        }

        private void Awake()
        {
            m_saveFilePath = "/sdcard/Documents/" + m_saveFilePath;
            m_loadFilePath = Application.streamingAssetsPath + "/" + m_loadFilePath;
        }

        public void StartTracing(float traceDurationSeconds = 60.0f)
        {
            if (m_isTracing)
            {
                return;
            }
            m_isTracing = true;
            m_traceStart = DateTime.Now;
            m_traceDuration = traceDurationSeconds;
            StartTrace();
        }

        private void StartTrace()
        {
            _ = SystemInfo.supportsParallelPSOCreation;
            m_graphicsStateCollection = new GraphicsStateCollection();
            _ = m_graphicsStateCollection.BeginTrace();
        }

        private void StopTrace()
        {
            m_graphicsStateCollection.EndTrace();
        }

        private void SaveTrace()
        {
            _ = m_graphicsStateCollection.SaveToFile(m_saveFilePath);
        }

        public void SendTrace()
        {
            if (PlayerConnection.instance.isConnected)
            {
                _ = m_graphicsStateCollection.SendToEditor(m_saveFilePath);
            }
            else
            {
                Debug.LogWarning("GraphicsStateCollectionTracing - No PlayerConnection found! Collection not sent to Editor.");
            }
        }

        private void Update()
        {
            if (m_isTracing)
            {
                var now = DateTime.Now;
                var diff = now - m_traceStart;
                if (diff.TotalSeconds > m_traceDuration)
                {
                    m_isTracing = false;
                    StopTrace();
                    SaveTrace();
                }
            }
        }
    }
}