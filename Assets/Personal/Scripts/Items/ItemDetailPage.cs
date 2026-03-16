using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailPage : MonoBehaviour
{
    [Title("Page")]
    public GameObject pageRoot;
    public Button closeButton;

    [Title("UI")]
    public TMP_Text displayNameText;
    public TMP_Text quantityText;
    public TMP_Text priceText;
    public TMP_Text descriptionText;
    public Image iconImage;

    public event System.Action CloseRequested;

    public ItemsData CurrentItem { get; private set; }

    void Awake()
    {
        closeButton.onClick.RemoveListener(HandleCloseClicked);
        closeButton.onClick.AddListener(HandleCloseClicked);
    }

    public void Display(ItemsData itemData, int quantity)
    {
        CurrentItem = itemData;

        if (itemData == null)
        {
            ApplyEmptyState();
            return;
        }

        if (displayNameText != null)
        {
            displayNameText.text = itemData.displayName;
        }

        if (quantityText != null)
        {
            quantityText.text = $"Owned: {Mathf.Max(0, quantity)}";
        }

        if (priceText != null)
        {
            priceText.text = $"{itemData.price}";
        }

        if (descriptionText != null)
        {
            descriptionText.text = itemData.itemDescriptions;
        }

        if (iconImage != null)
        {
            iconImage.sprite = itemData.icon;
            iconImage.enabled = itemData.icon != null;
        }
    }

    public void Clear()
    {
        CurrentItem = null;
        ApplyEmptyState();
    }

    void HandleCloseClicked()
    {
        pageRoot.SetActive(false);
        CloseRequested?.Invoke();
    }

    void ApplyEmptyState()
    {
        if (displayNameText != null)
        {
            displayNameText.text = "No Item";
        }

        if (quantityText != null)
        {
            quantityText.text = "Owned: 0";
        }

        if (priceText != null)
        {
            priceText.text = "Price: 0";
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
        }

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }
}
