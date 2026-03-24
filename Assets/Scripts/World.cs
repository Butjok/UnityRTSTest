using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class World : MonoBehaviour {

    public List<Player> players = new();
    public PlayerController playerControllerPrefab;
    public PlayerController playerController;

    private readonly Dictionary<Type, WorldSubsystem> subsystems = new();

    public event Action<Object> onObjectSpawned;
    public event Action<Object> onObjectDestroyed;

    public void Awake() {
        
        var subsystemTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(WorldSubsystem)));
        
        foreach (var subsystemType in subsystemTypes) {
            if (subsystemType.IsAbstract)
                continue;
            var subsystemGameObject = new GameObject(subsystemType.Name);
            subsystemGameObject.SetActive(false);
            var subsystem = (WorldSubsystem)subsystemGameObject.AddComponent(subsystemType);
            Debug.Log($"Initialized subsystem of type {subsystemType.Name}");
            if (subsystem) {
                subsystem.world = this;
                subsystemGameObject.SetActive(true);
                subsystems[subsystemType] = subsystem;
            }
        }

        var player = Spawn<Player>(player => {
            player.id = 0;
            player.Color = Color.red;
        });
        players.Add(player);
        if (playerControllerPrefab) {
            playerController = Spawn(playerControllerPrefab, playerController => playerController.player = player);
            player.controller = playerController;
        }
        
        var prePlacedPlayerProperties = FindObjectsByType<PrePlacedPlayerProperty>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var prePlacedPlayerProperty in prePlacedPlayerProperties) {
            Player playerToAssign = null;
            foreach (var p in players)
                if (p.id == prePlacedPlayerProperty.PlayerId) {
                    playerToAssign = p;
                    break;
                }
            Debug.Assert(playerToAssign, $"Could not find player with ID {prePlacedPlayerProperty.PlayerId} to assign pre-placed property {prePlacedPlayerProperty.name} to.");
            var playerProperty = prePlacedPlayerProperty.Target as IPlayerProperty;
            Debug.Assert(playerProperty != null, $"Pre-placed player property {prePlacedPlayerProperty.name} does not implement IPlayerProperty.");
            prePlacedPlayerProperty.Target.world = this;
            playerProperty.OwningPlayer = playerToAssign;
            prePlacedPlayerProperty.gameObject.SetActive(true);
        }
    }
    
    public T GetSubsystem<T>() where T : WorldSubsystem {
        return subsystems.TryGetValue(typeof(T), out var subsystem) ? (T)subsystem : null;
    }
    
    public T Spawn<T>(Action<T> setup = null) where T : WorldBehaviour {
        var instance = new GameObject(typeof(T).Name).AddComponent<T>();
        instance.world = this;
        setup?.Invoke(instance);
        onObjectSpawned?.Invoke(instance);
        return instance;
    }
    public T Spawn<T>(T prefab, Action<T> setup = null) where T : WorldBehaviour {
        var wasPrefabActive = prefab.gameObject.activeSelf;
        prefab.gameObject.SetActive(false);
        var instance = Instantiate(prefab);
        instance.world = this;
        setup?.Invoke(instance);
        prefab.gameObject.SetActive(wasPrefabActive);
        instance.gameObject.SetActive(wasPrefabActive);
        onObjectSpawned?.Invoke(instance);
        return instance;
    }
    public void Destroy(WorldBehaviour obj) {
        onObjectDestroyed?.Invoke(obj);
        Object.Destroy(obj);
    }
}

public abstract class WorldBehaviour : MonoBehaviour {
    public World world;
}
public abstract class WorldSubsystem : WorldBehaviour { }