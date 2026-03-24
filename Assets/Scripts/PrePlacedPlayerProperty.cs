using UnityEngine;

public class PrePlacedPlayerProperty : WorldBehaviour {
    [SerializeField] private WorldBehaviour target;
    [SerializeField] private int playerId = -1;
    
    public WorldBehaviour Target => target;
    public int PlayerId => playerId;
}