// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using Unity.VisualScripting;

namespace SpatialLingo.VisualScriptingUnits
{
    /// <summary>
    /// Unit that exits on debug skip or on a secondary input.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class SkippableEventUnit : SkippableUnit
    {
        [DoNotSerialize] public ControlInput ContinueSignal;

        protected override void Definition()
        {
            base.Definition();
            ContinueSignal = ControlInput(nameof(ContinueSignal), OnContinue);
        }

        private ControlOutput OnContinue(Flow flow)
        {
            if (m_isActive)
            {
                m_isDone = true;
            }

            return null;
        }
    }
}