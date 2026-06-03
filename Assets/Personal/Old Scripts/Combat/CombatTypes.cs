using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public enum CombatAttackType
{
    Melee,
    Ranged,
    Magic
}

public enum CombatEquipSlot
{
    None,
    Weapon,
    Offhand,
    Helmet,
    Body,
    Legs,
    Gloves,
    Boots,
    Cape,
    Ammo,
    Food,
    Potion,
    Utility
}

public enum CombatEquipmentSlot
{
    None,
    Weapon,
    Offhand,
    Helmet,
    Body,
    Legs,
    Gloves,
    Boots,
    Cape,
    Ammo,
    Utility
}

public enum CombatItemRole
{
    None,
    Equipment,
    Food,
    Potion
}

public enum CombatSkillType
{
    Hitpoints,
    Attack,
    Strength,
    Defence,
    Range
}

[Serializable]
public class CombatStatBonuses
{
    public int meleeAccuracyBonus;
    public int rangedAccuracyBonus;
    public int meleeStrengthBonus;
    public int rangedStrengthBonus;
    public int meleeEvasionBonus;
    public int rangedEvasionBonus;
    public int magicEvasionBonus;
    public int damageReductionPercent;
    // Positive values reduce food cooldown delay, negative values increase it.
    public int foodCooldownModifierPercent;

    public void Add(CombatStatBonuses other)
    {
        if (other == null)
        {
            return;
        }

        meleeAccuracyBonus += other.meleeAccuracyBonus;
        rangedAccuracyBonus += other.rangedAccuracyBonus;
        meleeStrengthBonus += other.meleeStrengthBonus;
        rangedStrengthBonus += other.rangedStrengthBonus;
        meleeEvasionBonus += other.meleeEvasionBonus;
        rangedEvasionBonus += other.rangedEvasionBonus;
        magicEvasionBonus += other.magicEvasionBonus;
        damageReductionPercent += other.damageReductionPercent;
        foodCooldownModifierPercent += other.foodCooldownModifierPercent;
    }

    public static CombatStatBonuses Combine(params CombatStatBonuses[] bonuses)
    {
        CombatStatBonuses combined = new();
        if (bonuses == null)
        {
            return combined;
        }

        for (int i = 0; i < bonuses.Length; i++)
        {
            combined.Add(bonuses[i]);
        }

        return combined;
    }
}

[Serializable]
public class FoodEffectData
{
    [Min(0)] public int healAmount = 1;
}

[Serializable]
public class PotionEffectData
{
    // Unique effect identity used to prevent duplicate effect stacking.
    public string effectId;

    [Min(0f)] public float durationSeconds = 30f;
    public int attackLevelBonus;
    public int strengthLevelBonus;
    public int defenceLevelBonus;
    public int rangeLevelBonus;
    public CombatStatBonuses statBonuses = new();

    public void EnsureInitialized()
    {
        effectId = string.IsNullOrWhiteSpace(effectId) ? string.Empty : effectId.Trim();
        statBonuses ??= new CombatStatBonuses();
    }
}

[Serializable]
public class CombatItemDefinition
{
    [HideInInspector] public CombatItemRole itemRole = CombatItemRole.None;
    [HideInInspector] public Catalog_ItemType sourceItemType = Catalog_ItemType.None;

    [ShowField(nameof(UsesSelectableEquipmentSlot))]
    public CombatEquipmentSlot equipmentSlot = CombatEquipmentSlot.None;

    [ShowField(nameof(IsWeaponItem))]
    public CombatAttackType weaponAttackType = CombatAttackType.Melee;

    [ShowField(nameof(IsWeaponItem))]
    [Min(0.1f)] public float attackIntervalSeconds = 2.4f;

    [ShowField(nameof(IsRangedWeaponItem))]
    public bool requiresAmmo;

    [ShowField(nameof(IsEquipmentRole))]
    public CombatStatBonuses statBonuses = new();

    [ShowField(nameof(IsFoodRole))]
    public FoodEffectData foodEffect = new();

    [ShowField(nameof(IsPotionRole))]
    public PotionEffectData potionEffect = new();

