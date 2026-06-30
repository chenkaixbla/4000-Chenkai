using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

/// <summary>An item plus a quantity. Used by crafting idle data for inputs/outputs.</summary>
[Serializable]
public class Idle_ItemStack
{
    public ItemsData item;
    [Min(1)] public int amount = 1;
}

/// <summary>
/// A crafting idle: the base <see cref="Idle_Data"/> plus what it produces and what it
/// requires. Because it IS an Idle_Data, you add a Idle_Data_Crafting asset straight into a
/// job's <c>idleDatas</c> list like any other idle. The crafting card
/// (<see cref="Idle_Card_Crafting"/>) reads these two lists to fill its Produces / Required
/// sections.
///
/// TODO: amounts here are the recipe; "You Have" / consuming / granting needs the inventory
/// and reward-granting systems (not built yet) - revisit when those exist.
/// </summary>
[CreateAssetMenu(fileName = "Idle_Data_Crafting", menuName = "Game/Idles/Idle_Data_Crafting")]
public class Idle_Data_Crafting : Idle_Data
{
    [Title("Crafting")]
    [Tooltip("Items this idle produces each cycle.")]
    public List<Idle_ItemStack> produces = new();
    [Tooltip("Items required (consumed) to run a cycle.")]
    public List<Idle_ItemStack> required = new();

    // A crafting idle is always Crafting kind - lock the field and keep it pinned.
    protected override bool LockIdleKind => true;

    protected override void OnEnable()
    {
        base.OnEnable();
        idleKind = Idle_Kind.Crafting;
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        idleKind = Idle_Kind.Crafting;
    }
}
