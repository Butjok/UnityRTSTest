using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class UnitsRegistry : WorldSubsystem {

    public List<Unit> units = new();
    private Dictionary<Unit, Vector2> pushForces = new();

    public void Start() {
        units = FindObjectsByType<Unit>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();

        world.onObjectSpawned += AddUnit;
        world.onObjectDestroyed += RemoveUnit;
    }

    public void OnDestroy() {
        world.onObjectSpawned -= AddUnit;
        world.onObjectDestroyed -= RemoveUnit;
    }

    public void Update() {
        pushForces.Clear();

        foreach (var pushingUnit in units)
        foreach (var pushedUnit in units) {
            if (pushedUnit == pushingUnit)
                continue;
            var distance = Vector2.Distance(pushingUnit.transform.position.ToVector2(), pushedUnit.transform.position.ToVector2());
            if (distance < pushingUnit.radiusInFormation + pushedUnit.radiusInFormation) {
                var pushDirection = (pushedUnit.transform.position - pushingUnit.transform.position).ToVector2().normalized;
                if (pushDirection == Vector2.zero) {
                    var randomAngle = Random.Range(0, 2 * Mathf.PI);
                    pushDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
                }
                var pushForce = (pushingUnit.radiusInFormation + pushedUnit.radiusInFormation - distance) * pushDirection * 5;
                pushForces[pushedUnit] = pushForces.GetValueOrDefault(pushedUnit, Vector2.zero) + pushForce;
            }
        }

        foreach (var (pushedUnit, pushForce) in pushForces) {
            var newPushedUnitPosition = pushedUnit.transform.position + pushForce.ToVector3() * Time.deltaTime;
            NavMesh.Raycast(pushedUnit.transform.position, newPushedUnitPosition, out var hit, -1);
            if (hit.hit)
                newPushedUnitPosition = hit.position;
            pushedUnit.transform.position = newPushedUnitPosition;
        }
    }

    public void AddUnit(Object obj) {
        if (obj is Unit unit)
            units.Add(unit);
    }

    public void RemoveUnit(Object obj) {
        if (obj is Unit unit)
            units.Remove(unit);
    }
}