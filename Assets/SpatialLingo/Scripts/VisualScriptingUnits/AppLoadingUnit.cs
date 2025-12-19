// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using SpatialLingo.AppSystems;
using SpatialLingo.States;
using Unity.VisualScripting;

namespace SpatialLingo.VisualScriptingUnits
{
    [MetaCodeSample("SpatialLingo")]
    public class AppLoadingUnit : SkippableUnit
    {
        [DoNotSerialize] public ValueInput AppLoadingStateInput;

        private AppLoadingState m_state;
        private SpatialLingoApp m_spatialLingoApp;

        protected override void Definition()
        {
            base.Definition();
            AppLoadingStateInput = ValueInput<AppLoadingState>(nameof(AppLoadingStateInput));
        }

        protected override void OnEnter(Flow flow)
        {
            m_spatialLingoApp = Variables.Application.Get<SpatialLingoApp>(nameof(SpatialLingoApp));
            m_state = flow.GetValue<AppLoadingState>(AppLoadingStateInput);
            m_state.SendFlowSignal += OnSendFlowSignal;
            m_state.WillGetFocus(m_spatialLingoApp.HeadsetEyeCenterTransform);
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