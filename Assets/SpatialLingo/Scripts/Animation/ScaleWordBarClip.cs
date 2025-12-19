// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SpatialLingo.Animation
{
    [MetaCodeSample("SpatialLingo")]
    public class ScaleWordBarClip : PlayableAsset, ITimelineClipAsset
    {
        public AnimationCurve ScaleCurve;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<ScaleWordBarPlayable>.Create(graph, new ScaleWordBarPlayable { ScaleCurve = ScaleCurve, Transform = owner.transform });
        }

        public ClipCaps clipCaps => ClipCaps.None;
    }

    [MetaCodeSample("SpatialLingo")]
    public class ScaleWordBarPlayable : PlayableBehaviour
    {
        public AnimationCurve ScaleCurve;
        public Transform Transform;

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            Transform.localScale = Vector3.one;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var lerp = playable.GetTime() / playable.GetDuration();
            Transform.localScale = Vector3.one * ScaleCurve.Evaluate((float)lerp);
        }
    }

    [MetaCodeSample("SpatialLingo")]
    [TrackClipType(typeof(ScaleWordBarClip))]
    public class ScaleWordBar : TrackAsset { }
}