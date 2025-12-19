// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace SpatialLingo.SceneObjects
{
    [MetaCodeSample("SpatialLingo")]
    [ExecuteInEditMode]
    public class TextCloudItem : MonoBehaviour
    {
        public enum WordType
        {
            noun,
            verb,
            adjective,
            none,
        }
        [SerializeField] private WordType m_wordUsage;
        [SerializeField] private Color m_nounColor = new(0.757f, 0.204f, 0.314f);
        [SerializeField] private Color m_verbColor = new(0.161f, 0.588f, 0.0f);
        [SerializeField] private Color m_adjColor = new(0.996f, 0.6745f, 0.145f);
        [SerializeField] private Color m_backerColor = Color.gray;
        [FormerlySerializedAs("m_tMP")] [SerializeField] private TextMeshProUGUI m_tmp = null; //required
        [SerializeField] private GameObject m_backer = null; //required
        [SerializeField] private float m_minWidth = 0.25f;
        [SerializeField] private float m_maxWidth = 2.0f;
        [SerializeField] private float m_minHeight = 0.15f;
        [SerializeField] private float m_maxHeight = 0.25f;
        [SerializeField] private float m_textPadding = 0.123f;
        [SerializeField] private Color m_fontColor = Color.black;
        [SerializeField] private float m_emmisiveIntensity = 0.0f;
        [ColorUsage(true, true)]
        [SerializeField] private Color m_emissiveColor = Color.yellow;
        [SerializeField] private string m_displayWord = "Enter Text Here";
        private MaterialPropertyBlock m_block;

        public WordType WordUsage { get => m_wordUsage; set => m_wordUsage = value; }
        public Color BackerColor => m_backerColor;
        public TextMeshProUGUI TMP => m_tmp;
        public string DisplayWord { get => m_displayWord; set => m_displayWord = value; }

        public Vector2 Size => new(m_backerWidth, m_backerHeight);

        private float m_backerWidth;
        private float m_backerHeight;

        [ColorUsage(true, true)]
        private Color m_faceColor = Color.white;

        private MeshRenderer m_renderer;

        private void Awake()
        {
            Assert.IsNotNull(m_tmp);
            Assert.IsNotNull(m_backer);
            m_renderer = m_backer.GetComponent<MeshRenderer>();
            m_block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            UpdateAllDisplayFromInternals();
        }

        public void SetDefaultSharedColors()
        {
            _ = ColorUtility.TryParseHtmlString("#c13450ff", out m_nounColor);
            _ = ColorUtility.TryParseHtmlString("#299600", out m_verbColor);
            _ = ColorUtility.TryParseHtmlString("#FEAC25", out m_adjColor);
        }

        public void SetTextDisplay(string displayText)
        {
            // This does not update the sizes of the backer
            m_tmp.text = displayText;
        }

        public void SetTextColor(Color color)
        {
            m_tmp.color = m_fontColor;
            m_tmp.faceColor = color;
        }

        public void UpdateAllDisplayFromInternals()
        {
            // Calculate the text size 
            var size = TMPSizeFromText(m_displayWord);

            SetBackerColor();
            // Calculate needed backer size
            size = BackerSizeFromTextSize(size.x, size.y);
            // Apply the backer size
            SetBackerFromSize(size.x, size.y);
        }

        public void SetBackerFromSize(float addWidth, float addHeight)
        {
            m_backerWidth = addWidth;
            m_backerHeight = addHeight;
            // Get and modify material property block
            m_renderer.GetPropertyBlock(m_block); // Get existing properties
            m_block.SetFloat("_Additive_Width", addWidth / 2);
            m_block.SetFloat("_Additive_Height", addHeight / 2);
            m_block.SetColor("_Color", m_backerColor);
            m_block.SetFloat("_Emissive_Intensity", m_emmisiveIntensity);
            m_block.SetColor("_Emissive_Color", m_emissiveColor);
            m_renderer.SetPropertyBlock(m_block); // Apply the block
        }

        private Vector2 BackerSizeFromTextSize(float textWidth, float textHeight)
        {
            var meshWidth = m_renderer.localBounds.extents[0] * 2;
            var meshHeight = m_renderer.localBounds.extents[1] * 2; ;
            textWidth = Mathf.Clamp(textWidth + m_textPadding, m_minWidth, m_maxWidth);
            var addWidth = textWidth - meshWidth;

            textHeight = Mathf.Clamp(textHeight + m_textPadding, m_minHeight, m_maxHeight);
            var addHeight = textHeight - meshHeight;
            return new Vector2(addWidth, addHeight);
        }

        private Vector2 TMPSizeFromText(string displayText)
        {
            // Get the TMPro object and retrieve some values
            m_tmp.text = displayText;
            m_tmp.ForceMeshUpdate(); // must be called before we get sizes

            var textWidth = m_tmp.preferredWidth;
            var textHeight = m_tmp.preferredHeight;

            m_tmp.color = m_fontColor;
            m_tmp.faceColor = m_faceColor;
            return new Vector2(textWidth, textHeight);
        }

        public Vector2 BackerSizeForDisplayText(string displayText)
        {
            var was = m_tmp.text;
            var size = TMPSizeFromText(displayText);
            size = BackerSizeFromTextSize(size.x, size.y);
            // Re-assign:
            _ = TMPSizeFromText(was);
            return size;
        }

        private void SetBackerColor()
        {
            m_backerColor = m_wordUsage switch
            {
                WordType.noun => m_nounColor,
                WordType.verb => m_verbColor,
                WordType.adjective => m_adjColor,
                _ => Color.gray,
            };
        }

        public void UpdateBlockColor(Color color)
        {
            var renderer = m_backer.GetComponent<MeshRenderer>();

            // Get and modify material property block 
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block); // Get existing properties
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block); // Apply the block
        }
    }
}