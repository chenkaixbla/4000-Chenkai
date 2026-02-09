using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public List<ItemsInstance> itemInstances = new();
    public List<ItemsData> allItems = new();   // List of all items in the game, used for reference when adding items to the inventory

    void Awake()
    {
        Instance = this;
    }

    public ItemsInstance AddItem(int itemID, int quantity = 1)
    {
        ItemsData itemData = GetItem(itemID);
        if (itemData != null)
        {
            return AddItem(itemData, quantity);
        }
        else
        {
            Debug.LogWarning($"Item with ID {itemID} not found in allItems list.");
        }
        
        return null;
    }

    public ItemsInstance AddItem(ItemsData itemData, int quantity = 1)
    {
        ItemsInstance inst = itemInstances.Find(instance => instance.itemData == itemData);

        if (inst != null)
        {
            inst.AddQuantity(quantity);
        }
        else
        {
            inst = new ItemsInstance();
            inst.SetItem(itemData, quantity);
            itemInstances.Add(inst);
        }

        return inst;
    }

    public bool RemoveItem(int itemID, out ItemsData itemData, int quantity = 1)
    {
        ItemsInstance inst = GetInstance(itemID);
        itemData = null;

        if (inst != null)
        {
            return RemoveItem(inst, out itemData, quantity);
        }
        else
        {
            Debug.LogWarning($"Item with ID {itemID} not found in inventory.");
        }

        return false;
    }

    public bool RemoveItem(ItemsInstance itemInstance, out ItemsData itemData, int quantity = 1)
    {
        itemData = null;

        if (itemInstance != null)
        {
            if (itemInstance.quantity >= quantity)
            {
                itemData = itemInstance.itemData;
                itemInstance.AddQuantity(-quantity);

                if (itemInstance.quantity <= 0)
                {
                    itemInstances.Remove(itemInstance);
                }
                return true;
            }
            else
            {
                Debug.LogWarning($"Not enough quantity of {itemInstance.itemData.displayName} to remove. Requested: {quantity}, Available: {itemInstance.quantity}");
            }
        }
        else
        {
            Debug.LogWarning("Item instance is null.");
        }

        return false;
    }

    public ItemsData GetItem(int id)
    {
        return allItems.Find(item => item.itemID == id);
    }

    public ItemsInstance GetInstance(int id)
    {
        return itemInstances.Find(instance => instance.itemData.itemID == id);
    }
}
