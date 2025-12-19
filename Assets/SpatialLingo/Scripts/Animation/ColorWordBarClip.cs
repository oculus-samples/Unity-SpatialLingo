// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using SpatialLingo.Lessons;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SpatialLingo.Animation
{
    [MetaCodeSample("SpatialLingo")]
    public class ColorWordBarClip : PlayableAsset, ITimelineClipAsset
    {
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<ColorWordBarPlayable>.Create(graph, new ColorWordBarPlayable());
        }

        public ClipCaps clipCaps => ClipCaps.None;
    }

    [MetaCodeSample("SpatialLingo")]
    public class ColorWordBarPlayable : PlayableBehaviour
    {
        private bool m_init;
        private WordBar3D m_wordBar;
        private Color m_initialFontColor;
        private Color m_targetFontColor = Color.white;
        private Color m_initialBackerColor;
        private Color m_targetBackerColor = Color.white;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (m_init)
            {
                return;
            }

            m_wordBar = (WordBar3D)info.output.GetUserData();
            m_initialFontColor = m_wordBar.TextNode.TMP.color;
            m_initialBackerColor = m_wordBar.TextNode.BackerColor;
            m_init = true;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var lerp = playable.GetTime() / playable.GetDuration();
            m_wordBar.TextNode.TMP.color = Color.Lerp(m_initialFontColor, m_targetFontColor, (float)lerp);
            var lerpedBarColor = Color.Lerp(m_initialBackerColor, m_targetBackerColor, (float)lerp);
            m_wordBar.TextNode.UpdateBlockColor(lerpedBarColor);
        }

        public override void OnGraphStart(Playable playable)
        {
            if (m_wordBar == null)
            {
                return;
            }

            m_wordBar.TextNode.TMP.color = m_initialFontColor;
            m_wordBar.TextNode.UpdateBlockColor(m_initialBackerColor);
            m_init = false;
        }
    }

    [MetaCodeSample("SpatialLingo")]
    [TrackClipType(typeof(ColorWordBarClip))]
    [TrackBindingType(typeof(WordBar3D))]
    public class ColorWordBar : TrackAsset { }
}