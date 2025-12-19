// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.Characters;
using SpatialLingo.States;
using Unity.VisualScripting;

namespace SpatialLingo.VisualScriptingUnits
{
    /// <summary>
    /// Unit that exits on debug skip or on a secondary input.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class LanguageSelectUnit : SkippableUnit
    {
        [DoNotSerialize] public ValueInput GollyGoshManagerInput;
        [DoNotSerialize] public ValueInput LanguageSelectStateInput;

        private SpatialLingoApp m_spatialLingoApp;
        private GollyGoshInteractionManager m_gollyGoshManager;
        private LanguageSeedController m_seedController;
        private LanguageSelectState m_state;

        protected override void Definition()
        {
            base.Definition();
            GollyGoshManagerInput = ValueInput<GollyGoshInteractionManager>(nameof(GollyGoshManagerInput));
            LanguageSelectStateInput = ValueInput<LanguageSelectState>(nameof(LanguageSelectStateInput));
        }

        protected override void OnEnter(Flow flow)
        {
            m_spatialLingoApp = Variables.Application.Get<SpatialLingoApp>(nameof(SpatialLingoApp));
            m_gollyGoshManager = flow.GetValue<GollyGoshInteractionManager>(GollyGoshManagerInput);
            var mound = Variables.Application.Get<FocusPointController>(nameof(FocusPointController));
            m_seedController = Variables.Application.Get<LanguageSeedController>(nameof(LanguageSeedController));
            m_state = flow.GetValue<LanguageSelectState>(LanguageSelectStateInput);

            // Input assets
            m_state.SendFlowSignal += OnSendFlowSignal;
            var spawnTransform = mound == null ? Variables.Application.Get<TreeController>(nameof(TreeController)).transform : mound.transform;
            m_state.WillGetFocus(m_gollyGoshManager, spawnTransform, m_spatialLingoApp.HeadsetEyeCenterTransform, m_seedController);
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