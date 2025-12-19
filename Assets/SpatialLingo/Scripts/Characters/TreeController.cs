// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using SpatialLingo.Audio;
using SpatialLingo.SceneObjects;
using UnityEngine;

namespace SpatialLingo.Characters
{
    [MetaCodeSample("SpatialLingo")]
    public class TreeController : MonoBehaviour
    {
        private const float TARGET_PROJECTILE_VELOCITY = 0.10f; // Meters per second - 1-2 seems like a good range
        private const float MAXIMUM_PROJECTILE_DURATION = 3.0f; // Seconds [want to limit time for very far things]
        private const float PATH_OFFSET_METERS = 1.0f; // Amount of offset to apply at center of path for curve
        private readonly Dictionary<(int, int), string> m_tierTriggers = new()
    {
        { (0, 1), "Growth Trigger 01" },
        { (1, 2), "Growth Trigger 02" },
        { (2, 3), "Growth Trigger 03" },
        { (1, 3), "Growth Full"},
        { (3, 0), "Shrink Trigger 00" },
        { (3, 1), "Shrink Trigger 01" }
    };

        private class FlyingBerry
        {
            public GameObject Berry { private set; get; }
            private Transform m_target;
            private Vector3 m_startPosition;
            private Vector3 m_controlPosition;
            private Vector3 m_endPosition;
            private Quaternion m_startRotation;
            private Quaternion m_endRotation;
            private float m_duration;
            private float m_startTime;
            private Action<GameObject> m_complete;
            private AnimationCurve m_curve;
            private bool m_useLinearPath = false;

            public FlyingBerry(GameObject item, Vector3 startPosition, Vector3 endPosition, Quaternion startRotation, Quaternion endRotation, Transform target, AnimationCurve curve, float duration, Action<GameObject> complete)
            {
                Berry = item;
                m_target = target;
                m_startPosition = startPosition;
                m_endPosition = endPosition;
                m_startRotation = startRotation;
                m_endRotation = endRotation;
                m_curve = curve;
                m_duration = duration;
                m_complete = complete;
                m_controlPosition = CharacterUtilities.ControlPositionForLinearArc(startPosition, endPosition, PATH_OFFSET_METERS);
                // Start now
                m_startTime = Time.time;
                Berry.transform.SetPositionAndRotation(startPosition, startRotation);
            }

            /// <summary>
            /// Move berry along path, return true if done, false if still in process
            /// </summary>
            /// <returns>boolean indicating if process is done or not</returns>
            public bool UpdateInterpolation()
            {
                var diff = Time.time - m_startTime;
                var ratio = diff / m_duration;
                // If target position is changing, endPosition should change:
                m_endPosition = m_target.position;
                if (ratio >= 1.0f)
                {
                    Berry.transform.rotation = m_endRotation;
                    Berry.transform.position = m_endPosition;
                    Berry.transform.parent = m_target; // add as child
                    m_complete?.Invoke(Berry);
                    return true;
                }
                var value = m_curve.Evaluate(ratio);

                Berry.transform.position = m_useLinearPath
                    ? Vector3.Lerp(m_startPosition, m_endPosition, value)
                    : CharacterUtilities.BezierQuadraticAtT(m_startPosition, m_controlPosition, m_endPosition, value);
                Berry.transform.rotation = Quaternion.Lerp(m_startRotation, m_endRotation, value);

                return false;
            }
        }

        [Header("Assets")]
        [SerializeField] private GameObject m_tree;
        [SerializeField] private Animator m_belowAnimator;
        [SerializeField] private Animator m_skeletonAnimator;
        [SerializeField] private GameObject m_ground;
        [SerializeField] private GrassRockGrowthController m_grassController;
        [SerializeField] private SkyWindowController m_skyWindowController;

        [Header("Berries")]
        [SerializeField] private Transform[] m_berryTransforms;
        [SerializeField] private AnimationCurve m_berryAnimationCurve;

        private List<(GameObject, int)> m_trackedObjectIndexList = new();
        private List<int> m_availableIndexes = new();
        private List<FlyingBerry> m_flyingBerries = new();

