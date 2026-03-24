using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UnitsRegistry : AbstractEntityRegistry<Unit> {

    private Dictionary<Unit, Vector2> pushForces = new();

    private void Update() {
        pushForces.Clear();

        foreach (var pushingUnit in Entities)
        foreach (var pushedUnit in Entities) {
            if (pushedUnit == pushingUnit)
                continue;
            var distance = Vector2.Distance(pushingUnit.transform.position.ToVector2(), pushedUnit.transform.position.ToVector2());
            if (distance < pushingUnit.RadiusInFormation + pushedUnit.RadiusInFormation) {
                var pushDirection = (pushedUnit.transform.position - pushingUnit.transform.position).ToVector2().normalized;
                if (pushDirection == Vector2.zero) {
                    var randomAngle = Random.Range(0, 2 * Mathf.PI);
                    pushDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
                }
                var pushForce = (pushingUnit.RadiusInFormation + pushedUnit.RadiusInFormation - distance) * pushDirection * 5;
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
}