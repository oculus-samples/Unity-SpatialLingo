// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Threading;
using Meta.Utilities.CameraTracking;
using Meta.Utilities.ImageUtilities;
using Meta.Utilities.ObjectClassifier;
using Meta.XR;
using Meta.XR.EnvironmentDepth;
using UnityEngine;

namespace Meta.Utilities.CameraTaxonTracking
{
    public class CameraTaxonTracker : IDisposable
    {
        // How near to the edge of an object rectangle to get a sample: in [0,1]
        // 1 will get the edge, 0 will get the center
        // Close to edges are less reliable than towards the center
        public const float TAXON_SAMPLE_EXTENT_FACTOR = 0.5f;
        private const float MAX_DISTANCE_DEPTH_RAYCAST = 5.0f; //5m = 16ft Results farther than this will be ignored
        private const float CROP_IMAGE_PADDING_SIDES = 0.10f; // Additional pixels added to cropped YOLO images (ratio)

        [SerializeField] public MeshRenderer DebugRenderer;
        [SerializeField] public GameObject DebugRay;

        public class TaxonUpdateResult
        {
            public CameraTrackedTaxon Taxon { get; }

            public TaxonUpdateResult(CameraTrackedTaxon taxon) => Taxon = taxon;
        }

        public delegate void TaxonUpdatedEvent(TaxonUpdateResult result);
        public event TaxonUpdatedEvent TaxonAdded;
        public event TaxonUpdatedEvent TaxonUpdated;
        public event TaxonUpdatedEvent TaxonRemoved;

        public bool IsIdle => !m_isPolling;
        public CameraTrackedTaxon[] TrackedTaxa => m_trackedTaxa.ToArray();

        private WebCamTextureManager m_cameraTextureManager;
        private EnvironmentRaycastManager m_raycastManager;

        private ImageObjectClassifier m_objectClassifier;
        private List<CameraTrackedTaxon> m_trackedTaxa = new();

        // Camera parameters recorded at time of image request, needed after processing
        private PassthroughCameraEye m_cachedCameraEye = PassthroughCameraEye.Left;
        private Pose m_cachedCameraPose;
        private Vector2Int m_cachedCameraResolution;

        private Thread m_queryWaitThread;
        private static int s_querySleepMilliseconds = 100;
        private static DateTime s_lastRequestTime;
        private bool m_isPolling;
        private DateTime m_observationCameraTimestamp;

        private Texture2D m_cameraImage;
        private Color32[] m_cameraImageBuffer;

        public CameraTaxonTracker(EnvironmentRaycastManager raycastManager, WebCamTextureManager cameraTextureManager, ImageObjectClassifier objectClassifier)
        {
            m_cameraTextureManager = cameraTextureManager;
            m_objectClassifier = objectClassifier;
            m_raycastManager = raycastManager;

            objectClassifier.ImageProcessedComplete += OnImageProcessedComplete;
        }

        public void Dispose()
        {
            if (m_objectClassifier != null)
            {
                m_objectClassifier.ImageProcessedComplete -= OnImageProcessedComplete;
                m_objectClassifier = null;
            }
        }

        public void SetLayersPerFrame(int layersPerFrame)
        {
            m_objectClassifier.SetLayersPerFrame(layersPerFrame);
        }

        public void StartPolling()
        {
            if (m_isPolling)
            {
                return;
            }

            m_isPolling = true;
            StartNewQueryThread();
        }

        public void StopPolling()
        {
            if (!m_isPolling)
            {
                return;
            }
            StopQueryThread();
        }

        private Texture2D GetCameraStillTexture2D(WebCamTexture webCamTexture)
        {
            var bufferSize = webCamTexture.width * webCamTexture.height;
            if (m_cameraImageBuffer == null || m_cameraImageBuffer.Length != bufferSize)
            {
                m_cameraImageBuffer = new Color32[bufferSize];
                m_cameraImage = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            }
            _ = webCamTexture.GetPixels32(m_cameraImageBuffer);
            m_cameraImage.SetPixels32(m_cameraImageBuffer);
            m_cameraImage.Apply();
            return m_cameraImage;
        }

        private void OnImageProcessedComplete(ImageObjectClassifier.ClassifiedImageObject.ClassifiedImageResult result)
        {
            // Perform result conversion iteratively:
            m_observationCameraTimestamp = DateTime.Now;
            ProcessImageClassifierResults(result);
        }

