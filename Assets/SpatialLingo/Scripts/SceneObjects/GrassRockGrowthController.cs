// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;

/// <summary>
/// Deterministic growth for floor/grass/rocks via per-renderer seeds.
// - Supports baked seeds, seed-salt tuning, one-click freeze (freezeNow).
// - Works consistently across Editor/Build.
/// </summary>
[MetaCodeSample("SpatialLingo")]
[ExecuteAlways]
[AddComponentMenu("Environment/Grass & Rocks Growth Controller (Unified)")]
public class GrassRockGrowthController : MonoBehaviour
{
    // ---------- Targets ----------
    [Header("Targets (Renderers must expose a float property named _Growth)")]
    public List<Renderer> FloorRenderers = new();  // curve remap only
    public List<Renderer> GrassRenderers = new();  // curve remap + stagger
    public List<Renderer> RockRenderers = new();  // stagger only

    // ---------- Master Growth ----------
    [Header("Master Growth")]
    [Range(0, 1)] public float Growth = 0f;
    public bool ApplyContinuouslyInEditMode = true;

    // ---------- Curves ----------
    [Header("Curves")]
    public AnimationCurve FloorGrowthCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve GrassGrowthCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // ---------- Stagger ----------
    [Header("Stagger")]
    [Tooltip("Window width (0..1) for one renderer to complete its growth.")]
    [Range(0.02f, 0.6f)] public float GrassAppearWindow = 0.20f;
    [Range(0.02f, 0.6f)] public float RockAppearWindow = 0.25f;

    [Tooltip("Tuning knobs used for global phase offset during salt-based tuning.")]
    public float SeedSaltGrass = 17.0f;
    public float SeedSaltRock = 29.0f;

    // ---------- Animate ----------
    [Header("Animate (Runtime)")]
    public float AnimateDuration = 1.5f; // linear tween

    // ---------- Options ----------
    [Header("Options")]
    [Tooltip("Recommended: use MaterialPropertyBlock to avoid touching sharedMaterials.")]
    public bool UsePropertyBlock = true;

    // ---------- Baked seeds ----------
    [MetaCodeSample("SpatialLingo")]
    [System.Serializable]
    public class SeedEntry
    {
        public Renderer Renderer;
        [Range(0f, 1f)] public float Seed01 = 0.5f; // smaller = earlier
    }

    [Header("Deterministic Seeds (Baked)")]
    [Tooltip("Renderer -> seed01 list. Bake once, then lock.")]
    public List<SeedEntry> BakedSeeds = new();

    [Tooltip("Auto add missing entries for grass/rock lists in Editor.")]
    public bool AutoBakeSeedsInEditor = true;

    // ---------- Deterministic control ----------
    [Header("Deterministic Control")]
    [Tooltip("When true: no more auto-bake and salt offset is disabled.")]
    public bool LockSeeds = false;

    [Tooltip("When true: only baked entries are used; missing ones fall back to a fixed 0.5.")]
    public bool RequireBakedSeeds = false;

    [Tooltip("Global 0..1 offset applied to seeds (repeat). Set back to 0 after tuning.")]
    [Range(0f, 1f)] public float GlobalSeedOffset = 0f;

    [Tooltip("Tick to bake current visible seeds (with salt/offset) back into bakedSeeds and lock. Auto resets to false.")]
    public bool FreezeNow = false;

    // ---------- Salt tuning ----------
    [Header("Seed Salt Tuning")]
    [Tooltip("When true and not locked: add salt phase offset even for baked seeds (for tuning).")]
    public bool UseSaltOnBaked = true;

    [Tooltip("Scale for mapping seedSalt to a 0..1 phase offset. A irrational-like value gives smooth sliding.")]
    [Range(0.0f, 2.0f)] public float SaltPhaseScale = 0.6180339887f;

    // ---------- Internals ----------
    private readonly int m_growthID = Shader.PropertyToID("_Growth");
    private MaterialPropertyBlock m_mpb;
    private readonly Dictionary<Renderer, float> m_seedCache = new();
    private readonly Dictionary<Renderer, int> m_bakedIndexCache = new();

    // ---------- Public API ----------
    public void SetGrowth(float value)
    {
        Growth = Mathf.Clamp01(value);
        ApplyGrowthToAll();
    }

    public void AnimateTo(float target)
    {
        target = Mathf.Clamp01(target);
        if (Application.isPlaying) _ = StartCoroutine(CoAnimateGrowth(target, AnimateDuration));
        else SetGrowth(target);
    }

    // ---------- Unity lifecycle ----------
    private void OnEnable()
    {
        m_mpb ??= new MaterialPropertyBlock();
        m_seedCache.Clear();
        m_bakedIndexCache.Clear();
#if UNITY_EDITOR
        if (AutoBakeSeedsInEditor && !LockSeeds) BakeSeedsForLists();
#endif
        ApplyGrowthToAll();
    }

