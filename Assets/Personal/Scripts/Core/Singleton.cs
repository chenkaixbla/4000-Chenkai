using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Base class for scene-wide single-instance managers. Exposes a static <see cref="Instance"/>,
/// enforces one-per-scene at runtime, and - in the editor - refuses to be added twice: drop a
/// second copy in the scene and it pops a warning dialog naming the object that already has one,
/// then removes itself.
///
/// Usage: <c>public class Foo_Manager : Singleton&lt;Foo_Manager&gt; { ... }</c>. If a subclass
/// needs its own Awake, override it, call <c>base.Awake()</c> first, then
/// <c>if (Instance != this) return;</c> before doing its own setup.
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[{typeof(T).Name}] Duplicate on '{name}' destroyed - '{Instance.name}' already has one.", this);
            Destroy(this);
            return;
        }

        Instance = (T)this;
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

#if UNITY_EDITOR
    // Called when the component is first added in the editor. Block a second copy.
    protected virtual void Reset()
    {
        T existing = FindExistingInstance();
        if (existing == null || existing == this)
            return;

        EditorUtility.DisplayDialog(
            $"{typeof(T).Name} already exists",
            $"A {typeof(T).Name} is already in the scene on '{existing.name}'.\n\n" +
            "Only one is allowed per scene, so this one will be removed.",
            "OK");

        // Can't DestroyImmediate inside Reset; defer one tick.
        EditorApplication.delayCall += () =>
        {
            if (this != null)
                DestroyImmediate(this);
        };
    }

    T FindExistingInstance()
    {
        T[] all = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != this)
                return all[i];
        }

        return null;
    }
#endif
}
