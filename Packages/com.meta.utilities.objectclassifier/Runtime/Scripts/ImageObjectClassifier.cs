// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.InferenceEngine;
using UnityEngine;

namespace Meta.Utilities.ObjectClassifier
{
    /// <summary>
    /// This class handles processing of an object recognizer and processing the results into 2D rectangles 
    /// </summary>
    public class ImageObjectClassifier : MonoBehaviour
    {
        /// <summary>
        /// Represents options that can be used internally on a classification level basis
        /// </summary>
        [BurstCompile]
        public struct ClassificationOption
        {
            public readonly string Classification;
            public readonly string DisplayName;
            public readonly bool Ignore;

            public ClassificationOption(string classification, string displayName = null, bool ignore = false)
            {
                Classification = classification;
                DisplayName = displayName;
                Ignore = ignore;
            }
        }

        /// <summary>
        /// Represents a 2D rectangle produced from a recognized object in an image
        /// </summary>
        [BurstCompile]
        public struct ClassifiedImageObject
        {
            public float CenterX { get; }
            public float CenterY { get; }
            public float Width { get; }
            public float Height { get; }
            public int ClassIndex { get; }
            public string ClassName { get; }

            public ClassifiedImageObject(float centerX, float centerY, float width, float height, int classIndex, string className)
            {
                CenterX = centerX;
                CenterY = centerY;
                Width = width;
                Height = height;
                ClassIndex = classIndex;
                ClassName = className;
            }

            /// <summary>
            /// This class is an aggregation of objects found during a single object recognition pass in an image
            /// </summary>
            [BurstCompile]
            public struct ClassifiedImageResult
            {
                public Texture2D Source;
                public ClassifiedImageObject[] ClassifiedObjects { get; }

                public ClassifiedImageResult(Texture2D image, ClassifiedImageObject[] objects)
                {
                    Source = image;
                    ClassifiedObjects = objects;
                }
            }

            /// <summary>
            /// Check collision between 2 separate objects
            /// </summary>
            /// <param name="objectA">Object A</param>
            /// <param name="objectB">Object B</param>
            /// <returns>Whether the objects are considered colliding</returns>
            public static bool Collides(ClassifiedImageObject objectA, ClassifiedImageObject objectB)
            {
                // 50% of rectangle hypotenuse

                var sizeMarginRatio = 0.5f;
                if (objectA.ClassName != objectB.ClassName)
                {
                    return false;
                }
                // if A's center is too close to B's center
                var centerA = new Vector2(objectA.CenterX, objectA.CenterY);
                var centerB = new Vector2(objectB.CenterX, objectB.CenterY);
                var hypA = Mathf.Sqrt(objectA.Width * objectA.Width + objectA.Height * objectB.Height);
                var hypB = Mathf.Sqrt(objectB.Width * objectB.Width + objectB.Height * objectB.Height);
                var centerDistance = (centerA - centerB).magnitude;
                var marginAllowedA = hypA * sizeMarginRatio;
                var marginAllowedB = hypB * sizeMarginRatio;
                if (centerDistance < marginAllowedA || centerDistance < marginAllowedB)
                {
                    return true;
                }
                return false;
            }

            public static ClassifiedImageObject Merge(ClassifiedImageObject objectA, ClassifiedImageObject objectB)
            {
                return MergeAverage(objectA, objectB);
            }

            /// <summary>
            /// Create a Merged object using average 
            /// </summary>
            /// <param name="objectA">Object A</param>
            /// <param name="objectB">Object B</param>
            /// <returns>A Merged object reprensenting the average of Objects A and B</returns>
            private static ClassifiedImageObject MergeAverage(ClassifiedImageObject objectA, ClassifiedImageObject objectB)
            {
                if (objectA.ClassName != objectB.ClassName)
                {
                    Debug.LogWarning($"{objectA.ClassName} and {objectB.ClassName} do not match.");
                }
                var centerX = (objectA.CenterX + objectB.CenterX) * 0.5f;
                var centerY = (objectA.CenterY + objectB.CenterY) * 0.5f;
                var width = (objectA.Width + objectB.Width) * 0.5f;
                var height = (objectA.Height + objectB.Height) * 0.5f;
                var merged = new ClassifiedImageObject(centerX, centerY, width, height, objectA.ClassIndex, objectA.ClassName);
                return merged;
            }
        }

