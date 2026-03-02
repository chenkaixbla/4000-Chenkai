using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public class IdleManager : MonoBehaviour
{
    [ReadOnly, SerializeField] List<IdleInstance> idleInstances = new();

    public void AddInstance(IdleInstance instance)
    {
        if (instance != null && !idleInstances.Contains(instance))
        {
            idleInstances.Add(instance);
        }
    }

    void Update()
    {
        foreach (var instance in idleInstances)
        {
            instance?.DoUpdate();
        }
    }
}
