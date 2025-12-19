// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Meta.Utilities.CameraTaxonTracking
{
    /// <summary>
    /// Represents a single (surface) sample of object
    /// </summary>
    public class TrackSample
    {
        public Texture2D CameraImage;
        public Vector3 CameraPosition;
        public bool[] IsEdges;
        public Vector3 Normal;
        public Vector3[] Points;
        public Vector3 Up;

        public TrackSample(Vector3 normal, Vector3 up, Vector3[] points, bool[] edges, Texture2D cameraImage, Vector3 cameraCenter)
        {
            Normal = normal;
            Up = up;
            Points = points;
            IsEdges = edges;
            CameraImage = cameraImage;
            CameraPosition = cameraCenter;
            Timestamp = DateTime.Now;
        }

        public DateTime Timestamp
        {
            get;
        }
    }

    public class ImageSampleContext
    {
        public Texture2D Image;
        public Vector3 Normal;
        public Vector2 Size;
        public Vector3 Up;
    }

    public class CameraTrackedTaxon
    {
        // Settings
        public const float MINIMUM_POINT_COUNT_KEEP = 4;
        public const float MINIMUM_EXTENT = 0.05f; // 5 cm
        public const int MAXIMUM_SAMPLE_COUNT = 10; // Throw away oldest values at this point
        public const int MAX_MISS_COUNT_FOR_REMOVAL = 5; // Should have been visible but was not allowance
        public const float MAXIMUM_TIMESTAMP_EXIST_SECONDS = 300.0f; // 5 Miniutes - After this time, considered stale
        public const float MAXIMUM_ANGLE_VISIBILITY_DEGREES = 35.0f; // Forgiving / narrow fov estimate
        public const float MINIMUM_DISTANCE_VISIBILITY = 0.50f; // Not too close
        public const float MAXIMUM_DISTANCE_VISIBILITY = 2.0f; // Not too far

        // Source values
        private readonly List<TrackSample> m_surfaceSamples = new();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public CameraTrackedTaxon(string classification, TrackSample[] samples)
        {
            Taxa = classification;
            AddSamples(samples);
        }

        public string Taxa { get; }

        // This can be a list from merged observations
        public ImageSampleContext ImageContext => GetRepresentativeImage(null);

        private int m_observeCount = 0; // As object is re-observed increase this by 1
        private int m_missedCount = 0; // As object is expected but not seen increase this by 1

        public Vector3 Center { get; private set; }

        public string Name => Taxa;

        public Vector3 Extent { get; private set; }

        public CameraTrackedTaxon UntrackedCopy()
        {
            var samples = new List<TrackSample>();
            foreach (var sample in m_surfaceSamples)
            {
                var copy = new TrackSample(sample.Normal, sample.Up, sample.Points, sample.IsEdges, sample.CameraImage, sample.CameraPosition);
                samples.Add(copy);
            }
            var taxon = new CameraTrackedTaxon(Taxa, samples.ToArray());
            return taxon;
        }

        public DateTime OldestTimestamp
        {
            get
            {
                var timestamp = DateTime.Now;
                foreach (var sample in m_surfaceSamples)
                {
                    var stamp = sample.Timestamp;
                    timestamp = timestamp < stamp ? timestamp : stamp;
                }

                return timestamp;
            }
        }

        public DateTime NewestTimestamp
        {
            get
            {
                var timestamp = DateTime.Now;
                foreach (var sample in m_surfaceSamples)
                {
                    var stamp = sample.Timestamp;
                    timestamp = timestamp > stamp ? timestamp : stamp;
                }

                return timestamp;
            }
        }

        public List<(Vector3, bool)> SamplePoints
        {
            get
            {
                var list = new List<(Vector3, bool)>();
                foreach (var sample in m_surfaceSamples)
                {
                    var pointCount = sample.Points.Length;
                    for (var i = 0; i < pointCount; i++)
                    {
                        var point = sample.Points[i];
                        var edge = sample.IsEdges[i];
                        list.Add((point, edge));
                    }
                }

                return list;
            }
        }

        public List<Vector3> SampleNormals
        {
            get
            {
                var list = new List<Vector3>();
                foreach (var sample in m_surfaceSamples)
                {
                    list.Add(sample.Normal);
                }

                return list;
            }
        }

        public float Reliability // Value in [0,1] for overall reliability / confidence of lesson
        {
            get
            {
                if (m_missedCount > 1)
                {
                    return 0.0f;
                }

                if (m_missedCount <= 1 && m_observeCount == 0)
                {
                    return 0.1f;
                }

                if (m_observeCount == 0)
                {
                    return 0.2f;
                }

                if (m_observeCount == 1)
                {
                    return 0.3f;
                }

                return 1.0f - 1.0f / m_observeCount; // 2 = 0.5, 3 = 0.66, 4 = 0.75, ...
            }
        }

        public bool ShouldRemove(DateTime observeTimestamp)
        {
            // Criteria:
            var pointCountTotal = 0;
            foreach (var sample in m_surfaceSamples)
            {
                var points = sample.Points.Length;
                pointCountTotal += points;
            }
            var mostRecentTimestamp = NewestTimestamp;
            var timeDifference = DateTime.Now - mostRecentTimestamp;

            // Not enough points to approximate center & extent
            if (pointCountTotal < MINIMUM_POINT_COUNT_KEEP)
            {
                return true;
            }

            // Most recent observation is stale
            if (timeDifference.TotalSeconds > MAXIMUM_TIMESTAMP_EXIST_SECONDS)
            {
                return true;
            }

            // Should have seen & missed too many times
            if (m_missedCount > MAX_MISS_COUNT_FOR_REMOVAL)
            {
                return true;
            }

            // If size or position is changing a lot, it may be best to remove here.
            // It will take comparing tracking data and checking for various tolerances. 

            return false;
        }

        private bool ShouldBeVisible(Vector3 cameraCenter, Vector3 cameraDirection)
        {
            var cameraToCenter = Center - cameraCenter;
            var angle = Vector3.Angle(cameraDirection, cameraToCenter);
            // For more accuracy, an extra check can be added here for the image facing towards the user.
            if (angle < MAXIMUM_ANGLE_VISIBILITY_DEGREES)
            {
                var distance = cameraToCenter.magnitude;
                // Is in reasonable distance for image recognition
                if (distance is >= MINIMUM_DISTANCE_VISIBILITY and <= MAXIMUM_DISTANCE_VISIBILITY)
                {
                    return true;
                }
            }
            return false;
        }

        public void NoteCameraObservation(DateTime timestamp, Vector3 cameraCenter, Vector3 cameraDirection)
        {
            // If object was seen this round == contains the recent timestamp (merged)
            if (TimestampExists(timestamp))
            {
                m_observeCount += 1;
                m_missedCount = 0;
                // If object was not seen but clearly should have been:
            }
            else if (ShouldBeVisible(cameraCenter, cameraDirection))
            {
                m_missedCount += 1;
                m_observeCount = 0;
            }
            // Else: gray area, eg: maybe not clearly visible => no action
        }

        private bool TimestampExists(DateTime timestamp)
        {
            // Most recent timestamps should be on end:
            if (m_surfaceSamples.Count > 0)
            {
                var last = m_surfaceSamples.Last();
                return last.Timestamp >= timestamp;
            }
            return false;
        }

        public ImageSampleContext GetRepresentativeImage(Transform transform)
        {
            Texture2D bestImage = null;
            var bestNormal = new Vector3();
            var bestUp = new Vector3();
            var bestAngle = -1.0f;
            var fewestEdges = 0;
            foreach (var sample in m_surfaceSamples)
            {
                var edges = sample.IsEdges;
                var edgeCount = 0;
                foreach (var edge in edges)
                {
                    if (edge)
                    {
                        edgeCount += 1;
                    }
                }

                if (bestImage == null || edgeCount <= fewestEdges)
                {
                    var keep = false;
                    if (transform != null)
                    {
                        var angle = Vector3.Angle(transform.forward, bestNormal);
                        if (bestAngle < 0.0f || angle > bestAngle)
                        {
                            keep = true;
                            bestAngle = angle;
                        }
                    }
                    else
                    {
                        keep = true;
                    }

                    if (keep)
                    {
                        fewestEdges = edgeCount;
                        bestImage = sample.CameraImage;
                        bestNormal = sample.Normal;
                        bestUp = sample.Up;
                    }
                }
            }

            // can get the SIZE from estimated extents in directions orthogonal to normal
            if (bestImage == null)
            {
                return null;
            }

            // Estimate image size based on sample origin and current size estimation
            var center = Center;
            var dotSumUp = 0.0f;
            var dotSumRight = 0.0f;
            var bestRight = Quaternion.AngleAxis(90.0f, bestNormal) * bestUp;
            var pointCount = 0;
            foreach (var sample in m_surfaceSamples)
            {
                foreach (var point in sample.Points)
                {
                    var dir = point - center;
                    // Height: up-down
                    var dot = Vector3.Dot(dir, bestUp);
                    if (dot < 0.0f)
                    {
                        dot = -dot;
                    }
                    dotSumUp += dot;

                    // Width: right-left
                    dot = Vector3.Dot(dir, bestRight);
                    if (dot < 0.0f)
                    {
                        dot = -dot;
                    }
                    dotSumRight += dot;

                    // Samples used
                    pointCount += 1;
                }
            }

            if (pointCount == 0)
            {
                return null;
            }

            dotSumUp /= pointCount;
            dotSumRight /= pointCount;

            var widthOverHeight = (float)bestImage.width / bestImage.height;
            var averageY = dotSumUp;
            var averageX = dotSumRight;
            // Preserve aspect ratio by averaging 2 size estimates
            var widthFromHeight = averageY * widthOverHeight;
            var heightFromWidth = averageX / widthOverHeight;
            // Take average of 2 estimates
            var scaleX = (averageX + widthFromHeight) * 0.5f;
            var scaleY = (averageY + heightFromWidth) * 0.5f;

            // Average is ~ 50% of expected size, samples are only in ~ interior portion of object
            var additionalScale = 2.0f / CameraTaxonTracker.TAXON_SAMPLE_EXTENT_FACTOR;
            scaleX *= additionalScale;
            scaleY *= additionalScale;

            var best = new ImageSampleContext
            {
                Image = bestImage,
                Normal = bestNormal,
                Up = bestUp,
                Size = new Vector2(scaleX, scaleY)
            };
            return best;
        }

        // Extents are boundary points in 3D that are derived from the planar projection of the image in 2D 
        public void AddSamples(TrackSample[] samples)
        {
            foreach (var sample in samples)
            {
                m_surfaceSamples.Add(sample);
            }

            FilterSamplesRepeatRANSAC();

            // Remove oldest samples first
            // Samples could also include direction prioritization here (for maximizing surface coverage)
            while (m_surfaceSamples.Count > MAXIMUM_SAMPLE_COUNT)
            {
                m_surfaceSamples.RemoveAt(0);
            }

            CalculateDerived();
        }

        public void SetSamples(TrackSample[] samples)
        {
            m_surfaceSamples.Clear();
            AddSamples(samples);
        }

        private void FilterSamplesRepeatRANSAC()
        {
            var maxRansacIterations = 5;
            for (var i = 0; i < maxRansacIterations; i++)
            {
                var didRemove = FilterSamplesRANSAC();
                if (!didRemove)
                {
                    return;
                }
            }
        }

        private bool FilterSamplesRANSAC()
        {
            var didDropAnyPoints = false;
            var ransacPointCount = 5; // Minimum count for sample 
            var sigmaPointDrop = 2.5f; // Points further away from 2sigma (95%) 3sigma (99%) are dropped
            var allPoints = new List<Vector3>();
            foreach (var sample in m_surfaceSamples)
            {
                foreach (var point in sample.Points)
                {
                    allPoints.Add(point);
                }
            }

            var allPointCount = allPoints.Count;
            if (allPointCount > ransacPointCount)
            {
                // Get a random sample of points
                var indexes = new List<int>();
                var com = new Vector3();
                var allPointIndices = Enumerable.Range(0, allPointCount).ToList();
                for (var point = 0; point < ransacPointCount; point++)
                {
                    var randomIndex = UnityEngine.Random.Range(0, allPointIndices.Count);
                    indexes.Add(allPointIndices[randomIndex]);
                    allPointIndices.RemoveAt(randomIndex);
                    var potential = allPoints[randomIndex];
                    com += potential;
                }

                // Get center of mass of points
                com /= ransacPointCount;

                // Calculate sigma of distances from center
                var sigma = 0.0f;
                for (var point = 0; point < ransacPointCount; point++)
                {
                    var index = indexes[point];
                    var potential = allPoints[index];
                    var distance = Vector3.Distance(potential, com);
                    sigma += distance * distance;
                }

                sigma = Mathf.Sqrt(sigma / ransacPointCount);

                // Drop points too far from center
                sigma *= sigmaPointDrop;
                for (var i = 0; i < m_surfaceSamples.Count; i++)
                {
                    var sample = m_surfaceSamples[i];
                    var originalPoints = sample.Points;
                    var points = new List<Vector3>(originalPoints);
                    for (var j = 0; j < points.Count; j++)
                    {
                        var point = points[j];
                        var distance = Vector3.Distance(point, com);
                        if (distance > sigma)
                        {
                            points.RemoveAt(j);
                            didDropAnyPoints = true;
                            --j;
                        }
                    }
                    // Replace points if deemed outliers
                    if (originalPoints.Length != points.Count)
                    {
                        sample.Points = points.ToArray();
                    }
                    if (m_surfaceSamples.Count == 0)
                    {
                        m_surfaceSamples.RemoveAt(i);
                        --i;
                    }
                }
            }

            return didDropAnyPoints;
        }

        private void CalculateDerived()
        {
            _ = m_surfaceSamples.Count;
            var pointCountTotal = 0;
            var averageCenter = new Vector3();
            foreach (var sample in m_surfaceSamples)
            {
                foreach (var point in sample.Points)
                {
                    averageCenter += point;
                    pointCountTotal += 1;
                }
            }

            averageCenter /= pointCountTotal;

            float dotX;
            float dotY;
            float dotZ;
            Vector3 centerToPoint;
            var averageExtents = new Vector3();
            var maximumExtents = new Vector3();

            // Edge points could be handled differently, for accuracy. Not a direct part of average, but part of extent minimum
            foreach (var sample in m_surfaceSamples)
            {
                foreach (var point in sample.Points)
                {
                    centerToPoint = point - averageCenter;
                    dotX = Vector3.Dot(centerToPoint, Vector3.right);
                    dotY = Vector3.Dot(centerToPoint, Vector3.up);
                    dotZ = Vector3.Dot(centerToPoint, Vector3.forward);
                    averageExtents.x += Mathf.Abs(dotX);
                    averageExtents.y += Mathf.Abs(dotY);
                    averageExtents.z += Mathf.Abs(dotZ);
                    maximumExtents.x = Mathf.Max(maximumExtents.x, Mathf.Abs(dotX));
                    maximumExtents.y = Mathf.Max(maximumExtents.y, Mathf.Abs(dotY));
                    maximumExtents.z = Mathf.Max(maximumExtents.z, Mathf.Abs(dotZ));
                }
            }
            averageExtents /= pointCountTotal;

            var derivedExtents = (averageExtents + maximumExtents) * 0.5f;
            derivedExtents.x = Mathf.Max(derivedExtents.x, MINIMUM_EXTENT);
            derivedExtents.y = Mathf.Max(derivedExtents.y, MINIMUM_EXTENT);
            derivedExtents.z = Mathf.Max(derivedExtents.z, MINIMUM_EXTENT);
            Center = averageCenter;
            Extent = derivedExtents;
        }

        public static bool Collides(CameraTrackedTaxon taxonA, CameraTrackedTaxon taxonB)
        {
            var sizeMarginRatioClosed = 0.5f; // 1 = 100% of cuboid hypotenuses
            var sizeMarginRatioOpen = 1.5f; // Search a lot further to join open edges
            if (taxonA.Name != taxonB.Name)
            {
                return false;
            }

            // if A's center is too close to B's center:
            var centerA = taxonA.Center;
            var centerB = taxonB.Center;
            var hypA = taxonA.Extent.magnitude;
            var hypB = taxonB.Extent.magnitude;

            var dirAtoB = centerB - centerA;

            var joinsOpenEdgeA = false;
            var joinsOpenEdgeB = false;


            // Consider each sample inside the 2 planes defined by A-B
            var samplesA = taxonA.SamplePoints;
            var samplesB = taxonB.SamplePoints;
            foreach (var sampleA in samplesA)
            {
                var pointA = sampleA.Item1;
                var edgeA = sampleA.Item2;
                var directionA = pointA - centerA;
                // Check if outside plane merger volume
                var dotA = Vector3.Dot(dirAtoB, directionA);
                if (dotA is > 1.0f or < 0.0f)
                {
                    continue;
                }

                var closestBEdge = false;
                var closestBPoint = new Vector3();
                var closestBDistance = -1.0f;
                foreach (var sampleB in samplesB)
                {
                    var pointB = sampleB.Item1;
                    var edgeB = sampleB.Item2;
                    var directionB = pointB - centerB;
                    // Check if outside plane merger volume
                    var dotB = Vector3.Dot(dirAtoB, directionB);
                    if (dotB is > 1.0f or < 0.0f)
                    {
                        continue;
                    }

                    // Find closest point that would join surfaces
                    var distanceAB = Vector3.Distance(pointA, pointB);
                    if (closestBDistance < 0 || distanceAB < closestBDistance)
                    {
                        closestBDistance = distanceAB;
                        closestBPoint = pointB;
                        closestBEdge = edgeB;
                    }
                }

                // If there is a closest point, check if closed-closed, open-closed x2, open-open
                if (closestBDistance > 0)
                {
                    joinsOpenEdgeA |= edgeA;
                    joinsOpenEdgeB |= closestBEdge;
                }
            }

            var sizeMarginRatioA = joinsOpenEdgeA ? sizeMarginRatioOpen : sizeMarginRatioClosed;
            var sizeMarginRatioB = joinsOpenEdgeB ? sizeMarginRatioOpen : sizeMarginRatioClosed;
            var centerDistance = dirAtoB.magnitude;
            var marginAllowedA = hypA * sizeMarginRatioA;
            var marginAllowedB = hypB * sizeMarginRatioB;
            var allowedDistance = marginAllowedA + marginAllowedB;
            return centerDistance < allowedDistance;
        }

        /// <summary>
        ///     This merges 2 objects into a single object
        /// </summary>
        /// <param name="taxonA">Object A - merge target</param>
        /// <param name="taxonB">Object B - other object to merge with</param>
        /// <returns>Object A is returned with updated internal representation</returns>
        public static CameraTrackedTaxon MergeInto(CameraTrackedTaxon taxonA, CameraTrackedTaxon taxonB)
        {
            var sampleList = new List<TrackSample>();
            sampleList.AddRange(taxonA.m_surfaceSamples);
            sampleList.AddRange(taxonB.m_surfaceSamples);
            _ = sampleList.OrderBy(s => s.Timestamp);
            var samples = sampleList.ToArray();
            taxonA.SetSamples(samples);
            return taxonA;
        }
    }
}