        private Coroutine m_animationDelayCoroutine;
        private int m_tier;

        public bool HasAvailableBerryLocations => m_availableIndexes.Count > 0;

        public delegate void BerryReachedDestinationEvent(GameObject berry);
        public event BerryReachedDestinationEvent BerryReachedDestination;

        public void OpenPortal(bool animated = false)
        {
            if (!m_skyWindowController.gameObject.activeSelf)
            {
                m_skyWindowController.gameObject.SetActive(true);
            }

            if (animated)
            {
                m_skyWindowController.OpenPortal();
            }
            else
            {
                m_skyWindowController.SetOpenImmediate();
            }

            _ = AppAudioController.Instance.PlaySingletonSound(SoundEffect.SkylightAmbience, m_skyWindowController.transform);
        }

        public void ClosePortal(bool animated = false)
        {
            if (animated)
            {
                m_skyWindowController.ClosePortal();
            }
            else
            {
                m_skyWindowController.SetCloseImmediate();
                m_skyWindowController.gameObject.SetActive(false);
            }

            AppAudioController.Instance.StopSound(SoundEffect.SkylightAmbience);
        }

        private void Awake()
        {
            _ = RemoveAllBerries();
        }

        private void Start()
        {
            m_grassController.SetGrowth(0.0f);
            m_skyWindowController.SetCloseImmediate();
        }

        private int MaxTransformCount()
        {
            return m_berryTransforms.Length;
        }

        private void PlayAnimation(int fromTier, int toTier)
        {
            if (!m_tierTriggers.TryGetValue((fromTier, toTier), out var trigger))
            {
                return;
            }
            StopAnimationDelayCoroutine();
            m_animationDelayCoroutine = StartCoroutine(PlayAnimationDelayed(fromTier, toTier, trigger));
        }

        private IEnumerator PlayAnimationDelayed(int fromTier, int toTier, string trigger)
        {
            yield return new WaitForEndOfFrame();
            m_belowAnimator.SetTrigger(trigger);
            m_skeletonAnimator.SetTrigger(trigger);
            var tierPct = toTier / 3f;
            m_grassController.AnimateTo(tierPct);
            if (fromTier == 1 && toTier == 3) // Special case for full tree growth
            {
                m_grassController.SetGrowth(0.0f);
            }

            m_animationDelayCoroutine = null;
        }

        private void StopAnimationDelayCoroutine()
        {
            if (m_animationDelayCoroutine != null)
            {
                StopCoroutine(m_animationDelayCoroutine);
                m_animationDelayCoroutine = null;
            }
        }

        public void SetOrientation(Vector3 position, Quaternion rotation)
        {
            gameObject.transform.SetPositionAndRotation(position, rotation);
        }

        public void SetTier(int tier)
        {
            // A better way to set the animator to the last frame is to cache a list of all states,
            // then call Animator.CrossFade() with 0 transition time
            m_belowAnimator.speed = 1E10f;
            PlayAnimation(m_tier, tier);
            m_tier = tier;
        }

        public void AnimateToTier(int tier)
        {
            m_belowAnimator.speed = 1;
            if (m_tier == 3)
            {
                // If going down from full, play restart SFX
                if (tier < 3)
                {
                    _ = StartCoroutine(PlaySoundsInterum(tier));
                }
            }
            else
            {
                _ = StartCoroutine(PlaySoundsEnding(tier));
            }
            _ = StartCoroutine(PlaySoundsGrowth(tier));
            PlayAnimation(m_tier, tier);
            m_tier = tier;
        }
        private IEnumerator PlaySoundsGrowth(int tier)
        {
            yield return new WaitForEndOfFrame();
            AppAudioController.Instance.PlaySound(SoundEffect.FoliageGrowth, m_grassController.transform.position, variation: tier - 1);
        }

