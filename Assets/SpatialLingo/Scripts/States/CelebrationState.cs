// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.Characters;
using SpatialLingo.Lessons;
using UnityEngine;

namespace SpatialLingo.States
{
    [MetaCodeSample("SpatialLingo")]
    public class CelebrationState : FlowState
    {
        public new delegate void SendFlowSignalEvent();
        public new SendFlowSignalEvent SendFlowSignal;

        private GollyGoshInteractionManager m_gollyGoshInteractionManager;
        private TreeController m_treeController;
        private Transform m_headsetTransform;

        public void WillGetFocus(GollyGoshInteractionManager manager, TreeController treeController, Transform headsetTransform)
        {
            m_gollyGoshInteractionManager = manager;
            m_treeController = treeController;
            m_headsetTransform = headsetTransform;

            CelebrationSteps();
        }

        private void ArrivedAtTree(bool completed)
        {
            m_gollyGoshInteractionManager.ShowFaceHappy();
            m_gollyGoshInteractionManager.Controller.Follow(m_headsetTransform, 0.50f, 1.0f);
            SendFlowSignal?.Invoke();
        }

        private void CelebrationSteps()
        {
            m_gollyGoshInteractionManager.ShowFaceSurprised();
            m_gollyGoshInteractionManager.Speak(AppSessionData.TargetLanguageAI, Tutorial.ReactToAllTiersComplete());
            m_gollyGoshInteractionManager.OnUserCompletedAllTiers();

            // Move GG to above tree as final destination:
            var aboveTree = m_treeController.transform.position;
            aboveTree.y = 1.0f;
            m_gollyGoshInteractionManager.Controller.MoveTo(aboveTree, false, ArrivedAtTree);
        }

        public void WillLoseFocus()
        {
            Destroy(gameObject);
        }
    }
}