using UnityEngine;

public enum IdleFinishActionType
{
    GiveItem,
    GiveStat
}

[System.Serializable]
public abstract class FinishAction
{
    public abstract IdleFinishActionType ActionType { get; }

    public abstract void Apply(Idle_Instance idleInstance, InventoryManager inventory = null);
}

[System.Serializable]
public class FinishAction_GiveItem : FinishAction
{
    public ItemsData itemData;
    [Min(1)] public int quantity = 1;
    [Range(0f, 1f)] public float baseDropChance = 1f;
    public bool enableVerboseLogging = true;

    public override IdleFinishActionType ActionType => IdleFinishActionType.GiveItem;

    public override void Apply(Idle_Instance idleInstance, InventoryManager inventory = null)
    {
        if (itemData == null)
        {
            return;
        }

        float effectiveDropChance = Mathf.Clamp01(baseDropChance * CombatManager.GetIdleItemDropChanceMultiplier());
        if (Random.value > effectiveDropChance)
        {
            LogVerbose($"GiveItem roll failed. Item: {itemData.displayName}, chance: {effectiveDropChance:0.###}.");
            return;
        }

        InventoryManager resolvedInventory = inventory != null ? inventory : InventoryManager.Instance;
        if (resolvedInventory == null)
        {
            LogVerboseWarning($"GiveItem skipped for '{itemData.displayName}' because InventoryManager was not found.");
            return;
        }

        resolvedInventory.AddItem(itemData, Mathf.Max(1, quantity));
        LogVerbose($"GiveItem success. Item: {itemData.displayName}, qty: {Mathf.Max(1, quantity)}.");
    }

    void LogVerbose(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.Log("IdleFinishAction", message);
    }

    void LogVerboseWarning(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.LogWarning("IdleFinishAction", message);
    }
}

[System.Serializable]
public class FinishAction_GiveStat : FinishAction
{
    public int attackDamageOffset;
    public bool enableVerboseLogging = true;

    public override IdleFinishActionType ActionType => IdleFinishActionType.GiveStat;

    public override void Apply(Idle_Instance idleInstance, InventoryManager inventory = null)
    {
        if (enableVerboseLogging)
        {
            VerboseProjectLogger.Log("IdleFinishAction", $"GiveStat applied. Changed Attack: {attackDamageOffset}.");
        }
    }
}
