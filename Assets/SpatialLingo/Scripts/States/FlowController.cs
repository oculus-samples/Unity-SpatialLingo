// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.States
{
    [MetaCodeSample("SpatialLingo")]
    public class FlowContext
    {
        public object Data;
    }

    [MetaCodeSample("SpatialLingo")]
    public class NextStateContext
    {
        public string Name;
        public FlowState State;
        public object Data;
    }

    [MetaCodeSample("SpatialLingo")]
    public class FlowController : MonoBehaviour
    {
        private FlowState m_currentState;
        private NextStateContext m_nextContext;
        private Dictionary<string, FlowState> m_states = new();

        public void AddState(string name, FlowState prefab)
        {
            m_states[name] = prefab;
        }

        private void StartFlow(string name, object data)
        {
            if (m_states.TryGetValue(name, out var state))
            {
                var context = new NextStateContext
                {
                    State = state,
                    Name = name,
                    Data = data
                };
                m_nextContext = context;
            }
        }

        internal virtual void OnSendFlowSignal(string name, object context)
        {
            // No op
        }

        public virtual void Update()
        {
            if (m_nextContext != null)
            {
                // Current state pre-stop
                if (m_currentState != null)
                {
                    m_currentState.WillLoseFocus(m_nextContext.Data);
                }

                // Next state pre-start
                FlowState nextState = null;
                var nextStatePrefab = m_nextContext.State;
                if (nextStatePrefab != null)
                {
                    nextState = Instantiate(nextStatePrefab);
                    nextState.SendFlowSignal += OnSendFlowSignal;
                    nextState.WillGetFocus(m_nextContext.Data);
                }

                // Switch states
                m_nextContext = null;
                var previousState = m_currentState;
                m_currentState = nextState;

                // Prev state stop
                if (previousState != null)
                {
                    previousState.DidLoseFocus();
                    Destroy(previousState);
                }
                // Next State Start
                if (m_currentState != null)
                {
                    m_currentState.DidGetFocus();
                }
            }
        }
    }
}