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
            var unitsRegistry = world.GetSubsystem<UnitsRegistry>();
            if (unitsRegistry)
                foreach (var unit in unitsRegistry.Entities)
                    if (unit.OwningPlayer == this)
                        unit.PlayerColor = color;
            var buildingsRegistry = world.GetSubsystem<BuildingsRegistry>();
            if (buildingsRegistry)
                foreach (var building in buildingsRegistry.Entities)
                    if (building.OwningPlayer == this)
                        building.PlayerColor = color;
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