using System;
using UnityEngine;

[System.Serializable]
public class IdleInstance
{
    public IdleData idleData;
    public float timer;
    public int currentXP;
    public int level;

    public Action OnUpdate;

    public IdleInstance(IdleData data)
    {
        Initialize(data);
    }

    public void Initialize(IdleData data)
    {
        idleData = data;

        if (idleData != null)
        {
            timer = 0;
        }
    }

    public void DoUpdate()
    {
        if (idleData == null) return;

        timer += Time.deltaTime;
        if (timer >= idleData.interval)
        {
            GrantReward();
            CheckLevel();

            timer = 0;

            // Call the onTimerFinish action if it's set
            idleData.onTimerFinish?.Invoke();
        }

        OnUpdate?.Invoke();
    }

    void GrantReward()
    {
        currentXP += idleData.xpReward;

        // Here you would also grant the actual reward based on the finishType and finishAction
        if(idleData.finishType == IdleFinishActionType.GiveItem)
        {
            FinishAction_GiveItem itemAction = idleData.finishAction as FinishAction_GiveItem;
            if(itemAction != null)
            {
                // Grant the item to the player
                InventoryManager.Instance.AddItem(itemAction.itemsData.itemID, itemAction.quantity);

                Debug.Log($"Granted item: {itemAction.itemsData.displayName} x{itemAction.quantity}");
            }
        }
        else if(idleData.finishType == IdleFinishActionType.GiveStat)
        {
            FinishAction_GiveStat statAction = idleData.finishAction as FinishAction_GiveStat;
            if(statAction != null)
            {
                // Grant the stat boost to the player
                Debug.Log($"Granted stat:\nChanged Attack: {statAction.attackDamageOffset}");
            }
        }
    }
    
    void CheckLevel()
    {
        if(currentXP >= idleData.maxXP)
        {
            currentXP = idleData.maxXP;
            level++;
            
            currentXP = 0;
        }
    }
}
