using EditorAttributes;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public InventoryManager inventory;
    public CurrencyManager currency;

    [Button]
    public void SellItem(int id, int quantity = 1)
    {
        if(inventory.RemoveItem(id, out ItemsData itemData, quantity))
        {
            currency.AddCurrency(itemData.price * quantity);
        }
    }
}
