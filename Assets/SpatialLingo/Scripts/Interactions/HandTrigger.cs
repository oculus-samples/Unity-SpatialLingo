// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using Meta.XR.Samples;
using Oculus.Interaction.Input;
using UnityEngine;

namespace SpatialLingo.Interactions
{
    [MetaCodeSample("SpatialLingo")]
    public class HandTrigger : MonoBehaviour
    {
        public event Action HandTriggered;

        private void OnTriggerEnter(Collider other)
        {
            var hand = other.GetComponentInParent<Hand>();
            var isValidHandDetected = hand != null && hand.IsConnected;

            var controller = other.GetComponentInParent<Controller>();
            var isValidControllerDetected = controller != null && controller.IsConnected;

            var isInValidPosition = Vector3.SqrMagnitude(Vector3.zero - other.transform.position) > 0.001f;

            if ((isValidHandDetected || isValidControllerDetected) && isInValidPosition)
            {
                HandTriggered?.Invoke();
            }
        }
    }
}