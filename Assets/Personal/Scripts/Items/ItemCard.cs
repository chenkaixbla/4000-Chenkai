using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemCard : MonoBehaviour
{
    [Header("Data")]
    public ItemsData itemData;

    [Header("Card UI")]
    public Button selectButton;
    public TMP_Text displayNameText;
    public TMP_Text quantityText;
    public Image iconImage;

    System.Action<ItemsData> onSelected;

    void Awake()
    {
        EnsureButtonReference();
        BindButton();
    }

    void OnEnable()
    {
        EnsureButtonReference();
        BindButton();
    }

    void OnDisable()
    {
        UnbindButton();
    }

    public void Bind(ItemsData data, int quantity, System.Action<ItemsData> onSelectedCallback)
    {
        itemData = data;
        onSelected = onSelectedCallback;
        Refresh(quantity);
    }

    public void Refresh(int quantity)
    {
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
            quantityText.text = $"x{Mathf.Max(0, quantity)}";
        }

        if (iconImage != null)
        {
            iconImage.sprite = itemData.icon;
            iconImage.enabled = itemData.icon != null;
        }
    }

    void HandleSelected()
    {
        if (itemData != null)
        {
            onSelected?.Invoke(itemData);
        }
    }

    void ApplyEmptyState()
    {
        itemData = null;

        if (displayNameText != null)
        {
            displayNameText.text = "No Item";
        }

        if (quantityText != null)
        {
            quantityText.text = "x0";
        }

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    void BindButton()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleSelected);
            selectButton.onClick.AddListener(HandleSelected);
        }
    }

    void UnbindButton()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleSelected);
        }
    }

    void EnsureButtonReference()
    {
        if (selectButton == null)
        {
            selectButton = GetComponent<Button>();
        }
    }
}
