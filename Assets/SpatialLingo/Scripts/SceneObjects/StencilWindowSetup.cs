// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpatialLingo.SceneObjects
{
    /// <summary>
    /// Sets up a stencil buffer window effect:
    /// A plane writes to the stencil buffer, and target objects read from it to only render within the window area.
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class StencilWindowSetup : MonoBehaviour
    {

        [Header("References")]
        public Renderer PlaneRenderer;          // A plane pointing to the template for writing
        public Renderer[] TargetRenderers;      // Points to objects that need to be visible only when the window is visible

        [Header("Materials")]
        public Material StencilWritePlaneMat;   // Shader: Stencil/URP_WritePlane
        public Material StencilReadObjMat;      // Shader: Stencil/URP_ShowWhenEqual_Unlit

        [Header("Stencil")]
        [Range(0, 255)] public int StencilRef = 3;
        public bool PlaneZWriteOff = true;      // It's recommended to only write templates and not delve into in-depth analysis

        private void Reset()
        {
            // The system will attempt to search for it automatically within the project;
            // if it can't find it, the field will be left blank for you to drag and drop manually.
            StencilWritePlaneMat = FindByName<Material>("MAT_StencilPlane");
            StencilReadObjMat = FindByName<Material>("MAT_ShowOnlyInWindow");
        }

        private void Start()
        {
            if (PlaneRenderer && StencilWritePlaneMat)
            {
                var mat = new Material(StencilWritePlaneMat);
                mat.SetInt("_StencilRef", StencilRef);

                // Dynamically switch ZWrite (if you modify the shader,
                // using keywords/multiple SubShaders is a more elegant approach).
                if (PlaneZWriteOff)
                {
                    // Simple approach: Clone a variant with ZWrite disabled
                    // (it's more reliable to provide a switch for this in your shader)
                    // This demonstrates directly modifying the render queue and its settings:
                    // In reality, ZWrite cannot be directly modified via material keywords.
                    // It's recommended to create a ZWrite [Toggle] in your Shader and control it using
                    // #pragma multi_compile For simplicity in this demonstration, only the render queue is being set.
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry - 1;
                }

                PlaneRenderer.sharedMaterial = mat;
                // Standard recommendation: The plane does not cast or receive shadows.
                PlaneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                PlaneRenderer.receiveShadows = false;
            }

            if (TargetRenderers != null && StencilReadObjMat)
            {
                foreach (var r in TargetRenderers)
                {
                    if (!r) continue;
                    var mat = new Material(StencilReadObjMat);
                    mat.SetInt("_StencilRef", StencilRef);
                    // Ensure that drawing is done after writing the template.
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    r.sharedMaterial = mat;
                }
            }
        }

        private T FindByName<T>(string name) where T : Object
        {
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets($"{name} t:{typeof(T).Name}");
            if (guids != null && guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }
#endif
            return null;
        }
    }
}