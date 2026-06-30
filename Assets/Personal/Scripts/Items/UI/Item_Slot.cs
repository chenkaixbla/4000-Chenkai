using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One item slot view (inventory / shop / crafting). Holds the <see cref="ItemsData"/> it shows
/// plus the three display widgets: name label, icon, and counter. Name and icon are synced from
/// the item data automatically; the counter is driven by code via <see cref="SetCount"/>.
///
/// Each widget has a show toggle (default on). Turn it off to hide that part of the slot - the
/// widget's GameObject is SetActive'd accordingly. The inspector layout (toggle + label + ref per
/// row) is drawn by Item_SlotEditor. Drag the refs in by hand; the rest is automatic.
/// </summary>
[DisallowMultipleComponent]
public class Item_Slot : MonoBehaviour
{
    [Tooltip("The item this slot shows. Name + icon are synced from it.")]
    public ItemsData itemData;

    [Tooltip("Show the name label.")]
    public bool showName = true;
    [Tooltip("Label showing the item's display name.")]
    public TMP_Text nameText;

    [Tooltip("Show the icon.")]
    public bool showIcon = true;
    [Tooltip("Image showing the item's icon.")]
    public Image iconImage;

    [Tooltip("Show the counter.")]
    public bool showCounter = true;
    [Tooltip("Label showing the stack count (set in code via SetCount).")]
    public TMP_Text counterText;

    int count;

    /// <summary>The current counter value last set via <see cref="SetCount"/>.</summary>
    public int Count => count;

    void Awake() => Refresh();

    /// <summary>Assigns the item this slot shows and refreshes the visuals.</summary>
    public void Bind(ItemsData item)
    {
        itemData = item;
        Refresh();
    }

    /// <summary>Sets the counter value and updates the counter label.</summary>
    public void SetCount(int amount)
    {
        count = amount;
        if (counterText != null)
            counterText.text = amount.ToString();
    }

    /// <summary>Syncs name + icon from the item data and applies the show/hide toggles.</summary>
    public void Refresh()
    {
        if (nameText != null)
        {
            nameText.text = itemData != null ? itemData.displayName : string.Empty;
            nameText.gameObject.SetActive(showName);
        }

        if (iconImage != null)
        {
            iconImage.sprite = itemData != null ? itemData.icon : null;
            iconImage.gameObject.SetActive(showIcon);
        }

        if (counterText != null)
        {
            counterText.text = count.ToString();
            counterText.gameObject.SetActive(showCounter);
        }
    }

    // Live sync while playing only. We intentionally don't touch the slot in edit mode so the
    // inspector never changes the scene/prefab on its own - use the Refresh button for that.
    void OnValidate()
    {
        if (Application.isPlaying)
            Refresh();
    }
}
