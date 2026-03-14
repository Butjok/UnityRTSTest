using System.Collections.Generic;
using UnityEngine;

public static class MathUtility {
    
    public static Vector3 FindClosestPointOnPolyline(Vector3 point, IReadOnlyList<Vector3> polyline, out float distance, out int segmentIndex) {
        var closestPoint = Vector3.zero;
        var closestDistanceSquared = float.MaxValue;
        distance = 0;
        segmentIndex = -1;

        var distanceWalked = 0f;
        for (var i = 0; i < polyline.Count - 1; i++) {
            var segmentStart = polyline[i];
            var segmentEnd = polyline[i + 1];

            var segmentVector = segmentEnd - segmentStart;
            var segmentLengthSquared = Vector3.Dot(segmentVector, segmentVector);
            
            var t = Vector3.Dot(point - segmentStart, segmentVector) / segmentLengthSquared;
            t = System.Math.Clamp(t, 0, 1);

            var projection = segmentStart + t * segmentVector;
            var distanceSquared = Vector3.SqrMagnitude(point - projection);

            if (distanceSquared < closestDistanceSquared) {
                closestDistanceSquared = distanceSquared;
                closestPoint = projection;
                distance = distanceWalked + Mathf.Sqrt(segmentLengthSquared) * t;
                segmentIndex = i;
            }
            distanceWalked += Mathf.Sqrt(segmentLengthSquared);
        }

        return closestPoint;
    }
    
    public static Vector3 GetPointOnPolylineByDistance(IReadOnlyList<Vector3> polyline, float distance, out bool isEndOfPolyline) {
        var distanceWalked = 0f;
        isEndOfPolyline = false;
        for (var i = 0; i < polyline.Count - 1; i++) {
            var segmentStart = polyline[i];
            var segmentEnd = polyline[i + 1];

            var segmentVector = segmentEnd - segmentStart;
            var segmentLength = segmentVector.magnitude;

            if (distanceWalked + segmentLength >= distance) {
                var t = (distance - distanceWalked) / segmentLength;
                return segmentStart + t * segmentVector;
            }
            distanceWalked += segmentLength;
        }
        isEndOfPolyline = true;
        return polyline[polyline.Count - 1];
    }
    
    public static Vector2 ToVector2(this Vector3 vector) {
        return new Vector2(vector.x, vector.z);
    }
    public static Vector3 ToVector3(this Vector2 vector) {
        return new Vector3(vector.x, 0, vector.y);
    }
}