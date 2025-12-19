// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using Meta.XR.Samples;
using Unity.VisualScripting;

namespace SpatialLingo.VisualScriptingUnits
{
    [MetaCodeSample("SpatialLingo")]
    [UnitTitle("Wait For User Lesson Attempt")]
    public class WaitForUserLessonAttempt : SkippableUnit
    {
        [DoNotSerialize] public ControlOutput Failed;
        private bool m_responseReceived;
        private bool m_successful;

        protected override void Definition()
        {
            base.Definition();
            Failed = ControlOutput(nameof(Failed));
        }

        protected override void OnEnter(Flow flow)
        {
            m_responseReceived = false;
            EventBus.Register<bool>(ScriptEventNames.USER_ATTEMPTED_LESSON, OnUserResponse);
            EventBus.Trigger(ScriptEventNames.START_USER_LESSON_ATTEMPT, new EmptyEventArgs());
        }

        protected override void OnExit()
        {
            if (!m_responseReceived)
            {
                EventBus.Trigger(ScriptEventNames.USER_ATTEMPTED_LESSON, true);
            }

            EventBus.Unregister(ScriptEventNames.USER_ATTEMPTED_LESSON, (Action<bool>)OnUserResponse);
        }

        private void OnUserResponse(bool success)
        {
            m_responseReceived = true;
            m_targetControlOutput = success ? exit : Failed;
            m_isDone = true;
        }
    }
}
