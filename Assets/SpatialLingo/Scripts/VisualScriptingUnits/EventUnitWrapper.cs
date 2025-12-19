// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using Unity.VisualScripting;

namespace SpatialLingo.VisualScriptingUnits
{
    [MetaCodeSample("SpatialLingo")]
    public abstract class EventUnitWrapper<T> : EventUnit<T>
    {
        [DoNotSerialize] public ValueOutput Result;

        protected abstract string EventName { get; }
        protected override bool register => true;

        public override EventHook GetHook(GraphReference reference) => new(EventName);

        protected override void Definition()
        {
            base.Definition();
            Result = ValueOutput<T>(nameof(Result));
        }

        protected override void AssignArguments(Flow flow, T data)
        {
            flow.SetValue(Result, data);
        }
    }

    [MetaCodeSample("SpatialLingo")]
    public abstract class EventUnitWrapper : EventUnit<EmptyEventArgs>
    {
        protected abstract string EventName { get; }
        protected override bool register => true;

        public override EventHook GetHook(GraphReference reference) => new(EventName);
    }

    [MetaCodeSample("SpatialLingo")]
    public class TriggerCustomEvent : Unit
    {
        [DoNotSerialize]
        [PortLabelHidden]
        public ControlInput InputTrigger { get; private set; }

        [DoNotSerialize]
        [PortLabelHidden]
        public ControlOutput OutputTrigger { get; private set; }

        [DoNotSerialize] public ValueInput EventName { get; private set; }

        protected override void Definition()
        {
            InputTrigger = ControlInput(nameof(InputTrigger), Trigger);
            OutputTrigger = ControlOutput(nameof(OutputTrigger));
            EventName = ValueInput(nameof(EventName), string.Empty);
        }

        private ControlOutput Trigger(Flow flow)
        {
            EventBus.Trigger(flow.GetValue<string>(EventName));
            return OutputTrigger;
        }
    }
}