// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Unity.InferenceEngine;
using Unity.InferenceEngine.Layers;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using static Unity.InferenceEngine.CPUTensorData;

public static class QuestCPUBackend
{
    public static Worker CreateWorker(Model model)
    {
        if (JobsUtility.JobWorkerCount != JobsUtility.JobWorkerMaximumCount)
        {
            UnityEngine.Debug.Log($"JobWorkerCount was {JobsUtility.JobWorkerCount}, setting to {JobsUtility.JobWorkerMaximumCount}");
            JobsUtility.JobWorkerCount = JobsUtility.JobWorkerMaximumCount;
        }

        if (JobsUtility.JobWorkerCount < 3)
        {
            UnityEngine.Debug.LogWarning($"JobWorkerCount is {JobsUtility.JobWorkerCount}, but needs to be at least 3 in order to prevent frame stalling.");
        }

        return new Worker(model, new QuestCPUBackendImpl(), new ModelStorage());
    }
}

/// CPUBackend with some functionality modified to eliminate frame hitching by long Conv jobs
internal class QuestCPUBackendImpl : CPUBackend, IBackend
{
    /// max number of threads to occupy with ConvJob
    /// (set to 2 to ensure that job threads remain available for Physics)
    private const int MAX_THREADS = 2;

#pragma warning disable IDE1006, IDE0058, IDE0007
    // copy-paste of CPUBackend.Conv, but splitting into 2 batches instead of N
    void IBackend.Conv(Tensor<float> X, Tensor<float> K, Tensor<float> B, Tensor<float> O, int groups, Span<int> strides, Span<int> pads, Span<int> dilations, FusableActivation fusedActivation)
    {
        var job = new ConvJob();
        int arrayLength = job.Prepare(X.shape, K.shape, O.shape, groups, strides, pads, dilations, fusedActivation);
        if (B != null)
        {
            job.useBias = true;
            job.ScheduleXSBO(Pin(X), Pin(K), Pin(B), Pin(O), arrayLength, arrayLength / MAX_THREADS + 1);
        }
        else
        {
            job.useBias = false;
            var pinX = Pin(X);
            var pinW = Pin(K);
            var pinO = Pin(O);
            var fenceBeforeJobStart = JobHandle.CombineDependencies(pinX.fence, pinW.fence, pinO.reuse);
            unsafe
            {
                job.X = new ReadOnlyMemResource
                {
                    ptr = pinX.rawPtr
                };
                job.S = new ReadOnlyMemResource
                {
                    ptr = pinW.rawPtr
                };
                job.O = new ReadWriteMemResource { ptr = pinO.rawPtr };
            }
            var jobFence = job.Schedule(arrayLength, arrayLength / MAX_THREADS + 1, fenceBeforeJobStart);
            pinX.reuse = jobFence;
            pinW.reuse = jobFence;
            pinO.fence = jobFence;
        }
    }
#pragma warning restore IDE1006, IDE0058, IDE0007
}
