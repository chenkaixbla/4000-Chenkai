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

/// <summary>Whether a crafting reward targets a job or an idle.</summary>
public enum Crafting_RewardTarget { Job, Idle }

/// <summary>What a crafting reward grants: raw XP, or whole levels.</summary>
public enum Crafting_RewardGrant { XP, Level }

/// <summary>
/// A bonus a crafting idle grants: XP or whole levels awarded to a specific job or idle. Distinct
/// from the generic <see cref="Reward"/> - this one always targets a chosen job/idle's progression.
/// Granting is runtime work (not built yet); this is the data definition only.
/// </summary>
[Serializable]
public class Crafting_Reward
{
    [Tooltip("Whether this reward feeds a job's or an idle's progression.")]
    public Crafting_RewardTarget target = Crafting_RewardTarget.Job;

    [ShowField(nameof(IsJob))]
    public Job_Data job;

    [ShowField(nameof(IsIdle))]
    public Idle_Data idle;

    [Tooltip("Grant raw XP, or whole levels.")]
    public Crafting_RewardGrant grant = Crafting_RewardGrant.XP;

    [Min(1)]
    [Tooltip("XP points, or number of levels - depending on Grant.")]
    public int amount = 1;

    public bool IsJob => target == Crafting_RewardTarget.Job;
    public bool IsIdle => target == Crafting_RewardTarget.Idle;
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
    [Tooltip("Bonus rewards per craft: XP or whole levels for a specific job or idle.")]
    public List<Crafting_Reward> craftingRewards = new();

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
