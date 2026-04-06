using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InventorySortMode
{
    ByName = 0,
    ByAmount = 1,
    ByPrice = 2,
    ByAddedTime = 3
}

[System.Serializable]
public class InventoryCategoryPage
{
    public Catalog_ItemType category;
    public Button button;
}

public class InventoryPageController : MonoBehaviour
{
    [Title("Dependencies")]
    [SerializeField] bool enableVerboseLogging = true;
    public InventoryManager inventory;
    public ItemCardsView itemCardsView;
    public ItemDetailPage itemDetailPage;

    [Title("Page Navigation")]
    public CardViewManager cardViewManager;
    public Button inventoryButton;
    public int inventoryViewIndex = 0;
    public int detailViewIndex = 1;

    [Title("Categories")]
    public Button allCategoryButton;
    public Catalog_ItemType currentCategory;
    public List<InventoryCategoryPage> categoryPages = new();

    [Title("Action Buttons")]
    [SerializeField, ReadOnly] bool _isSellMode = false;
    public Button sellModeButton;
    public Button stopSellButton;
    public Button sellButton;
    public TMP_Dropdown filterDropdown; // By name, amount, price, added time
    public Button directionToggleButton; // Ascending / Descending
    public Transform arrowIndicator; // Optional visual indicator for sorting direction (default rotation 0 = ascending)

    public event System.Action<ItemsData, int> SellRequested; // Item and quantity to sell
    public event System.Action<ItemsData> StopSellRequested;

    ItemsData selectedItem;
    InventorySortMode _sortMode = InventorySortMode.ByName;
    bool _sortAscending = true;
    readonly HashSet<ItemsData> _selectedForSell = new();

    void Start()
    {
        LogVerbose("Start called.");

        if (InventoryManager.Instance != null && inventory != null && inventory != InventoryManager.Instance)
        {
            LogVerboseWarning($"Assigned inventory reference differs from singleton. Assigned id: {inventory.GetInstanceID()}, singleton id: {InventoryManager.Instance.GetInstanceID()}. Using singleton to keep grant and view paths aligned.");
            inventory = InventoryManager.Instance;
        }

        if (inventory == null)
        {
            inventory = InventoryManager.Instance;
        }

        LogVerbose(inventory != null
            ? $"Resolved inventory reference id: {inventory.GetInstanceID()}."
            : "Inventory reference is null at Start.");

        if (inventory != null)
        {
            inventory.OnInventoryChanged += HandleInventoryChanged;
            LogVerbose("Subscribed to InventoryManager.OnInventoryChanged.");
        }

        if (itemDetailPage != null)
        {
            itemDetailPage.CloseRequested += HandleDetailCloseRequested;
            itemDetailPage.Clear();
        }

        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveListener(ShowInventoryPage);
            inventoryButton.onClick.AddListener(ShowInventoryPage);
        }

