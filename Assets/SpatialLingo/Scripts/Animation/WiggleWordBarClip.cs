// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SpatialLingo.Animation
{
    [MetaCodeSample("SpatialLingo")]
    public class WiggleWordBarClip : PlayableAsset, ITimelineClipAsset
    {
        public float WiggleMaxDeg = 5f;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<WiggleWordBarPlayable>.Create(graph, new WiggleWordBarPlayable { WiggleMaxDeg = WiggleMaxDeg, Transform = owner.transform });
        }

        public ClipCaps clipCaps => ClipCaps.None;
    }

    [MetaCodeSample("SpatialLingo")]
    public class WiggleWordBarPlayable : PlayableBehaviour
    {
        public float WiggleMaxDeg = 5f;
        public Transform Transform;
        private Vector3 m_initialRot;
        private bool m_init;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            m_init = false;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!m_init)
            {
                m_initialRot = Transform.localEulerAngles;
                m_init = true;
            }

            Transform.localEulerAngles = m_initialRot + new Vector3(
                Random.Range(-WiggleMaxDeg, WiggleMaxDeg),
                Random.Range(-WiggleMaxDeg, WiggleMaxDeg),
                Random.Range(-WiggleMaxDeg, WiggleMaxDeg));
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            Transform.localEulerAngles = m_initialRot;
            m_init = false;
        }
    }

    [MetaCodeSample("SpatialLingo")]
    [TrackClipType(typeof(WiggleWordBarClip))]
    public class WiggleWordBar : TrackAsset { }
}