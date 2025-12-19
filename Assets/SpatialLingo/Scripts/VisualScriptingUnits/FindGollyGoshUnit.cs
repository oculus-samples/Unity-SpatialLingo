// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.Characters;
using SpatialLingo.States;
using Unity.VisualScripting;

namespace SpatialLingo.VisualScriptingUnits
{
    [MetaCodeSample("SpatialLingo")]
    public class FindGollyGoshUnit : SkippableUnit
    {
        [DoNotSerialize] public ValueInput GollyGoshManagerInput;
        [DoNotSerialize] public ValueInput FindGollyGoshStateInput;

        private GollyGoshInteractionManager m_gollyGoshInteractionManager;
        private FindGollyGoshState m_state;
        private LanguageSeedController m_seed;
        private SpatialLingoApp m_spatialLingoApp;
        private FocusPointController m_mound;

        protected override void Definition()
        {
            base.Definition();
            GollyGoshManagerInput = ValueInput<GollyGoshInteractionManager>(nameof(GollyGoshManagerInput));
            FindGollyGoshStateInput = ValueInput<FindGollyGoshState>(nameof(FindGollyGoshStateInput));
        }

        protected override void OnEnter(Flow flow)
        {
            m_spatialLingoApp = Variables.Application.Get<SpatialLingoApp>(nameof(SpatialLingoApp));
            m_gollyGoshInteractionManager = flow.GetValue<GollyGoshInteractionManager>(GollyGoshManagerInput);
            m_state = flow.GetValue<FindGollyGoshState>(FindGollyGoshStateInput);
            m_state.SendFlowSignal += OnSendFlowSignal;
            m_state.WillGetFocus(m_gollyGoshInteractionManager, m_spatialLingoApp.HeadsetEyeCenterTransform, m_spatialLingoApp.AudioController);
        }

        private void OnSendFlowSignal(LanguageSeedController seedController, FocusPointController focusController)
        {
            m_seed = seedController;
            Variables.Application.Set(nameof(LanguageSeedController), m_seed);
            m_mound = focusController;
            Variables.Application.Set(nameof(FocusPointController), m_mound);
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