// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpatialLingo.UI
{
    [MetaCodeSample("SpatialLingo")]
    public class UIBuilder : MonoBehaviour
    {
        [SerializeField] private GameObject m_buttonPrefab;
        [Header("Layout Settings")]
        [SerializeField] private float m_buttonGap = 25f;
        [SerializeField] private float m_buttonW = 590f;
        [SerializeField] private float m_buttonH = 125f;
        [SerializeField] private float m_textPadding = 20f;

        public static UIBuilder Instance;
        private Canvas m_canvas;
        private List<GameObject> m_uiElements = new();
        private float m_currYPos = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                m_canvas = GetComponent<Canvas>();
                if (m_canvas == null)
                {
                    Debug.LogError("canvas not attached");
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public Button AddButton(string buttonText, int sceneIndex, System.Action onClickAction = null)
        {
            var buttonObj = Instantiate(m_buttonPrefab, m_canvas.transform);
            var button = buttonObj.GetComponent<Button>();

            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(m_buttonW, m_buttonH);

            var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (text != null)
            {
                text.text = buttonText;
                var textRect = text.GetComponent<RectTransform>();
                textRect.offsetMin = new Vector2(m_textPadding, m_textPadding);
                textRect.offsetMax = new Vector2(-m_textPadding, -m_textPadding);
            }
            else
            {
                Debug.LogWarning("no text comp in prefab");
            }

            if (onClickAction != null)
            {
                button.onClick.AddListener(() => onClickAction.Invoke());
            }

            PositionUIElement(buttonObj, sceneIndex);
            m_uiElements.Add(buttonObj);

            return button;
        }

        public void AddDivider()
        {
            m_currYPos -= m_buttonGap * 2f;
        }

        private void PositionUIElement(GameObject uiElement, int sceneIndex)
        {
            var rectTrans = uiElement.GetComponent<RectTransform>();

            rectTrans.anchorMin = new Vector2(1.0f + -sceneIndex % 2, 1f);
            rectTrans.anchorMax = new Vector2(1.0f + -sceneIndex % 2, 1f);
            rectTrans.pivot = new Vector2(1.0f + -sceneIndex % 2, 1f);
            rectTrans.anchoredPosition = new Vector2(0f, m_currYPos);

            if ((m_uiElements.Count + 1) % 2 == 0)
            {
                m_currYPos -= rectTrans.sizeDelta.y + m_buttonGap;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}