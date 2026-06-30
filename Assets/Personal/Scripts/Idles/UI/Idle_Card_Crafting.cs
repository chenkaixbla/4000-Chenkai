using System.Collections.Generic;
using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One item slot on a card: an icon, a title label, and a reference to the item it shows.
/// Drag the icon and title in by hand; <see cref="item"/> is set when the slot is filled.
/// </summary>
[System.Serializable]
public class Idle_Card_ItemSlot
{
    public Image icon;
    public TMP_Text title;

    /// <summary>The item this slot is currently showing (set by Set, not authored).</summary>
    [System.NonSerialized] public ItemsData item;

    public void Set(ItemsData data, string text)
    {
        item = data;

        if (icon != null)
        {
            icon.sprite = data != null ? data.icon : null;
            icon.enabled = data != null && data.icon != null;
        }

        if (title != null)
            title.text = text;
    }

    public void Clear()
    {
        item = null;
        if (icon != null) icon.enabled = false;
        if (title != null) title.text = string.Empty;
    }
}

/// <summary>
/// Special idle-card visuals for crafting idles. Sits alongside the base <see cref="Idle_Card"/>
/// (which drives name / icon / level &amp; timer bars / the Create button). It reads the bound
/// <see cref="Idle_Data_Crafting"/> and fills the Produces and Required slots. Drag one slot row
/// per item you want to show.
///
/// TODO: "You Have" (owned counts) needs the inventory system, and "Grants" needs the
/// reward-granting system - neither exists yet, so those lists aren't populated. Revisit when
/// those systems land (see CLAUDE.md crafting TODO).
/// </summary>
public class Idle_Card_Crafting : Idle_Card_Extension
{
    [Tooltip("Items produced each cycle. Filled from Idle_Data_Crafting.produces.")]
    public List<Idle_Card_ItemSlot> produces = new();

    [Tooltip("Items required to run a cycle. Filled from Idle_Data_Crafting.required.")]
    public List<Idle_Card_ItemSlot> required = new();

    [Line]

    [Tooltip("TODO: owned amount of each required item - needs the inventory system.")]
    public List<Idle_Card_ItemSlot> youHave = new();

    [Tooltip("TODO: bonus rewards granted - needs the reward-granting system.")]
    public List<Idle_Card_ItemSlot> grants = new();

    public override void OnRefresh(Idle_Runtime runtime)
    {
        Idle_Data_Crafting crafting = runtime != null ? runtime.idleData as Idle_Data_Crafting : null;
        if (crafting == null)
        {
            ClearAll(produces);
            ClearAll(required);
            return;
        }

        FillSlots(produces, crafting.produces);
        FillSlots(required, crafting.required);

        // TODO: youHave (from inventory) and grants (from rewards) once those systems exist.
    }

    public override void OnUnbind()
    {
        ClearAll(produces);
        ClearAll(required);
        ClearAll(youHave);
        ClearAll(grants);
    }

    static void FillSlots(List<Idle_Card_ItemSlot> slots, List<Idle_ItemStack> stacks)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
                continue;

            if (i < stacks.Count && stacks[i] != null && stacks[i].item != null)
                slots[i].Set(stacks[i].item, stacks[i].amount.ToString());
            else
                slots[i].Clear();
        }
    }

    static void ClearAll(List<Idle_Card_ItemSlot> slots)
    {
        for (int i = 0; i < slots.Count; i++)
            slots[i]?.Clear();
    }
}
