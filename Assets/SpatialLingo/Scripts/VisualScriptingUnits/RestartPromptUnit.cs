// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.Characters;
using SpatialLingo.Lessons;
using SpatialLingo.SceneObjects;
using Unity.VisualScripting;
using UnityEngine;

namespace SpatialLingo.VisualScriptingUnits
{
    [MetaCodeSample("SpatialLingo")]
    public class RestartPromptUnit : SkippableUnit
    {
        private const float TIMEOUT_RESTART_NAG_FIRST = 5.0f;
        private const float TIMEOUT_RESTART_NAG = 8.0f;

        public ControlOutput PickNewLanguage;

        [DoNotSerialize] public ValueInput WordBarPrefabInput;
        [DoNotSerialize] public ValueInput GollyGoshManagerInput;

        private WordBar3D m_wordBarPrefab;
        private GollyGoshInteractionManager m_gollyGoshManager;
        private TreeController m_treeController;
        private RestartPrompt m_restartPrompt;
        private Coroutine m_nagRoutine;

        protected override void Definition()
        {
            base.Definition();
            PickNewLanguage = ControlOutput(nameof(PickNewLanguage));
            WordBarPrefabInput = ValueInput<WordBar3D>(nameof(WordBarPrefabInput));
            GollyGoshManagerInput = ValueInput<GollyGoshInteractionManager>(nameof(GollyGoshManagerInput));
        }

        protected override void OnEnter(Flow flow)
        {
            m_wordBarPrefab = flow.GetValue<WordBar3D>(WordBarPrefabInput);
            m_gollyGoshManager = flow.GetValue<GollyGoshInteractionManager>(GollyGoshManagerInput);
            m_treeController = Variables.Application.Get<TreeController>(nameof(TreeController));
            ShowRestartOptions();
        }

        private void ShowRestartOptions()
        {
            var distanceFromTree = 1.75f;
            var camPos = Camera.main.transform.position;
            var treePos = m_treeController.transform.position;
            var treeToPlayer = camPos - treePos;
            var optionsSpawn = treePos + treeToPlayer.normalized * distanceFromTree;
            optionsSpawn.y = 1.25f;
            var optionsRot = Quaternion.LookRotation(camPos - optionsSpawn, Vector3.up);

            m_restartPrompt = new RestartPrompt(m_wordBarPrefab, optionsSpawn, optionsRot);
            m_restartPrompt.SelectedOption += OnRestartOptionSelected;
            m_restartPrompt.AnimateIn();
            m_nagRoutine = CoroutineRunner.instance.StartCoroutine(GGNagCycle());
        }

        private void OnRestartOptionSelected(RestartPrompt.RestartOption option)
        {
            switch (option)
            {
                case RestartPrompt.RestartOption.Language:
                    m_targetControlOutput = PickNewLanguage;
                    RestartExperience(0);
                    break;
                case RestartPrompt.RestartOption.Lesson:
                    m_targetControlOutput = exit;
                    RestartExperience(1);
                    break;
            }
        }

        private void RestartExperience(int treeTier)
        {
            m_restartPrompt.AnimateOut();
            m_restartPrompt = null;

            if (m_nagRoutine != null)
            {
                CoroutineRunner.instance.StopCoroutine(m_nagRoutine);
                m_nagRoutine = null;
            }

            // Remove all berries from tree
            var berries = m_treeController.RemoveAllBerries();

            foreach (var berry in berries)
            {
                var controller = berry.GetComponent<BerryController>();
                if (controller != null)
                {
                    controller.FadeOutDestroy();
                }
            }

            m_treeController.ClosePortal();

            // Update tree tier
            AppSessionData.Tier = treeTier;
            m_treeController.AnimateToTier(AppSessionData.Tier);
            m_isDone = true;
        }

        private IEnumerator GGNagCycle()
        {
            var isFirstNag = true;
            while (!m_isDone)
            {
                var nagWaitTime = isFirstNag ? TIMEOUT_RESTART_NAG_FIRST : TIMEOUT_RESTART_NAG;
                yield return new WaitForSeconds(nagWaitTime);
                isFirstNag = false;
                m_gollyGoshManager.Speak(AppSessionData.TargetLanguageAI, Tutorial.WaitGameRestart());
            }
        }
    }
}