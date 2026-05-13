using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

[DisallowMultipleComponent]
public class Object_Pooling : MonoBehaviour
{
    public GameObject pooledPrefab;
    public Transform pooledParent;
    [Min(0)] public int initialPoolSize = 0;
    public bool prewarmOnAwake = true;

    readonly List<GameObject> pooledObjects = new();

    void Awake()
    {
        if (prewarmOnAwake)
        {
            EnsurePoolSize(initialPoolSize);
        }
    }

    public GameObject GetPooledObject()
    {
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            GameObject pooledObject = pooledObjects[i];
            if (pooledObject == null || pooledObject.activeInHierarchy)
            {
                continue;
            }

            pooledObject.transform.SetParent(GetPoolParent(), false);
            pooledObject.SetActive(true);
            return pooledObject;
        }

        GameObject created = CreatePooledObject();
        if (created != null)
        {
            created.SetActive(true);
        }

        return created;
    }

    public void ReturnToPool(GameObject pooledObject)
    {
        if (pooledObject == null)
        {
            return;
        }

        pooledObject.SetActive(false);
        pooledObject.transform.SetParent(GetPoolParent(), false);

        if (!pooledObjects.Contains(pooledObject))
        {
            pooledObjects.Add(pooledObject);
        }
    }

    public void ReturnAllToPool()
    {
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            GameObject pooledObject = pooledObjects[i];
            if (pooledObject == null)
            {
                continue;
            }

            pooledObject.SetActive(false);
            pooledObject.transform.SetParent(GetPoolParent(), false);
        }
    }

    public void ShowSpecificAmount(int amount)
    {
        amount = Mathf.Max(0, amount);
        EnsurePoolSize(amount);

        for (int i = 0; i < pooledObjects.Count; i++)
        {
            GameObject pooledObject = pooledObjects[i];
            if (pooledObject == null)
            {
                continue;
            }

            pooledObject.transform.SetParent(GetPoolParent(), false);
            pooledObject.SetActive(i < amount);
        }
    }

    void EnsurePoolSize(int requiredAmount)
    {
        if (pooledPrefab == null)
        {
            Debug.LogWarning("Object_Pooling requires a pooled prefab reference.", this);
            return;
        }

        pooledObjects.RemoveAll(item => item == null);
        while (pooledObjects.Count < requiredAmount)
        {
            CreatePooledObject();
        }
    }

    GameObject CreatePooledObject()
    {
        if (pooledPrefab == null)
        {
            return null;
        }

        GameObject pooledObject = Instantiate(pooledPrefab, GetPoolParent(), false);
        pooledObject.SetActive(false);
        pooledObjects.Add(pooledObject);
        return pooledObject;
    }

    Transform GetPoolParent()
    {
        return pooledParent != null ? pooledParent : transform;
    }
}
