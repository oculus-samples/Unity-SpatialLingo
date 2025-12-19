// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using Meta.Utilities;
using Meta.XR.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpatialLingo.UI
{
    [MetaCodeSample("SpatialLingo")]
    public class ConsentUI : MonoBehaviour, IPointerClickHandler
    {
        public Action OnConsentGiven;
        [SerializeField, AutoSet] private TMP_Text m_text;

        public void ConsentButtonPressed()
        {
            OnConsentGiven?.Invoke();
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_text == null)
                return;

            var camera = Camera.main;
            var position = camera.WorldToScreenPoint(eventData.pointerCurrentRaycast.worldPosition);
            var index = TMP_TextUtilities.FindIntersectingLink(m_text, position, camera);
            if (index == -1)
                return;

            var id = m_text.textInfo.linkInfo[index].GetLinkID();
            var url = id switch
            {
                "privacy" => "https://www.meta.com/legal/privacy-policy/",
                "tos" => "https://www.facebook.com/legal/ai-terms",
                _ => null,
            };
            if (url == null)
                return;

            Application.OpenURL(url);
        }
    }
}
