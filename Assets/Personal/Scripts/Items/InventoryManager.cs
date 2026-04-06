using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    public event System.Action OnInventoryChanged;

    public List<ItemsInstance> itemInstances = new();
    [SerializeField] bool enableVerboseLogging = true;
    int _addedOrderCounter = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            LogVerboseWarning($"Replacing previous InventoryManager singleton. Previous instance id: {Instance.GetInstanceID()}, new instance id: {GetInstanceID()}.");
        }

        Instance = this;
        LogVerbose($"Awake complete. tracked inventory entries: {itemInstances.Count}.");
    }

    public ItemsInstance AddItem(ItemsData itemData, int quantity = 1)
    {
        if (itemData == null)
        {
            LogVerboseWarning("AddItem failed: itemData is null.");
            return null;
        }

        if (quantity <= 0)
        {
            LogVerboseWarning($"AddItem ignored: non-positive quantity {quantity} for item {itemData.displayName}.");
            return null;
        }

        int previousQuantity = GetQuantity(itemData);
        ItemsInstance inst = itemInstances.Find(instance => instance.itemData == itemData);

        if (inst != null)
        {
            inst.AddQuantity(quantity);
        }
        else
        {
            inst = new ItemsInstance();
            inst.SetItem(itemData, quantity);
            inst.addedOrder = _addedOrderCounter++;
            itemInstances.Add(inst);
        }

        int newQuantity = GetQuantity(itemData);
        LogVerbose($"AddItem success: {itemData.displayName} +{quantity}. Previous quantity: {previousQuantity}, new quantity: {newQuantity}. Tracked entries: {itemInstances.Count}.");
        NotifyInventoryChanged();
        return inst;
    }

    public bool RemoveItem(int itemID, out ItemsData itemData, int quantity = 1)
    {
        LogVerbose($"RemoveItem by id requested. itemID: {itemID}, quantity: {quantity}.");
        ItemsInstance inst = GetInstance(itemID);
        itemData = null;

        if (inst != null)
        {
            return RemoveItem(inst, out itemData, quantity);
        }
        else
        {
            LogVerboseWarning($"RemoveItem failed: item id {itemID} not found in inventory.");
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
                int previousQuantity = itemInstance.quantity;
                itemInstance.AddQuantity(-quantity);

                if (itemInstance.quantity <= 0)
                {
                    itemInstances.Remove(itemInstance);
                }

                int newQuantity = itemData != null ? GetQuantity(itemData) : 0;
                LogVerbose($"RemoveItem success: {itemData?.displayName ?? "Unknown"} -{quantity}. Previous quantity: {previousQuantity}, new quantity: {newQuantity}. Tracked entries: {itemInstances.Count}.");

                NotifyInventoryChanged();
                return true;
            }
            else
            {
                LogVerboseWarning($"RemoveItem failed: not enough quantity for {itemInstance.itemData.displayName}. Requested: {quantity}, available: {itemInstance.quantity}.");
            }
        }
        else
        {
            LogVerboseWarning("RemoveItem failed: itemInstance is null.");
        }

        return false;
    }

    public ItemsInstance GetInstance(int id)
    {
        return itemInstances.Find(instance => instance.itemData != null && instance.itemData.itemID == id);
    }

    public int GetQuantity(ItemsData itemData)
    {
        if (itemData == null)
        {
            return 0;
        }

        ItemsInstance itemInstance = itemInstances.Find(instance => instance.itemData == itemData);
        return itemInstance != null ? itemInstance.quantity : 0;
    }

    public bool HasItem(ItemsData itemData, int quantity = 1)
    {
        return GetQuantity(itemData) >= Mathf.Max(0, quantity);
    }

    public IReadOnlyList<ItemsInstance> GetTrackedItems()
    {
        return itemInstances;
    }

    void NotifyInventoryChanged()
    {
        LogVerbose($"OnInventoryChanged invoked. Tracked entries: {itemInstances.Count}.");
        OnInventoryChanged?.Invoke();
    }

    void LogVerbose(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.Log("InventoryManager", message);
    }

    void LogVerboseWarning(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.LogWarning("InventoryManager", message);
    }
}
