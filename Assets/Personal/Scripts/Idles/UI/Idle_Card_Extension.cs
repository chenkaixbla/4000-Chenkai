using UnityEngine;

/// <summary>
/// Base class for a special idle card's custom visuals. Add a subclass of this to a
/// special card prefab (alongside the base <see cref="Idle_Card"/>) and drag in the
/// extra slot references it needs - e.g. a crafting card's input/output slots.
///
/// The base <see cref="Idle_Card"/> auto-discovers every extension on its prefab and
/// forwards the bind/refresh/unbind lifecycle here, so you never wire these calls by
/// hand. You only write a subclass when a job's idles need formatting the basic card
/// doesn't cover; basic cards need no extension at all.
/// </summary>
public abstract class Idle_Card_Extension : MonoBehaviour
{
    /// <summary>Called when a runtime is bound to the card. Cache/setup here.</summary>
    public virtual void OnBind(Idle_Runtime runtime) { }

    /// <summary>Called whenever the card refreshes. Update your custom slots here.</summary>
    public virtual void OnRefresh(Idle_Runtime runtime) { }

    /// <summary>Called when the card is unbound (e.g. returned to the pool). Clear here.</summary>
    public virtual void OnUnbind() { }
}
