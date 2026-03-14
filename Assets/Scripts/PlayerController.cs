using System.Collections.Generic;
using UnityEngine;

public class PlayerController : WorldBehaviour {

    public Camera playerCamera;
    public PlayerCameraManager playerCameraManagerPrefab;
    public PlayerCameraManager playerCameraManager;

    public Vector2? marqueeStart;
    public Vector2 marqueeEnd;
    public List<Unit> selectedUnits = new();
    public Dictionary<Unit, Vector3> formationPositions = new();

    public PlayerHUD playerHUDPrefab;
    public PlayerHUD playerHUD;

    public Unit unitPrefab;

    private LayerMask nonUnitLayerMask;

    public void Awake() {
        nonUnitLayerMask = ~LayerMask.GetMask("Unit");
        if (playerCameraManagerPrefab)
            playerCameraManager = world.Spawn(playerCameraManagerPrefab, o => o.playerController = this);
        if (playerHUDPrefab)
            playerHUD = world.Spawn(playerHUDPrefab);

        UpdatePlayerCameraTransform();

        if (TryTraceRay(new Vector2(Screen.width, Screen.height) / 2, out var hitInfo))
            for (var i = 0; i < 10; i++)
                world.Spawn(unitPrefab, unit => unit.transform.position = hitInfo.point);
    }

    public void Update() {
        var movementInput = Vector2.zero;
        if (Input.GetKey(KeyCode.W))
            movementInput.y += 1;
        if (Input.GetKey(KeyCode.S))
            movementInput.y -= 1;
        if (Input.GetKey(KeyCode.A))
            movementInput.x -= 1;
        if (Input.GetKey(KeyCode.D))
            movementInput.x += 1;
        if (movementInput != Vector2.zero) {
            var cameraForward = Vector3.Scale(playerCamera.transform.forward, new Vector3(1, 0, 1)).normalized;
            var cameraRight = new Vector3(cameraForward.z, 0, -cameraForward.x);
            var movementDirection = cameraForward * movementInput.y + cameraRight * movementInput.x;
            playerCameraManager.position += movementDirection * (Time.deltaTime * 5);
        }

        var rotationInput = 0;
        if (Input.GetKey(KeyCode.Q))
            rotationInput += 1;
        if (Input.GetKey(KeyCode.E))
            rotationInput -= 1;
        if (rotationInput != 0)
            playerCameraManager.TryRotateCamera(rotationInput);

        UpdatePlayerCameraTransform();

        if (Input.GetMouseButtonDown(MouseButton.left)) {
            marqueeStart = Input.mousePosition;
            marqueeEnd = marqueeStart.Value;

            selectedUnits.Clear();
        }
        else if (Input.GetMouseButton(MouseButton.left)) {
            marqueeEnd = Input.mousePosition;

            selectedUnits.Clear();
            var unitsRegistry = world.GetSubsystem<UnitsRegistry>();
            if (unitsRegistry)
                foreach (var unit in unitsRegistry.units) {
                    var onScreenBounds = playerHUD.GetOnScreenBounds(unit.meshRenderer, playerCamera);
                    var marqueeMin = Vector2.Min(marqueeStart.Value, marqueeEnd);
                    var marqueeMax = Vector2.Max(marqueeStart.Value, marqueeEnd);
                    var marqueeRect = Rect.MinMaxRect(marqueeMin.x, marqueeMin.y, marqueeMax.x, marqueeMax.y);
                    if (marqueeRect.Overlaps(onScreenBounds))
                        selectedUnits.Add(unit);
                }
        }
        else if (Input.GetMouseButtonUp(MouseButton.left)) {
            marqueeStart = null;
            marqueeEnd = Vector2.zero;
        }

        if (Input.GetMouseButtonDown(MouseButton.right) && selectedUnits.Count > 0) {
            if (TryTraceRay(Input.mousePosition, out var hitInfo)) {
                var targetPosition = hitInfo.point;

                formationPositions.Clear();
                foreach (var unit in selectedUnits)
                    formationPositions[unit] = Vector3.zero;
                UnitFormation.FormAround(targetPosition, formationPositions);

                UnitFormation.ProjectToNavMesh(formationPositions);

                foreach (var unit in selectedUnits)
                    unit.SetMoveDestination(formationPositions[unit]);
            }
            else
                foreach (var unit in selectedUnits)
                    unit.ClearMoveDestination();
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            if (TryTraceRay(Input.mousePosition, out var hitInfo))
                world.Spawn(unitPrefab, unit => unit.transform.position = hitInfo.point);
        }
    }

    public bool TryTraceRay(Vector2 screenPosition, out RaycastHit hitInfo) {
        var ray = playerCamera.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out hitInfo, 100, nonUnitLayerMask);
    }

    public void UpdatePlayerCameraTransform() {
        if (playerCameraManager) {
            var cameraPosition = Vector3.zero;
            var cameraRotation = Quaternion.identity;
            playerCameraManager.GetView(ref cameraPosition, ref cameraRotation);
            playerCamera.transform.position = cameraPosition;
            playerCamera.transform.rotation = cameraRotation;
        }
    }
}