        RebuildInventoryView();
        InitCategoryButtons();
        InitSortControls();
        ShowInventoryPage();
    }

    void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= HandleInventoryChanged;
            LogVerbose("Unsubscribed from InventoryManager.OnInventoryChanged.");
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

    void InitCategoryButtons()
    {
        LogVerbose($"Initializing category buttons. Configured category pages: {categoryPages.Count}.");

        for (int i = 0; i < categoryPages.Count; i++)
        {
            InventoryCategoryPage categoryPage = categoryPages[i];
            if (categoryPage == null || categoryPage.button == null)
            {
                continue;
            }

            Catalog_ItemType category = categoryPage.category;
            Button button = categoryPage.button;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                currentCategory = category;
                itemCardsView.FilterByCategory(category);
                LogVerbose($"Category button clicked. Active category: {category}.");
            });
        }

        // Default to showing all items to avoid newly granted items appearing "missing" behind a category filter.
        currentCategory = Catalog_ItemType.None;
        if (itemCardsView != null)
        {
            itemCardsView.FilterByCategory(currentCategory);
        }
        LogVerbose("Default category set to None (show all).");

        if (allCategoryButton != null)
        {
            allCategoryButton.onClick.RemoveAllListeners();
            allCategoryButton.onClick.AddListener(() =>
            {
                currentCategory = Catalog_ItemType.None;
                itemCardsView.FilterByCategory(currentCategory);
                LogVerbose("All category button clicked. Showing all categories.");
            });
        }
    }

    void InitSortControls()
    {
        if (filterDropdown != null)
        {
            PopulateFilterDropdownOptions();
            filterDropdown.onValueChanged.RemoveAllListeners();
            filterDropdown.onValueChanged.AddListener(OnFilterDropdownChanged);
        }

        if (directionToggleButton != null)
        {
            directionToggleButton.onClick.RemoveAllListeners();
            directionToggleButton.onClick.AddListener(OnDirectionToggleClicked);
        }

        if (sellModeButton != null)
        {
            sellModeButton.onClick.RemoveAllListeners();
            sellModeButton.onClick.AddListener(EnterSellMode);
        }

        if (stopSellButton != null)
        {
            stopSellButton.onClick.RemoveAllListeners();
            stopSellButton.onClick.AddListener(ExitSellMode);
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(OnSellClicked);
        }

        UpdateArrowIndicator();
        UpdateActionButtons();
    }

    void PopulateFilterDropdownOptions()
    {
        if (filterDropdown == null)
        {
            return;
        }

        List<string> options = new()
        {
            "By Name",
            "By Amount",
            "By Price",
            "By Added Time"
        };

        filterDropdown.ClearOptions();
        filterDropdown.AddOptions(options);
        filterDropdown.SetValueWithoutNotify((int)_sortMode);
        filterDropdown.RefreshShownValue();
    }

    void OnFilterDropdownChanged(int index)
    {
        _sortMode = (InventorySortMode)index;
        LogVerbose($"Sort mode changed to {_sortMode}.");
        RebuildInventoryView();
    }

    void OnDirectionToggleClicked()
    {
        _sortAscending = !_sortAscending;
        LogVerbose($"Sort direction changed. Ascending: {_sortAscending}.");
        UpdateArrowIndicator();
        RebuildInventoryView();
    }

    void OnSellClicked()
    {
        if (!_isSellMode) return;

        LogVerbose($"Sell clicked. Selected item count: {_selectedForSell.Count}.");

        if (inventory == null)
        {
            LogVerboseError("Cannot process sell: inventory reference is null.");
            return;
        }

        foreach (ItemsData item in _selectedForSell)
        {
            int amount = inventory.GetQuantity(item);
            LogVerbose($"Sell request emitted for {item?.displayName ?? "Unknown"} with quantity {amount}.");
            SellRequested?.Invoke(item, amount);
        }

        ExitSellMode();
    }

    void EnterSellMode()
    {
        _isSellMode = true;
        _selectedForSell.Clear();
        LogVerbose("Entered sell mode.");
        RebuildInventoryView();
        UpdateActionButtons();
    }

    void ExitSellMode()
    {
        _isSellMode = false;
        _selectedForSell.Clear();
        LogVerbose("Exited sell mode.");
        RebuildInventoryView();
        UpdateActionButtons();
    }

    void HandleSellModeItemSelected(ItemsData itemData)
    {
        if (itemData == null) return;

        ItemCard card = itemCardsView.GetActiveCards().FirstOrDefault(c => c.itemData == itemData);
        if (_selectedForSell.Contains(itemData))
        {
            _selectedForSell.Remove(itemData);
            card?.SetSellSelected(false);
            LogVerbose($"Removed item from sell selection: {itemData.displayName}.");
        }
        else
        {
            _selectedForSell.Add(itemData);
            card?.SetSellSelected(true);
            LogVerbose($"Added item to sell selection: {itemData.displayName}.");
        }
    }

    void UpdateArrowIndicator()
    {
        if (arrowIndicator != null)
        {
            arrowIndicator.localRotation = Quaternion.Euler(0f, 0f, _sortAscending ? 0f : 180f);
        }
    }

    void UpdateActionButtons()
    {
        if (sellModeButton != null) sellModeButton.gameObject.SetActive(!_isSellMode);
        if (stopSellButton != null) stopSellButton.gameObject.SetActive(_isSellMode);
        if (sellButton != null) sellButton.gameObject.SetActive(_isSellMode);
    }

    List<ItemsInstance> GetSortedItems()
    {
        IEnumerable<ItemsInstance> items = inventory.GetTrackedItems();
        IEnumerable<ItemsInstance> sorted;

        switch (_sortMode)
        {
            case InventorySortMode.ByAmount:
                sorted = _sortAscending ? items.OrderBy(i => i.quantity) : items.OrderByDescending(i => i.quantity);
                break;
            case InventorySortMode.ByPrice:
                sorted = _sortAscending ? items.OrderBy(i => i.itemData?.price ?? 0) : items.OrderByDescending(i => i.itemData?.price ?? 0);
                break;
            case InventorySortMode.ByAddedTime:
                sorted = _sortAscending ? items.OrderBy(i => i.addedOrder) : items.OrderByDescending(i => i.addedOrder);
                break;
            default:
                sorted = _sortAscending ? items.OrderBy(i => i.itemData?.displayName ?? "") : items.OrderByDescending(i => i.itemData?.displayName ?? "");
                break;
        }

        return sorted.ToList();
    }

    public void RebuildInventoryView()
    {
        if (inventory == null || itemCardsView == null)
        {
            LogVerboseWarning($"Rebuild skipped. inventory null: {inventory == null}, itemCardsView null: {itemCardsView == null}.");
            return;
        }

        List<ItemsInstance> sortedItems = GetSortedItems();
        int trackedCount = sortedItems.Count;
        int positiveCount = 0;
        for (int i = 0; i < sortedItems.Count; i++)
        {
            ItemsInstance instance = sortedItems[i];
            if (instance != null && instance.itemData != null && instance.quantity > 0)
            {
                positiveCount++;
            }
        }

        System.Action<ItemsData> callback = _isSellMode ? (System.Action<ItemsData>) HandleSellModeItemSelected : HandleItemSelected;
        itemCardsView.Rebuild(sortedItems, callback);
        itemCardsView.ApplySellMode(_isSellMode, _selectedForSell);
        itemCardsView.FilterByCategory(currentCategory);
        LogVerbose($"Rebuild complete. Sort mode: {_sortMode}, ascending: {_sortAscending}, tracked items: {trackedCount}, positive quantity items: {positiveCount}, category filter: {currentCategory}, sell mode: {_isSellMode}.");
    }


    public void ShowInventoryPage()
    {
        selectedItem = null;
        LogVerbose("Showing inventory page.");

        cardViewManager?.ShowScrollView(inventoryViewIndex);
        itemDetailPage?.Clear();
        UpdateActionButtons();
    }

    public void ShowDetailPage(ItemsData itemData)
    {
        if (itemData == null || itemDetailPage == null)
        {
            LogVerboseWarning($"ShowDetailPage skipped. itemData null: {itemData == null}, itemDetailPage null: {itemDetailPage == null}.");
            return;
        }

        cardViewManager?.ShowScrollView(detailViewIndex, hideOthers: false);

        selectedItem = itemData;
        itemDetailPage.Display(itemData, GetQuantity(itemData));
        LogVerbose($"Showing detail page for {itemData.displayName} with quantity {GetQuantity(itemData)}.");
        UpdateActionButtons();
    }

    void HandleInventoryChanged()
    {
        LogVerbose("Inventory changed event received.");
        RebuildInventoryView();
        RefreshSelectedItem();
    }

    void HandleItemSelected(ItemsData itemData)
    {
        LogVerbose($"Item selected: {itemData?.displayName ?? "Unknown"}.");
        ShowDetailPage(itemData);
    }

    void HandleDetailCloseRequested()
    {
        LogVerbose("Detail close requested.");
        ShowInventoryPage();
    }

    void RefreshSelectedItem()
    {
        if (selectedItem == null)
        {
            return;
        }

        int quantity = GetQuantity(selectedItem);
        LogVerbose($"Refreshing selected item {selectedItem.displayName}. Current quantity: {quantity}.");
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

    void LogVerbose(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.Log("InventoryPageController", message);
    }

    void LogVerboseWarning(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.LogWarning("InventoryPageController", message);
    }

    void LogVerboseError(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.LogError("InventoryPageController", message);
    }
}