        private async void ProcessImageClassifierResults(ImageObjectClassifier.ClassifiedImageObject.ClassifiedImageResult result)
        {
            await Awaitable.MainThreadAsync();

            var processedResultsPerFrame = 4; // 5-9 depth api calls per item, each ~ 0.1ms
            // Create default possible new objects for tracking
            var putativeList = new List<CameraTrackedTaxon>();
            var cameraImage = result.Source;

            foreach (var classified in result.ClassifiedObjects)
            {
                var taxon = ClassifiedObjectToTaxa(classified, cameraImage);
                if (taxon != null)
                {
                    putativeList.Add(taxon);
                }
            }

            // Keep track of existing list
            var previousList = new List<CameraTrackedTaxon>();
            previousList.AddRange(m_trackedTaxa);
            var mergedList = new List<CameraTrackedTaxon>();

            // Merge any new or changed objects until set is empty
            var iterations = 0;
            while (putativeList.Count > 0)
            {
                var putative = putativeList[0];
                putativeList.RemoveAt(0);
                var didCollide = false;
                for (var i = 0; i < m_trackedTaxa.Count; ++i)
                {
                    // trackedTaxa
                    var existing = m_trackedTaxa[i];
                    var collides = CameraTrackedTaxon.Collides(existing, putative);
                    if (collides)
                    {
                        // Keep the older object around
                        var tsExisting = existing.OldestTimestamp;
                        var tsPutative = putative.OldestTimestamp;
                        var mergeInto = existing;
                        var mergeFrom = putative;
                        if (tsExisting > tsPutative)
                        {
                            mergeInto = putative;
                            mergeFrom = existing;
                        }

                        var merged = CameraTrackedTaxon.MergeInto(mergeInto, mergeFrom);
                        m_trackedTaxa.RemoveAt(i); // merged object needs to be re-evaluated
                        putativeList.Add(merged); // place merged object into putative list
                        mergedList.Add(merged);
                        didCollide = true;
                        break; // quit inner loop
                    }
                }

                if (!didCollide)
                {
                    // No collision = OK to add
                    m_trackedTaxa.Add(putative);
                }

                ++iterations;
                if (iterations % processedResultsPerFrame == 0)
                {
                    await Awaitable.NextFrameAsync();
                }
            }

            // Return lists for events
            var removedList = new List<CameraTrackedTaxon>();
            var addedList = new List<CameraTrackedTaxon>();
            var updatedList = new List<CameraTrackedTaxon>();

            // Mark any objects for removal:
            for (var i = 0; i < m_trackedTaxa.Count; ++i)
            {
                var taxaCurrent = m_trackedTaxa[i];
                taxaCurrent.NoteCameraObservation(m_observationCameraTimestamp, m_cachedCameraPose.position, m_cachedCameraPose.forward);
                if (taxaCurrent.ShouldRemove(m_observationCameraTimestamp))
                {
                    m_trackedTaxa.RemoveAt(i);
                    removedList.Add(taxaCurrent);
                    --i;
                }
            }

            // Added list = items in current list that are not in previous list
            foreach (var taxaCurrent in m_trackedTaxa)
            {
                if (!previousList.Contains(taxaCurrent))
                {
                    addedList.Add(taxaCurrent);
                }
            }
            // Updated list = items in mergedList that are still in existing list
            foreach (var taxaMerged in mergedList)
            {
                if (m_trackedTaxa.Contains(taxaMerged) && !addedList.Contains(taxaMerged))
                {
                    updatedList.Add(taxaMerged);
                }
            }
            // Removed list = items in previous list that are not in updated or added list
            foreach (var taxaPrevious in previousList)
            {
                if (!m_trackedTaxa.Contains(taxaPrevious))
                {
                    removedList.Add(taxaPrevious);
                }
            }

            // Take a break before calling results
            await Awaitable.NextFrameAsync();

            if (DebugRenderer != null)
            {
                if (addedList.Count > 0)
                {
                    var taxon = addedList[0];
                    var context = taxon.ImageContext;
                    if (context != null)
                    {
                        var texture2D = context.Image;
                        if (texture2D != null)
                        {
                            DebugRenderer.material.mainTexture = texture2D;
                            await Awaitable.NextFrameAsync();
                        }
                    }
                }
            }

            foreach (var taxon in updatedList)
            {
                var data = new TaxonUpdateResult(taxon);
                TaxonUpdated?.Invoke(data);
            }

            foreach (var taxon in addedList)
            {
                var data = new TaxonUpdateResult(taxon);
                TaxonAdded?.Invoke(data);
            }

            foreach (var taxon in removedList)
            {
                var data = new TaxonUpdateResult(taxon);
                TaxonRemoved?.Invoke(data);
            }

            // Done
            m_isPolling = false;
        }

