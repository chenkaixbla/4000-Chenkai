using UnityEngine;
using UnityEngine.UI;

public class InventoryPageController : MonoBehaviour
{
    [Header("Dependencies")]
    public InventoryManager inventory;
    public ItemCardsView itemCardsView;
    public ItemDetailPage itemDetailPage;

    [Header("Page Navigation")]
    public CardViewManager cardViewManager;
    public int inventoryViewIndex = 0;
    public int detailViewIndex = 1;
    public GameObject inventoryPageRoot;
    public Button inventoryButton;

    ItemsData selectedItem;

    void Start()
    {
        if (inventory == null)
        {
            inventory = InventoryManager.Instance;
        }

        if (inventory != null)
        {
            inventory.OnInventoryChanged += HandleInventoryChanged;
        }

        if (itemDetailPage != null)
        {
            itemDetailPage.CloseRequested += HandleDetailCloseRequested;
            itemDetailPage.Clear();
            itemDetailPage.SetPageVisible(false);
        }

        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveListener(ShowInventoryPage);
            inventoryButton.onClick.AddListener(ShowInventoryPage);
        }

        RebuildInventoryView();
        ShowInventoryPage();
    }

    void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        if (itemDetailPage != null)
        {
            itemDetailPage.CloseRequested -= HandleDetailCloseRequested;
        }

        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveListener(ShowInventoryPage);
        }
    }

    public void RebuildInventoryView()
    {
        if (inventory == null || itemCardsView == null)
        {
            return;
        }

        itemCardsView.Rebuild(inventory.GetTrackedItems(), HandleItemSelected);
    }

    public void ShowInventoryPage()
    {
        selectedItem = null;

        if (inventoryPageRoot != null)
        {
            inventoryPageRoot.SetActive(true);
        }

        if (itemDetailPage != null)
        {
            itemDetailPage.SetPageVisible(false);
            itemDetailPage.Clear();
        }

        if (cardViewManager != null)
        {
            cardViewManager.ShowScrollView(inventoryViewIndex);
        }
    }

    public void ShowDetailPage(ItemsData itemData)
    {
        if (itemData == null || itemDetailPage == null)
        {
            return;
        }

        selectedItem = itemData;
        itemDetailPage.Display(itemData, GetQuantity(itemData));
        itemDetailPage.SetPageVisible(true);

        if (inventoryPageRoot != null)
        {
            inventoryPageRoot.SetActive(false);
        }

        if (cardViewManager != null)
        {
            cardViewManager.ShowScrollView(detailViewIndex);
        }
    }

    void HandleInventoryChanged()
    {
        RebuildInventoryView();
        RefreshSelectedItem();
    }

    void HandleItemSelected(ItemsData itemData)
    {
        ShowDetailPage(itemData);
    }

    void HandleDetailCloseRequested()
    {
        ShowInventoryPage();
    }

    void RefreshSelectedItem()
    {
        if (selectedItem == null)
        {
            return;
        }

        int quantity = GetQuantity(selectedItem);
        if (quantity <= 0)
        {
            ShowInventoryPage();
            return;
        }

        if (itemDetailPage != null)
        {
            itemDetailPage.Display(selectedItem, quantity);
        }
    }

    int GetQuantity(ItemsData itemData)
    {
        if (inventory == null || itemData == null)
        {
            return 0;
        }

        return inventory.GetQuantity(itemData);
    }
}
