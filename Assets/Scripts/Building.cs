using System.Collections;
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
    [SerializeField] private bool isReady = true;
    
    private List<(Renderer renderer, int materialIndex, Material material)> dynamicMaterials;
    private float health = 1;
    private Color playerColor;
    private bool isGhost;
    private bool playConstructionAnimationOnStart;

    public Bounds SelectionBounds => meshRenderer.bounds;

    private bool isSelected = false;

    public bool IsSelected {
        get => isSelected;
        set { isSelected = value; }
    }

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
    
    public void SetPlayConstructionAnimationOnStart(bool value) {
        playConstructionAnimationOnStart = value;
        isReady = !playConstructionAnimationOnStart;
    }

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

    public Color PlayerColor {
        get => playerColor;
        set {
            playerColor = value;
            EnsureDynamicMaterialsAreSetUp();
            foreach (var (_, _, material) in dynamicMaterials)
                material.SetColor("_BaseColor", playerColor);
        }
    }

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

    private IEnumerator constructionAnimationCoroutine;

    private void Start() {
        if (playConstructionAnimationOnStart) {
            constructionAnimationCoroutine = ConstructionAnimation();
            StartCoroutine(constructionAnimationCoroutine);
        }
    }

    private IEnumerator ConstructionAnimation() {
        var startScale = transform.localScale;
        var elapsed = 0f;
        var duration = 1f;
        while (elapsed < duration) {
            var t = elapsed / duration;
            var scale = startScale;
            scale.y = t;
            transform.localScale = scale;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = startScale;
        isReady = true;
    }
}