    [ShowField(nameof(IsEquipmentRole))]
    public List<ConditionRuleEntry> equipRequirements = new();

    public bool HasCombatRole => itemRole != CombatItemRole.None;
    public bool IsEquipmentRole => itemRole == CombatItemRole.Equipment;
    public bool IsFoodRole => itemRole == CombatItemRole.Food;
    public bool IsPotionRole => itemRole == CombatItemRole.Potion;
    public bool IsWeaponItem => itemRole == CombatItemRole.Equipment && equipmentSlot == CombatEquipmentSlot.Weapon;
    public bool IsRangedWeaponItem => IsWeaponItem && weaponAttackType == CombatAttackType.Ranged;
    public bool UsesSelectableEquipmentSlot => sourceItemType == Catalog_ItemType.Armor;

    public bool AllowsSlot(CombatEquipSlot slot)
    {
        if (slot == CombatEquipSlot.None)
        {
            return false;
        }

        return slot == GetResolvedEquipSlot();
    }

    public CombatEquipSlot GetResolvedEquipSlot()
    {
        return itemRole switch
        {
            CombatItemRole.Food => CombatEquipSlot.Food,
            CombatItemRole.Potion => CombatEquipSlot.Potion,
            CombatItemRole.Equipment when equipmentSlot != CombatEquipmentSlot.None => ToRuntimeEquipSlot(equipmentSlot),
            _ => CombatEquipSlot.None
        };
    }

    static CombatEquipSlot ToRuntimeEquipSlot(CombatEquipmentSlot slot)
    {
        return slot switch
        {
            CombatEquipmentSlot.Weapon => CombatEquipSlot.Weapon,
            CombatEquipmentSlot.Offhand => CombatEquipSlot.Offhand,
            CombatEquipmentSlot.Helmet => CombatEquipSlot.Helmet,
            CombatEquipmentSlot.Body => CombatEquipSlot.Body,
            CombatEquipmentSlot.Legs => CombatEquipSlot.Legs,
            CombatEquipmentSlot.Gloves => CombatEquipSlot.Gloves,
            CombatEquipmentSlot.Boots => CombatEquipSlot.Boots,
            CombatEquipmentSlot.Cape => CombatEquipSlot.Cape,
            CombatEquipmentSlot.Ammo => CombatEquipSlot.Ammo,
            CombatEquipmentSlot.Utility => CombatEquipSlot.Utility,
            _ => CombatEquipSlot.None
        };
    }

    public void EnsureInitialized()
    {
        statBonuses ??= new CombatStatBonuses();
        foodEffect ??= new FoodEffectData();
        potionEffect ??= new PotionEffectData();
        potionEffect.EnsureInitialized();
        equipRequirements ??= new List<ConditionRuleEntry>();

        for (int i = 0; i < equipRequirements.Count; i++)
        {
            ConditionRuleEntry entry = equipRequirements[i];
            if (entry == null)
            {
                entry = new ConditionRuleEntry();
                equipRequirements[i] = entry;
            }

            ConditionRuleUtility.EnsureConditionRuleType(entry);
        }

        ApplyItemTypeConstraints();
    }

    public void SynchronizeWithItemType(Catalog_ItemType itemType)
    {
        sourceItemType = itemType;
        ApplyItemTypeConstraints();
    }

    void ApplyItemTypeConstraints()
    {
        switch (sourceItemType)
        {
            case Catalog_ItemType.Weapon:
                itemRole = CombatItemRole.Equipment;
                equipmentSlot = CombatEquipmentSlot.Weapon;
                break;
            case Catalog_ItemType.Armor:
                itemRole = CombatItemRole.Equipment;
                if (!IsValidArmorSlot(equipmentSlot))
                {
                    equipmentSlot = CombatEquipmentSlot.None;
                }
                break;
            case Catalog_ItemType.Food:
                itemRole = CombatItemRole.Food;
                equipmentSlot = CombatEquipmentSlot.None;
                break;
            case Catalog_ItemType.Potion:
                itemRole = CombatItemRole.Potion;
                equipmentSlot = CombatEquipmentSlot.None;
                break;
            case Catalog_ItemType.Utility:
                itemRole = CombatItemRole.Equipment;
                equipmentSlot = CombatEquipmentSlot.Utility;
                break;
            default:
                itemRole = CombatItemRole.None;
                equipmentSlot = CombatEquipmentSlot.None;
                break;
        }
    }

