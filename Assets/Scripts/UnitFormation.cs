using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class UnitFormation {
    
    private static List<Unit> units = new();

    public static void FormAround(Vector3 center, Dictionary<Unit, Vector3> positions) {
        var unitRadius = 0f;

        float Circumference(float radius) {
            return 2 * Mathf.PI * radius;
        }
        float RingRadius(int ringIndex) {
            return ringIndex * 2 * unitRadius;
        }
        float RingLength(int ringIndex) {
            return Circumference(RingRadius(ringIndex));
        }
        int RingCapacity(int ringIndex) {
            if (ringIndex == 0) 
                return 1;
            var length = RingLength(ringIndex);
            return Mathf.FloorToInt(length / (2 * unitRadius));
        }

        // we assume all the units have the same radius for now
        
        var positionAccumulator = Vector3.zero;
        var placedCount = 0;

        var ringIndex = 0;
        var indexInRing = 0;
        units.Clear();
        units.AddRange(positions.Keys);
        foreach (var unit in units) {
            if (unitRadius == 0)
                unitRadius = unit.radiusInFormation;

            var ringCapacity = RingCapacity(ringIndex);
            if (indexInRing >= ringCapacity) {
                ringIndex++;
                indexInRing = 0;
                ringCapacity = RingCapacity(ringIndex);
            }

            var angle = (float)indexInRing / ringCapacity * 2 * Mathf.PI;
            var radius = RingRadius(ringIndex);
            var position = new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                center.y,
                center.z + Mathf.Sin(angle) * radius);
            
            positions[unit] = position;
             positionAccumulator += position;
                placedCount++;
            
            indexInRing++;
        }

        if (placedCount > 0) {
            var averagePosition = positionAccumulator / placedCount;
            foreach (var unit in units) {
                var position = positions[unit];
                var offset = position - averagePosition;
                positions[unit] = center + offset;
            }
        }
    }
    
    public static void ProjectToNavMesh(Dictionary<Unit, Vector3> positions) {
        units.Clear();
        units.AddRange(positions.Keys);
        foreach (var unit in units) {
            var position = positions[unit];
            if (NavMesh.SamplePosition(position, out var hit, 10f, NavMesh.AllAreas))
                positions[unit] = hit.position;
        }
    }
}