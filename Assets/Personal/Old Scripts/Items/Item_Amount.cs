using System;
using UnityEngine;

[Serializable]
public class Item_Amount
{
    public ItemsData itemData;
    [Min(1)] public int quantity = 1;

    public int SafeQuantity => Mathf.Max(1, quantity);
}
