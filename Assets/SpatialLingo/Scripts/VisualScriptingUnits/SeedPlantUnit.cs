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
    public class SeedPlantUnit : SkippableUnit
    {
        [DoNotSerialize] public ValueInput GollyGoshManagerInput;
        [DoNotSerialize] public ValueInput SeedPlantStateInput;

        private SpatialLingoApp m_spatialLingoApp;
        private LanguageSeedController m_seed;
        private GollyGoshInteractionManager m_gollyGoshInteractionManager;
        private SeedPlantState m_state;

        protected override void Definition()
        {
            base.Definition();
            GollyGoshManagerInput = ValueInput<GollyGoshInteractionManager>(nameof(GollyGoshManagerInput));
            SeedPlantStateInput = ValueInput<SeedPlantState>(nameof(SeedPlantStateInput));
        }

        protected override void OnEnter(Flow flow)
        {
            m_spatialLingoApp = Variables.Application.Get<SpatialLingoApp>(nameof(SpatialLingoApp));
            m_state = flow.GetValue<SeedPlantState>(SeedPlantStateInput);
            m_seed = Variables.Application.Get<LanguageSeedController>(nameof(LanguageSeedController));
            m_gollyGoshInteractionManager = flow.GetValue<GollyGoshInteractionManager>(GollyGoshManagerInput);
            var mound = Variables.Application.Get<FocusPointController>(nameof(FocusPointController));
            m_state.SendFlowSignal += OnSendFlowSignal;
            m_state.WillGetFocus(m_gollyGoshInteractionManager, mound, m_seed, m_spatialLingoApp.HeadsetEyeCenterTransform);
        }

        private void OnSendFlowSignal(TreeController treeController)
        {
            Variables.Application.Set(nameof(TreeController), treeController);

            EndInternalState();
            m_isDone = true;
        }

        protected override void OnExit()
        {
            EndInternalState();
            base.OnExit();
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