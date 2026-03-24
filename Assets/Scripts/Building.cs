using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Building : WorldBehaviour, ISelectable, IHasHealth, IPlayerProperty {

    [SerializeField] private Player owningPlayer;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private List<Unit> buildableUnits = new();
    [SerializeField] private List<Material> sharedGhostMaterials = new();
    [SerializeField] private Collider collider;
    [SerializeField] private NavMeshObstacle navMeshObstacle;

    public Bounds SelectionBounds => meshRenderer.bounds;

    private bool isSelected = false;

    public bool IsSelected {
        get => isSelected;
        set { isSelected = value; }
    }

    private float health = 1;

    public float Health {
        get => health;
        set => health = Mathf.Clamp01(value);
    }

    public Player OwningPlayer {
        get => owningPlayer;
        set {
            owningPlayer = value;
            PlayerColor = owningPlayer ? owningPlayer.Color : Color.white;
        }
    }
    
    private List<(Renderer renderer, int materialIndex, Material material)> dynamicMaterials;
    private void EnsureDynamicMaterialsAreSetUp() {
        if (dynamicMaterials == null) {
            dynamicMaterials = new ();
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

    private bool isGhost;
    public void SetUpAsGhost() {
        if (collider)
            collider.enabled = false;
        if (navMeshObstacle)
            navMeshObstacle.enabled = false;
        isGhost = true;
        meshRenderer.SetSharedMaterials(sharedGhostMaterials);
    }

    public bool IgnoreSelection => isGhost;
    
    public Vector3 PlacementExtents => meshRenderer.bounds.extents;

    public Color GhostColor {
        set {
            foreach (var sharedMaterial in sharedGhostMaterials) 
                sharedMaterial.SetColor("_BaseColor", value);
        }
    }
}