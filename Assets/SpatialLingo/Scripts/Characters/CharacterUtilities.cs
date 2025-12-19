// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.Characters
{
    [MetaCodeSample("SpatialLingo")]
    public class CharacterUtilities
    {
        public static Vector3 BezierQuadraticAtT(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            var t1 = 1 - t;
            var tA = t1 * t1;
            var tB = 2 * t1 * t;
            var tC = t * t;
            return new Vector3(a.x * tA + b.x * tB + c.x * tC, a.y * tA + b.y * tB + c.y * tC, a.z * tA + b.z * tB + c.z * tC);
        }

        public static Vector3 PerpendicularComponent(Vector3 baseDir, Vector3 vector)
        {
            var parallel = ParallelComponent(baseDir, vector);
            if (parallel.magnitude == 0.0f)
            {
                return parallel;
            }
            parallel.x = vector.x - parallel.x;
            parallel.y = vector.y - parallel.y;
            parallel.z = vector.z - parallel.z;
            return parallel;
        }

        public static Vector3 ParallelComponent(Vector3 baseDir, Vector3 vector)
        {
            var lenB = baseDir.magnitude;
            if (lenB == 0)
            {
                return new Vector3(0, 0, 0);
            }
            var dotBv = Vector3.Dot(baseDir, vector);
            dotBv /= lenB * lenB;
            baseDir.Scale(new Vector3(dotBv, dotBv, dotBv));
            return baseDir;
        }

        public static Vector3 ControlPositionForLinearArc(Vector3 startPosition, Vector3 endPosition, float offsetPathDistance, float percentAlongPath = 0.5f)
        {
            percentAlongPath = Math.Clamp(percentAlongPath, 0.0f, 1.0f);
            var startToEnd = endPosition - startPosition;
            var startToEndDistance = startToEnd.magnitude;
            Vector3 controlPosition;
            if (startToEndDistance == 0.0f)
            {
                controlPosition = endPosition;
            }
            else
            {
                // Get direction perpendicular to the linear path also m the Y direction 
                var perpendicular = PerpendicularComponent(startToEnd, Vector3.up);
                perpendicular.Normalize();
                // Set perpendicular offset to desired distance
                perpendicular.Scale(new Vector3(offsetPathDistance, offsetPathDistance, offsetPathDistance));
                // Middle = half the direction
                controlPosition = startToEnd;
                controlPosition.Scale(new Vector3(percentAlongPath, percentAlongPath, percentAlongPath));
                // Offset by the starting point
                controlPosition += startPosition;
                // Offset from the line
                controlPosition += perpendicular;
            }

            return controlPosition;
        }
    }
}