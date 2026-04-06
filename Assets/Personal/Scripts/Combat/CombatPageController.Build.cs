using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class CombatPageController
{
    void EnsureBuilt()
    {
        if (isBuilt)
        {
            return;
        }

        ValidateReferences();

        if (monsterSwitcherButton != null)
        {
            monsterSwitcherButton.onClick.RemoveListener(ToggleMonsterSwitcherPanel);
            monsterSwitcherButton.onClick.AddListener(ToggleMonsterSwitcherPanel);
        }

        // All manual action buttons are scene-authored; we only wire core callbacks.
        foodButtonRefs = CacheButtonRefs(foodButton, () =>
        {
            if (manager != null)
            {
                manager.UseFood();
            }
        });
        potionButtonRefs = CacheButtonRefs(potionButton, () =>
        {
            if (manager != null)
            {
                manager.UsePotion();
            }
        });
        stopButtonRefs = CacheButtonRefs(stopButton, () =>
        {
            if (manager != null)
            {
                manager.StopCombat();
            }
        });

        BindLoadoutSlots();

        if (monsterSwitcherPanelRoot != null)
        {
            monsterSwitcherPanelRoot.SetActive(false);
        }

        if (itemSelectionPanelRoot != null)
        {
            itemSelectionPanelRoot.SetActive(false);
        }

        ResolveAndBindSelectionCloseButton();

        LogVerbose(
            $"EnsureBuilt completed. Item selection refs -> panelRoot: {itemSelectionPanelRoot != null}, titleText: {selectionTitleText != null}, spawn: {itemSelectionSpawn != null}, spawnContent: {itemSelectionSpawn?.Content != null}, spawnPrefab: {itemSelectionSpawn?.ItemSlotPrefab != null}.");

        isBuilt = true;
    }

    void ResolveAndBindSelectionCloseButton()
    {
        if (itemSelectionCloseButton == null)
        {
            itemSelectionCloseButton = FindSelectionCloseButtonInPanel();
        }

        if (itemSelectionCloseButton == null)
        {
            LogVerboseWarning("Item selection close button is not assigned and could not be auto-resolved. Panel can still be closed by clicking the source slot again.");
            return;
        }

        itemSelectionCloseButton.onClick.RemoveListener(HandleSelectionCloseButtonClicked);
        itemSelectionCloseButton.onClick.AddListener(HandleSelectionCloseButtonClicked);
        LogVerbose($"Item selection close button wired: {itemSelectionCloseButton.gameObject.name}.");
    }

    Button FindSelectionCloseButtonInPanel()
    {
        if (itemSelectionPanelRoot == null)
        {
            return null;
        }

        Button[] buttons = itemSelectionPanelRoot.GetComponentsInChildren<Button>(true);
        Button fallback = null;
        for (int i = 0; i < buttons.Length; i++)
        {
            Button candidate = buttons[i];
            if (candidate == null)
            {
                continue;
            }

            // Ignore ItemSlot-owned buttons; we only want a dedicated panel close button.
            if (candidate.GetComponentInParent<ItemSlot>() != null)
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = candidate;
            }

            string candidateName = candidate.gameObject.name;
            if (!string.IsNullOrWhiteSpace(candidateName) &&
                candidateName.IndexOf("close", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return candidate;
            }
        }

        return fallback;
    }

    void HandleSelectionCloseButtonClicked()
    {
        CloseSelectionPanel();
    }

    void UnbindSelectionCloseButton()
    {
        if (itemSelectionCloseButton == null)
        {
            return;
        }

        itemSelectionCloseButton.onClick.RemoveListener(HandleSelectionCloseButtonClicked);
    }

    void ValidateReferences()
    {
        ValidateReference(monsterSwitcherButton, nameof(monsterSwitcherButton));
        ValidateReference(monsterListSpawn?.Content, nameof(monsterListSpawn) + ".Content");
        ValidateReference(monsterListSpawn?.ElementPrefab, nameof(monsterListSpawn) + ".ElementPrefab");

        ValidateReference(itemSelectionPanelRoot, nameof(itemSelectionPanelRoot));
        ValidateReference(itemSelectionSpawn?.Content, nameof(itemSelectionSpawn) + ".Content");
        ValidateReference(itemSelectionSpawn?.ItemSlotPrefab, nameof(itemSelectionSpawn) + ".ItemSlotPrefab");

        ValidateReference(foodSlot, nameof(foodSlot));
        if (potionSlots == null || potionSlots.Count == 0)
        {
            Debug.LogWarning("CombatPageController has no potion slots assigned.", this);
        }

        if (equipmentSlots == null || equipmentSlots.Count == 0)
        {
            Debug.LogWarning("CombatPageController has no equipment slots assigned.", this);
        }
    }

    bool ValidateReference(Object target, string fieldName)
    {
        if (target != null)
        {
            return true;
        }

        Debug.LogWarning($"CombatPageController missing reference: {fieldName}", this);
        return false;
    }

    ButtonRefs CacheButtonRefs(Button button, UnityEngine.Events.UnityAction onClick)
    {
        if (button == null)
        {
            return null;
        }

        ButtonRefs refs = new()
        {
            button = button,
            background = button.targetGraphic as Image
        };

        refs.background ??= button.GetComponent<Image>();
        refs.label = button.GetComponentInChildren<TMP_Text>(true);

        button.onClick.RemoveAllListeners();
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        return refs;
    }

    void BindLoadoutSlots()
    {
        UnbindLoadoutSlots();

        RegisterLoadoutSlot(foodSlot, CombatEquipSlot.Food, 0, true);

        if (potionSlots != null)
        {
            for (int i = 0; i < potionSlots.Count; i++)
            {
                RegisterLoadoutSlot(potionSlots[i], CombatEquipSlot.Potion, i, true);
            }
        }

        if (equipmentSlots != null)
        {
            for (int i = 0; i < equipmentSlots.Count; i++)
            {
                int fallbackUtilityIndex;
                CombatEquipSlot fallbackSlot = ResolveEquipmentSlotForIndex(i, out fallbackUtilityIndex);
                RegisterLoadoutSlot(equipmentSlots[i], fallbackSlot, fallbackUtilityIndex, false);
            }
        }
    }

    void RegisterLoadoutSlot(ItemSlot slot, CombatEquipSlot fallbackSlot, int fallbackIndex, bool forceBinding)
    {
        if (slot == null)
        {
            return;
        }

        if (forceBinding || slot.Slot == CombatEquipSlot.None)
        {
            slot.SetSlotBinding(fallbackSlot, fallbackIndex);
        }

        slot.Clicked -= HandleItemSlotClicked;
        slot.Clicked += HandleItemSlotClicked;
        slot.ToggleChanged -= HandleItemSlotToggleChanged;
        slot.ToggleChanged += HandleItemSlotToggleChanged;

        if (!allLoadoutSlots.Contains(slot))
        {
            allLoadoutSlots.Add(slot);
        }
    }

    CombatEquipSlot ResolveEquipmentSlotForIndex(int index, out int utilityIndex)
    {
        utilityIndex = -1;
        return index switch
        {
            0 => CombatEquipSlot.Weapon,
            1 => CombatEquipSlot.Offhand,
            2 => CombatEquipSlot.Helmet,
            3 => CombatEquipSlot.Body,
            4 => CombatEquipSlot.Legs,
            5 => CombatEquipSlot.Gloves,
            6 => CombatEquipSlot.Boots,
            7 => CombatEquipSlot.Cape,
            8 => CombatEquipSlot.Ammo,
            _ => ResolveUtilitySlot(index, out utilityIndex)
        };
    }

    CombatEquipSlot ResolveUtilitySlot(int index, out int utilityIndex)
    {
        utilityIndex = Mathf.Max(0, index - 9);
        return CombatEquipSlot.Utility;
    }

    void UnbindLoadoutSlots()
    {
        for (int i = 0; i < allLoadoutSlots.Count; i++)
        {
            ItemSlot slot = allLoadoutSlots[i];
            if (slot == null)
            {
                continue;
            }

            slot.Clicked -= HandleItemSlotClicked;
            slot.ToggleChanged -= HandleItemSlotToggleChanged;
        }

        allLoadoutSlots.Clear();
    }

    ItemSlot CreateSelectionItemSlot()
    {
        if (itemSelectionSpawn == null || itemSelectionSpawn.ItemSlotPrefab == null || itemSelectionSpawn.Content == null)
        {
            LogVerboseWarning(
                $"CreateSelectionItemSlot failed due to missing refs. spawn null: {itemSelectionSpawn == null}, prefab null: {itemSelectionSpawn?.ItemSlotPrefab == null}, content null: {itemSelectionSpawn?.Content == null}.");
            return null;
        }

        ItemSlot slot = Instantiate(itemSelectionSpawn.ItemSlotPrefab, itemSelectionSpawn.Content, false);
        slot.gameObject.SetActive(true);
        LogVerbose(
            $"CreateSelectionItemSlot instantiated {slot.gameObject.name}. Content child count is now {itemSelectionSpawn.Content.childCount}.");
        return slot;
    }

    MonsterSelectionButtonElement CreateSelectionButton(CombatSpawnListReferences spawnReferences, string value, UnityEngine.Events.UnityAction onClick, float height)
    {
        if (spawnReferences == null || spawnReferences.ElementPrefab == null || spawnReferences.Content == null)
        {
            return null;
        }

        MonsterSelectionButtonElement element = Instantiate(spawnReferences.ElementPrefab, spawnReferences.Content, false);
        element.gameObject.SetActive(true);
        element.Configure(value, onClick, true, height);
        return element;
    }

    MonsterSelectionButtonElement CreateSelectionInfoButton(CombatSpawnListReferences spawnReferences, string value, float height)
    {
        MonsterSelectionButtonElement element = CreateSelectionButton(spawnReferences, value, null, height);
        if (element == null)
        {
            return null;
        }

        element.SetInteractable(false);
        element.SetLabelAlignment(TextAlignmentOptions.Center);
        return element;
    }

    void ClearChildren(RectTransform parent)
    {
        if (parent == null)
        {
            return;
        }

        int removedCount = 0;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
            removedCount++;
        }

        if (itemSelectionSpawn != null && parent == itemSelectionSpawn.Content)
        {
            LogVerbose($"Cleared selection panel children. Removed rows: {removedCount}.");
        }
    }
}
