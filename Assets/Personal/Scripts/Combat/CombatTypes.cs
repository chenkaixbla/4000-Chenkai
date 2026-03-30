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
    [Min(0f)] public float durationSeconds = 30f;
    public int attackLevelBonus;
    public int strengthLevelBonus;
    public int defenceLevelBonus;
    public int rangeLevelBonus;
    public CombatStatBonuses statBonuses = new();

    public void EnsureInitialized()
    {
        statBonuses ??= new CombatStatBonuses();
    }
}

[Serializable]
public class CombatItemDefinition
{
    [HideInInspector] public CombatItemRole itemRole = CombatItemRole.None;
    [HideInInspector] public Catalog_ItemType sourceItemType = Catalog_ItemType.None;

    [HideInInspector] public List<CombatEquipSlot> allowedEquipSlots = new();

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
        allowedEquipSlots ??= new List<CombatEquipSlot>();
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

        if (allowedEquipSlots.Count > 0)
        {
            equipmentSlot = GetLegacyEquipmentSlot();
            allowedEquipSlots.Clear();
        }
        ApplyItemTypeConstraints();
    }

    CombatEquipmentSlot GetLegacyEquipmentSlot()
    {
        if (allowedEquipSlots == null)
        {
            return CombatEquipmentSlot.None;
        }

        for (int i = 0; i < allowedEquipSlots.Count; i++)
        {
            CombatEquipSlot slot = allowedEquipSlots[i];
            switch (slot)
            {
                case CombatEquipSlot.Weapon:
                    return CombatEquipmentSlot.Weapon;
                case CombatEquipSlot.Offhand:
                    return CombatEquipmentSlot.Offhand;
                case CombatEquipSlot.Helmet:
                    return CombatEquipmentSlot.Helmet;
                case CombatEquipSlot.Body:
                    return CombatEquipmentSlot.Body;
                case CombatEquipSlot.Legs:
                    return CombatEquipmentSlot.Legs;
                case CombatEquipSlot.Gloves:
                    return CombatEquipmentSlot.Gloves;
                case CombatEquipSlot.Boots:
                    return CombatEquipmentSlot.Boots;
                case CombatEquipSlot.Cape:
                    return CombatEquipmentSlot.Cape;
                case CombatEquipSlot.Ammo:
                    return CombatEquipmentSlot.Ammo;
                case CombatEquipSlot.Utility:
                    return CombatEquipmentSlot.Utility;
            }
        }

        return equipmentSlot;
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
    public ItemsData potion;
    public List<ItemsData> utilitySlots = new();

    public void EnsureInitialized()
    {
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
            CombatEquipSlot.Potion => potion,
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
                potion = itemData;
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
        yield return (CombatEquipSlot.Potion, -1, potion);

        for (int i = 0; i < utilitySlots.Count; i++)
        {
            yield return (CombatEquipSlot.Utility, i, utilitySlots[i]);
        }
    }
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
    public float potionExpiresAt;
    public float deathDebuffExpiresAt;
    public ItemsData activePotionItem;
    public bool isInitialized;

    public void EnsureInitialized()
    {
        loadout ??= new EquipmentLoadout();
        loadout.EnsureInitialized();

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
