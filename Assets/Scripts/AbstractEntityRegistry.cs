using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractEntityRegistry<T> : WorldSubsystem {

    private List<T> entities = new();

    public virtual IEnumerable<T> Entities => entities;

    protected virtual void Start() {
        var objects = FindObjectsByType<Object>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var obj in objects)
            if (obj is T entity)
                entities.Add(entity);

        world.onObjectSpawned += AddUnit;
        world.onObjectDestroyed += RemoveUnit;
    }

    protected virtual void OnDestroy() {
        world.onObjectSpawned -= AddUnit;
        world.onObjectDestroyed -= RemoveUnit;
    }

    private void AddUnit(Object obj) {
        if (obj is T entity)
            entities.Add(entity);
    }

    private void RemoveUnit(Object obj) {
        if (obj is T entity)
            entities.Remove(entity);
    }
}