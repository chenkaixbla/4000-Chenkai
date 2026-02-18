using System.Collections.Generic;
using UnityEngine;

public class IdleManager : MonoBehaviour
{
    public List<IdleInstance> idleInstances = new();

    void Update()
    {
        foreach (var instance in idleInstances)
        {
            instance?.DoUpdate();
        }
    }
}