        private Ray GetCameraRay(Vector2 normalizedImageLocation)
        {
            var cameraEye = m_cachedCameraEye;
            var cameraPose = m_cachedCameraPose;
            var cameraRes = m_cachedCameraResolution;
            return GetCameraRay(normalizedImageLocation, cameraEye, cameraPose, cameraRes);
        }

        // Normalized image location axis is in the center of image, x & y in [-0.5, 0.5]
        // Screen plane axis is in lower left, x & y in [0,wid] & [0,hei]
        // Camera resolution and image resolution are not necessarily equal
        private Ray GetCameraRay(Vector2 normalizedImageLocation, PassthroughCameraEye cameraEye, Pose cameraPose, Vector2Int cameraResolution)
        {
            var locX = Mathf.RoundToInt(cameraResolution.x * (0.5f + normalizedImageLocation.x));
            var locY = Mathf.RoundToInt(cameraResolution.y * (0.5f - normalizedImageLocation.y));

            var center = new Vector2Int(locX, locY);
            // Ray in camera space
            var ray = PassthroughCameraUtils.ScreenPointToRayInCamera(cameraEye, center);
            // Ray in world space
            ray.origin += cameraPose.position;
            ray.direction = cameraPose.rotation * ray.direction;
            return ray;
        }

        private CameraTrackedTaxon ClassifiedObjectToTaxa(ImageObjectClassifier.ClassifiedImageObject imageObject, Texture2D fullImage)
        {
            if (!EnvironmentDepthManager.IsSupported)
            {
                return null;
            }

            var cameraPose = m_cachedCameraPose;
            var extentFactor = TAXON_SAMPLE_EXTENT_FACTOR;
            var areaSmallToBigLimit = 0.12f; // Big areas get more samples image = 1x1, 1/3 to 1/4 ^ 2 ~ 0.06 to 0.10
            var edgeLimitMin = 0.01f; // Closer than 1% of image edges means only part of the object is seen in that direction
            var edgeLimitMax = 1.0f - edgeLimitMin;
            var areaRect = imageObject.Width * imageObject.Height;

            var extentWide = imageObject.Width * 0.5f;
            var extentHigh = imageObject.Height * 0.5f;
            var extentWideFactor = extentWide * extentFactor;
            var extentHighFactor = extentHigh * extentFactor;

            var leftEdge = imageObject.CenterX - extentWide < edgeLimitMin;
            var rightEdge = imageObject.CenterX + extentWide > edgeLimitMax;
            var topEdge = imageObject.CenterY - extentWide < edgeLimitMin;
            var bottomEdge = imageObject.CenterY + extentWide > edgeLimitMax;

            // Make a list of 2D points to make into rays
            var putativePoints2D = new List<Vector2>();
            var putativeEdges = new List<bool>();
            putativePoints2D.Add(new Vector2(imageObject.CenterX, imageObject.CenterY)); // center
            putativePoints2D.Add(new Vector2(imageObject.CenterX - extentWideFactor, imageObject.CenterY)); // left
            putativePoints2D.Add(new Vector2(imageObject.CenterX + extentWideFactor, imageObject.CenterY)); // right
            putativePoints2D.Add(new Vector2(imageObject.CenterX, imageObject.CenterY - extentHighFactor)); // top
            putativePoints2D.Add(new Vector2(imageObject.CenterX, imageObject.CenterY + extentHighFactor)); // bottom
            putativeEdges.AddRange(new bool[] { false, leftEdge, rightEdge, topEdge, bottomEdge });

            // Add more samples for object with larger surfaces on image
            if (areaRect > areaSmallToBigLimit)
            {
                putativePoints2D.Add(new Vector2(imageObject.CenterX - extentWideFactor, imageObject.CenterY - extentHighFactor)); // top left
                putativePoints2D.Add(new Vector2(imageObject.CenterX + extentWideFactor, imageObject.CenterY - extentHighFactor)); // top right
                putativePoints2D.Add(new Vector2(imageObject.CenterX - extentWideFactor, imageObject.CenterY + extentHighFactor)); // bottom left
                putativePoints2D.Add(new Vector2(imageObject.CenterX + extentWideFactor, imageObject.CenterY + extentHighFactor)); // bottom right
                putativeEdges.AddRange(new bool[] { leftEdge || topEdge, rightEdge || topEdge, leftEdge || bottomEdge, rightEdge || bottomEdge });
            }

            // Loop thru potential 2D points to get 3D position locations
            var samplePoints3D = new List<Vector3>();
            var sampleEdges3D = new List<bool>();

            for (var i = 0; i < putativePoints2D.Count; i++)
            {
                var point2D = putativePoints2D[i];
                var ray = GetCameraRay(point2D);
                // Depth API samples could return values not on the actual object, as the rectangle is only rough
                var didHit = m_raycastManager.Raycast(ray, out var hitInfo, MAX_DISTANCE_DEPTH_RAYCAST);
                if (didHit)
                {
                    var hitCenter = new Vector3(hitInfo.point.x, hitInfo.point.y, hitInfo.point.z);
                    samplePoints3D.Add(hitCenter);
                    sampleEdges3D.Add(putativeEdges[i]);
                }
            }

            // 2+ samples
            if (samplePoints3D.Count > 1)
            {
                // extract image:
                var width = fullImage.width;
                var height = fullImage.height;

                // Convert from normalized center coordinates to image absolute scale coordinates
                var locX = Mathf.RoundToInt(width * (0.5f + imageObject.CenterX) - imageObject.Width * width * 0.5f);
                var locY = Mathf.RoundToInt(height * (0.5f - imageObject.CenterY) - imageObject.Height * height * 0.5f);
                var wid = Mathf.RoundToInt(imageObject.Width * width);
                var hei = Mathf.RoundToInt(imageObject.Height * height);

                // Append some pixels to get a more zoomed out context
                var addPixels = (int)Math.Min(wid * CROP_IMAGE_PADDING_SIDES, hei * CROP_IMAGE_PADDING_SIDES);
                // Expand uniformly (if possible)
                locX -= addPixels;
                locY -= addPixels;
                wid += addPixels * 2;
                hei += addPixels * 2;

                var rect = new Rect(locX, locY, wid, hei);
                rect.x = Mathf.Clamp(rect.x, 0, width - 1);
                rect.y = Mathf.Clamp(rect.y, 0, height - 1);

                var cropped = ImageOperations.CropTexture(fullImage, rect);

                // Estimated surface normal is opposite of camera direction
                var center = cameraPose.position;
                var normal = cameraPose.forward;
                normal.Scale(new Vector3(-1.0f, -1.0f, -1.0f));
                var up = cameraPose.up;
                var sample = new TrackSample(normal, up, samplePoints3D.ToArray(), sampleEdges3D.ToArray(), cropped, center);
                var samples = new TrackSample[] { sample };
                // closest point on left & right of image plane should be size
                //var size = new Vector2();
                var taxon = new CameraTrackedTaxon(imageObject.ClassName, samples);

                return taxon;
            }
            return null;
        }

