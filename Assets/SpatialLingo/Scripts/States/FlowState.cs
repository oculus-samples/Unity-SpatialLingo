// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.States
{
    [MetaCodeSample("SpatialLingo")]
    public class FlowState : MonoBehaviour
    {
        public delegate void SendFlowSignalEvent(string name, object context);
        public SendFlowSignalEvent SendFlowSignal;

        public const string FLOW_SIGNAL = "FlowSignal";

        public virtual void WillGetFocus(object payload)
        {
            // 
        }

        public virtual void DidGetFocus()
        {
            // 
        }

        public virtual void WillLoseFocus(object payload)
        {
            // 
        }

        public virtual void DidLoseFocus()
        {
            // 
        }
    }
}