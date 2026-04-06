using System.Collections.Generic;
using UnityEngine;

public class ItemCardsView : MonoBehaviour
{
    [SerializeField] bool enableVerboseLogging = true;
    public InventoryPageController inventoryPage;
    public Transform cardContainer;
    public ItemCard itemCardPrefab;

    readonly List<ItemCard> activeCards = new();
    readonly List<ItemCard> pooledCards = new();

    public void Rebuild(IReadOnlyList<ItemsInstance> itemInstances, System.Action<ItemsData> onItemSelected)
    {
        LogVerbose($"Rebuild requested. Incoming item count: {itemInstances?.Count ?? 0}.");
        ClearActiveCards();

        if (itemInstances == null || cardContainer == null || itemCardPrefab == null)
        {
            LogVerboseWarning($"Rebuild aborted. itemInstances null: {itemInstances == null}, cardContainer null: {cardContainer == null}, itemCardPrefab null: {itemCardPrefab == null}.");
            return;
        }

        int skippedCount = 0;
        int createdCount = 0;
        for (int i = 0; i < itemInstances.Count; i++)
        {
            ItemsInstance itemInstance = itemInstances[i];
            if (itemInstance == null || itemInstance.itemData == null || itemInstance.quantity <= 0)
            {
                skippedCount++;
                continue;
            }

            ItemCard card = GetCardFromPool();
            if (card == null)
            {
                skippedCount++;
                continue;
            }

            card.transform.SetParent(cardContainer, false);
            card.Bind(itemInstance.itemData, itemInstance.quantity, onItemSelected);
            activeCards.Add(card);
            createdCount++;
        }

        LogVerbose($"Rebuild complete. Active cards: {activeCards.Count}, created/bound this pass: {createdCount}, skipped entries: {skippedCount}, pooled cards remaining: {pooledCards.Count}.");
    }

    public void FilterByCategory(Catalog_ItemType category)
    {
        int visibleCount = 0;
        int hiddenCount = 0;
        for (int i = 0; i < activeCards.Count; i++)
        {
            ItemCard card = activeCards[i];
            if (card == null)
            {
                continue;
            }

            bool shouldShow = card.itemData != null && (card.itemData.itemType == category || category == Catalog_ItemType.None);
            card.gameObject.SetActive(shouldShow);
            if (shouldShow)
            {
                visibleCount++;
            }
            else
            {
                hiddenCount++;
            }
        }

        LogVerbose($"Filter applied. Category: {category}, visible cards: {visibleCount}, hidden cards: {hiddenCount}, total active cards: {activeCards.Count}.");
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
            LogVerbose($"Reused card from pool for item: {card.itemData?.displayName ?? "Unbound"}. Pool size after pop: {pooledCards.Count}.");
            return card;
        }

        LogVerbose("Instantiating new ItemCard because pool is empty.");
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
        LogVerbose($"Cleared active cards into pool. Pool size: {pooledCards.Count}.");
    }

    void LogVerbose(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.Log("ItemCardsView", message);
    }

    void LogVerboseWarning(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.LogWarning("ItemCardsView", message);
    }


}
