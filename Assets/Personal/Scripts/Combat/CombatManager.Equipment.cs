using System;
using System.Collections.Generic;
using UnityEngine;

public partial class CombatManager
{
    public bool EquipItem(CombatEquipSlot slot, ItemsData itemData, int utilityIndex = -1)
    {
        if (slot == CombatEquipSlot.None)
        {
            return false;
        }

        if (itemData != null && !CanEquipItem(itemData, slot))
        {
            AddCombatLog($"Cannot equip {itemData.displayName} in {GetSlotDisplayName(slot, utilityIndex)}.");
            return false;
        }

        if (itemData != null && GetAvailableEquipCopies(itemData, slot, utilityIndex) <= 0)
        {
            AddCombatLog($"No spare copies of {itemData.displayName} are available to equip.");
            return false;
        }

        profile.loadout.EnsureInitialized();
        profile.loadout.SetItem(slot, itemData, utilityIndex);
        AddCombatLog(itemData != null
            ? $"Equipped {itemData.displayName} to {GetSlotDisplayName(slot, utilityIndex)}."
            : $"Unequipped {GetSlotDisplayName(slot, utilityIndex)}.");
        NotifyStateChanged();
        return true;
    }

    public bool CanEquipItem(ItemsData itemData, CombatEquipSlot slot)
    {
        if (itemData == null || inventory == null)
        {
            return false;
        }

        if (inventory.GetQuantity(itemData) <= 0)
        {
            return false;
        }

        CombatItemDefinition combatData = itemData.combatData;
        if (combatData == null)
        {
            return false;
        }

        combatData.EnsureInitialized();
        if (!combatData.AllowsSlot(slot))
        {
            return false;
        }

        if (slot == CombatEquipSlot.Food && combatData.itemRole != CombatItemRole.Food)
        {
            return false;
        }

        if (slot == CombatEquipSlot.Potion && combatData.itemRole != CombatItemRole.Potion)
        {
            return false;
        }

        if (slot != CombatEquipSlot.Food && slot != CombatEquipSlot.Potion && combatData.itemRole != CombatItemRole.Equipment)
        {
            return false;
        }

        return ConditionRuleUtility.AreAllMet(combatData.equipRequirements, ConditionContext.Empty);
    }