        /// <summary>
        /// Asynchronous complete event when (any) all 2D rectangle objects are available. 
        /// </summary>
        public delegate void ImageProcessedCompleteEvent(ClassifiedImageObject.ClassifiedImageResult result);
        public event ImageProcessedCompleteEvent ImageProcessedComplete;

        // Static model input dimensions:
        private const int YOLO_MODEL_IMAGE_WIDTH = 640;
        private const int YOLO_MODEL_IMAGE_HEIGHT = 640;
        private ModelAsset m_model;
        private Worker m_worker;
        private string[] m_labels;
        private Model m_sentisModel;

        private const BackendType BACKEND = BackendType.GPUCompute;
        // private const BackendType backend = BackendType.CPU;
        // async variables
        private int m_layersPerFrame = 1; // Default to lowest option
        private IEnumerator m_workerFrameSchedule = null;
        private bool m_hasEnumerator = false;
        private int m_layersSoFar = 0;
        private bool m_waitingForOutput0 = false;
        private Tensor<float> m_outputPull0 = null;
        private Tensor<float> m_outputResult0 = null;
        private bool m_waitingForOutput1 = false;
        private Tensor<int> m_outputPull1 = null;
        private Tensor<int> m_outputResult1 = null;
        private bool m_waitingReturnCall = false;
        private DateTime m_startRequest;
        // Passed image for processing, cached to return in result response
        private Texture2D m_sourceImageToProcess;
        private Dictionary<string, ClassificationOption> m_optionsDictionary = new();

        public void Initialize(ModelAsset model, string[] labels, ClassificationOption[] options = null)
        {
            m_model = model;
            m_labels = labels;
            m_sentisModel = ModelLoader.Load(model);
            m_worker = new Worker(m_sentisModel, BACKEND);
            if (options != null)
            {
                foreach (var option in options)
                {
                    var key = option.Classification;
                    m_optionsDictionary[key] = option;
                }
            }
        }

        // cache/buffer for reusing tensor
        private Tensor<float> m_inputTensor = new(new TensorShape(1, 3, YOLO_MODEL_IMAGE_WIDTH, YOLO_MODEL_IMAGE_HEIGHT));
        public void ProcessImageForClassification(Texture2D imageSource)
        {
            if (m_sourceImageToProcess != null)
            {
                // Already in process
                return;
            }
            m_sourceImageToProcess = imageSource;

            TextureConverter.ToTensor(imageSource, m_inputTensor, default);
            m_startRequest = DateTime.Now;
            m_worker.SetInput(0, m_inputTensor);
            m_workerFrameSchedule = m_worker.ScheduleIterable();
            m_hasEnumerator = true;
        }

        public void SetLayersPerFrame(int layersPerFrame)
        {
            m_layersPerFrame = layersPerFrame;
        }

        private void Update()
        {
            CheckProcessWorkerLayer();
        }

