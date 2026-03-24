using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine; 

public class PlayerController : WorldBehaviour {

    public Player player;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerCameraManager playerCameraManagerPrefab;
    private PlayerCameraManager playerCameraManager;

    [NonSerialized] public Vector2? marqueeStart;
    [NonSerialized] public Vector2 marqueeEnd;

    private List<ISelectable> selectedEntities = new();
    private HashSet<ISelectable> oldSelectedEntitiesSet = new();
    private HashSet<ISelectable> selectedEntitiesSet = new();
    
    private List<Unit> selectedUnits = new();
    private List<Building> selectedBuildings = new();

    private Dictionary<Unit, Vector3> formationPositions = new();

    [SerializeField] private PlayerHUD playerHUDPrefab;
    private PlayerHUD playerHUD;

    [SerializeField] private Unit unitPrefab;

    private LayerMask nonUnitLayerMask;
    private LayerMask nonGroundLayerMask;

    public Camera PlayerCamera => playerCamera;
    
    public bool enableSelection = true;
    public bool enableUnitOrders = true;
    public bool enableBuildingPlacement = true;

    public Building buildingPrefabToPlace;
    public Dictionary<Building, Building> buildingGhosts = new();
    public float buildingPlacementYaw = 0;
    
    public Collider[] placementOverlapColliders = new Collider[10];

    private void Awake() {
        nonUnitLayerMask = ~LayerMask.GetMask("Unit");
        nonGroundLayerMask = ~LayerMask.GetMask("Ground");
        if (playerCameraManagerPrefab)
            playerCameraManager = world.Spawn(playerCameraManagerPrefab, o => o.playerController = this);
        if (playerHUDPrefab)
            playerHUD = world.Spawn(playerHUDPrefab, world.canvas.transform, playerHUD => {
                playerHUD.owningPlayerController = this;
            });

        UpdatePlayerCameraTransform();

        if (TryTraceRay(new Vector2(Screen.width, Screen.height) / 2, out var hitInfo))
            for (var i = 0; i < 10; i++)
                world.Spawn(unitPrefab, unit => {
                    unit.OwningPlayer = player;
                    unit.transform.position = hitInfo.point;
                });
    }

    private void Update() {
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
            var speedUp = Input.GetKey(KeyCode.LeftShift) ? 2 : 1;
            playerCameraManager.position += movementDirection * (Time.deltaTime * 5 * speedUp);
        }

        var rotationInput = 0;
        if (Input.GetKey(KeyCode.Q))
            rotationInput += 1;
        if (Input.GetKey(KeyCode.E))
            rotationInput -= 1;
        if (rotationInput != 0)
            playerCameraManager.TryRotateCamera(rotationInput);

        UpdatePlayerCameraTransform();

        if (enableSelection) {
            if (Input.GetMouseButtonDown(MouseButton.left)) {
                marqueeStart = Input.mousePosition;
                marqueeEnd = marqueeStart.Value;

                selectedEntities.Clear();
                selectedEntitiesSet.Clear();
            }

            else if (Input.GetMouseButton(MouseButton.left)) {
                marqueeEnd = Input.mousePosition;

                selectedEntities.Clear();
                selectedEntitiesSet.Clear();
                selectedUnits.Clear();
                selectedBuildings.Clear();

                var selectablesRegistry = world.GetSubsystem<SelectablesRegistry>();
                if (selectablesRegistry)
                    foreach (var selectable in selectablesRegistry.Entities) {
                        var onScreenBounds = playerHUD.GetOnScreenBounds(selectable.SelectionBounds, playerCamera);
                        var marqueeMin = Vector2.Min(marqueeStart.Value, marqueeEnd);
                        var marqueeMax = Vector2.Max(marqueeStart.Value, marqueeEnd);
                        var marqueeRect = Rect.MinMaxRect(marqueeMin.x, marqueeMin.y, marqueeMax.x, marqueeMax.y);
                        if (marqueeRect.Overlaps(onScreenBounds)) {
                            selectedEntities.Add(selectable);
                            if (selectable is Unit unit)
                                selectedUnits.Add(unit);
                            else if (selectable is Building building)
                                selectedBuildings.Add(building);
                        }
                    }
                selectedEntitiesSet.AddRange(selectedEntities);

                foreach (var selectable in selectedEntitiesSet)
                    if (!oldSelectedEntitiesSet.Contains(selectable))
                        selectable.IsSelected = true;
                foreach (var selectable in oldSelectedEntitiesSet)
                    if (!selectedEntitiesSet.Contains(selectable))
                        selectable.IsSelected = false;

                oldSelectedEntitiesSet.Clear();
                oldSelectedEntitiesSet.UnionWith(selectedEntitiesSet);
            }

            else if (Input.GetMouseButtonUp(MouseButton.left)) {
                marqueeStart = null;
                marqueeEnd = Vector2.zero;
            }
        }

        if (enableUnitOrders) {
            if (Input.GetMouseButtonDown(MouseButton.right) && selectedEntities.Count > 0) {
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
        }

        if (enableBuildingPlacement) {
            if (buildingPrefabToPlace) {
                
                EnsureBuildingGhostExists(buildingPrefabToPlace);
                
                buildingPlacementYaw += Input.mouseScrollDelta.y * 5;
                
                var ghost = buildingGhosts[buildingPrefabToPlace];
                if (TryTraceRay(Input.mousePosition, out var hitInfo)) {
                    ghost.gameObject.SetActive(true);
                    ghost.transform.position = hitInfo.point;
                    ghost.transform.rotation = Quaternion.Euler(0, buildingPlacementYaw, 0);
                    
                    var canBePlaced = CanBePlaced(buildingPrefabToPlace, hitInfo.point, buildingPlacementYaw);
                    ghost.GhostColor = canBePlaced ? new Color(0, 1, 0, .5f) : new Color(1, 0, 0, .5f);

                    if (canBePlaced && Input.GetMouseButtonDown(MouseButton.left)) {
                        world.Spawn(buildingPrefabToPlace, building => {
                            building.OwningPlayer = player;
                            building.transform.position = hitInfo.point;
                            building.transform.rotation = Quaternion.Euler(0, buildingPlacementYaw, 0);
                            building.SetPlayConstructionAnimationOnStart(true);
                        });
                    }
                }
                else 
                    ghost.gameObject.SetActive(false);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            if (TryTraceRay(Input.mousePosition, out var hitInfo))
                world.Spawn(unitPrefab, unit => {
                    unit.OwningPlayer = player;
                    unit.transform.position = hitInfo.point;
                });
        }
    }
    
    public void EnsureBuildingGhostExists(Building building) {
        if (!buildingGhosts.ContainsKey(building)) {
            var ghost = world.Spawn(buildingPrefabToPlace, ghost => ghost.SetUpAsGhost());
            buildingGhosts[building] = ghost;
            ghost.gameObject.SetActive(false);
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
            playerCameraManager.GetView(out cameraPosition, out cameraRotation);
            playerCamera.transform.position = cameraPosition;
            playerCamera.transform.rotation = cameraRotation;
        }
    }

    public bool CanBePlaced(Building building, Vector3 position, float yaw) {
        return Physics.OverlapBoxNonAlloc(position, building.PlacementExtents, placementOverlapColliders, Quaternion.Euler(0, yaw, 0), nonGroundLayerMask) == 0;
    }
}