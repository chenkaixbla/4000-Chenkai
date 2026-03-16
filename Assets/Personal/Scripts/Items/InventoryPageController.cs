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
            });
        }

        // Set initial category filter to the first category page if available
        if (categoryPages.Count > 0)
        {
            currentCategory = categoryPages[0].category;
            itemCardsView.FilterByCategory(currentCategory);
        }

        allCategoryButton.onClick.AddListener(() =>
        {
            currentCategory = Catalog_ItemType.None; // Assuming None means show all categories
            itemCardsView.FilterByCategory(currentCategory);
        });
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
        RebuildInventoryView();
    }

    void OnDirectionToggleClicked()
    {
        _sortAscending = !_sortAscending;
        UpdateArrowIndicator();
        RebuildInventoryView();
    }

    void OnSellClicked()
    {
        if (!_isSellMode) return;

        foreach (ItemsData item in _selectedForSell)
        {
            int amount = inventory.GetQuantity(item);
            SellRequested?.Invoke(item, amount);
        }

        ExitSellMode();
    }

    void EnterSellMode()
    {
        _isSellMode = true;
        _selectedForSell.Clear();
        RebuildInventoryView();
        UpdateActionButtons();
    }

    void ExitSellMode()
    {
        _isSellMode = false;
        _selectedForSell.Clear();
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
        }
        else
        {
            _selectedForSell.Add(itemData);
            card?.SetSellSelected(true);
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
            return;
        }

        System.Action<ItemsData> callback = _isSellMode ? (System.Action<ItemsData>) HandleSellModeItemSelected : HandleItemSelected;
        itemCardsView.Rebuild(GetSortedItems(), callback);
        itemCardsView.ApplySellMode(_isSellMode, _selectedForSell);
        itemCardsView.FilterByCategory(currentCategory);
    }


    public void ShowInventoryPage()
    {
        selectedItem = null;

        cardViewManager?.ShowScrollView(inventoryViewIndex);
        itemDetailPage?.Clear();
        UpdateActionButtons();
    }

    public void ShowDetailPage(ItemsData itemData)
    {
        if (itemData == null || itemDetailPage == null)
        {
            return;
        }

        cardViewManager?.ShowScrollView(detailViewIndex, hideOthers: false);

        selectedItem = itemData;
        itemDetailPage.Display(itemData, GetQuantity(itemData));
        UpdateActionButtons();
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
