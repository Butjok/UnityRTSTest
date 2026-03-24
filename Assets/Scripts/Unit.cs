using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : WorldBehaviour, ISelectable, IHasHealth, IPlayerProperty {

    [SerializeField] private Player owningPlayer;
    [SerializeField] private int cost = 1000;

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private LineRenderer pathRenderer;
    [SerializeField] private Renderer selectionCircleRenderer;

    [SerializeField] private float radiusInFormation = .5f;
    [SerializeField] private float moveSpeed = 3;
    [SerializeField] private float health = 1;

    private Vector3? moveDestination = null;
    private NavMeshPath navMeshPath;
    private List<Vector3> movePath = new();

    private Vector3? lastPosition = null;
    private List<(Vector3 start, Vector3 end)> segments = new();

    public Bounds SelectionBounds => meshRenderer.bounds;

    private bool isSelected = false;

    public bool IsSelected {
        get => isSelected;
        set {
            isSelected = value;
            selectionCircleRenderer.enabled = value;
        }
    }

    public float Health {
        get => health;
        set => health = Mathf.Clamp01(value);
    }

    public float RadiusInFormation => radiusInFormation;

    public void Awake() {
        navMeshPath = new NavMeshPath();
        if (!meshRenderer)
            meshRenderer = GetComponent<MeshRenderer>();
        PlayerColor = owningPlayer ? owningPlayer.Color : Color.white;

        NavMesh.onPreUpdate += Repath;
    }

    private void OnDestroy() {
        NavMesh.onPreUpdate -= Repath;
    }

    private List<(Renderer renderer, int materialIndex, Material material)> dynamicMaterials;

    private void EnsureDynamicMaterialsAreSetUp() {
        if (dynamicMaterials == null) {
            dynamicMaterials = new();
            foreach (var renderer in GetComponentsInChildren<Renderer>()) {
                var materials = renderer.materials;
                for (var i = 0; i < materials.Length; i++)
                    dynamicMaterials.Add((renderer, i, materials[i]));
            }
        }
    }

    private Color playerColor;

    public Color PlayerColor {
        get => playerColor;
        set {
            playerColor = value;
            EnsureDynamicMaterialsAreSetUp();
            foreach (var (_, _, material) in dynamicMaterials)
                material.SetColor("_BaseColor", playerColor);
        }
    }

    public Player OwningPlayer {
        get => owningPlayer;
        set {
            owningPlayer = value;
            PlayerColor = owningPlayer ? owningPlayer.Color : Color.white;
        }
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
        movePath.Clear();
        if (moveDestination.HasValue && NavMesh.CalculatePath(transform.position, moveDestination.Value, -1, navMeshPath)) {
            movePath.AddRange(navMeshPath.corners);
            return true;
        }
        return false;
    }

    public void Repath() {
        TryRepath();
    }

    public void Update() {
        pathRenderer.enabled = IsSelected && movePath.Count >= 2;

        if (moveDestination != null && movePath.Count >= 2) {
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

        if (lastPosition.HasValue) {
            var delta = transform.position - lastPosition.Value;
            if (delta != Vector3.zero) {
                var targetRotation = Quaternion.LookRotation(Vector3.Scale(delta, new Vector3(1, 0, 1)), Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 180 * delta.magnitude);
            }
        }
        lastPosition = transform.position;
    }
}