    private void OnValidate()
    {
        m_mpb ??= new MaterialPropertyBlock();
#if UNITY_EDITOR
        if (AutoBakeSeedsInEditor && !LockSeeds) BakeSeedsForLists();
#endif
        // Always drop caches so salt/offset changes show up immediately.
        m_seedCache.Clear();
        m_bakedIndexCache.Clear();

        // Freeze by checkbox (no context menu needed).
        if (FreezeNow)
        {
            FreezeCurrent_Core();
            FreezeNow = false; // auto reset
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            return; // Freeze already reapplies
        }

        if (!ApplyContinuouslyInEditMode && !Application.isPlaying) return;
        ApplyGrowthToAll();
    }

    // ---------- Core ----------
    private void ApplyGrowthToAll()
    {
        var g = Growth;

        // Floor: curve remap
        ApplyUniformGrowth(FloorRenderers, EvaluateCurve(FloorGrowthCurve, g));
        // Grass: curve remap + stagger
        ApplyStaggeredOver01(GrassRenderers, EvaluateCurve(GrassGrowthCurve, g), GrassAppearWindow, SeedSaltGrass);
        // Rocks: stagger only
        ApplyStaggeredOver01(RockRenderers, g, RockAppearWindow, SeedSaltRock);

#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.SceneView.RepaintAll();
#endif
    }

    private void ApplyUniformGrowth(List<Renderer> list, float value)
    {
        if (list == null) return;
        foreach (var r in list)
        {
            if (!r) continue;
            SetRendererGrowth(r, value);
        }
    }

    private void ApplyStaggeredOver01(List<Renderer> list, float gGlobal, float appearWindow, float salt)
    {
        if (list == null) return;

        var w = Mathf.Clamp(appearWindow, 0.0001f, 0.999f);
        foreach (var r in list)
        {
            if (!r) continue;

            var seed = GetDeterministicSeed(r, salt); // baked -> (optional) salt -> global offset
            var start = seed * Mathf.Max(0f, 1f - w);
            var end = start + w;
            var gLocal = InverseLerpClamped(start, end, gGlobal);
            SetRendererGrowth(r, gLocal);
        }
    }

    private void SetRendererGrowth(Renderer r, float value)
    {
        if (UsePropertyBlock)
        {
            r.GetPropertyBlock(m_mpb);
            m_mpb.SetFloat(m_growthID, value);
            r.SetPropertyBlock(m_mpb);
        }
        else
        {
            var mats = r.sharedMaterials;
            for (var i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m != null && m.HasProperty(m_growthID)) m.SetFloat(m_growthID, value);
            }
        }
    }

    // ---------- Seeds ----------
    private float GetDeterministicSeed(Renderer r, float salt)
    {
        if (m_seedCache.TryGetValue(r, out var s)) return s;

        // 1) Baked first
        var idx = FindBakedIndex(r);
        if (idx >= 0 && idx < BakedSeeds.Count && BakedSeeds[idx].Renderer == r)
        {
            s = Mathf.Clamp01(BakedSeeds[idx].Seed01);
        }
        else
        {
            if (RequireBakedSeeds)
            {
                s = 0.5f; // fixed fallback in strict mode
            }
            else
            {
                s = StableSeed01_Fallback(r, salt); // legacy deterministic fallback
            }
        }

        // 2) Optional salt phase (tuning only, disabled when locked)
        if (UseSaltOnBaked && !LockSeeds)
        {
            var saltOffset01 = Mathf.Repeat(salt * SaltPhaseScale, 1f);
            s = Mathf.Repeat(s + saltOffset01, 1f);
        }

        // 3) global offset
        s = Mathf.Repeat(s + GlobalSeedOffset, 1f);

        m_seedCache[r] = s;
        return s;
    }

    private int FindBakedIndex(Renderer r)
    {
        if (r == null) return -1;
        if (m_bakedIndexCache.TryGetValue(r, out var cached)) return cached;

        for (var i = 0; i < BakedSeeds.Count; i++)
        {
            if (BakedSeeds[i].Renderer == r)
            {
                m_bakedIndexCache[r] = i;
                return i;
            }
        }
        return -1;
    }

    private static float InverseLerpClamped(float a, float b, float v)
    {
        if (b <= a + 1e-6f) return v >= b ? 1f : 0f;
        return Mathf.Clamp01((v - a) / (b - a));
    }

    // Deterministic fallback: hierarchy path + quantized world position + salt
    private static float StableSeed01_Fallback(Renderer r, float salt)
    {
        var path = GetHierarchyPath(r.transform);
        var h = DeterministicHash(path);
        var p = r.transform.position;
        unchecked
        {
            h = (h * 397) ^ Mathf.RoundToInt(p.x * 1000f);
            h = (h * 397) ^ Mathf.RoundToInt(p.y * 1000f);
            h = (h * 397) ^ Mathf.RoundToInt(p.z * 1000f);
            h = (h * 397) ^ Mathf.RoundToInt(salt * 100f);
        }
        var u = (uint)h;
        u *= 2654435761u; // Knuth
        return u / 4294967296f; // 2^32
    }

    private static int DeterministicHash(string s)
    {
        unchecked
        {
            var hash = 2166136261u; // FNV-1a
            for (var i = 0; i < s.Length; i++)
            {
                hash ^= s[i];
                hash *= 16777619u;
            }
            return (int)hash;
        }
    }

    private static string GetHierarchyPath(Transform t)
    {
        var stack = new Stack<string>();
        while (t != null) { stack.Push(t.name); t = t.parent; }
        return "/" + string.Join("/", stack.ToArray());
    }

    private static float EvaluateCurve(AnimationCurve curve, float t01)
    {
        if (curve == null || curve.length == 0) return Mathf.Clamp01(t01);
        var first = curve.keys[0];
        var last = curve.keys[curve.length - 1];
        var x = Mathf.Lerp(first.time, last.time, Mathf.Clamp01(t01));
        return Mathf.Clamp01(curve.Evaluate(x));
    }

    // ---------- Runtime animation ----------
    private System.Collections.IEnumerator CoAnimateGrowth(float target, float dur)
    {
        var start = Growth;
        var t = 0f;
        dur = Mathf.Max(0.0001f, dur);
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            SetGrowth(Mathf.Lerp(start, target, Mathf.Clamp01(t)));
            yield return null;
        }
        SetGrowth(target);
    }