        private IEnumerator PlaySoundsEnding(int tier)
        {
            yield return new WaitForEndOfFrame();
            AppAudioController.Instance.PlaySound(SoundEffect.TreeGrowing, transform.position, variation: tier - 1);
            yield return new WaitForEndOfFrame();
            AppAudioController.Instance.PlaySound(SoundEffect.TreeStingers, transform.position, variation: tier - 1);
        }

        private IEnumerator PlaySoundsInterum(int tier)
        {
            yield return new WaitForEndOfFrame();
            var restartSound = tier == 1 ? SoundEffect.TreeRestartHalf : SoundEffect.TreeRestartFull;
            AppAudioController.Instance.PlaySound(restartSound, transform.position, variation: tier - 1);
            yield return new WaitForEndOfFrame();
            AppAudioController.Instance.PlaySound(SoundEffect.TreeStingers, transform.position, variation: m_tier);
        }

        public GameObject[] RemoveAllBerries()
        {
            var removed = new List<GameObject>();
            m_flyingBerries.Clear();
            foreach (var pair in m_trackedObjectIndexList)
            {
                pair.Item1.transform.parent = null; // unparent from tree node
                removed.Add(pair.Item1);
            }
            m_trackedObjectIndexList.Clear();

            m_availableIndexes.Clear();
            // Setup again
            var maxIndex = MaxTransformCount();
            for (var i = 0; i < maxIndex; ++i)
            {
                m_availableIndexes.Add(i);
            }

            return removed.ToArray();
        }

        public GameObject[] CurrentBerries()
        {
            var berries = new List<GameObject>();
            foreach (var pair in m_trackedObjectIndexList)
            {
                berries.Add(pair.Item1);
            }
            return berries.ToArray();
        }

        public bool MoveBerryToIndex(GameObject berry, Action<GameObject> complete = null, int berryIndex = -1, float velocity = -1.0f)
        {
            if (m_tier is <= 0 or > 3)
            {
                Debug.LogWarning($"Tier is outside range: {m_tier}");
                return false;
            }
            if (berryIndex < 0)
            {
                if (m_availableIndexes.Count == 0)
                {
                    Debug.LogWarning("No more available locations");
                    return false;
                }
                var useIndex = 0; // Non-random next in line index
                berryIndex = m_availableIndexes[useIndex];
                m_availableIndexes.RemoveAt(useIndex);
            }
            foreach (var index in m_availableIndexes)
            {
                if (berryIndex == index)
                {
                    Debug.LogWarning("Index already in use");
                    return false;
                }
            }

            if (berryIndex >= MaxTransformCount())
            {
                Debug.LogWarning("Index outside range");
                return false;
            }
            var transform = m_berryTransforms[berryIndex];
            var tuple = (berry, berryIndex);
            m_trackedObjectIndexList.Add(tuple);

            MoveItemToTarget(berry, complete, transform, velocity);

            return true;
        }

        private void MoveItemToTarget(GameObject item, Action<GameObject> complete, Transform target, float velocity = -1.0f)
        {
            if (velocity <= 0.0f)
            {
                velocity = TARGET_PROJECTILE_VELOCITY;
            }
            target.GetPositionAndRotation(out var targetPosition, out var targetRotation);
            item.transform.GetPositionAndRotation(out var startingPosition, out var startingRotation);
            var distance = Vector3.Distance(startingPosition, targetPosition);
            var duration = distance / velocity;
            duration = Math.Min(duration, MAXIMUM_PROJECTILE_DURATION);

            var berry = new FlyingBerry(item, startingPosition, targetPosition, targetRotation, startingRotation, target, m_berryAnimationCurve, duration, complete);
            m_flyingBerries.Add(berry);
        }

        private void Update()
        {
            if (m_flyingBerries.Count > 0)
            {
                for (var i = 0; i < m_flyingBerries.Count; i++)
                {
                    var berry = m_flyingBerries[i];
                    if (berry.UpdateInterpolation())
                    {
                        var item = berry.Berry;
                        m_flyingBerries.RemoveAt(i);
                        --i;
                        BerryReachedDestination?.Invoke(item);
                    }
                }
            }
        }
    }
}