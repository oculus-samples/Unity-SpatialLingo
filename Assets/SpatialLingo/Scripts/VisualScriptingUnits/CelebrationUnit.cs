// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.Characters;
using SpatialLingo.SceneObjects;
using SpatialLingo.States;
using Unity.VisualScripting;

namespace SpatialLingo.VisualScriptingUnits
{
    /// <summary>
    /// Unit that exits on debug skip or on a secondary input.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class CelebrationUnit : SkippableUnit
    {
        [DoNotSerialize] public ValueInput GollyGoshManagerInput;
        [DoNotSerialize] public ValueInput CelebrationStateInput;

        private SpatialLingoApp m_spatialLingoApp;
        private LanguageSelect m_languageSelect;
        private GollyGoshInteractionManager m_gollyGoshManager;
        private TreeController m_treeController;
        private CelebrationState m_state;

        protected override void Definition()
        {
            base.Definition();
            GollyGoshManagerInput = ValueInput<GollyGoshInteractionManager>(nameof(GollyGoshManagerInput));
            CelebrationStateInput = ValueInput<CelebrationState>(nameof(CelebrationStateInput));
        }

        protected override void OnEnter(Flow flow)
        {
            m_spatialLingoApp = Variables.Application.Get<SpatialLingoApp>(nameof(SpatialLingoApp));
            m_gollyGoshManager = flow.GetValue<GollyGoshInteractionManager>(GollyGoshManagerInput);
            m_treeController = Variables.Application.Get<TreeController>(nameof(TreeController));
            m_state = flow.GetValue<CelebrationState>(CelebrationStateInput);
            // Input assets
            m_state.SendFlowSignal += OnSendFlowSignal;
            m_state.WillGetFocus(m_gollyGoshManager, m_treeController, m_spatialLingoApp.HeadsetEyeCenterTransform);
        }

        private void OnSendFlowSignal()
        {
            EndInternalState();
            m_isDone = true;
        }

        protected override void OnExit()
        {
            EndInternalState();
        }

        private void EndInternalState()
        {
            if (m_state != null)
            {
                m_state.SendFlowSignal -= OnSendFlowSignal;
                m_state.WillLoseFocus();
                m_state = null;
            }
        }
    }
}