        private void StartNewQueryThread()
        {
            var wasThread = m_queryWaitThread;
            m_queryWaitThread = new Thread(QueryService);
            m_queryWaitThread.Start();
            if (wasThread != null)
            {
                wasThread.Abort();
            }
        }

        private void StopQueryThread()
        {
            if (m_queryWaitThread != null)
            {
                m_queryWaitThread.Abort();
                m_queryWaitThread = null;
            }
        }

        private void QueryService()
        {
            // Wait some minimum amount of time:
            Thread.Sleep(100);

            var now = DateTime.Now;
            var diff = now - s_lastRequestTime;
            s_lastRequestTime = DateTime.Now;
            var sleepTime = (int)Math.Max(0.0, s_querySleepMilliseconds - diff.TotalMilliseconds);
            if (sleepTime > 0)
            {
                Thread.Sleep(sleepTime);
            }
            // Switch to main thread:
            CheckEnumerator();
        }

        private async void CheckEnumerator()
        {
            await Awaitable.MainThreadAsync();

            // Save position of camera while wait for process to complete:
            if (m_cachedCameraResolution.x == 0)
            {
                m_cachedCameraResolution = PassthroughCameraUtils.GetCameraIntrinsics(m_cachedCameraEye).Resolution;
            }

            var cachedCameraEye = PassthroughCameraEye.Left;
            var cachedCameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(cachedCameraEye);
            m_cachedCameraPose.position = cachedCameraPose.position;
            m_cachedCameraPose.rotation = cachedCameraPose.rotation;

            var webTexture = m_cameraTextureManager.WebCamTexture;
            if (webTexture != null)
            {
                var texture2D = GetCameraStillTexture2D(webTexture);
                await Awaitable.NextFrameAsync();

                m_objectClassifier.ProcessImageForClassification(texture2D);
            }
            else
            {
                Debug.LogWarning($"WebCamTexture was not ready, retry again");
                QueryService();
            }
            await Awaitable.NextFrameAsync();
        }
    }
}
