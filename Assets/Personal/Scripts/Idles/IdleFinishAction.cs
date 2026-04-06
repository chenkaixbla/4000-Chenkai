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

    public abstract void Apply(IdleInstance idleInstance);
}

[System.Serializable]
public class FinishAction_GiveItem : FinishAction
{
    public ItemsData itemsData;
    public int quantity;
    [Range(0f, 1f)] public float baseDropChance = 1f;
    public bool enableVerboseLogging = true;

    public override IdleFinishActionType ActionType => IdleFinishActionType.GiveItem;

    public override void Apply(IdleInstance idleInstance)
    {
        if (itemsData == null || quantity <= 0)
        {
            LogVerboseWarning($"GiveItem skipped. itemsData null: {itemsData == null}, quantity: {quantity}.");
            return;
        }

        float effectiveDropChance = Mathf.Clamp01(baseDropChance * CombatManager.GetIdleItemDropChanceMultiplier());
        if (UnityEngine.Random.value > effectiveDropChance)
        {
            LogVerbose($"GiveItem roll failed. Item: {itemsData.displayName}, quantity: {quantity}, effective drop chance: {effectiveDropChance:0.###}.");
            return;
        }

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            LogVerboseError($"GiveItem failed because InventoryManager.Instance is null. Item: {itemsData.displayName}, quantity: {quantity}.");
            return;
        }

        // Grant directly from the action's assigned asset to keep reward flow data-driven.
        ItemsInstance added = inventory.AddItem(itemsData, quantity);
        if (added == null)
        {
            LogVerboseError($"GiveItem failed during AddItem. Item: {itemsData.displayName}, itemID: {itemsData.itemID}, quantity: {quantity}.");
            return;
        }

        int finalQuantity = inventory.GetQuantity(itemsData);
        LogVerbose($"GiveItem success. Granted {itemsData.displayName} x{quantity}. Final inventory quantity: {finalQuantity}.");
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

    void LogVerboseError(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.LogError("IdleFinishAction", message);
    }
}

[System.Serializable]
public class FinishAction_GiveStat : FinishAction
{
    public int attackDamageOffset;
    public bool enableVerboseLogging = true;

    public override IdleFinishActionType ActionType => IdleFinishActionType.GiveStat;

    public override void Apply(IdleInstance idleInstance)
    {
        if (enableVerboseLogging)
        {
            VerboseProjectLogger.Log("IdleFinishAction", $"GiveStat applied. Changed Attack: {attackDamageOffset}.");
        }
    }
}