    public List<ItemsInstance> GetCompatibleItemsForSlot(CombatEquipSlot slot, int utilityIndex = -1)
    {
        List<ItemsInstance> results = new();
        if (inventory == null)
        {
            return results;
        }

        IReadOnlyList<ItemsInstance> trackedItems = inventory.GetTrackedItems();
        for (int i = 0; i < trackedItems.Count; i++)
        {
            ItemsInstance instance = trackedItems[i];
            if (instance == null || instance.itemData == null || instance.quantity <= 0)
            {
                continue;
            }

            if (CanEquipItem(instance.itemData, slot) && GetAvailableEquipCopies(instance.itemData, slot, utilityIndex) > 0)
            {
                results.Add(instance);
            }
        }

        results.Sort((left, right) => string.Compare(left.itemData.displayName, right.itemData.displayName, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    public ItemsData GetEquippedItem(CombatEquipSlot slot, int utilityIndex = -1)
    {
        profile.loadout.EnsureInitialized();
        return profile.loadout.GetItem(slot, utilityIndex);
    }

    public bool CanUseFood()
    {
        ItemsData foodItem = profile.loadout != null ? profile.loadout.food : null;
        if (foodItem == null || inventory == null)
        {
            return false;
        }

        CombatItemDefinition combatData = foodItem.combatData;
        if (combatData == null || combatData.itemRole != CombatItemRole.Food)
        {
            return false;
        }

        if (foodItem.combatData.foodEffect == null || foodItem.combatData.foodEffect.healAmount <= 0)
        {
            return false;
        }

        return inventory.GetQuantity(foodItem) > 0 &&
               GetFoodCooldownRemainingSeconds() <= 0f &&
               profile.currentHp < GetPlayerMaxHp();
    }

    public bool UseFood()
    {
        if (!CanUseFood())
        {
            return false;
        }

        ItemsData foodItem = profile.loadout.food;
        int healAmount = Mathf.Max(0, foodItem.combatData.foodEffect.healAmount);
        if (!inventory.RemoveItem(foodItem.itemID, out ItemsData _, 1))
        {
            return false;
        }

        profile.currentHp = Mathf.Clamp(profile.currentHp + healAmount, 0, GetPlayerMaxHp());
        profile.foodCooldownEndsAt = Time.unscaledTime + Mathf.Max(0f, foodCooldownSeconds);
        AddCombatLog($"Consumed {foodItem.displayName} and restored {healAmount} HP.");
        ValidateLoadout();
        NotifyStateChanged();
        return true;
    }

    public bool CanUsePotion()
    {
        ItemsData potionItem = profile.loadout != null ? profile.loadout.potion : null;
        if (potionItem == null || inventory == null)
        {
            return false;
        }

        CombatItemDefinition combatData = potionItem.combatData;
        if (combatData == null || combatData.itemRole != CombatItemRole.Potion)
        {
            return false;
        }

        return inventory.GetQuantity(potionItem) > 0 && combatData.potionEffect != null && combatData.potionEffect.durationSeconds > 0f;
    }

    public bool UsePotion()
    {
        if (!CanUsePotion())
        {
            return false;
        }

        ItemsData potionItem = profile.loadout.potion;
        if (!inventory.RemoveItem(potionItem.itemID, out ItemsData _, 1))
        {
            return false;
        }

        profile.activePotionItem = potionItem;
        profile.potionExpiresAt = Time.unscaledTime + Mathf.Max(0f, potionItem.combatData.potionEffect.durationSeconds);
        AddCombatLog($"Activated potion: {potionItem.displayName}.");
        ValidateLoadout();
        NotifyStateChanged();
        return true;
    }

    void HandleInventoryChanged()
    {
        ValidateLoadout();
        NotifyStateChanged();
    }

    void ValidateLoadout()
    {
        if (profile == null)
        {
            return;
        }

        profile.EnsureInitialized();
        bool stateChanged = false;

        List<(CombatEquipSlot slot, int utilityIndex, ItemsData itemData)> equippedItems = new();
        foreach ((CombatEquipSlot slot, int utilityIndex, ItemsData itemData) in profile.loadout.Enumerate())
        {
            equippedItems.Add((slot, utilityIndex, itemData));
        }

        for (int i = 0; i < equippedItems.Count; i++)
        {
            (CombatEquipSlot slot, int utilityIndex, ItemsData itemData) entry = equippedItems[i];
            if (entry.itemData == null)
            {
                continue;
            }

            if (!IsEquippedItemStillValid(entry.itemData, entry.slot))
            {
                profile.loadout.SetItem(entry.slot, null, entry.utilityIndex);
                stateChanged = true;
            }
        }

        profile.currentHp = Mathf.Clamp(profile.currentHp, 0, GetPlayerMaxHp());
        if (stateChanged)
        {
            AddCombatLog("Loadout updated to match inventory.");
        }
    }

    bool IsEquippedItemStillValid(ItemsData itemData, CombatEquipSlot slot)
    {
        if (itemData == null || inventory == null || inventory.GetQuantity(itemData) <= 0)
        {
            return false;
        }

        CombatItemDefinition combatData = itemData.combatData;
        if (combatData == null)
        {
            return false;
        }

        combatData.EnsureInitialized();
        if (!combatData.AllowsSlot(slot))
        {
            return false;
        }

        if (slot == CombatEquipSlot.Food)
        {
            return combatData.itemRole == CombatItemRole.Food;
        }

        if (slot == CombatEquipSlot.Potion)
        {
            return combatData.itemRole == CombatItemRole.Potion;
        }

        return combatData.itemRole == CombatItemRole.Equipment &&
               ConditionRuleUtility.AreAllMet(combatData.equipRequirements, ConditionContext.Empty);
    }

    int GetAvailableEquipCopies(ItemsData itemData, CombatEquipSlot targetSlot, int targetUtilityIndex)
    {
        if (itemData == null || inventory == null)
        {
            return 0;
        }

        int ownedCopies = inventory.GetQuantity(itemData);
        int equippedCopies = CountEquippedCopies(itemData);
        if (GetEquippedItem(targetSlot, targetUtilityIndex) == itemData)
        {
            equippedCopies--;
        }

        return ownedCopies - equippedCopies;
    }

    int CountEquippedCopies(ItemsData itemData)
    {
        if (itemData == null || profile?.loadout == null)
        {
            return 0;
        }

        int count = 0;
        foreach ((CombatEquipSlot slot, int utilityIndex, ItemsData equippedItem) in profile.loadout.Enumerate())
        {
            if (equippedItem == itemData)
            {
                count++;
            }
        }

        return count;
    }

    public static string GetSlotDisplayName(CombatEquipSlot slot, int utilityIndex = -1)
    {
        return slot switch
        {
            CombatEquipSlot.Weapon => "Weapon",
            CombatEquipSlot.Offhand => "Offhand",
            CombatEquipSlot.Helmet => "Helmet",
            CombatEquipSlot.Body => "Body",
            CombatEquipSlot.Legs => "Legs",
            CombatEquipSlot.Gloves => "Gloves",
            CombatEquipSlot.Boots => "Boots",
            CombatEquipSlot.Cape => "Cape",
            CombatEquipSlot.Ammo => "Ammo",
            CombatEquipSlot.Food => "Food",
            CombatEquipSlot.Potion => "Potion",
            CombatEquipSlot.Utility => utilityIndex >= 0 ? $"Utility {utilityIndex + 1}" : "Utility",
            _ => "Unknown"
        };
    }
}
