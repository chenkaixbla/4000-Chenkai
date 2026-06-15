using System;
using EditorAttributes;
using UnityEngine;

/// <summary>What a single reward grants.</summary>
public enum Reward_Type
{
    Coins,
    Item,
    XP,
    UnlockJob,
    UnlockIdle
}

/// <summary>
/// One reward entry, reused anywhere something can pay out (idle cycle, job, monster defeat).
/// Only the fields relevant to the chosen <see cref="type"/> are shown in the inspector.
///
/// For <see cref="Reward_Type.UnlockIdle"/> you just drag the Idle_Data - the runtime finds
/// which job owns it, so there's no need to also pick the job.
/// </summary>
[Serializable]
public class Reward
{
    public Reward_Type type = Reward_Type.Coins;

    [Range(0f, 100f)]
    [Tooltip("Chance to receive this reward, as a percent (100 = always).")]
    public float chance = 100f;

    [ShowField(nameof(IsCoins))]
    [Min(0)] public int coins;

    [ShowField(nameof(IsItem))]
    public ItemsData item;
    [ShowField(nameof(IsItem))]
    [Min(1)] public int itemAmount = 1;

    [ShowField(nameof(IsXP))]
    [Min(0)] public int xp;

    [ShowField(nameof(IsUnlockJob))]
    public Job_Data unlockJob;

    [ShowField(nameof(IsUnlockIdle))]
    [Tooltip("Drag the idle to unlock - the game auto-finds which job owns it.")]
    public Idle_Data unlockIdle;

    /// <summary>Rolls this reward's <see cref="chance"/>. True if it should be granted this time.</summary>
    public bool Rolls() => chance >= 100f || UnityEngine.Random.value * 100f < chance;

    // Condition flags for the conditional inspector display above (also handy at runtime).
    public bool IsCoins => type == Reward_Type.Coins;
    public bool IsItem => type == Reward_Type.Item;
    public bool IsXP => type == Reward_Type.XP;
    public bool IsUnlockJob => type == Reward_Type.UnlockJob;
    public bool IsUnlockIdle => type == Reward_Type.UnlockIdle;
}

/// <summary>When a leveled reward pays out.</summary>
public enum Reward_Trigger
{
    OnEachLevel,
    AtLevel
}

/// <summary>
/// A <see cref="Reward"/> gated by a level trigger, used by jobs and idles. Granted either on
/// every level gained (<see cref="Reward_Trigger.OnEachLevel"/>) or once when a specific level
/// is reached (<see cref="Reward_Trigger.AtLevel"/>). The level checked is the owner's level
/// (the job's level for job rewards, the idle's level for idle rewards).
/// </summary>
[Serializable]
public class Reward_Leveled
{
    [Tooltip("OnEachLevel = every level up. AtLevel = once, when a specific level is reached.")]
    public Reward_Trigger trigger = Reward_Trigger.OnEachLevel;

    [ShowField(nameof(IsAtLevel))]
    [Min(1)] public int level = 2;

    public Reward reward = new();

    public bool IsAtLevel => trigger == Reward_Trigger.AtLevel;

    /// <summary>True if this reward should be granted for the level that was just reached.</summary>
    public bool ShouldGrant(int reachedLevel)
        => trigger == Reward_Trigger.OnEachLevel || reachedLevel == level;
}
