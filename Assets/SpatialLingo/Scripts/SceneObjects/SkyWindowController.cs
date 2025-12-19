// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections;
using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.SceneObjects
{
    [MetaCodeSample("SpatialLingo")]
    [ExecuteAlways]
    public class SkyWindowController : MonoBehaviour
    {
        // Sky window (thin cylinder). Scale X/Z only to open/close.
        [Header("Sky Window (thin cylinder)")]
        public Transform SkyWindow;
        public float ClosedRadius = 0.05f;  // local X/Z when closed
        public float OpenRadius = 2.50f;  // local X/Z when open

        // God-ray particle systems: emission rate only.
        [Header("God Rays (emission only)")]
        public ParticleSystem[] GodRays;
        public float MinEmissionRate = 0f;
        public float MaxEmissionRate = 60f;

        // Edge particles: emission driven by 'open'; transform scale = ABSOLUTE 0..0.3 by 'open'
        [Header("Portal Edge Particles (absolute scale by 'open')")]
        public ParticleSystem[] EdgeParticles;
        public float MinEdgeEmissionRate = 0f;
        public float MaxEdgeEmissionRate = 250f;

        [Tooltip("Absolute local scale at open=0 and open=1 (uniform).")]
        public float EdgeLocalScaleMin = 0f;    // open = 0  -> scale = 0
        public float EdgeLocalScaleMax = 0.3f;  // open = 1  -> scale = 0.3

        [Tooltip("If true, only scale X/Y and keep Z as-is.")]
        public bool EdgeScaleXYOnly = false;

        // Open/Close animation

        [Header("Open/Close Animation")]
        [Range(0, 1)] public float Open = 0f;  // 0=closed, 1=open
        public float OpenDuration = 1.2f;
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine m_anim;

        private void OnEnable() { Apply(Open); }
        private void OnValidate()
        {
            EdgeLocalScaleMin = Mathf.Max(0f, EdgeLocalScaleMin);
            EdgeLocalScaleMax = Mathf.Max(EdgeLocalScaleMin, EdgeLocalScaleMax);
            Apply(Open);
        }

        // Public controls
        public void OpenPortal() => StartAnim(1f);
        public void ClosePortal() => StartAnim(0f);
        public void TogglePortal() => StartAnim(Open > 0.5f ? 0f : 1f);
        public void SetOpenImmediate(float t = 1f) { Open = Mathf.Clamp01(t); Apply(Open); }
        public void SetCloseImmediate() { Open = 0; Apply(Open); }

        private void StartAnim(float target)
        {
            if (!Application.isPlaying) { SetOpenImmediate(target); return; }
            if (m_anim != null) StopCoroutine(m_anim);
            m_anim = StartCoroutine(AnimTo(target));
        }

        private IEnumerator AnimTo(float target)
        {
            float start = Open, t = 0f, dur = Mathf.Max(0.01f, OpenDuration);
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                Open = Mathf.Lerp(start, target, Curve.Evaluate(t));
                Apply(Open);
                yield return null;
            }
            Open = target; Apply(Open); m_anim = null;
        }

        private void Apply(float k)
        {
            k = Mathf.Clamp01(k);

            // 1) Scale sky window (X/Z only)
            if (SkyWindow != null)
            {
                var rScale = Mathf.Lerp(ClosedRadius, OpenRadius, k);
                var ls = SkyWindow.localScale;
                SkyWindow.localScale = new Vector3(rScale, ls.y, rScale);
            }

            // 2) God Rays: emission rate
            if (GodRays != null)
            {
                var rateValue = Mathf.Lerp(MinEmissionRate, MaxEmissionRate, k);
                foreach (var ps in GodRays)
                {
                    if (!ps) continue;
                    var em = ps.emission; em.enabled = true;
                    var rate = em.rateOverTime; rate.mode = ParticleSystemCurveMode.Constant; rate.constant = rateValue;
                    em.rateOverTime = rate;
                }
            }

            // 3) Edge particles: emission + ABSOLUTE transform scale by 'open' (0..0.3)
            if (EdgeParticles != null)
            {
                var edgeRate = Mathf.Lerp(MinEdgeEmissionRate, MaxEdgeEmissionRate, k);
                var s = Mathf.Lerp(EdgeLocalScaleMin, EdgeLocalScaleMax, k); // absolute uniform scale

                foreach (var ps in EdgeParticles)
                {
                    if (!ps) continue;

                    // Emission
                    var em = ps.emission; em.enabled = true;
                    var rate = em.rateOverTime; rate.mode = ParticleSystemCurveMode.Constant; rate.constant = edgeRate;
                    em.rateOverTime = rate;

                    // Absolute scale
                    var t = ps.transform;
                    if (EdgeScaleXYOnly)
                        t.localScale = new Vector3(s, s, t.localScale.z); // keep Z
                    else
                        t.localScale = new Vector3(s, s, s);               // uniform XYZ
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!SkyWindow) return;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            var rWorld = 0.5f * Mathf.Abs(SkyWindow.lossyScale.x);
            Gizmos.DrawWireSphere(SkyWindow.position, rWorld);
        }
    }
}