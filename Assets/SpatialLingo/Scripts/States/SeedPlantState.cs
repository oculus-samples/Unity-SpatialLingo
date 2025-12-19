// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.Audio;
using SpatialLingo.Characters;
using SpatialLingo.Lessons;
using UnityEngine;

namespace SpatialLingo.States
{
    [MetaCodeSample("SpatialLingo")]
    public class SeedPlantState : FlowState
    {
        public new delegate void SendFlowSignalEvent(TreeController treeController);
        public new SendFlowSignalEvent SendFlowSignal;

        [SerializeField] private GameObject m_treeControllerPrefab;

        private GollyGoshInteractionManager m_gollyGoshInteractionManager;
        private FocusPointController m_moundController;
        private TreeController m_treeController;
        private LanguageSeedController m_seedController;
        private Transform m_headsetTransform;

        public void WillGetFocus(GollyGoshInteractionManager manager, FocusPointController mound, LanguageSeedController seedController, Transform headsetTransform)
        {
            m_gollyGoshInteractionManager = manager;
            m_moundController = mound;
            m_seedController = seedController;
            m_headsetTransform = headsetTransform;

            // Tree
            var gameObjectTree = Instantiate(m_treeControllerPrefab);
            m_treeController = gameObjectTree.GetComponent<TreeController>();
            m_treeController.gameObject.SetActive(false);
            m_gollyGoshInteractionManager.SetTreeController(m_treeController);

            // Start performance
            _ = StartCoroutine(PlantSeedSteps());
        }

        public IEnumerator PlantSeedSteps()
        {
            var moundPosition = m_moundController.transform.position;

            // Transition from user to seed
            m_gollyGoshInteractionManager.Controller.StopFollowing();
            m_gollyGoshInteractionManager.LookAt(m_seedController.transform, 0.5f);

            // Move the seed to the mound
            m_seedController.PlayRumble();
            yield return new WaitForSeconds(0.25f);
            m_seedController.MoveTo(moundPosition, false);
            yield return new WaitForSeconds(0.30f);
            m_moundController.ShowDiveInEffect();
            AppAudioController.Instance.PlaySound(SoundEffect.DirtMoundHit, moundPosition);
            yield return new WaitForSeconds(0.05f);
            // Disappear mound
            m_moundController.FadeAway();
            m_moundController = null;
            // Look at area around where seed was
            m_seedController.FadeOutDestroy();
            m_seedController = null;
            yield return new WaitForSeconds(0.40f);
            // Transition from seed to tree "growing"
            var treeFocusPoint = moundPosition + new Vector3(0.0f, 0.50f, 0.0f);
            m_gollyGoshInteractionManager.LookAt(treeFocusPoint, 2.0f);

            // GG watch & move somewhat down and to side:
            var distanceSideMove = 0.30f;
            var distanceDownMove = 0.60f;
            var distanceBackMove = -0.30f;
            var startGG = m_gollyGoshInteractionManager.Controller.GetPosition();
            var ggToMound = moundPosition - startGG;
            ggToMound.y = 0;
            ggToMound.Normalize();
            var backMove = ggToMound;
            backMove.Scale(new Vector3(distanceBackMove, distanceBackMove, distanceBackMove));
            ggToMound = Quaternion.Euler(0.0f, 90.0f, 0.0f) * ggToMound; // Character's right direction
            ggToMound.Scale(new Vector3(distanceSideMove, distanceSideMove, distanceSideMove));
            var downMove = new Vector3(0.0f, -distanceDownMove, 0.0f);
            var endGG = startGG + downMove + backMove + ggToMound;
            endGG.y = Math.Max(endGG.y, moundPosition.y + 0.20f); // Keep GG out of ground if user eye level is low
            m_gollyGoshInteractionManager.Controller.MoveTo(endGG, false, null, false, GollyGoshController.GollyGoshMovement.EaseInOut, 1.5f);

            // Sprout tree
            var treePosition = moundPosition;
            treePosition.y = 0; // make sure to ground
            m_treeController.transform.position = treePosition;
            m_treeController.gameObject.SetActive(true);

            // Make tree animate in
            yield return new WaitForEndOfFrame();
            m_treeController.AnimateToTier(1);

            // Sprouting speaking
            m_gollyGoshInteractionManager.Speak(AppSessionData.TargetLanguageAI, Tutorial.PlantCompletePhrase());
            yield return m_gollyGoshInteractionManager.WaitForSpeechOrTimeout();
            yield return m_gollyGoshInteractionManager.WaitForSpeechPause();

            // Transition from tree to user, 
            m_gollyGoshInteractionManager.LookAt(m_headsetTransform, 0.5f);
            endGG = m_gollyGoshInteractionManager.Controller.GetPosition() + new Vector3(0.0f, 0.50f, 0.0f);
            m_gollyGoshInteractionManager.Controller.MoveTo(endGG, false, null, false, GollyGoshController.GollyGoshMovement.EaseInOut, 1.0f);

            m_gollyGoshInteractionManager.Speak(AppSessionData.TargetLanguageAI, Tutorial.StartGameplayPhrase(AppSessionData.TargetLanguageName));
            yield return m_gollyGoshInteractionManager.WaitForSpeechOrTimeout();
            yield return m_gollyGoshInteractionManager.WaitForSpeechPause();

            // medium range XZ & short range Y follow while waiting
            m_gollyGoshInteractionManager.Controller.FollowXZandY(m_headsetTransform, 0.40f, 1.0f, 0.0f, 0.10f);

            // Done
            SendFlowSignal?.Invoke(m_treeController);
        }

        public void WillLoseFocus()
        {
            if (m_seedController != null)
            {
                Destroy(m_seedController.gameObject);
            }

            Destroy(gameObject);
        }
    }
}