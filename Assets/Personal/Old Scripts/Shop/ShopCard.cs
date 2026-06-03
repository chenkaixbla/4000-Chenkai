using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopCard : MonoBehaviour
{
    public Button buyButton; // Button to purchase the item
    public string itemName; // Name of the item being sold
    public TMP_Text itemNameText;
    public Image icon; // Icon representing the item
    public Transform requirementContainer;

    // Acts as the cost or requirement for purchasing the item. This can be expanded to include more complex requirements.
    // Example: "Cost: 100 Gold", "Requires Level 5", etc.
    // We will have 4 by default (We will set in the inspector)
    public List<Image_Text> requirements = new();

    [Header("Requirement Styling")]
    public Sprite currencyRequirementIcon;
    public Color metRequirementColor = Color.white;
    public Color unmetRequirementColor = new Color(1f, 0.45f, 0.45f, 1f);

    ItemsData itemData;
    ShopManager shopManager;

    readonly List<RequirementViewData> activeRequirements = new();

    struct RequirementViewData
    {
        public readonly Sprite icon;
        public readonly string text;
        public readonly bool isMet;

        public RequirementViewData(Sprite icon, string text, bool isMet)
        {
            this.icon = icon;
            this.text = text;
            this.isMet = isMet;
        }
    }

    void OnDestroy()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(HandleBuyClicked);
        }
    }

    public void Bind(ItemsData data, ShopManager manager)
    {
        itemData = data;
        shopManager = manager;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(HandleBuyClicked);
            buyButton.onClick.AddListener(HandleBuyClicked);
        }

        Refresh();
    }

    public void Refresh()
    {
        if (itemData == null)
        {
            ApplyEmptyState();
            return;
        }

        itemName = itemData.displayName;

        if (itemNameText != null)
        {
            itemNameText.text = itemName;
        }

        if (icon != null)
        {
            icon.sprite = itemData.icon;
            icon.enabled = itemData.icon != null;
        }

        BuildRequirementViews();
        EnsureRequirementSlots(activeRequirements.Count);

        for (int i = 0; i < requirements.Count; i++)
        {
            Image_Text requirementSlot = requirements[i];
            if (requirementSlot == null)
            {
                continue;
            }

            bool shouldShow = i < activeRequirements.Count;
            requirementSlot.gameObject.SetActive(shouldShow);
            if (!shouldShow)
            {
                continue;
            }

            RequirementViewData requirementData = activeRequirements[i];
            requirementSlot.Set(requirementData.icon, requirementData.text);

            if (requirementSlot.text != null)
            {
                requirementSlot.text.color = requirementData.isMet ? metRequirementColor : unmetRequirementColor;
            }

            if (requirementSlot.image != null && requirementSlot.image.enabled)
            {
                requirementSlot.image.color = requirementData.isMet ? metRequirementColor : unmetRequirementColor;
            }
        }

        if (buyButton != null)
        {
            buyButton.interactable = shopManager != null && shopManager.CanBuy(itemData);
        }
    }

    void HandleBuyClicked()
    {
        if (shopManager == null || itemData == null)
        {
            return;
        }

        shopManager.OpenQuantityPrompt(itemData);
    }

    void BuildRequirementViews()
    {
        activeRequirements.Clear();

        if (itemData == null)
        {
            return;
        }

        ulong price = (ulong)Mathf.Max(0, itemData.price);
        bool canAfford = shopManager != null && shopManager.CanAfford(price);
        activeRequirements.Add(new RequirementViewData(currencyRequirementIcon, price.ToString("N0"), canAfford));

        if (itemData.purchaseRequirements == null)
        {
            return;
        }

        for (int i = 0; i < itemData.purchaseRequirements.Count; i++)
        {
            ConditionRuleEntry entry = itemData.purchaseRequirements[i];
            string label = ConditionRuleUtility.GetRequirementLabel(entry);
            Sprite requirementIcon = ConditionRuleUtility.GetRequirementIcon(entry);
            bool isMet = ConditionRuleUtility.IsMet(entry, ConditionContext.Empty);

            activeRequirements.Add(new RequirementViewData(requirementIcon, label, isMet));
        }
    }

    void ApplyEmptyState()
    {
        itemName = string.Empty;

        if (itemNameText != null)
        {
            itemNameText.text = "No Item";
        }

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        for (int i = 0; i < requirements.Count; i++)
        {
            if (requirements[i] != null)
            {
                requirements[i].gameObject.SetActive(false);
            }
        }

        if (buyButton != null)
        {
            buyButton.interactable = false;
        }
    }

    void EnsureRequirementSlots(int requiredCount)
    {
        if (requirements == null)
        {
            requirements = new List<Image_Text>();
        }

        requirements.RemoveAll(slot => slot == null);
        requirements.Sort((left, right) => left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));

        if (requiredCount <= requirements.Count || requirementContainer == null || requirements.Count == 0)
        {
            return;
        }

        Image_Text template = requirements[requirements.Count - 1];
        while (requirements.Count < requiredCount)
        {
            GameObject requirementObject = Instantiate(template.gameObject, requirementContainer);
            requirementObject.name = template.gameObject.name;
            Image_Text newSlot = requirementObject.GetComponent<Image_Text>();
            if (newSlot != null)
            {
                requirements.Add(newSlot);
            }
        }

        requirements.Sort((left, right) => left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
    }
}
