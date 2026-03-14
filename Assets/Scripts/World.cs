using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class World : MonoBehaviour {

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
            var subsystemGameObject = new GameObject(subsystemType.Name);
            subsystemGameObject.SetActive(false);
            var subsystem = (WorldSubsystem)subsystemGameObject.AddComponent(subsystemType);
            subsystem.world = this;
            subsystemGameObject.SetActive(true);
            subsystems[subsystemType] = subsystem;
        }

        if (playerControllerPrefab)
            playerController = Spawn(playerControllerPrefab);
    }
    
    public T GetSubsystem<T>() where T : WorldSubsystem {
        return subsystems.TryGetValue(typeof(T), out var subsystem) ? (T)subsystem : null;
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