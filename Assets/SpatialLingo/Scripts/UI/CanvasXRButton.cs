// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace SpatialLingo.UI
{
    /// <summary>
    /// Allows for basic listening on: poke / ray-select interaction events
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class CanvasXRButton : MonoBehaviour
    {
        public delegate void CanvasWasSelectedEvent(CanvasXRButton button);
        public event CanvasWasSelectedEvent ButtonWasSelected;

        private void Start()
        {
            var interactable = GetComponentInChildren<PokeInteractable>();
            if (interactable != null)
            {
                interactable.WhenPointerEventRaised += OnPokePointerEventRaised;
            }

            var rayInteractable = GetComponentInChildren<RayInteractable>();
            if (rayInteractable != null)
            {
                rayInteractable.WhenPointerEventRaised += OnRayPointerEventRaised;
            }
        }

        private void OnPokePointerEventRaised(PointerEvent pointerEvent)
        {
            if (pointerEvent.Type == PointerEventType.Select)
            {
                ButtonWasSelected?.Invoke(this);
            }
        }

        private void OnRayPointerEventRaised(PointerEvent pointerEvent)
        {
            if (pointerEvent.Type == PointerEventType.Select)
            {
                ButtonWasSelected?.Invoke(this);
            }
        }

        [ContextMenu("Click button")]
        public void ClickButton() => ButtonWasSelected?.Invoke(this);
    }
}