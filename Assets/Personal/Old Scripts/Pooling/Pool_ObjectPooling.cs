using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

[DisallowMultipleComponent]
public class Pool_ObjectPooling : MonoBehaviour
{
    [Title("Default")]
    public Idle_CardUI singlePrefab;
    public Transform pooledObjectsRoot;
    [Min(0)] public int prewarmCount = 0;

    readonly Dictionary<string, Stack<Idle_CardUI>> poolByKey = new();
    readonly Dictionary<string, Idle_CardUI> prefabByKey = new();
    readonly Dictionary<Idle_CardUI, string> keyByInstance = new();

    void Awake()
    {
        Prewarm(singlePrefab, prewarmCount);
    }

    public Idle_CardUI Spawn(Idle_CardUI prefabOverride = null, Transform parent = null, string poolKey = null)
    {
        Idle_CardUI prefab = prefabOverride != null ? prefabOverride : singlePrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"{nameof(Pool_ObjectPooling)} has no prefab to spawn.", this);
            return null;
        }

        string resolvedPoolKey = BuildPoolKey(prefab, poolKey);
        Stack<Idle_CardUI> stack = GetOrCreateStack(resolvedPoolKey, prefab);
        Idle_CardUI instance = null;

        while (stack.Count > 0 && instance == null)
        {
            instance = stack.Pop();
        }

        if (instance == null)
        {
            Transform instantiateParent = pooledObjectsRoot != null ? pooledObjectsRoot : transform;
            instance = Instantiate(prefab, instantiateParent);
        }

        keyByInstance[instance] = resolvedPoolKey;
        instance.gameObject.SetActive(true);
        instance.transform.SetParent(parent != null ? parent : transform, false);
        return instance;
    }

    public void Return(Idle_CardUI instance)
    {
        if (instance == null)
        {
            return;
        }

        instance.Unbind();
        instance.gameObject.SetActive(false);
        instance.transform.SetParent(pooledObjectsRoot != null ? pooledObjectsRoot : transform, false);

        if (!keyByInstance.TryGetValue(instance, out string poolKey) || string.IsNullOrWhiteSpace(poolKey))
        {
            Destroy(instance.gameObject);
            return;
        }

        if (!prefabByKey.ContainsKey(poolKey))
        {
            Destroy(instance.gameObject);
            return;
        }

        GetOrCreateStack(poolKey, prefabByKey[poolKey]).Push(instance);
    }

    public void Prewarm(Idle_CardUI prefab, int count, string poolKey = null)
    {
        if (prefab == null || count <= 0)
        {
            return;
        }

        string resolvedPoolKey = BuildPoolKey(prefab, poolKey);
        Stack<Idle_CardUI> stack = GetOrCreateStack(resolvedPoolKey, prefab);
        Transform instantiateParent = pooledObjectsRoot != null ? pooledObjectsRoot : transform;

        for (int i = stack.Count; i < count; i++)
        {
            Idle_CardUI instance = Instantiate(prefab, instantiateParent);
            instance.gameObject.SetActive(false);
            keyByInstance[instance] = resolvedPoolKey;
            stack.Push(instance);
        }
    }

    Stack<Idle_CardUI> GetOrCreateStack(string poolKey, Idle_CardUI prefab)
    {
        if (!poolByKey.TryGetValue(poolKey, out Stack<Idle_CardUI> stack))
        {
            stack = new Stack<Idle_CardUI>();
            poolByKey[poolKey] = stack;
        }

        prefabByKey[poolKey] = prefab;
        return stack;
    }

    static string BuildPoolKey(Idle_CardUI prefab, string poolKey)
    {
        string scope = string.IsNullOrWhiteSpace(poolKey) ? "default" : poolKey.Trim();
        return $"{prefab.GetInstanceID()}::{scope}";
    }
}
