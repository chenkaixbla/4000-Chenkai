using System;
using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour
{
    [Title("Slot Binding")]
    // Which combat slot this UI element represents when used as an equipped/loadout slot.
    [SerializeField] CombatEquipSlot slot = CombatEquipSlot.None;

    // Optional index for list-based slots (Utility and Potion). Keep -1 for single slots.
    [SerializeField] int slotIndex = -1;

    [Title("UI References")]
    // Optional button used for both equipped-slot click behavior and candidate-selection entries.
    [SerializeField] Button button;

    // Optional icon for the currently bound item.
    [SerializeField] Image iconImage;

    // Optional text displaying quantity for the currently bound item.
    [SerializeField] TMP_Text countText;

    // Optional name/label text for item or action entries.
    [SerializeField] TMP_Text nameText;

    // Optional toggle used for auto-use behavior (food/potion slots).
    [SerializeField] Toggle autoUseToggle;

    // Optional background for empty/filled visual state.
    [SerializeField] Image background;

    // Optional placeholder/ghost image shown when no item is bound.
    [SerializeField] GameObject placeholderGhostImage;

    // Per-slot toggle for verbose diagnostics.
    [SerializeField] bool enableVerboseLogging = true;

    [SerializeField, ReadOnly] ItemsData boundItemData;
    [SerializeField, ReadOnly] int boundQuantity;
    bool isCandidateEntry;
    Action candidateSelectAction;
    bool suppressToggleEvent;

    public event Action<ItemSlot> Clicked;
    public event Action<ItemSlot, bool> ToggleChanged;

    public CombatEquipSlot Slot => slot;

    // Utility and potion slots are indexed; all other slot types intentionally stay at -1.
    public int SlotIndex => slot is CombatEquipSlot.Utility or CombatEquipSlot.Potion ? Mathf.Max(0, slotIndex) : -1;

    public ItemsData BoundItemData => boundItemData;
    public int BoundQuantity => boundQuantity;

    void Awake()
    {
        CacheMissingReferences();
        NormalizeSlotIndex();
        WireEvents();
    }

    void OnDestroy()
    {
        UnwireEvents();
    }

    void OnValidate()
    {
        NormalizeSlotIndex();
        CacheMissingReferences(includeOptionalTextFields: false);
        UpdateGameObjectNameForSlotBinding();
    }

    public void SetSlotBinding(CombatEquipSlot boundSlot, int boundSlotIndex = -1)
    {
        slot = boundSlot;
        slotIndex = boundSlotIndex;
        NormalizeSlotIndex();
        UpdateGameObjectNameForSlotBinding();
    }

    // Binds this element as a real item slot (equipped/loadout/candidate item entry).
    public void BindItem(ItemsData itemData, int quantity, string overrideLabel = null)
    {
        isCandidateEntry = false;
        candidateSelectAction = null;
        boundItemData = itemData;
        boundQuantity = Mathf.Max(0, quantity);

        string label = overrideLabel;
        if (string.IsNullOrWhiteSpace(label))
        {
            label = itemData != null ? itemData.displayName : "Empty";
        }

        LogVerbose(
            $"BindItem on {gameObject.name}. slot: {slot}, slotIndex: {SlotIndex}, item: {itemData?.displayName ?? "None"}, quantity: {boundQuantity}, label: {label}.");
        ApplyVisuals(label);
    }

    // Binds this element as a non-item action entry (for example "Unequip").
    public void BindActionEntry(string actionLabel, Action onSelect)
    {
        isCandidateEntry = true;
        candidateSelectAction = onSelect;
        boundItemData = null;
        boundQuantity = 0;

        LogVerbose(
            $"BindActionEntry on {gameObject.name}. slot: {slot}, slotIndex: {SlotIndex}, label: {actionLabel}, hasAction: {onSelect != null}.");
        ApplyVisuals(string.IsNullOrWhiteSpace(actionLabel) ? "Action" : actionLabel);
    }

    // Marks a bound item row as selectable without clearing the currently bound item data.
    public void SetCandidateSelectAction(Action onSelect)
    {
        isCandidateEntry = true;
        candidateSelectAction = onSelect;

        LogVerbose(
            $"SetCandidateSelectAction on {gameObject.name}. slot: {slot}, slotIndex: {SlotIndex}, hasAction: {onSelect != null}, boundItem: {boundItemData?.displayName ?? "None"}, boundQuantity: {boundQuantity}.");
    }

    public void SetAutoToggleVisible(bool visible)
    {
        if (autoUseToggle != null)
        {
            autoUseToggle.gameObject.SetActive(visible);
        }
    }

    public void SetAutoToggleValue(bool enabled)
    {
        if (autoUseToggle == null)
        {
            return;
        }

        suppressToggleEvent = true;
        autoUseToggle.SetIsOnWithoutNotify(enabled);
        suppressToggleEvent = false;
    }

    void ApplyVisuals(string label)
    {
        if (nameText != null)
        {
            nameText.text = label ?? string.Empty;
        }

        if (countText != null)
        {
            countText.text = boundItemData != null && boundQuantity > 0 ? $"x{boundQuantity}" : string.Empty;
        }

        if (iconImage != null)
        {
            Sprite icon = boundItemData != null ? boundItemData.icon : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.gameObject.SetActive(icon != null);
        }

        if (placeholderGhostImage != null)
        {
            placeholderGhostImage.SetActive(boundItemData == null);
        }
    }

    void WireEvents()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
            button.onClick.AddListener(HandleButtonClicked);
        }

        if (autoUseToggle != null)
        {
            autoUseToggle.onValueChanged.RemoveListener(HandleToggleChanged);
            autoUseToggle.onValueChanged.AddListener(HandleToggleChanged);
        }
    }

    void UnwireEvents()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }

        if (autoUseToggle != null)
        {
            autoUseToggle.onValueChanged.RemoveListener(HandleToggleChanged);
        }
    }

    void HandleButtonClicked()
    {
        LogVerbose(
            $"HandleButtonClicked on {gameObject.name}. slot: {slot}, slotIndex: {SlotIndex}, isCandidateEntry: {isCandidateEntry}, boundItem: {boundItemData?.displayName ?? "None"}, boundQuantity: {boundQuantity}.");

        if (isCandidateEntry)
        {
            candidateSelectAction?.Invoke();
            return;
        }

        Clicked?.Invoke(this);
    }

    void HandleToggleChanged(bool enabled)
    {
        if (suppressToggleEvent)
        {
            return;
        }

        ToggleChanged?.Invoke(this, enabled);
    }

    void NormalizeSlotIndex()
    {
        if (slot is CombatEquipSlot.Utility or CombatEquipSlot.Potion)
        {
            slotIndex = Mathf.Max(0, slotIndex);
            return;
        }

        slotIndex = -1;
    }

    void CacheMissingReferences(bool includeOptionalTextFields = true)
    {
        button ??= GetComponent<Button>();

        if (button != null)
        {
            background ??= button.targetGraphic as Image;
        }

        background ??= GetComponent<Image>();

        if (includeOptionalTextFields && (nameText == null || countText == null))
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0 && nameText == null)
            {
                nameText = texts[0];
            }

            if (texts.Length > 1 && countText == null)
            {
                countText = texts[1];
            }
        }

        iconImage ??= ResolveAutoIconImage();
        autoUseToggle ??= GetComponentInChildren<Toggle>(true);
    }

    Image ResolveAutoIconImage()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        Image fallback = null;
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image == background)
            {
                continue;
            }

            if (button != null && image == button.targetGraphic)
            {
                continue;
            }

            if (image.name.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return image;
            }

            if (fallback == null)
            {
                fallback = image;
            }
        }

        return fallback;
    }

    void UpdateGameObjectNameForSlotBinding()
    {
        if (gameObject == null)
        {
            return;
        }

        string currentName = gameObject.name ?? "ItemSlot";
        string baseName = GetBaseNameWithoutSlotSuffix(currentName);
        string suffix = GetSlotSuffixForName();
        string desiredName = $"{baseName} [{suffix}]";

        if (!string.Equals(currentName, desiredName, StringComparison.Ordinal))
        {
            gameObject.name = desiredName;
        }
    }

    string GetBaseNameWithoutSlotSuffix(string currentName)
    {
        if (string.IsNullOrWhiteSpace(currentName))
        {
            return "ItemSlot";
        }

        int suffixStart = currentName.LastIndexOf(" [", StringComparison.Ordinal);
        if (suffixStart >= 0 && currentName.EndsWith("]", StringComparison.Ordinal))
        {
            return currentName[..suffixStart];
        }

        return currentName;
    }

    string GetSlotSuffixForName()
    {
        if (slot is CombatEquipSlot.Utility or CombatEquipSlot.Potion)
        {
            // Use 1-based index in names to match UI-facing slot numbering.
            return $"{slot} {Mathf.Max(0, slotIndex) + 1}";
        }

        return slot.ToString();
    }

    void LogVerbose(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.Log("ItemSlot", message);
    }
}
