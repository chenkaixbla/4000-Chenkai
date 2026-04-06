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
            LogVerboseWarning($"GetCompatibleItemsForSlot aborted: inventory is null. slot: {slot}, slotIndex: {utilityIndex}.");
            return results;
        }

        IReadOnlyList<ItemsInstance> trackedItems = inventory.GetTrackedItems();
        LogVerbose($"GetCompatibleItemsForSlot started. slot: {slot}, slotIndex: {utilityIndex}, trackedItemCount: {trackedItems.Count}.");

        for (int i = 0; i < trackedItems.Count; i++)
        {
            ItemsInstance instance = trackedItems[i];
            if (instance == null || instance.itemData == null || instance.quantity <= 0)
            {
                LogVerboseWarning($"Skipping tracked item at index {i} during compatibility query. Null instance: {instance == null}, null itemData: {instance?.itemData == null}, quantity: {instance?.quantity ?? 0}.");
                continue;
            }

            if (TryGetEquipIncompatibilityReason(instance.itemData, slot, utilityIndex, out string incompatibilityReason, out int availableCopies))
            {
                results.Add(instance);
                LogVerbose($"Compatible item accepted: {instance.itemData.displayName}. quantity: {instance.quantity}, availableCopies: {availableCopies}.");
            }
            else
            {
                LogVerboseWarning($"Compatible item rejected: {instance.itemData.displayName}. quantity: {instance.quantity}, reason: {incompatibilityReason}.");
            }
        }

        results.Sort((left, right) => string.Compare(left.itemData.displayName, right.itemData.displayName, StringComparison.OrdinalIgnoreCase));
        LogVerbose($"GetCompatibleItemsForSlot finished. slot: {slot}, slotIndex: {utilityIndex}, compatibleCount: {results.Count}.");
        return results;
    }

    public void LogItemSelectionCompatibilityBreakdown(CombatEquipSlot slot, int utilityIndex = -1)
    {
        if (inventory == null)
        {
            LogVerboseWarning($"Compatibility breakdown skipped: inventory is null. slot: {slot}, slotIndex: {utilityIndex}.");
            return;
        }

        IReadOnlyList<ItemsInstance> trackedItems = inventory.GetTrackedItems();
        LogVerbose($"Compatibility breakdown started. slot: {slot}, slotIndex: {utilityIndex}, trackedItemCount: {trackedItems.Count}.");

        for (int i = 0; i < trackedItems.Count; i++)
        {
            ItemsInstance instance = trackedItems[i];
            if (instance == null)
            {
                LogVerboseWarning($"Breakdown item {i}: instance is null.");
                continue;
            }

            if (instance.itemData == null)
            {
                LogVerboseWarning($"Breakdown item {i}: itemData is null.");
                continue;
            }

            if (TryGetEquipIncompatibilityReason(instance.itemData, slot, utilityIndex, out string incompatibilityReason, out int availableCopies))
            {
                LogVerbose($"Breakdown item {i}: {instance.itemData.displayName} is compatible. quantity: {instance.quantity}, availableCopies: {availableCopies}.");
            }
            else
            {
                LogVerboseWarning($"Breakdown item {i}: {instance.itemData.displayName} is incompatible. quantity: {instance.quantity}, reason: {incompatibilityReason}.");
            }
        }
    }

    bool TryGetEquipIncompatibilityReason(ItemsData itemData, CombatEquipSlot slot, int utilityIndex, out string incompatibilityReason, out int availableCopies)
    {
        availableCopies = 0;
        incompatibilityReason = string.Empty;

        if (itemData == null)
        {
            incompatibilityReason = "itemData is null.";
            return false;
        }

        if (inventory == null)
        {
            incompatibilityReason = "inventory is null.";
            return false;
        }

        int quantity = inventory.GetQuantity(itemData);
        if (quantity <= 0)
        {
            incompatibilityReason = "quantity is zero in inventory.";
            return false;
        }

        CombatItemDefinition combatData = itemData.combatData;
        if (combatData == null)
        {
            incompatibilityReason = "combatData is null.";
            return false;
        }

        combatData.EnsureInitialized();
        if (!combatData.AllowsSlot(slot))
        {
            incompatibilityReason = $"item does not allow slot {slot}.";
            return false;
        }

        if (slot == CombatEquipSlot.Food && combatData.itemRole != CombatItemRole.Food)
        {
            incompatibilityReason = $"itemRole is {combatData.itemRole}, expected Food.";
            return false;
        }

        if (slot == CombatEquipSlot.Potion && combatData.itemRole != CombatItemRole.Potion)
        {
            incompatibilityReason = $"itemRole is {combatData.itemRole}, expected Potion.";
            return false;
        }

        if (slot != CombatEquipSlot.Food && slot != CombatEquipSlot.Potion && combatData.itemRole != CombatItemRole.Equipment)
        {
            incompatibilityReason = $"itemRole is {combatData.itemRole}, expected Equipment for slot {slot}.";
            return false;
        }

        if (!ConditionRuleUtility.AreAllMet(combatData.equipRequirements, ConditionContext.Empty))
        {
            incompatibilityReason = "equip requirements are not met.";
            return false;
        }

        availableCopies = GetAvailableEquipCopies(itemData, slot, utilityIndex);
        if (availableCopies <= 0)
        {
            incompatibilityReason = "no un-equipped copies available.";
            return false;
        }

        return true;
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
        profile.foodCooldownEndsAt = Time.unscaledTime + GetFoodCooldownDurationSeconds();
        AddCombatLog($"Consumed {foodItem.displayName} and restored {healAmount} HP.");
        ValidateLoadout();
        NotifyStateChanged();
        return true;
    }

    public ItemsData GetPotionSlotItem(int potionSlotIndex)
    {
        profile.loadout.EnsureInitialized();
        return profile.loadout.GetPotionSlot(potionSlotIndex);
    }

    public bool CanUsePotion()
    {
        return CanUsePotion(0);
    }

    public bool CanUsePotion(int potionSlotIndex)
    {
        ItemsData potionItem = GetPotionSlotItem(potionSlotIndex);
        if (potionItem == null || inventory == null)
        {
            return false;
        }

        CombatItemDefinition combatData = potionItem.combatData;
        if (combatData == null || combatData.itemRole != CombatItemRole.Potion)
        {
            return false;
        }

        if (combatData.potionEffect == null || combatData.potionEffect.durationSeconds <= 0f)
        {
            return false;
        }

        string effectId = ResolvePotionEffectId(potionItem);
        if (IsPotionEffectActive(effectId))
        {
            return false;
        }

        return inventory.GetQuantity(potionItem) > 0;
    }

    public bool UsePotion()
    {
        return UsePotion(0);
    }

    public bool UsePotion(int potionSlotIndex)
    {
        if (!CanUsePotion(potionSlotIndex))
        {
            return false;
        }

        ItemsData potionItem = GetPotionSlotItem(potionSlotIndex);
        if (!inventory.RemoveItem(potionItem.itemID, out ItemsData _, 1))
        {
            return false;
        }

        string effectId = ResolvePotionEffectId(potionItem);
        float expiresAt = Time.unscaledTime + Mathf.Max(0f, potionItem.combatData.potionEffect.durationSeconds);
        profile.activePotionEffects.Add(new ActivePotionEffectState
        {
            effectId = effectId,
            sourceItem = potionItem,
            expiresAt = expiresAt
        });

        AddCombatLog($"Activated potion: {potionItem.displayName}.");
        ValidateLoadout();
        NotifyStateChanged();
        return true;
    }

    bool TryAutoUseConfiguredConsumables()
    {
        if (!autoCombatEnabled || encounter.monsterData == null || profile.currentHp <= 0)
        {
            return false;
        }

        bool consumedAnything = false;
        if (profile.foodAutoUseEnabled && TryAutoUseFood())
        {
            consumedAnything = true;
        }

        if (TryAutoUsePotions())
        {
            consumedAnything = true;
        }

        return consumedAnything;
    }

    bool TryAutoUseFood()
    {
        int maxHp = GetPlayerMaxHp();
        if (maxHp <= 0)
        {
            return false;
        }

        float hpPercent = (float)profile.currentHp / maxHp;
        if (hpPercent > Mathf.Clamp01(profile.foodAutoUseThresholdPercent))
        {
            return false;
        }

        return UseFood();
    }

    bool TryAutoUsePotions()
    {
        bool consumedAnything = false;
        for (int slotIndex = 0; slotIndex < EquipmentLoadout.PotionSlotCount; slotIndex++)
        {
            if (!GetPotionAutoUseEnabled(slotIndex))
            {
                continue;
            }

            ItemsData potionItem = GetPotionSlotItem(slotIndex);
            if (potionItem == null)
            {
                continue;
            }

            string effectId = ResolvePotionEffectId(potionItem);
            if (IsPotionEffectActive(effectId))
            {
                continue;
            }

            if (UsePotion(slotIndex))
            {
                consumedAnything = true;
            }
        }

        return consumedAnything;
    }

    float GetFoodCooldownDurationSeconds()
    {
        int cooldownModifierPercent = GetFoodCooldownModifierPercent();
        float cooldownMultiplier = Mathf.Max(0f, 1f - (cooldownModifierPercent / 100f));
        return Mathf.Max(0f, foodCooldownSeconds * cooldownMultiplier);
    }

    string ResolvePotionEffectId(ItemsData potionItem)
    {
        if (potionItem == null || potionItem.combatData == null || potionItem.combatData.potionEffect == null)
        {
            return string.Empty;
        }

        string configuredEffectId = potionItem.combatData.potionEffect.effectId;
        if (!string.IsNullOrWhiteSpace(configuredEffectId))
        {
            return configuredEffectId.Trim();
        }

        return $"potion-item-{potionItem.itemID}";
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
            CombatEquipSlot.Potion => utilityIndex >= 0 ? $"Potion {utilityIndex + 1}" : "Potion 1",
            CombatEquipSlot.Utility => utilityIndex >= 0 ? $"Utility {utilityIndex + 1}" : "Utility",
            _ => "Unknown"
        };
    }
}
