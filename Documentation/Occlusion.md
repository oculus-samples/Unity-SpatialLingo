# Occlusion

## Introduction

Meta's [occlusion package](https://developers.meta.com/horizon/documentation/unity/unity-customize-passthrough-passthrough-occlusions/) allows developers to blend AR content with the environment around them. Depth information can be passed to materials and shaders to ensure necessary objects are occluded by the environment, while allowing other effects to render over all else to grab the user's attention.

## Use in Spatial Lingo

For this example, we'll look at the material used for the grass at the base of the tree.
![GrassOcclusion.png](Images/Occlusion/GrassOcclusion.png)

The material for the grass is [M_TreeGrassGroundPatch.mat](../Assets/SpatialLingo/Props/LanguageTree/Mat/M_TreeGrassGroundPatch.mat), with
[SG_GrassAlphaCut_Mask.shadergraph](../Assets/SpatialLingo/Shaders/SG_GrassAlphaCut_Mask.shadergraph) being the shader. `EnvironmentOcclusion.cginc` provides the depth information, which is wrapped for Shadergraph use in [CalculateEnvironmentDepthOcclusion.hlsl](../Assets/SpatialLingo/Shaders/MetaOcclusion/CalculateEnvironmentDepthOcclusion.hlsl), shown below.

```cs
void CalculateEnvironmentDepthOcclusion_float(float3 posWorld, float environmentDepthBias, out float occlusionValue)
{
#ifndef SHADERGRAPH_PREVIEW
 occlusionValue = META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS(posWorld, environmentDepthBias);
#else
 occlusionValue = 1.0;
#endif
}
```

Within the shadergraph for SG_GrassAlphaCut_Mask, we call into `CalculateEnvironmentDepthOcclusion()` using the world position and provided environment depth, which returns 1.0 for objects completely unoccluded, to 0.0 for total occlusion. This value is multiplied with the outgoing alpha calculated from the rest of the shader to ensure the fragment appears consistent with its occlusion status.
![GrassAlphaCut.png](Images/Occlusion/GrassAlphaCut.png)

## Additional References

- https://developers.meta.com/horizon/documentation/unity/unity-depthapi-occlusions
- https://developers.meta.com/horizon/documentation/unity/unity-depthapi-occlusions-get-started
- https://developers.meta.com/horizon/documentation/unity/unity-depthapi-occlusions-advanced-usage