    static bool IsValidArmorSlot(CombatEquipmentSlot slot)
    {
        return slot is CombatEquipmentSlot.None
            or CombatEquipmentSlot.Offhand
            or CombatEquipmentSlot.Helmet
            or CombatEquipmentSlot.Body
            or CombatEquipmentSlot.Legs
            or CombatEquipmentSlot.Gloves
            or CombatEquipmentSlot.Boots
            or CombatEquipmentSlot.Cape
            or CombatEquipmentSlot.Ammo;
    }
}

[Serializable]
public class CombatSkillProgress
{
    public int currentLevel;
    public int currentXP;
    public int maxXP;

    public void EnsureInitialized(int defaultLevel)
    {
        currentLevel = Mathf.Max(defaultLevel, currentLevel);
        currentXP = Mathf.Max(0, currentXP);
        CheckLevel();
    }

    public int AddXP(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int beforeLevel = currentLevel;
        currentXP += amount;
        CheckLevel();
        return currentLevel - beforeLevel;
    }

    void CheckLevel()
    {
        maxXP = XPUtility.GetMaxXPForLevel(currentLevel);
        if (maxXP <= 0)
        {
            return;
        }

        while (currentXP >= maxXP)
        {
            currentXP -= maxXP;
            currentLevel++;
            maxXP = XPUtility.GetMaxXPForLevel(currentLevel);
            if (maxXP <= 0)
            {
                break;
            }
        }
    }
}

[Serializable]
public class EquipmentLoadout
{
    public const int UtilitySlotCount = 3;
    public const int PotionSlotCount = 3;

    public ItemsData weapon;
    public ItemsData offhand;
    public ItemsData helmet;
    public ItemsData body;
    public ItemsData legs;
    public ItemsData gloves;
    public ItemsData boots;
    public ItemsData cape;
    public ItemsData ammo;
    public ItemsData food;
    public List<ItemsData> potionSlots = new();
    public List<ItemsData> utilitySlots = new();

    public void EnsureInitialized()
    {
        potionSlots ??= new List<ItemsData>();
        while (potionSlots.Count < PotionSlotCount)
        {
            potionSlots.Add(null);
        }

        while (potionSlots.Count > PotionSlotCount)
        {
            potionSlots.RemoveAt(potionSlots.Count - 1);
        }

        utilitySlots ??= new List<ItemsData>();
        while (utilitySlots.Count < UtilitySlotCount)
        {
            utilitySlots.Add(null);
        }

        while (utilitySlots.Count > UtilitySlotCount)
        {
            utilitySlots.RemoveAt(utilitySlots.Count - 1);
        }
    }

    public ItemsData GetPotionSlot(int potionIndex)
    {
        if (potionIndex < 0 || potionIndex >= potionSlots.Count)
        {
            return null;
        }

        return potionSlots[potionIndex];
    }

    public void SetPotionSlot(int potionIndex, ItemsData itemData)
    {
        if (potionIndex < 0 || potionIndex >= potionSlots.Count)
        {
            return;
        }

        potionSlots[potionIndex] = itemData;
    }

    public ItemsData GetItem(CombatEquipSlot slot, int utilityIndex = -1)
    {
        return slot switch
        {
            CombatEquipSlot.Weapon => weapon,
            CombatEquipSlot.Offhand => offhand,
            CombatEquipSlot.Helmet => helmet,
            CombatEquipSlot.Body => body,
            CombatEquipSlot.Legs => legs,
            CombatEquipSlot.Gloves => gloves,
            CombatEquipSlot.Boots => boots,
            CombatEquipSlot.Cape => cape,
            CombatEquipSlot.Ammo => ammo,
            CombatEquipSlot.Food => food,
            CombatEquipSlot.Potion => utilityIndex >= 0 ? GetPotionSlot(utilityIndex) : GetPotionSlot(0),
            CombatEquipSlot.Utility => utilityIndex >= 0 && utilityIndex < utilitySlots.Count ? utilitySlots[utilityIndex] : null,
            _ => null
        };
    }