        private void CheckProcessWorkerLayer()
        {
            if (m_waitingForOutput0)
            {
                if (m_outputPull0 != null)
                {
                    var isReadDone = m_outputPull0.IsReadbackRequestDone();
                    if (isReadDone)
                    {
                        var result = m_outputPull0.ReadbackAndClone();
                        if (result.shape[0] > 0)
                        {
                            m_outputResult0 = result;
                        }
                        else
                        {
                            m_waitingForOutput1 = false; // no rects means no labels
                            m_waitingReturnCall = true;
                        }
                        // got data or no data
                        m_waitingForOutput0 = false;
                    }
                }
                else
                {
                    // Make request, or if no data: no longer waiting
                    m_outputPull0 = m_worker.PeekOutput(0) as Tensor<float>;
                    var backendData = m_outputPull0.dataOnBackend;
                    if (backendData != null)
                    {
                        m_outputPull0.ReadbackRequest();
                    }
                    else
                    {
                        m_waitingForOutput0 = false;
                        m_waitingForOutput1 = false; // no rects means no labels
                        m_waitingReturnCall = true;
                    }
                }
                return;
            }

            if (m_waitingForOutput1)
            {
                if (m_outputPull1 != null)
                {
                    var isReadDone = m_outputPull1.IsReadbackRequestDone();
                    if (isReadDone)
                    {
                        var result = m_outputPull1.ReadbackAndClone();
                        if (result.shape[0] > 0)
                        {
                            m_outputResult1 = result;
                        }
                        else
                        {
                            m_waitingReturnCall = true;
                        }
                        m_waitingForOutput1 = false;
                        m_waitingReturnCall = true;
                    }
                }
                else
                {
                    // make request, or if no data: no longer waiting
                    m_outputPull1 = m_worker.PeekOutput(1) as Tensor<int>;
                    var backendData = m_outputPull1.dataOnBackend;
                    if (backendData != null)
                    {
                        m_outputPull0.ReadbackRequest();
                    }
                    else
                    {
                        m_waitingForOutput1 = false;
                        m_waitingReturnCall = true;
                    }
                }
                return;
            }

            if (m_waitingReturnCall)
            {
                m_waitingReturnCall = false;
                m_outputResult0 ??= new Tensor<float>(new TensorShape(0), false);
                m_outputResult1 ??= new Tensor<int>(new TensorShape(0), false);
                var now = DateTime.Now;
                var requestTotalTime = now - m_startRequest;
                var warningLevelTimeSpan = TimeSpan.FromSeconds(10.0f);
                if (requestTotalTime > warningLevelTimeSpan)
                {
                    Debug.LogWarning($"ImageObjectClassifier - request round-trip: {requestTotalTime} s");
                }

                ClassificationWithTensors(m_outputResult0, m_outputResult1);

                // reset for next request
                if (m_outputPull0 != null)
                {
                    m_outputPull0.Dispose();
                    m_outputPull0 = null;
                }
                if (m_outputResult0 != null)
                {
                    m_outputResult0.Dispose();
                    m_outputResult0 = null;
                }
                if (m_outputPull1 != null)
                {
                    m_outputPull1.Dispose();
                    m_outputPull1 = null;
                }
                if (m_outputResult1 != null)
                {
                    m_outputResult1.Dispose();
                    m_outputResult1 = null;
                }

                return;
            }

            if (m_hasEnumerator && m_workerFrameSchedule != null)
            {
                var layerProcessedCount = 0;
                while (m_workerFrameSchedule.MoveNext())
                {
                    ++m_layersSoFar;
                    if (layerProcessedCount == m_layersPerFrame)
                    {
                        return;
                    }
                    ++layerProcessedCount;
                }
                m_layersSoFar = 0;
                m_hasEnumerator = false;
                m_workerFrameSchedule = null;
                m_waitingForOutput0 = true;
                m_waitingForOutput1 = true;
            }
        }

