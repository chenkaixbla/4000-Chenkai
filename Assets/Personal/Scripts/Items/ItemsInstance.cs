using UnityEngine;

[System.Serializable]
public class ItemsInstance
{
    public ItemsData itemData;
    public int quantity;

    public void SetItem(ItemsData data, int qty = 1)
    {
        itemData = data;
        quantity = qty;
    }

    public void AddQuantity(int amount)
    {
        quantity += amount;
        if(quantity <= 0)
        {
            quantity = 0;
        }
    }
}