    public void SetItem(CombatEquipSlot slot, ItemsData itemData, int utilityIndex = -1)
    {
        switch (slot)
        {
            case CombatEquipSlot.Weapon:
                weapon = itemData;
                break;
            case CombatEquipSlot.Offhand:
                offhand = itemData;
                break;
            case CombatEquipSlot.Helmet:
                helmet = itemData;
                break;
            case CombatEquipSlot.Body:
                body = itemData;
                break;
            case CombatEquipSlot.Legs:
                legs = itemData;
                break;
            case CombatEquipSlot.Gloves:
                gloves = itemData;
                break;
            case CombatEquipSlot.Boots:
                boots = itemData;
                break;
            case CombatEquipSlot.Cape:
                cape = itemData;
                break;
            case CombatEquipSlot.Ammo:
                ammo = itemData;
                break;
            case CombatEquipSlot.Food:
                food = itemData;
                break;
            case CombatEquipSlot.Potion:
                SetPotionSlot(utilityIndex >= 0 ? utilityIndex : 0, itemData);
                break;
            case CombatEquipSlot.Utility:
                if (utilityIndex >= 0 && utilityIndex < utilitySlots.Count)
                {
                    utilitySlots[utilityIndex] = itemData;
                }
                break;
        }
    }

    public IEnumerable<(CombatEquipSlot slot, int utilityIndex, ItemsData itemData)> Enumerate()
    {
        yield return (CombatEquipSlot.Weapon, -1, weapon);
        yield return (CombatEquipSlot.Offhand, -1, offhand);
        yield return (CombatEquipSlot.Helmet, -1, helmet);
        yield return (CombatEquipSlot.Body, -1, body);
        yield return (CombatEquipSlot.Legs, -1, legs);
        yield return (CombatEquipSlot.Gloves, -1, gloves);
        yield return (CombatEquipSlot.Boots, -1, boots);
        yield return (CombatEquipSlot.Cape, -1, cape);
        yield return (CombatEquipSlot.Ammo, -1, ammo);
        yield return (CombatEquipSlot.Food, -1, food);

        for (int i = 0; i < potionSlots.Count; i++)
        {
            yield return (CombatEquipSlot.Potion, i, potionSlots[i]);
        }

        for (int i = 0; i < utilitySlots.Count; i++)
        {
            yield return (CombatEquipSlot.Utility, i, utilitySlots[i]);
        }
    }
}

[Serializable]
public class ActivePotionEffectState
{
    public string effectId;
    public ItemsData sourceItem;
    public float expiresAt;
}

[Serializable]
public class CombatProfile
{
    public CombatSkillProgress hitpoints = new();
    public CombatSkillProgress attack = new();
    public CombatSkillProgress strength = new();
    public CombatSkillProgress defence = new();
    public CombatSkillProgress range = new();
    public EquipmentLoadout loadout = new();
    public int currentHp;
    public float foodCooldownEndsAt;

    // Active potion effects are now tracked independently and can run concurrently.
    public List<ActivePotionEffectState> activePotionEffects = new();

    // Auto-use toggles map to potion slot indices.
    public List<bool> potionAutoUseEnabled = new();

    // Global food auto-use settings.
    public bool foodAutoUseEnabled;
    [Range(0f, 1f)] public float foodAutoUseThresholdPercent = 0.35f;

    public float deathDebuffExpiresAt;
    public bool isInitialized;