        private void ClassificationWithTensors(Tensor<float> output, Tensor<int> labelIDs)
        {
            var boxesFound = output.shape[0];
            var labelsFound = labelIDs.shape[0];
            if (labelsFound != boxesFound)
            {
                Debug.LogWarning($"MISMATCH OUTPUT COUNTS: {boxesFound} & {labelsFound}");
            }

            if (output.shape.length != labelIDs.shape.length * 4)
            {
                Debug.LogWarning($"MISMATCH LENGTHS: {output.shape.length} & {labelIDs.shape.length}");
            }

            boxesFound = Math.Min(boxesFound, labelsFound);

            // Display plane is expected to be 1 unit length
            var displayWidth = 1.0f;
            var displayHeight = 1.0f;

            // Scale the square to a box of expected size
            var scaleX = displayWidth / YOLO_MODEL_IMAGE_WIDTH;
            var scaleY = displayHeight / YOLO_MODEL_IMAGE_HEIGHT;

            var classifiedObjects = new List<ClassifiedImageObject>();
            // Turn the bounding boxes into return objects

            var maxN = Mathf.Min(boxesFound, 200); // 200 is an arbitrary limit
            for (var n = 0; n < maxN; n++)
            {
                var centerX = output[n, 0] * scaleX - displayWidth / 2;
                var centerY = output[n, 1] * scaleY - displayHeight / 2;
                var width = output[n, 2] * scaleX;
                var height = output[n, 3] * scaleY;
                var labelIndex = labelIDs[n];
                var label = m_labels[labelIndex];

                var classified = new ClassifiedImageObject(centerX, centerY, width, height, labelIndex, label);
                classifiedObjects.Add(classified);
            }

            List<ClassifiedImageObject> clonedObjects = new(classifiedObjects); // Clone list to prevent mutation
            ProcessClassificationTensors(new List<ClassifiedImageObject>(clonedObjects));
        }

        private async void ProcessClassificationTensors(List<ClassifiedImageObject> classifiedObjects)
        {
            await Awaitable.MainThreadAsync();

            var result = await ProcessClassificationTensors(classifiedObjects, m_optionsDictionary, m_sourceImageToProcess);

            // take a break before calling results 
            await Awaitable.NextFrameAsync();

            m_sourceImageToProcess = null; // Mark as done
            ImageProcessedComplete?.Invoke(result);
        }

        public static async Awaitable<ClassifiedImageObject.ClassifiedImageResult> ProcessClassificationTensors(List<ClassifiedImageObject> classifiedObjects, Dictionary<string, ClassificationOption> optionsDictionary, Texture2D sourceImage)
        {
            var filteredObjects = new List<ClassifiedImageObject>();
            var processedMergesPerFrame = 10;

            // Ignore: drop list
            for (var i = 0; i < classifiedObjects.Count; ++i)
            {
                var obj = classifiedObjects[i];
                var key = obj.ClassName;
                var shouldAddObject = true;
                if (optionsDictionary.TryGetValue(key, out var options))
                {
                    if (options.Ignore)
                    {
                        shouldAddObject = false;
                    }
                }
                if (shouldAddObject)
                {
                    filteredObjects.Add(obj);
                }
            }

            // Merge duplicate / overlapping rectangles
            var processed = new List<ClassifiedImageObject>();
            var iterations = 0;
            while (filteredObjects.Count > 0)
            {
                var putative = filteredObjects[0];
                filteredObjects.RemoveAt(0);
                var didCollide = false;
                for (var i = 0; i < processed.Count; ++i)
                {
                    var existing = processed[i];
                    // order of merges matters too
                    var collides = ClassifiedImageObject.Collides(existing, putative);
                    if (collides)
                    {
                        var merged = ClassifiedImageObject.Merge(existing, putative);
                        processed.RemoveAt(i); // merged object needs to be re-evaluated
                        filteredObjects.Add(merged); // place merged object into putative list
                        didCollide = true;
                        break; // quit inner loop
                    }
                }

                if (!didCollide)
                {
                    // no collision = ok to add
                    processed.Add(putative);
                }
                ++iterations;
                if (iterations % processedMergesPerFrame == 0)
                {
                    await Awaitable.NextFrameAsync();
                }
            }

            // return aggregated result
            return new ClassifiedImageObject.ClassifiedImageResult(sourceImage, processed.ToArray());
        }

        private void OnDestroy()
        {
            m_worker?.Dispose();
        }
    }
}
