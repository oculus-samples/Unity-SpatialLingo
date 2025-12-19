// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace SpatialLingo.Interactions
{
    [MetaCodeSample("SpatialLingo")]
    public class OneGrabFixedTransformer : MonoBehaviour, ITransformer
    {
        // Distance past which the interaction is forced to stop
        [SerializeField] private float m_maxDistanceEndInteraction = -1f;

        // Can't find these:
        public static Pose WorldToLocalPose(Pose worldPose, Matrix4x4 worldToLocal)
        {
            return new Pose(worldToLocal.MultiplyPoint3x4(worldPose.position),
                worldToLocal.rotation * worldPose.rotation);
        }

        public static Pose AlignLocalToWorldPose(Matrix4x4 localToWorld, Pose local, Pose world)
        {
            var basePose = new Pose(localToWorld.MultiplyPoint3x4(local.position),
                localToWorld.rotation * local.rotation);
            var baseInverse = new Pose();
            PoseUtils.Inverse(basePose, ref baseInverse);
            var poseInBase = PoseUtils.Multiply(baseInverse, new Pose(localToWorld.GetPosition(), localToWorld.rotation));
            var poseInWorld = PoseUtils.Multiply(world, poseInBase);
            return poseInWorld;
        }

        private Vector3 m_initialPosition;
        private IGrabbable m_grabbable;

        public void Initialize(IGrabbable grabbable)
        {
            m_grabbable = grabbable;
            m_initialPosition = m_grabbable.Transform.localPosition;
        }

        public void UpdateTransform()
        {
            var target = m_grabbable.Transform;
            target.localPosition = m_initialPosition; // don't move

            var grabPoint = m_grabbable.GrabPoints[0].position;
            var objectPoint = target.position;
            var distance = Vector3.Distance(grabPoint, objectPoint);
            if (m_maxDistanceEndInteraction >= 0) // negative values are ignored
            {
                if (distance > m_maxDistanceEndInteraction)
                {
                    // Cancel the interaction
                    gameObject.SetActive(false);
                    gameObject.SetActive(true);
                }
            }
        }

        private void ConstrainTransform()
        {
            var target = m_grabbable.Transform;
            var constrainedPosition = target.localPosition;
            target.localPosition = constrainedPosition;
        }

        public void BeginTransform()
        {
        }

        public void EndTransform()
        {
        }
    }
}