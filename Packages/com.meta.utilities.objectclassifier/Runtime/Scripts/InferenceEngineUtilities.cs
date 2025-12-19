// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Meta.Utilities.ObjectClassifier
{
    /// <summary>
    /// This class handles loading compute shaders before unity inference engine uses them
    /// Otherwise when Unity.InferenceEngine.ComputeFunctions is first referenced:
    /// the GPU is blocked for ~10s on very first load
    /// subsequent loads are much faster, things must be cached somewhere
    /// </summary>
    public class InferenceEngineUtilities
    {
        private static Dictionary<string, ComputeShader> s_computeShadersMap = new();

        private static List<(string shaderName, string shaderPath)> s_shaderResourceLoadList;
        private static List<(string functionName, string shaderName, string kernelName)> s_computeFunctionLoadList;

        /// <summary>
        /// Asynchronous event when loading sequence is complete 
        /// </summary>
        public delegate void InferenceEnginePreloadingCompleteEvent();
        public static event InferenceEnginePreloadingCompleteEvent PreloadingComplete;

        private static bool s_isLoading;
        private static bool s_isLoaded;
        private static DateTime s_startLoadTime;
        private static DateTime s_endLoadTime;

        private static string s_playerPrefsKeyHasBeenLoaded = "IEUtilities.hasBeenLoaded";
        private static bool s_hasBeenLoaded;
        public static bool IsLoaded => s_isLoaded;

        public static bool LoadFastest = true;

        // this only needs to complete the very first run of the app, 
        public static bool LoadAll()
        {
            if (s_isLoading || s_isLoaded)
            {
                return false;
            }

            s_isLoading = true;
            s_startLoadTime = DateTime.Now;
            LoadComputeShaderResources();
            return true;
        }

        private static async void LoadComputeShaderResources()
        {
            await Awaitable.MainThreadAsync();
            await LoadComputeShaderResourcesInternal();
        }

        private static async Awaitable LoadComputeShaderResourcesInternal()
        {
            var read = PlayerPrefs.GetInt(s_playerPrefsKeyHasBeenLoaded);
            s_hasBeenLoaded = read > 0;
            s_shaderResourceLoadList = new List<(string, string)>
            {
                ("k_TextureToTensor", "InferenceEngine/TextureConversion/TextureToTensor"),
                ("k_TensorToTexture", "InferenceEngine/TextureConversion/TensorToTexture"),
                ("k_AxisActivations", "InferenceEngine/ComputeShaders/AxisActivations"),
                ("k_CumSum", "InferenceEngine/ComputeShaders/CumSum"),
                ("k_ReferenceImpl", "InferenceEngine/ComputeShaders/ReferenceImpl"),
                ("k_RNN", "InferenceEngine/ComputeShaders/RNN"),
                ("k_LogicalOps", "InferenceEngine/ComputeShaders/LogicalOps"),
                ("k_CompareOps", "InferenceEngine/ComputeShaders/CompareOps"),
                ("k_GroupConv", "InferenceEngine/ComputeShaders/GroupConv"),
                ("k_ConvGeneric", "InferenceEngine/ComputeShaders/ConvGeneric"),
                ("k_DepthwiseConv", "InferenceEngine/ComputeShaders/DepthwiseConv"),
                ("k_Dense", "InferenceEngine/ComputeShaders/Dense"),
                ("k_GemmT", "InferenceEngine/ComputeShaders/GemmT"),
                ("k_Pool", "InferenceEngine/ComputeShaders/Pool"),
                ("k_Normalization", "InferenceEngine/ComputeShaders/Normalization"),
                ("k_NMS", "InferenceEngine/ComputeShaders/NMS"),
                ("k_ReduceIndices", "InferenceEngine/ComputeShaders/ReduceIndices"),
                ("k_CopyOps", "InferenceEngine/ComputeShaders/CopyOps"),
                ("k_Random", "InferenceEngine/ComputeShaders/Random"),
                ("k_IndexingOps", "InferenceEngine/ComputeShaders/IndexingOps"),
                ("k_SortingOps", "InferenceEngine/ComputeShaders/SortingOps"),
                ("k_ScatterOps", "InferenceEngine/ComputeShaders/ScatterOps"),
                ("k_GridSample", "InferenceEngine/ComputeShaders/GridSample"),
                ("k_Resize", "InferenceEngine/ComputeShaders/Resize"),
                ("k_ImageBased", "InferenceEngine/ComputeShaders/ImageBased"),
                ("k_BroadcastGen", "InferenceEngine/ComputeShaders/Compute.Shaders.Broadcast.gen"),
                ("k_ConvGen", "InferenceEngine/ComputeShaders/Compute.Shaders.Conv.gen"),
                ("k_ConvTransposeGen", "InferenceEngine/ComputeShaders/Compute.Shaders.ConvTranspose.gen"),
                ("k_ReductionGen", "InferenceEngine/ComputeShaders/Compute.Shaders.Reduction.gen"),
                ("k_ReductionUnrolledGen", "InferenceEngine/ComputeShaders/Compute.Shaders.ReductionUnrolled.gen"),
                ("k_PointwiseUnaryGen", "InferenceEngine/ComputeShaders/Compute.Shaders.PointwiseUnary.gen"),
                ("k_GenericA", "InferenceEngine/ComputeShaders/ReferenceImpl.GenericA"),
                ("k_PadA", "InferenceEngine/ComputeShaders/ReferenceImpl.PadA"),
                ("k_PoolA", "InferenceEngine/ComputeShaders/ReferenceImpl.PoolA"),
                ("k_Einsum", "InferenceEngine/ComputeShaders/ReferenceImpl.Einsum"),
                ("k_IndexingOpsA", "InferenceEngine/ComputeShaders/ReferenceImpl.IndexingOpsA"),
                ("k_Logical", "InferenceEngine/ComputeShaders/ReferenceImpl.Logical"),
                ("k_BitonicSort", "InferenceEngine/ComputeShaders/BitonicSort"),
                ("k_RoiAlignShader", "InferenceEngine/ComputeShaders/RoiAlign")
            };

            for (var i = 0; i < s_shaderResourceLoadList.Count; i++)
            {
                var (shaderName, shaderPath) = s_shaderResourceLoadList[i];
                var index = shaderName;
                var path = shaderPath;
                var shader = Resources.LoadAsync<ComputeShader>(path);
                while (!shader.isDone)
                {
                    await Awaitable.NextFrameAsync(); // Wait for the next frame
                }
                s_computeShadersMap[index] = shader.asset as ComputeShader;
                await Awaitable.NextFrameAsync();
            }
            LoadComputeShaderKernels();
        }

        public static async void LoadComputeShaderKernels()
        {
            await Awaitable.MainThreadAsync();
            await LoadComputeShaderKernelsInternal();
        }

        private static async Awaitable LoadComputeShaderKernelsInternal()
        {
            s_computeFunctionLoadList = new List<(string, string, string)>
            {
                // format: computeFunctionLoadList.Add(("A","B","C"));
                ("k_TextureToTensorExact", "k_TextureToTensor", "TextureToTensorExact"),
                ("k_TextureToTensorLinear", "k_TextureToTensor", "TextureToTensorLinear"),
                ("k_TensorToTextureExact", "k_TensorToTexture", "TensorToTextureExact"),
                ("k_TensorToTextureLinear", "k_TensorToTexture", "TensorToTextureLinear"),
                ("k_LogSoftmaxEnd", "k_AxisActivations", "LogSoftmaxEnd"),
                ("k_SoftmaxEnd", "k_AxisActivations", "SoftmaxEnd"),
                ("k_HardmaxEnd", "k_AxisActivations", "HardmaxEnd"),
                ("k_CumSumFloatForwardInclusive", "k_CumSum", "CumSumFloatForwardInclusive"),
                ("k_CumSumFloatForwardExclusive", "k_CumSum", "CumSumFloatForwardExclusive"),
                ("k_CumSumFloatReverseInclusive", "k_CumSum", "CumSumFloatReverseInclusive"),
                ("k_CumSumFloatReverseExclusive", "k_CumSum", "CumSumFloatReverseExclusive"),
                ("k_CumSumIntForwardInclusive", "k_CumSum", "CumSumIntForwardInclusive"),
                ("k_CumSumIntForwardExclusive", "k_CumSum", "CumSumIntForwardExclusive"),
                ("k_CumSumIntReverseInclusive", "k_CumSum", "CumSumIntReverseInclusive"),
                ("k_CumSumIntReverseExclusive", "k_CumSum", "CumSumIntReverseExclusive"),
                ("k_MatMul", "k_ReferenceImpl", "MatMul"),
                ("k_LSTMEnd", "k_RNN", "LSTMEnd"),
                ("k_OrInt", "k_LogicalOps", "OrInt"),
                ("k_AndInt", "k_LogicalOps", "AndInt"),
                ("k_XorInt", "k_LogicalOps", "XorInt"),
                ("k_IsInf", "k_LogicalOps", "IsInf"),
                ("k_GreaterFloat", "k_CompareOps", "GreaterFloat"),
                ("k_GreaterInt", "k_CompareOps", "GreaterInt"),
                ("k_GreaterOrEqualFloat", "k_CompareOps", "GreaterOrEqualFloat"),
                ("k_GreaterOrEqualInt", "k_CompareOps", "GreaterOrEqualInt"),
                ("k_LessFloat", "k_CompareOps", "LessFloat"),
                ("k_LessInt", "k_CompareOps", "LessInt"),
                ("k_LessOrEqualFloat", "k_CompareOps", "LessOrEqualFloat"),
                ("k_LessOrEqualInt", "k_CompareOps", "LessOrEqualInt"),
                ("k_EqualFloat", "k_CompareOps", "EqualFloat"),
                ("k_EqualInt", "k_CompareOps", "EqualInt"),
                ("k_GroupedConv3D", "k_GroupConv", "GroupedConv3D"),
                ("k_GroupedConv2D", "k_GroupConv", "GroupedConv2D"),
                ("k_GroupedConv1D", "k_GroupConv", "GroupedConv1D"),
                ("k_GroupedConv3D_AlignsTo64", "k_GroupConv", "GroupedConv3D_AlignsTo64"),
                ("k_GroupedConv2D_AlignsTo64", "k_GroupConv", "GroupedConv2D_AlignsTo64"),
                ("k_GroupedConv1D_AlignsTo64", "k_GroupConv", "GroupedConv1D_AlignsTo64"),
                ("k_Conv3D_Generic", "k_ConvGeneric", "Conv3D_Generic"),
                ("k_Conv2D_Generic", "k_ConvGeneric", "Conv2D_Generic"),
                ("k_Conv1D_Generic", "k_ConvGeneric", "Conv1D_Generic"),
                ("k_Conv3D_1x1_Generic", "k_ConvGeneric", "Conv3D_1x1_Generic"),
                ("k_Conv2D_1x1_Generic", "k_ConvGeneric", "Conv2D_1x1_Generic"),
                ("k_Conv1D_1x1_Generic", "k_ConvGeneric", "Conv1D_1x1_Generic"),
                ("k_ConvTranspose3D_Generic", "k_ConvGeneric", "ConvTranspose3D_Generic"),
                ("k_ConvTranspose2D_Generic", "k_ConvGeneric", "ConvTranspose2D_Generic"),
                ("k_ConvTranspose1D_Generic", "k_ConvGeneric", "ConvTranspose1D_Generic"),
                ("k_DepthwiseConv2DDirect", "k_DepthwiseConv", "DepthwiseConv2DDirect"),
                ("k_DepthwiseConv2DWinograd", "k_DepthwiseConv", "DepthwiseConv2DWinograd"),
                ("k_KernelWinoExpand", "k_DepthwiseConv", "KernelWinoExpand"),
                ("k_Dense_T8x8_R4x4", "k_Dense", "Dense_T8x8_R4x4"),
                ("k_DenseBatched_T8x8_R4x4", "k_Dense", "DenseBatched_T8x8_R4x4"),
                ("k_Gemm_T8x8_R4x4", "k_Dense", "Gemm_T8x8_R4x4"),
                ("k_GemmBatched_T8x8_R4x4", "k_Dense", "GemmBatched_T8x8_R4x4"),
                ("k_Dense_T16x16_R4x4", "k_Dense", "Dense_T16x16_R4x4"),
                ("k_DenseBatched_T16x16_R4x4", "k_Dense", "DenseBatched_T16x16_R4x4"),
                ("k_Gemm_T16x16_R4x4", "k_Dense", "Gemm_T16x16_R4x4"),
                ("k_GemmBatched_T16x16_R4x4", "k_Dense", "GemmBatched_T16x16_R4x4"),
                ("k_Dense_V_L1Cached64", "k_Dense", "Dense_V_L1Cached64"),
                ("k_DenseBatched_V_L1Cached64", "k_Dense", "DenseBatched_V_L1Cached64"),
                ("k_Gemm_V_L1Cached64", "k_Dense", "Gemm_V_L1Cached64"),
                ("k_GemmBatched_V_L1Cached64", "k_Dense", "GemmBatched_V_L1Cached64"),
                ("k_GemmT_XT_T8x8_R4x4", "k_GemmT", "GemmT_XT_T8x8_R4x4"),
                ("k_GemmT_WT_T8x8_R4x4", "k_GemmT", "GemmT_WT_T8x8_R4x4"),
                ("k_GemmT_XT_WT_T8x8_R4x4", "k_GemmT", "GemmT_XT_WT_T8x8_R4x4"),
                ("k_AveragePoolReduce", "k_Pool", "AveragePoolReduce"),
                ("k_MaxPoolReduce", "k_Pool", "MaxPoolReduce"),
                ("k_GlobalAveragePool", "k_Pool", "GlobalAveragePool"),
                ("k_GlobalMaxPool", "k_Pool", "GlobalMaxPool"),
                ("k_AverageVariancePoolReduce", "k_Pool", "AverageVariancePoolReduce"),
                ("k_GlobalAverageVariancePool", "k_Pool", "GlobalAverageVariancePool"),
                ("k_ArgMaxReduce", "k_Pool", "ArgMaxReduce"),
                ("k_GlobalArgMaxReduce", "k_Pool", "GlobalArgMaxReduce"),
                ("k_LayerNormalizationTail", "k_Normalization", "LayerNormalizationTail"),
                ("k_RMSNormalizationTail", "k_Normalization", "RMSNormalizationTail"),
                ("k_BatchNormalization", "k_Normalization", "BatchNormalization"),
                ("k_ScaleBias", "k_Normalization", "ScaleBias"),
                ("k_NMSBitmaskCorners", "k_NMS", "NMSBitmaskCorners"),
                ("k_NMSBitmaskCenter", "k_NMS", "NMSBitmaskCenter"),
                ("k_NMSSelect", "k_NMS", "NMSSelect"),
                ("k_NMSCompact", "k_NMS", "NMSCompact"),
                ("k_ArgMaxFloatFirst", "k_ReduceIndices", "ArgMaxFloatFirst"),
                ("k_ArgMinFloatFirst", "k_ReduceIndices", "ArgMinFloatFirst"),
                ("k_ArgMaxFloatLast", "k_ReduceIndices", "ArgMaxFloatLast"),
                ("k_ArgMinFloatLast", "k_ReduceIndices", "ArgMinFloatLast"),
                ("k_ArgMaxIntFirst", "k_ReduceIndices", "ArgMaxIntFirst"),
                ("k_ArgMinIntFirst", "k_ReduceIndices", "ArgMinIntFirst"),
                ("k_ArgMaxIntLast", "k_ReduceIndices", "ArgMaxIntLast"),
                ("k_ArgMinIntLast", "k_ReduceIndices", "ArgMinIntLast"),
                ("k_MemCopy", "k_CopyOps", "MemCopy"),
                ("k_MemCopyStride", "k_CopyOps", "MemCopyStride"),
                ("k_MemSet", "k_CopyOps", "MemSet"),
                ("k_Split", "k_CopyOps", "Split"),
                ("k_Tril", "k_CopyOps", "Tril"),
                ("k_Triu", "k_CopyOps", "Triu"),
                ("k_CastHalfToFloat", "k_CopyOps", "CastHalfToFloat"),
                ("k_DequantizeUint8", "k_CopyOps", "DequantizeUint8"),
                ("k_Transpose2D", "k_CopyOps", "Transpose2D"),
                ("k_RandomUniform", "k_Random", "RandomUniform"),
                ("k_RandomNormal", "k_Random", "RandomNormal"),
                ("k_BernoulliFloat", "k_Random", "BernoulliFloat"),
                ("k_BernoulliInt", "k_Random", "BernoulliInt"),
                ("k_TopP", "k_Random", "TopP"),
                ("k_OneHot", "k_IndexingOps", "OneHot"),
                ("k_GatherND", "k_IndexingOps", "GatherND"),
                ("k_SliceSet", "k_IndexingOps", "SliceSet"),
                ("k_TopK", "k_SortingOps", "TopK"),
                ("k_ScatterND", "k_ScatterOps", "ScatterND"),
                ("k_GridSample2D", "k_GridSample", "GridSample2D"),
                ("k_GridSample3D", "k_GridSample", "GridSample3D"),
                ("k_Upsample1D_Nearest_Floor", "k_Resize", "Upsample1D_Nearest_Floor"),
                ("k_Upsample1D_Nearest_Ceil", "k_Resize", "Upsample1D_Nearest_Ceil"),
                ("k_Upsample1D_Linear_None", "k_Resize", "Upsample1D_Linear_None"),
                ("k_Upsample2D_Nearest_Floor", "k_Resize", "Upsample2D_Nearest_Floor"),
                ("k_Upsample2D_Nearest_Ceil", "k_Resize", "Upsample2D_Nearest_Ceil"),
                ("k_Upsample2D_Linear_None", "k_Resize", "Upsample2D_Linear_None"),
                ("k_Upsample3D_Nearest_Floor", "k_Resize", "Upsample3D_Nearest_Floor"),
                ("k_Upsample3D_Nearest_Ceil", "k_Resize", "Upsample3D_Nearest_Ceil"),
                ("k_Upsample3D_Linear_None", "k_Resize", "Upsample3D_Linear_None"),
                ("k_Resize1D_Nearest_Floor", "k_Resize", "Resize1D_Nearest_Floor"),
                ("k_Resize1D_Nearest_Ceil", "k_Resize", "Resize1D_Nearest_Ceil"),
                ("k_Resize1D_Linear_None", "k_Resize", "Resize1D_Linear_None"),
                ("k_DepthToSpaceDepthColumnRow", "k_ImageBased", "DepthToSpaceDepthColumnRow"),
                ("k_DepthToSpaceColumnRowDepth", "k_ImageBased", "DepthToSpaceColumnRowDepth"),
                ("k_SpaceToDepth", "k_ImageBased", "SpaceToDepth"),
                ("k_ScalarBroadcastPRelu", "k_BroadcastGen", "ScalarBroadcastPRelu"),
                ("k_BroadcastPRelu", "k_BroadcastGen", "BroadcastPRelu"),
                ("k_ElementwisePRelu", "k_BroadcastGen", "ElementwisePRelu"),
                ("k_ScalarBroadcastPowFloat", "k_BroadcastGen", "ScalarBroadcastPowFloat"),
                ("k_BroadcastPowFloat", "k_BroadcastGen", "BroadcastPowFloat"),
                ("k_ElementwisePowFloat", "k_BroadcastGen", "ElementwisePowFloat"),
                ("k_ScalarBroadcastPowInt", "k_BroadcastGen", "ScalarBroadcastPowInt"),
                ("k_BroadcastPowInt", "k_BroadcastGen", "BroadcastPowInt"),
                ("k_ElementwisePowInt", "k_BroadcastGen", "ElementwisePowInt"),
                ("k_ScalarBroadcastAddFloat", "k_BroadcastGen", "ScalarBroadcastAddFloat"),
                ("k_BroadcastAddFloat", "k_BroadcastGen", "BroadcastAddFloat"),
                ("k_ElementwiseAddFloat", "k_BroadcastGen", "ElementwiseAddFloat"),
                ("k_ScalarBroadcastSubFloat", "k_BroadcastGen", "ScalarBroadcastSubFloat"),
                ("k_BroadcastSubFloat", "k_BroadcastGen", "BroadcastSubFloat"),
                ("k_ElementwiseSubFloat", "k_BroadcastGen", "ElementwiseSubFloat"),
                ("k_ScalarBroadcastMulFloat", "k_BroadcastGen", "ScalarBroadcastMulFloat"),
                ("k_BroadcastMulFloat", "k_BroadcastGen", "BroadcastMulFloat"),
                ("k_ElementwiseMulFloat", "k_BroadcastGen", "ElementwiseMulFloat"),
                ("k_ScalarBroadcastDivFloat", "k_BroadcastGen", "ScalarBroadcastDivFloat"),
                ("k_BroadcastDivFloat", "k_BroadcastGen", "BroadcastDivFloat"),
                ("k_ElementwiseDivFloat", "k_BroadcastGen", "ElementwiseDivFloat"),
                ("k_ScalarBroadcastMinFloat", "k_BroadcastGen", "ScalarBroadcastMinFloat"),
                ("k_BroadcastMinFloat", "k_BroadcastGen", "BroadcastMinFloat"),
                ("k_ElementwiseMinFloat", "k_BroadcastGen", "ElementwiseMinFloat"),
                ("k_ScalarBroadcastMaxFloat", "k_BroadcastGen", "ScalarBroadcastMaxFloat"),
                ("k_BroadcastMaxFloat", "k_BroadcastGen", "BroadcastMaxFloat"),
                ("k_ElementwiseMaxFloat", "k_BroadcastGen", "ElementwiseMaxFloat"),
                ("k_ScalarBroadcastMeanFloat", "k_BroadcastGen", "ScalarBroadcastMeanFloat"),
                ("k_BroadcastMeanFloat", "k_BroadcastGen", "BroadcastMeanFloat"),
                ("k_ElementwiseMeanFloat", "k_BroadcastGen", "ElementwiseMeanFloat"),
                ("k_ScalarBroadcastModFloat", "k_BroadcastGen", "ScalarBroadcastModFloat"),
                ("k_BroadcastModFloat", "k_BroadcastGen", "BroadcastModFloat"),
                ("k_ElementwiseModFloat", "k_BroadcastGen", "ElementwiseModFloat"),
                ("k_ScalarBroadcastFModFloat", "k_BroadcastGen", "ScalarBroadcastFModFloat"),
                ("k_BroadcastFModFloat", "k_BroadcastGen", "BroadcastFModFloat"),
                ("k_ElementwiseFModFloat", "k_BroadcastGen", "ElementwiseFModFloat"),
                ("k_ScalarBroadcastAddInt", "k_BroadcastGen", "ScalarBroadcastAddInt"),
                ("k_BroadcastAddInt", "k_BroadcastGen", "BroadcastAddInt"),
                ("k_ElementwiseAddInt", "k_BroadcastGen", "ElementwiseAddInt"),
                ("k_ScalarBroadcastSubInt", "k_BroadcastGen", "ScalarBroadcastSubInt"),
                ("k_BroadcastSubInt", "k_BroadcastGen", "BroadcastSubInt"),
                ("k_ElementwiseSubInt", "k_BroadcastGen", "ElementwiseSubInt"),
                ("k_ScalarBroadcastMulInt", "k_BroadcastGen", "ScalarBroadcastMulInt"),
                ("k_BroadcastMulInt", "k_BroadcastGen", "BroadcastMulInt"),
                ("k_ElementwiseMulInt", "k_BroadcastGen", "ElementwiseMulInt"),
                ("k_ScalarBroadcastDivInt", "k_BroadcastGen", "ScalarBroadcastDivInt"),
                ("k_BroadcastDivInt", "k_BroadcastGen", "BroadcastDivInt"),
                ("k_ElementwiseDivInt", "k_BroadcastGen", "ElementwiseDivInt"),
                ("k_ScalarBroadcastMinInt", "k_BroadcastGen", "ScalarBroadcastMinInt"),
                ("k_BroadcastMinInt", "k_BroadcastGen", "BroadcastMinInt"),
                ("k_ElementwiseMinInt", "k_BroadcastGen", "ElementwiseMinInt"),
                ("k_ScalarBroadcastMaxInt", "k_BroadcastGen", "ScalarBroadcastMaxInt"),
                ("k_BroadcastMaxInt", "k_BroadcastGen", "BroadcastMaxInt"),
                ("k_ElementwiseMaxInt", "k_BroadcastGen", "ElementwiseMaxInt"),
                ("k_ScalarBroadcastModInt", "k_BroadcastGen", "ScalarBroadcastModInt"),
                ("k_BroadcastModInt", "k_BroadcastGen", "BroadcastModInt"),
                ("k_ElementwiseModInt", "k_BroadcastGen", "ElementwiseModInt"),
                ("k_ScalarBroadcastFModInt", "k_BroadcastGen", "ScalarBroadcastFModInt"),
                ("k_BroadcastFModInt", "k_BroadcastGen", "BroadcastFModInt"),
                ("k_ElementwiseFModInt", "k_BroadcastGen", "ElementwiseFModInt"),
                ("k_Conv2D_KxK", "k_ConvGen", "Conv2D_KxK"),
                ("k_Conv2D_1x1", "k_ConvGen", "Conv2D_1x1"),
                ("k_Conv1D_KxK", "k_ConvGen", "Conv1D_KxK"),
                ("k_Conv1D_1x1", "k_ConvGen", "Conv1D_1x1"),
                ("k_ConvTranspose2D_KxK", "k_ConvTransposeGen", "ConvTranspose2D_KxK"),
                ("k_ConvTranspose1D_KxK", "k_ConvTransposeGen", "ConvTranspose1D_KxK"),
                ("k_ReduceMaxFloat", "k_ReductionGen", "ReduceMaxFloat"),
                ("k_GlobalReduceMaxFloat", "k_ReductionGen", "GlobalReduceMaxFloat"),
                ("k_ReduceMinFloat", "k_ReductionGen", "ReduceMinFloat"),
                ("k_GlobalReduceMinFloat", "k_ReductionGen", "GlobalReduceMinFloat"),
                ("k_ReduceSumFloat", "k_ReductionGen", "ReduceSumFloat"),
                ("k_GlobalReduceSumFloat", "k_ReductionGen", "GlobalReduceSumFloat"),
                ("k_ReduceSumSquareFloat", "k_ReductionGen", "ReduceSumSquareFloat"),
                ("k_GlobalReduceSumSquareFloat", "k_ReductionGen", "GlobalReduceSumSquareFloat"),
                ("k_ReduceMeanFloat", "k_ReductionGen", "ReduceMeanFloat"),
                ("k_ReduceMeanSquareFloat", "k_ReductionGen", "ReduceMeanSquareFloat"),
                ("k_GlobalReduceMeanFloat", "k_ReductionGen", "GlobalReduceMeanFloat"),
                ("k_GlobalReduceMeanSquareFloat", "k_ReductionGen", "GlobalReduceMeanSquareFloat"),
                ("k_ReduceProdFloat", "k_ReductionGen", "ReduceProdFloat"),
                ("k_GlobalReduceProdFloat", "k_ReductionGen", "GlobalReduceProdFloat"),
                ("k_ReduceL1Float", "k_ReductionGen", "ReduceL1Float"),
                ("k_GlobalReduceL1Float", "k_ReductionGen", "GlobalReduceL1Float"),
                ("k_ReduceL2Float", "k_ReductionGen", "ReduceL2Float"),
                ("k_GlobalReduceL2Float", "k_ReductionGen", "GlobalReduceL2Float"),
                ("k_ReduceSqrtFloat", "k_ReductionGen", "ReduceSqrtFloat"),
                ("k_GlobalReduceSqrtFloat", "k_ReductionGen", "GlobalReduceSqrtFloat"),
                ("k_ReduceLogSumFloat", "k_ReductionGen", "ReduceLogSumFloat"),
                ("k_GlobalReduceLogSumFloat", "k_ReductionGen", "GlobalReduceLogSumFloat"),
                ("k_ReduceLogSumExpFloat", "k_ReductionGen", "ReduceLogSumExpFloat"),
                ("k_GlobalReduceLogSumExpFloat", "k_ReductionGen", "GlobalReduceLogSumExpFloat"),
                ("k_ReduceSumExpFloat", "k_ReductionGen", "ReduceSumExpFloat"),
                ("k_GlobalReduceSumExpFloat", "k_ReductionGen", "GlobalReduceSumExpFloat"),
                ("k_ReduceMaxInt", "k_ReductionGen", "ReduceMaxInt"),
                ("k_GlobalReduceMaxInt", "k_ReductionGen", "GlobalReduceMaxInt"),
                ("k_ReduceMinInt", "k_ReductionGen", "ReduceMinInt"),
                ("k_GlobalReduceMinInt", "k_ReductionGen", "GlobalReduceMinInt"),
                ("k_ReduceSumInt", "k_ReductionGen", "ReduceSumInt"),
                ("k_GlobalReduceSumInt", "k_ReductionGen", "GlobalReduceSumInt"),
                ("k_ReduceSumSquareInt", "k_ReductionGen", "ReduceSumSquareInt"),
                ("k_GlobalReduceSumSquareInt", "k_ReductionGen", "GlobalReduceSumSquareInt"),
                ("k_ReduceProdInt", "k_ReductionGen", "ReduceProdInt"),
                ("k_GlobalReduceProdInt", "k_ReductionGen", "GlobalReduceProdInt"),
                ("k_ReduceL1Int", "k_ReductionGen", "ReduceL1Int"),
                ("k_GlobalReduceL1Int", "k_ReductionGen", "GlobalReduceL1Int"),
                ("k_UnrolledReduceMaxFloat", "k_ReductionUnrolledGen", "UnrolledReduceMaxFloat"),
                ("k_UnrolledReduceMinFloat", "k_ReductionUnrolledGen", "UnrolledReduceMinFloat"),
                ("k_UnrolledReduceSumFloat", "k_ReductionUnrolledGen", "UnrolledReduceSumFloat"),
                ("k_UnrolledReduceSumSquareFloat", "k_ReductionUnrolledGen", "UnrolledReduceSumSquareFloat"),
                ("k_UnrolledReduceMeanFloat", "k_ReductionUnrolledGen", "UnrolledReduceMeanFloat"),
                ("k_UnrolledReduceMeanSquareFloat", "k_ReductionUnrolledGen", "UnrolledReduceMeanSquareFloat"),
                ("k_UnrolledReduceProdFloat", "k_ReductionUnrolledGen", "UnrolledReduceProdFloat"),
                ("k_UnrolledReduceL1Float", "k_ReductionUnrolledGen", "UnrolledReduceL1Float"),
                ("k_UnrolledReduceL2Float", "k_ReductionUnrolledGen", "UnrolledReduceL2Float"),
                ("k_UnrolledReduceSqrtFloat", "k_ReductionUnrolledGen", "UnrolledReduceSqrtFloat"),
                ("k_UnrolledReduceLogSumFloat", "k_ReductionUnrolledGen", "UnrolledReduceLogSumFloat"),
                ("k_UnrolledReduceLogSumExpFloat", "k_ReductionUnrolledGen", "UnrolledReduceLogSumExpFloat"),
                ("k_UnrolledReduceSumExpFloat", "k_ReductionUnrolledGen", "UnrolledReduceSumExpFloat"),
                ("k_UnrolledReduceMaxInt", "k_ReductionUnrolledGen", "UnrolledReduceMaxInt"),
                ("k_UnrolledReduceMinInt", "k_ReductionUnrolledGen", "UnrolledReduceMinInt"),
                ("k_UnrolledReduceSumInt", "k_ReductionUnrolledGen", "UnrolledReduceSumInt"),
                ("k_UnrolledReduceSumSquareInt", "k_ReductionUnrolledGen", "UnrolledReduceSumSquareInt"),
                ("k_UnrolledReduceProdInt", "k_ReductionUnrolledGen", "UnrolledReduceProdInt"),
                ("k_UnrolledReduceL1Int", "k_ReductionUnrolledGen", "UnrolledReduceL1Int"),
                ("k_LeakyRelu", "k_PointwiseUnaryGen", "LeakyRelu"),
                ("k_Swish", "k_PointwiseUnaryGen", "Swish"),
                ("k_Relu", "k_PointwiseUnaryGen", "Relu"),
                ("k_Relu6", "k_PointwiseUnaryGen", "Relu6"),
                ("k_Mish", "k_PointwiseUnaryGen", "Mish"),
                ("k_Tanh", "k_PointwiseUnaryGen", "Tanh"),
                ("k_Sigmoid", "k_PointwiseUnaryGen", "Sigmoid"),
                ("k_GeluFast", "k_PointwiseUnaryGen", "GeluFast"),
                ("k_HardSigmoid", "k_PointwiseUnaryGen", "HardSigmoid"),
                ("k_Gelu", "k_PointwiseUnaryGen", "Gelu"),
                ("k_Erf", "k_PointwiseUnaryGen", "Erf"),
                ("k_Celu", "k_PointwiseUnaryGen", "Celu"),
                ("k_Shrink", "k_PointwiseUnaryGen", "Shrink"),
                ("k_ThresholdedRelu", "k_PointwiseUnaryGen", "ThresholdedRelu"),
                ("k_Elu", "k_PointwiseUnaryGen", "Elu"),
                ("k_Selu", "k_PointwiseUnaryGen", "Selu"),
                ("k_Softplus", "k_PointwiseUnaryGen", "Softplus"),
                ("k_Ceil", "k_PointwiseUnaryGen", "Ceil"),
                ("k_Floor", "k_PointwiseUnaryGen", "Floor"),
                ("k_Round", "k_PointwiseUnaryGen", "Round"),
                ("k_Reciprocal", "k_PointwiseUnaryGen", "Reciprocal"),
                ("k_Exp", "k_PointwiseUnaryGen", "Exp"),
                ("k_Log", "k_PointwiseUnaryGen", "Log"),
                ("k_Sqrt", "k_PointwiseUnaryGen", "Sqrt"),
                ("k_Acos", "k_PointwiseUnaryGen", "Acos"),
                ("k_Acosh", "k_PointwiseUnaryGen", "Acosh"),
                ("k_Asin", "k_PointwiseUnaryGen", "Asin"),
                ("k_Asinh", "k_PointwiseUnaryGen", "Asinh"),
                ("k_Atan", "k_PointwiseUnaryGen", "Atan"),
                ("k_Atanh", "k_PointwiseUnaryGen", "Atanh"),
                ("k_Cos", "k_PointwiseUnaryGen", "Cos"),
                ("k_Cosh", "k_PointwiseUnaryGen", "Cosh"),
                ("k_Sin", "k_PointwiseUnaryGen", "Sin"),
                ("k_Sinh", "k_PointwiseUnaryGen", "Sinh"),
                ("k_Tan", "k_PointwiseUnaryGen", "Tan"),
                ("k_Softsign", "k_PointwiseUnaryGen", "Softsign"),
                ("k_HardSwish", "k_PointwiseUnaryGen", "HardSwish"),
                ("k_AbsInt", "k_PointwiseUnaryGen", "AbsInt"),
                ("k_AbsFloat", "k_PointwiseUnaryGen", "AbsFloat"),
                ("k_NegInt", "k_PointwiseUnaryGen", "NegInt"),
                ("k_NegFloat", "k_PointwiseUnaryGen", "NegFloat"),
                ("k_SquareInt", "k_PointwiseUnaryGen", "SquareInt"),
                ("k_SquareFloat", "k_PointwiseUnaryGen", "SquareFloat"),
                ("k_IsNaN", "k_PointwiseUnaryGen", "IsNaN"),
                ("k_CastIntToFloat", "k_PointwiseUnaryGen", "CastIntToFloat"),
                ("k_CastFloatToInt", "k_PointwiseUnaryGen", "CastFloatToInt"),
                ("k_SignFloat", "k_PointwiseUnaryGen", "SignFloat"),
                ("k_SignInt", "k_PointwiseUnaryGen", "SignInt"),
                ("k_Not", "k_PointwiseUnaryGen", "Not"),
                ("k_ClipFloat", "k_PointwiseUnaryGen", "ClipFloat"),
                ("k_ClipInt", "k_PointwiseUnaryGen", "ClipInt"),
                ("k_ScalarMadFloat", "k_PointwiseUnaryGen", "ScalarMadFloat"),
                ("k_ScalarMadInt", "k_PointwiseUnaryGen", "ScalarMadInt"),
                ("k_RangeFloat", "k_PointwiseUnaryGen", "RangeFloat"),
                ("k_RangeInt", "k_PointwiseUnaryGen", "RangeInt"),
                ("k_Transpose", "k_GenericA", "Transpose"),
                ("k_InstanceNormalizationTail", "k_GenericA", "InstanceNormalizationTail"),
                ("k_PadBorderND", "k_PadA", "PadBorderND"),
                ("k_PadReflectND", "k_PadA", "PadReflectND"),
                ("k_PadSymmetricND", "k_PadA", "PadSymmetricND"),
                ("k_PadEdgeND", "k_PadA", "PadEdgeND"),
                ("k_PadWrapND", "k_PadA", "PadWrapND"),
                ("k_MaxPool2D", "k_PoolA", "MaxPool2D"),
                ("k_AveragePool2D", "k_PoolA", "AveragePool2D"),
                ("k_MaxPool1D", "k_PoolA", "MaxPool1D"),
                ("k_AveragePool1D", "k_PoolA", "AveragePool1D"),
                ("k_EinsumOne", "k_Einsum", "EinsumOne"),
                ("k_EinsumTwo", "k_Einsum", "EinsumTwo"),
                ("k_Tile", "k_IndexingOpsA", "Tile"),
                ("k_Gather", "k_IndexingOpsA", "Gather"),
                ("k_GatherElementsFast", "k_IndexingOpsA", "GatherElementsFast"),
                ("k_GatherElements", "k_IndexingOpsA", "GatherElements"),
                ("k_ScatterElementsFast", "k_IndexingOpsA", "ScatterElementsFast"),
                ("k_ScatterElements", "k_IndexingOpsA", "ScatterElements"),
                ("k_Expand", "k_IndexingOpsA", "Expand"),
                ("k_Slice", "k_IndexingOpsA", "Slice"),
                ("k_Where", "k_Logical", "Where"),
                ("k_BitonicSortStep", "k_BitonicSort", "BitonicSortStep"),
                ("k_BitonicSortKeyStep", "k_BitonicSort", "BitonicSortKeyStep"),
                ("k_RoiAlign", "k_RoiAlignShader", "RoiAlign")
            };

            var waitTimeFindKernel = s_hasBeenLoaded ? 1E-9f : 0.001f; // 0.001 - 0.01 | 0.01 slow in debugger
            var waitTimeGroupSizes = s_hasBeenLoaded ? 1E-9f : 0.05f; // 0.1f is still low for some of the sequentially large ones

            var count = s_computeFunctionLoadList.Count;
            for (var i = 0; i < count; i++)
            {
                var (function, shaderIndex, kernel) = s_computeFunctionLoadList[i];
                var shader = s_computeShadersMap[shaderIndex];
                if (shader == null)
                {
                    Debug.LogError($"Compute shader {function} {shaderIndex} {kernel} could not be found.");
                    continue;
                }

                // minimal lag
                if (!shader.HasKernel(kernel))
                {
                    Debug.LogError($"Compute shader kernel {function} {shaderIndex} {kernel} could not be found.");
                    continue;
                }
                var kernelIndex = shader.FindKernel(kernel);
                // if loaded before, this additional wait is not useful
                if (!s_hasBeenLoaded && !LoadFastest)
                {
                    await Awaitable.WaitForSecondsAsync(waitTimeFindKernel);
                }
                // lag 
                shader.GetKernelThreadGroupSizes(kernelIndex, out _, out _, out _);
                // if loaded before, waiting a single frame is enough:
                if (!s_hasBeenLoaded && !LoadFastest)
                {
                    await Awaitable.WaitForSecondsAsync(waitTimeGroupSizes);
                }
                else if (!LoadFastest)
                {
                    await Awaitable.NextFrameAsync();
                }

#pragma warning disable IDE0059 // Unnecessary assignment of a value
                // No lag, this is unused here, but ProfilerMarker is used inside IE
                var profilerMarker = new ProfilerMarker(kernel);
#pragma warning restore IDE0059 // Unnecessary assignment of a value

            }
            PlayerPrefs.SetInt(s_playerPrefsKeyHasBeenLoaded, 1);
            s_hasBeenLoaded = true;
            s_isLoaded = true;
            s_endLoadTime = DateTime.Now;
            var diff = s_endLoadTime - s_startLoadTime;
            PreloadingComplete?.Invoke();
        }
    }
}