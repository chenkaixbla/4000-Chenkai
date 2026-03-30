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

    public override IdleFinishActionType ActionType => IdleFinishActionType.GiveItem;

    public override void Apply(IdleInstance idleInstance)
    {
        if (itemsData == null || quantity <= 0)
        {
            return;
        }

        float effectiveDropChance = Mathf.Clamp01(baseDropChance * CombatManager.GetIdleItemDropChanceMultiplier());
        if (UnityEngine.Random.value > effectiveDropChance)
        {
            return;
        }

        InventoryManager.Instance?.AddItem(itemsData.itemID, quantity);
        Debug.Log($"Granted item: {itemsData.displayName} x{quantity}");
    }
}

[System.Serializable]
public class FinishAction_GiveStat : FinishAction
{
    public int attackDamageOffset;

    public override IdleFinishActionType ActionType => IdleFinishActionType.GiveStat;

    public override void Apply(IdleInstance idleInstance)
    {
        Debug.Log($"Granted stat:\nChanged Attack: {attackDamageOffset}");
    }
}
