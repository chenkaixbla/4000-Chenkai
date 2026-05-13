using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public class Idle_Manager : MonoBehaviour
{
    [ReadOnly, SerializeField] List<Idle_Instance> idleInstances = new();

    public void AddInstance(Idle_Instance instance)
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