#if UNITY_EDITOR
    // ---------- Editor helpers ----------
    [ContextMenu("Bake Seeds Now")]
    private void BakeSeedsForLists()
    {
        if (LockSeeds) return;
        BakeList(GrassRenderers);
        BakeList(RockRenderers);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private void BakeList(List<Renderer> list)
    {
        if (list == null) return;
        var exist = new HashSet<Renderer>();
        for (var i = 0; i < BakedSeeds.Count; i++)
        {
            var r = BakedSeeds[i].Renderer;
            if (r) _ = exist.Add(r);
        }
        foreach (var r in list)
        {
            if (!r || exist.Contains(r)) continue;
            BakedSeeds.Add(new SeedEntry { Renderer = r, Seed01 = Random.value });
            _ = exist.Add(r);
        }
    }

    [ContextMenu("Clear Seed Cache (Force Recompute)")]
    private void ClearSeedCache()
    {
        m_seedCache.Clear();
        m_bakedIndexCache.Clear();
        UnityEditor.SceneView.RepaintAll();
        ApplyGrowthToAll();
    }
#endif

    // ---------- Freeze core (no menu; driven by freezeNow) ----------
    private void FreezeCurrent_Core()
    {
        var all = new List<Renderer>();
        if (GrassRenderers != null) all.AddRange(GrassRenderers);
        if (RockRenderers != null) all.AddRange(RockRenderers);
#if UNITY_EDITOR
        if (!LockSeeds && AutoBakeSeedsInEditor) BakeSeedsForLists();
#endif
        var map = new Dictionary<Renderer, int>();
        for (var i = 0; i < BakedSeeds.Count; i++)
        {
            var rr = BakedSeeds[i].Renderer;
            if (rr && !map.ContainsKey(rr)) map.Add(rr, i);
        }
        foreach (var r in all)
        {
            if (!r) continue;
            var isGrass = GrassRenderers != null && GrassRenderers.Contains(r);
            var salt = isGrass ? SeedSaltGrass : SeedSaltRock;

            // Base seed
            float s;
            var idx = FindBakedIndex(r);
            s = idx >= 0 && idx < BakedSeeds.Count && BakedSeeds[idx].Renderer == r
                ? Mathf.Clamp01(BakedSeeds[idx].Seed01)
                : StableSeed01_Fallback(r, salt);

            // Apply current tuning (matches visible result)
            if (UseSaltOnBaked)
            {
                var saltOffset01 = Mathf.Repeat(salt * SaltPhaseScale, 1f);
                s = Mathf.Repeat(s + saltOffset01, 1f);
            }
            s = Mathf.Repeat(s + GlobalSeedOffset, 1f);

            // Write back
            if (!map.TryGetValue(r, out var bi))
                BakedSeeds.Add(new SeedEntry { Renderer = r, Seed01 = s });
            else
                BakedSeeds[bi].Seed01 = s;
        }

        // Lock and reset tuning knobs
        UseSaltOnBaked = false;
        GlobalSeedOffset = 0f;
        LockSeeds = true;

        m_seedCache.Clear();
        m_bakedIndexCache.Clear();
        ApplyGrowthToAll();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}