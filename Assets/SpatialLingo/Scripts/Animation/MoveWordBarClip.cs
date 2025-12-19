// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SpatialLingo.Animation
{
    [MetaCodeSample("SpatialLingo")]
    public class MoveWordBarClip : PlayableAsset, ITimelineClipAsset
    {
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<MoveWordBarPlayable>.Create(graph, new MoveWordBarPlayable { ThisTransform = owner.transform });
        }

        public ClipCaps clipCaps => ClipCaps.None;
    }

    [MetaCodeSample("SpatialLingo")]
    public class MoveWordBarPlayable : PlayableBehaviour
    {
        public Transform ThisTransform;
        private bool m_init;
        private Vector3 m_initialPosition;
        private Vector3 m_targetPosition;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (m_init)
            {
                return;
            }

            var targetTransform = (Transform)info.output.GetUserData();
            m_initialPosition = ThisTransform.position;
            m_targetPosition = targetTransform.position;
            m_init = true;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var lerp = playable.GetTime() / playable.GetDuration();
            ThisTransform.position = Vector3.Lerp(m_initialPosition, m_targetPosition, (float)lerp);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            ThisTransform.position = m_initialPosition;
            m_init = false;
        }
    }

    [MetaCodeSample("SpatialLingo")]
    [TrackClipType(typeof(MoveWordBarClip))]
    [TrackBindingType(typeof(Transform))]
    public class MoveWordBar : TrackAsset { }
}