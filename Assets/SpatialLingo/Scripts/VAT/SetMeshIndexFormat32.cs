// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.VAT
{
    [MetaCodeSample("SpatialLingo")]
    public class SetMeshIndexFormat32 : MonoBehaviour
    {
        private void Start()
        {
            // Use a copy of the mesh to specify the format type
            var mesh = GetComponent<MeshFilter>().mesh;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
    }
}