    public void EnsureInitialized()
    {
        loadout ??= new EquipmentLoadout();
        loadout.EnsureInitialized();

        activePotionEffects ??= new List<ActivePotionEffectState>();
        activePotionEffects.RemoveAll(effect => effect == null || effect.sourceItem == null);

        potionAutoUseEnabled ??= new List<bool>();
        while (potionAutoUseEnabled.Count < EquipmentLoadout.PotionSlotCount)
        {
            potionAutoUseEnabled.Add(false);
        }

        while (potionAutoUseEnabled.Count > EquipmentLoadout.PotionSlotCount)
        {
            potionAutoUseEnabled.RemoveAt(potionAutoUseEnabled.Count - 1);
        }

        foodAutoUseThresholdPercent = Mathf.Clamp01(foodAutoUseThresholdPercent);

        if (!isInitialized)
        {
            hitpoints.currentLevel = Mathf.Max(10, hitpoints.currentLevel);
            attack.currentLevel = Mathf.Max(1, attack.currentLevel);
            strength.currentLevel = Mathf.Max(1, strength.currentLevel);
            defence.currentLevel = Mathf.Max(1, defence.currentLevel);
            range.currentLevel = Mathf.Max(1, range.currentLevel);
        }

        hitpoints.EnsureInitialized(10);
        attack.EnsureInitialized(1);
        strength.EnsureInitialized(1);
        defence.EnsureInitialized(1);
        range.EnsureInitialized(1);

        if (!isInitialized)
        {
            currentHp = GetMaxHitpoints();
        }
        else
        {
            currentHp = Mathf.Clamp(currentHp, 0, GetMaxHitpoints());
        }

        isInitialized = true;
    }

    public CombatSkillProgress GetSkill(CombatSkillType skillType)
    {
        return skillType switch
        {
            CombatSkillType.Hitpoints => hitpoints,
            CombatSkillType.Attack => attack,
            CombatSkillType.Strength => strength,
            CombatSkillType.Defence => defence,
            CombatSkillType.Range => range,
            _ => null
        };
    }

    public int GetMaxHitpoints()
    {
        return Mathf.Max(10, hitpoints.currentLevel);
    }
}

[Serializable]
public class CombatEncounterState
{
    public MonsterData monsterData;
    public int currentMonsterHp;
    public float playerAttackReadyTime;
    public float monsterAttackReadyTime;
    public bool isRespawning;
    public float respawnReadyTime;

    public void Clear()
    {
        monsterData = null;
        currentMonsterHp = 0;
        playerAttackReadyTime = 0f;
        monsterAttackReadyTime = 0f;
        isRespawning = false;
        respawnReadyTime = 0f;
    }
}

public static class CombatMath
{
    public static float GetHitChance(int attackRoll, int defenceRoll)
    {
        attackRoll = Mathf.Max(0, attackRoll);
        defenceRoll = Mathf.Max(0, defenceRoll);

        if (attackRoll <= 0)
        {
            return 0f;
        }

        if (defenceRoll <= 0)
        {
            return 1f;
        }

        if (attackRoll > defenceRoll)
        {
            return 1f - ((defenceRoll + 2f) / (2f * (attackRoll + 1f)));
        }

        return attackRoll / (2f * (defenceRoll + 1f));
    }

    public static int GetAttackRoll(int effectiveLevel, int attackBonus)
    {
        return Mathf.Max(0, effectiveLevel) * (Mathf.Max(0, attackBonus) + 64);
    }

    public static int GetDefenceRoll(int effectiveLevel, int evasionBonus)
    {
        return Mathf.Max(0, effectiveLevel) * (Mathf.Max(0, evasionBonus) + 64);
    }

    public static int GetMaxHit(int effectiveLevel, int strengthBonus)
    {
        if (effectiveLevel <= 0)
        {
            return 0;
        }

        int rawMaxHit = (effectiveLevel * (Mathf.Max(0, strengthBonus) + 64) + 320) / 640;
        return Mathf.Max(1, rawMaxHit);
    }

    public static int ApplyDamageReduction(int rawDamage, int damageReductionPercent)
    {
        rawDamage = Mathf.Max(0, rawDamage);
        if (rawDamage <= 0)
        {
            return 0;
        }

        int clampedReduction = Mathf.Clamp(damageReductionPercent, 0, 80);
        float reducedDamage = rawDamage * (1f - (clampedReduction / 100f));
        return Mathf.Max(1, Mathf.FloorToInt(reducedDamage));
    }
}
