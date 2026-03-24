using System.Collections.Generic;
using UnityEngine;

public class Building : WorldBehaviour, ISelectable, IHasHealth, IPlayerProperty {

    [SerializeField] private Player owningPlayer;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private List<Unit> buildableUnits = new();
    [SerializeField] private List<Material> ghostMaterials = new();

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
        isGhost = true;
        meshRenderer.SetSharedMaterials(ghostMaterials);
    }

    public bool IgnoreSelection => isGhost;
}