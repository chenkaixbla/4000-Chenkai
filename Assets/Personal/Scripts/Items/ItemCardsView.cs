using System.Collections.Generic;
using UnityEngine;

public class ItemCardsView : MonoBehaviour
{
    public InventoryPageController inventoryPage;
    public Transform cardContainer;
    public ItemCard itemCardPrefab;

    readonly List<ItemCard> activeCards = new();
    readonly List<ItemCard> pooledCards = new();

    public void Rebuild(IReadOnlyList<ItemsInstance> itemInstances, System.Action<ItemsData> onItemSelected)
    {
        ClearActiveCards();

        if (itemInstances == null || cardContainer == null || itemCardPrefab == null)
        {
            return;
        }

        for (int i = 0; i < itemInstances.Count; i++)
        {
            ItemsInstance itemInstance = itemInstances[i];
            if (itemInstance == null || itemInstance.itemData == null || itemInstance.quantity <= 0)
            {
                continue;
            }

            ItemCard card = GetCardFromPool();
            if (card == null)
            {
                continue;
            }

            card.transform.SetParent(cardContainer, false);
            card.Bind(itemInstance.itemData, itemInstance.quantity, onItemSelected);
            activeCards.Add(card);
        }
    }

    public void FilterByCategory(Catalog_ItemType category)
    {
        for (int i = 0; i < activeCards.Count; i++)
        {
            ItemCard card = activeCards[i];
            if (card == null)
            {
                continue;
            }

            bool shouldShow = card.itemData != null && (card.itemData.itemType == category || category == Catalog_ItemType.None);
            card.gameObject.SetActive(shouldShow);
        }
    }

    ItemCard GetCardFromPool()
    {
        while (pooledCards.Count > 0)
        {
            int lastIndex = pooledCards.Count - 1;
            ItemCard card = pooledCards[lastIndex];
            pooledCards.RemoveAt(lastIndex);

            if (card == null)
            {
                continue;
            }

            card.gameObject.SetActive(inventoryPage != null && 
                            (inventoryPage.currentCategory == Catalog_ItemType.None || 
                            inventoryPage.currentCategory == card.itemData.itemType));
            return card;
        }

        return itemCardPrefab != null ? Instantiate(itemCardPrefab) : null;
    }

        public IReadOnlyList<ItemCard> GetActiveCards() => activeCards;

        public void ApplySellMode(bool active, HashSet<ItemsData> selectedItems = null)
        {
            for (int i = 0; i < activeCards.Count; i++)
            {
                ItemCard card = activeCards[i];
                if (card == null) continue;

                card.SetSellMode(active);
                if (active && selectedItems != null && card.itemData != null && selectedItems.Contains(card.itemData))
                {
                    card.SetSellSelected(true);
                }
            }
        }

        void ClearActiveCards()
    {
        for (int i = activeCards.Count - 1; i >= 0; i--)
        {
            ItemCard card = activeCards[i];
            if (card == null)
            {
                continue;
            }

            card.gameObject.SetActive(false);
            pooledCards.Add(card);
        }

        activeCards.Clear();
    }


}
