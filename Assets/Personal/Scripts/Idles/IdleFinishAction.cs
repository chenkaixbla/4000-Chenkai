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

    public override IdleFinishActionType ActionType => IdleFinishActionType.GiveItem;

    public override void Apply(IdleInstance idleInstance)
    {
        if (itemsData == null || quantity <= 0)
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
