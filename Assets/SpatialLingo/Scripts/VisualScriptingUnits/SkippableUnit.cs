// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections;
using Meta.XR.Samples;
using Unity.VisualScripting;
using UnityEngine;

namespace SpatialLingo.VisualScriptingUnits
{
    /// <summary>
    /// Time unit that can exit early for debug purposes. Also reports when entered/exited.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class SkippableUnit : WaitUnit
    {
        public static event Action<string> UnitEntered;
        public static event Action<string> UnitExited;
        public static string CurrentName;
        [DoNotSerialize] public ValueInput Name;
        protected bool m_isDone;
        protected bool m_isActive;
        protected ControlOutput m_targetControlOutput;

        protected override void Definition()
        {
            base.Definition();
            Name = ValueInput(nameof(Name), GetType().Name);
        }

        protected override IEnumerator Await(Flow flow)
        {
            m_isDone = false;
            m_isActive = true;
            m_targetControlOutput = exit;
            CurrentName = flow.GetValue<string>(Name);
            UnitEntered?.Invoke(CurrentName);
            EventBus.Register<EmptyEventArgs>(ScriptEventNames.DEBUG_SKIP, OnSkip);
            return SkipCheck(flow);
        }

        protected IEnumerator SkipCheck(Flow flow)
        {
            OnEnter(flow);
            yield return new WaitUntil(() => m_isDone);
            UnitExited?.Invoke(CurrentName);
            EventBus.Unregister(ScriptEventNames.DEBUG_SKIP, (Action<EmptyEventArgs>)OnSkip);
            OnExit();
            m_isActive = false;
            yield return m_targetControlOutput;
        }

        protected void OnSkip(EmptyEventArgs args)
        {
            m_isDone = true;
        }

        protected virtual void OnEnter(Flow flow) { }
        protected virtual void OnExit() { }
    }
}