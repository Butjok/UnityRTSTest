using UnityEngine;

public class Player : WorldBehaviour {
    
    public int id = -1;
    public PlayerController controller;
    [SerializeField] private Color color;
    [SerializeField] private int credits;

    public Color Color {
        get => color;
        set {
            color = value;
        }
    }

    private void Awake() {
        var behaviours = FindObjectsByType<PrePlacedPlayerProperty>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var behaviour in behaviours) {
            if (behaviour.PlayerId == id) {
                var building = behaviour.GetComponent<Building>();
                if (building)
                    building.OwningPlayer = this;
                var unit = behaviour.GetComponent<Unit>();
                if (unit)
                    unit.OwningPlayer = this;
            }
        }
    }
}