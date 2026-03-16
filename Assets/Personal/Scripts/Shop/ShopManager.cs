using System.Collections.Generic;
using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Title("Dependencies")]
    public InventoryManager inventory;
    public CurrencyManager currency;
    public InventoryPageController inventoryPageController;
    public CardViewManager cardViewManager;
    public int shopViewIndex = 1;

    [Title("UI")]
    public Button shopButton;
        
    // Quantity prompt 
    public GameObject quantityPromptRoot;
    public TMP_Text quantityItemText;
    public TMP_InputField quantityField; // Integer input field for quantity (Format all input to 1,000,000 format)
    public Button subtractButton;
    public Button addButton;
    public Button confirmButton;
    public Button cancelButton;

    [Title("Configuration")]

    public List<ItemsData> itemsForSale = new();
    [Min(0f)] public float refreshInterval = 0.25f;


    ScrollViewData viewData;
    readonly List<ShopCard> spawnedCards = new();
    float nextRefreshTime;
    ItemsData promptItemData;
    int promptQuantity = 1;
    bool suppressQuantityFieldEvents;

    void Start()
    {
        if (inventoryPageController != null)
        {
            inventoryPageController.SellRequested -= HandleInventorySellRequested;
            inventoryPageController.SellRequested += HandleInventorySellRequested;
        }

        viewData = cardViewManager != null ? cardViewManager.GetScrollViewData(shopViewIndex) : null;
        if (shopButton != null)
        {
            shopButton.onClick.AddListener(OnShopButtonClick);
        }

        RegisterQuantityPromptListeners();
        SetQuantityPromptActive(false);
        RebuildShop();
        RefreshAllCards();
    }

    void Update()
    {
        if (refreshInterval <= 0f || viewData == null || viewData.scrollView == null || !viewData.scrollView.activeInHierarchy)
        {
            return;
        }

        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + refreshInterval;
        RefreshAllCards();
    }

    void OnDestroy()
    {
        if (inventoryPageController != null)
        {
            inventoryPageController.SellRequested -= HandleInventorySellRequested;
        }

        if (shopButton != null)
        {
            shopButton.onClick.RemoveListener(OnShopButtonClick);
        }

        if (subtractButton != null)
        {
            subtractButton.onClick.RemoveListener(HandleSubtractButtonClicked);
        }

        if (addButton != null)
        {
            addButton.onClick.RemoveListener(HandleAddButtonClicked);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(HandleConfirmButtonClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(HandleCancelButtonClicked);
        }

        if (quantityField != null)
        {
            quantityField.onValueChanged.RemoveListener(HandleQuantityFieldValueChanged);
        }
    }

    void HandleInventorySellRequested(ItemsData itemData, int quantity)
    {
        if (itemData == null)
        {
            return;
        }

        SellItem(itemData.itemID, quantity);
    }

    void OnShopButtonClick()
    {
        if (cardViewManager != null)
        {
            cardViewManager.ShowScrollView(shopViewIndex);
        }

        RefreshAllCards();
    }

    [Button]
    public bool TryBuyItem(int id, int quantity = 1)
    {
        return TryBuyItem(GetItemForSale(id), quantity);
    }

    public bool OpenQuantityPrompt(ItemsData itemData)
    {
        if (itemData == null || !CanBuy(itemData))
        {
            return false;
        }

        if (!HasQuantityPrompt())
        {
            Debug.LogWarning("ShopManager is missing quantity prompt references.", this);
            return false;
        }

        promptItemData = itemData;
        SetPromptQuantity(1);
        SetQuantityPromptActive(true);
        UpdateQuantityPromptUI();

        if (quantityField != null)
        {
            quantityField.ActivateInputField();
            quantityField.Select();
        }

        return true;
    }

    public bool TryBuyItem(ItemsData itemData, int quantity = 1)
    {
        int safeQuantity = Mathf.Max(1, quantity);
        if (!CanBuy(itemData, safeQuantity))
        {
            return false;
        }

        ulong totalPrice = GetTotalPrice(itemData, safeQuantity);
        if (totalPrice > 0UL && (currency == null || !currency.TrySpendCurrency(totalPrice)))
        {
            return false;
        }

        inventory.AddItem(itemData, safeQuantity);
        RefreshAllCards();
        return true;
    }

    public bool CanBuy(ItemsData itemData, int quantity = 1)
    {
        if (itemData == null || inventory == null)
        {
            return false;
        }

        if (!IsItemForSale(itemData))
        {
            return false;
        }

        return AreRequirementsMet(itemData) && CanAfford(GetTotalPrice(itemData, quantity));
    }

    public bool AreRequirementsMet(ItemsData itemData)
    {
        if (itemData == null)
        {
            return false;
        }

        return ConditionRuleUtility.AreAllMet(itemData.purchaseRequirements, ConditionContext.Empty);
    }

    public bool CanAfford(ulong amount)
    {
        return amount == 0UL || (currency != null && currency.CanAfford(amount));
    }

    public ulong GetTotalPrice(ItemsData itemData, int quantity = 1)
    {
        if (itemData == null)
        {
            return 0UL;
        }

        ulong price = (ulong)Mathf.Max(0, itemData.price);
        ulong safeQuantity = (ulong)Mathf.Max(1, quantity);
        return price * safeQuantity;
    }

    [Button]
    public void SellItem(int id, int quantity = 1)
    {
        if (inventory == null)
        {
            return;
        }

        int safeQuantity = Mathf.Max(1, quantity);
        if (inventory.RemoveItem(id, out ItemsData itemData, safeQuantity))
        {
            if (currency != null && itemData != null)
            {
                currency.AddCurrency(GetTotalPrice(itemData, safeQuantity));
            }

            RefreshAllCards();
        }
    }

    [Button]
    public void RebuildShop()
    {
        viewData = cardViewManager != null ? cardViewManager.GetScrollViewData(shopViewIndex) : null;
        ClearSpawnedCards();

        if (viewData == null || viewData.container == null || viewData.prefab == null)
        {
            Debug.LogWarning("ShopManager is missing a valid shop view configuration.", this);
            return;
        }

        for (int i = 0; i < itemsForSale.Count; i++)
        {
            SpawnItemForSale(itemsForSale[i]);
        }
    }

    public void RefreshAllCards()
    {
        for (int i = spawnedCards.Count - 1; i >= 0; i--)
        {
            ShopCard card = spawnedCards[i];
            if (card == null)
            {
                spawnedCards.RemoveAt(i);
                continue;
            }

            card.Refresh();
        }

        if (quantityPromptRoot != null && quantityPromptRoot.activeSelf)
        {
            UpdateQuantityPromptUI();
        }
    }

    ItemsData GetItemForSale(int id)
    {
        for (int i = 0; i < itemsForSale.Count; i++)
        {
            ItemsData itemData = itemsForSale[i];
            if (itemData != null && itemData.itemID == id)
            {
                return itemData;
            }
        }

        return null;
    }

    bool IsItemForSale(ItemsData itemData)
    {
        if (itemData == null)
        {
            return false;
        }

        for (int i = 0; i < itemsForSale.Count; i++)
        {
            if (itemsForSale[i] == itemData)
            {
                return true;
            }
        }

        return false;
    }

    void SpawnItemForSale(ItemsData data)
    {
        if (data == null || viewData == null || viewData.prefab == null || viewData.container == null)
        {
            return;
        }

        GameObject cardObject = Instantiate(viewData.prefab, viewData.container);
        ShopCard card = cardObject.GetComponent<ShopCard>();
        if (card == null)
        {
            Debug.LogWarning("ShopManager requires the shop prefab to already have a ShopCard component.", cardObject);
            if (Application.isPlaying)
            {
                Destroy(cardObject);
            }
            else
            {
                DestroyImmediate(cardObject);
            }

            return;
        }

        card.Bind(data, this);
        spawnedCards.Add(card);
    }

    void ClearSpawnedCards()
    {
        for (int i = spawnedCards.Count - 1; i >= 0; i--)
        {
            ShopCard card = spawnedCards[i];
            if (card == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(card.gameObject);
            }
            else
            {
                DestroyImmediate(card.gameObject);
            }
        }

        spawnedCards.Clear();
    }

    void RegisterQuantityPromptListeners()
    {
        if (subtractButton != null)
        {
            subtractButton.onClick.RemoveListener(HandleSubtractButtonClicked);
            subtractButton.onClick.AddListener(HandleSubtractButtonClicked);
        }

        if (addButton != null)
        {
            addButton.onClick.RemoveListener(HandleAddButtonClicked);
            addButton.onClick.AddListener(HandleAddButtonClicked);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(HandleConfirmButtonClicked);
            confirmButton.onClick.AddListener(HandleConfirmButtonClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(HandleCancelButtonClicked);
            cancelButton.onClick.AddListener(HandleCancelButtonClicked);
        }

        if (quantityField != null)
        {
            quantityField.onValueChanged.RemoveListener(HandleQuantityFieldValueChanged);
            quantityField.onValueChanged.AddListener(HandleQuantityFieldValueChanged);
        }
    }

    void HandleSubtractButtonClicked()
    {
        SetPromptQuantity(promptQuantity - 1);
        UpdateQuantityPromptUI();
    }

    void HandleAddButtonClicked()
    {
        if (promptQuantity < int.MaxValue)
        {
            SetPromptQuantity(promptQuantity + 1);
            UpdateQuantityPromptUI();
        }
    }

    void HandleConfirmButtonClicked()
    {
        if (promptItemData == null)
        {
            return;
        }

        if (TryBuyItem(promptItemData, promptQuantity))
        {
            CloseQuantityPrompt();
        }
        else
        {
            UpdateQuantityPromptUI();
        }
    }

    void HandleCancelButtonClicked()
    {
        CloseQuantityPrompt();
    }

    void HandleQuantityFieldValueChanged(string rawValue)
    {
        if (suppressQuantityFieldEvents)
        {
            return;
        }

        SetPromptQuantity(ParsePromptQuantity(rawValue));
        UpdateQuantityPromptUI();
    }

    void UpdateQuantityPromptUI()
    {
        if (!HasQuantityPrompt())
        {
            return;
        }

        if (quantityItemText != null)
        {
            quantityItemText.text = promptItemData != null ? promptItemData.displayName : "Item";
        }

        UpdateQuantityFieldText();

        if (subtractButton != null)
        {
            subtractButton.interactable = promptQuantity > 1;
        }

        if (addButton != null)
        {
            addButton.interactable = promptQuantity < int.MaxValue;
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = promptItemData != null && CanBuy(promptItemData, promptQuantity);
        }
    }

    void UpdateQuantityFieldText()
    {
        if (quantityField == null)
        {
            return;
        }

        string formattedQuantity = promptQuantity.ToString("N0");
        if (quantityField.text == formattedQuantity)
        {
            return;
        }

        suppressQuantityFieldEvents = true;
        quantityField.SetTextWithoutNotify(formattedQuantity);
        suppressQuantityFieldEvents = false;
    }

    void SetPromptQuantity(int quantity)
    {
        promptQuantity = Mathf.Clamp(quantity, 1, int.MaxValue);
        UpdateQuantityFieldText();
    }

    int ParsePromptQuantity(string rawValue)
    {
        if (string.IsNullOrEmpty(rawValue))
        {
            return 1;
        }

        string digitsOnly = string.Empty;
        for (int i = 0; i < rawValue.Length; i++)
        {
            if (char.IsDigit(rawValue[i]))
            {
                digitsOnly += rawValue[i];
            }
        }

        if (string.IsNullOrEmpty(digitsOnly))
        {
            return 1;
        }

        ulong parsedValue;
        if (!ulong.TryParse(digitsOnly, out parsedValue))
        {
            return int.MaxValue;
        }

        if (parsedValue > int.MaxValue)
        {
            return int.MaxValue;
        }

        return Mathf.Max(1, (int)parsedValue);
    }

    void CloseQuantityPrompt()
    {
        promptItemData = null;
        promptQuantity = 1;
        SetQuantityPromptActive(false);
    }

    void SetQuantityPromptActive(bool isActive)
    {
        if (quantityPromptRoot != null)
        {
            quantityPromptRoot.SetActive(isActive);
        }
    }

    bool HasQuantityPrompt()
    {
        return quantityPromptRoot != null
            && quantityItemText != null
            && quantityField != null
            && subtractButton != null
            && addButton != null
            && confirmButton != null
            && cancelButton != null;
    }
}
