using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : WorldBehaviour {

    public MeshRenderer meshRenderer;
    public LineRenderer pathRenderer;
    public Renderer selectionCircleRenderer;

    public float radiusInFormation = .5f;
    public float moveSpeed = 3;
    public float health = 1;

    private Vector3? moveDestination = null;
    private NavMeshPath navMeshPath;
    private List<Vector3> movePath = new();

    // this is used to rotate the unit to face the direction it's moving in
    private Vector3? lastPosition = null;
    
    // this is a scratch variable to avoid allocating a new list every frame for the path segments to render
    private List<(Vector3 start, Vector3 end)> segments = new();

    // this is used to avoid enabling/disabling the selection circle renderer every frame when the unit is selected
    private bool? oldIsSelected = null;

    public bool IsSelected {
        get {
            var playerController = world.playerController;
            return playerController != null && playerController.selectedUnits.Contains(this);
        }
    }

    public void Awake() {
        navMeshPath = new NavMeshPath();
        if (!meshRenderer)
            meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetMoveDestination(Vector3 destination) {
        moveDestination = destination;
        TryRepath();
    }

    public void ClearMoveDestination() {
        moveDestination = null;
        movePath.Clear();
    }

    public bool TryRepath() {
        if (NavMesh.CalculatePath(transform.position, moveDestination.Value, -1, navMeshPath)) {
            movePath.Clear();
            movePath.AddRange(navMeshPath.corners);
            return true;
        }
        return false;
    }

    public void Update() {
        pathRenderer.enabled = IsSelected && movePath.Count >= 2;

        if (moveDestination != null) {
            if (movePath.Count >= 2) {
                MathUtility.FindClosestPointOnPolyline(transform.position, movePath, out var polylineDistance, out int closestSegmentIndex);
                var targetPoint = MathUtility.GetPointOnPolylineByDistance(movePath, polylineDistance + moveSpeed * Time.deltaTime, out var isEndOfPolyline);
                var toTargetPoint = targetPoint - transform.position;
                if (toTargetPoint != Vector3.zero) {
                    var moveDirection = toTargetPoint.normalized;
                    transform.position += moveDirection * moveSpeed * Time.deltaTime;
                    if (isEndOfPolyline)
                        ClearMoveDestination();
                }

                if (pathRenderer.enabled) {
                    segments.Clear();
                    for (var segmentIndex = closestSegmentIndex; segmentIndex < movePath.Count - 1; segmentIndex++) {
                        var start = segmentIndex == closestSegmentIndex ? targetPoint : movePath[segmentIndex];
                        var end = movePath[segmentIndex + 1];
                        segments.Add((start, end));
                    }
                    pathRenderer.positionCount = segments.Count + 1;
                    if (segments.Count > 0) {
                        pathRenderer.SetPosition(0, segments[0].start);
                        for (var i = 0; i < segments.Count; i++)
                            pathRenderer.SetPosition(i + 1, segments[i].end);
                    }
                }
            }
        }

        if (lastPosition.HasValue) {
            var delta = transform.position - lastPosition.Value;
            if (delta != Vector3.zero) {
                var targetRotation = Quaternion.LookRotation(Vector3.Scale(delta, new Vector3(1, 0, 1)), Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 180 * delta.magnitude);
            }
        }
        lastPosition = transform.position;

        if (oldIsSelected != IsSelected) {
            oldIsSelected = IsSelected;
            
            selectionCircleRenderer.enabled = IsSelected;
        }
    }
}