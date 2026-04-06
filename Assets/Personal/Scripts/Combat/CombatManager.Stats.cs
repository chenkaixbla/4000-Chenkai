using UnityEngine;

public partial class CombatManager
{
    public int GetPlayerMaxHp()
    {
        profile.EnsureInitialized();
        return profile.GetMaxHitpoints();
    }

    public int GetPlayerAccuracyRating()
    {
        CombatStatBonuses bonuses = GetCombinedCombatBonuses();
        int effectiveLevel = GetEffectiveAccuracyLevel();
        CombatAttackType attackType = GetPlayerAttackType();
        int attackBonus = attackType == CombatAttackType.Ranged ? bonuses.rangedAccuracyBonus : bonuses.meleeAccuracyBonus;
        return CombatMath.GetAttackRoll(effectiveLevel, attackBonus);
    }

    public int GetPlayerMaxHit()
    {
        CombatStatBonuses bonuses = GetCombinedCombatBonuses();
        int effectiveLevel = GetEffectiveStrengthLevel();
        CombatAttackType attackType = GetPlayerAttackType();
        int strengthBonus = attackType == CombatAttackType.Ranged ? bonuses.rangedStrengthBonus : bonuses.meleeStrengthBonus;
        return CombatMath.GetMaxHit(effectiveLevel, strengthBonus);
    }

    public int GetPlayerEvasionRating(CombatAttackType attackType)
    {
        CombatStatBonuses bonuses = GetCombinedCombatBonuses();
        int evasionBonus = attackType switch
        {
            CombatAttackType.Melee => bonuses.meleeEvasionBonus,
            CombatAttackType.Ranged => bonuses.rangedEvasionBonus,
            CombatAttackType.Magic => bonuses.magicEvasionBonus,
            _ => 0
        };

        return CombatMath.GetDefenceRoll(GetEffectiveDefenceLevel(), evasionBonus);
    }

    public int GetPlayerDamageReductionPercent()
    {
        return Mathf.Clamp(GetCombinedCombatBonuses().damageReductionPercent, 0, 80);
    }

    public int GetFoodCooldownModifierPercent()
    {
        return GetCombinedCombatBonuses().foodCooldownModifierPercent;
    }

    public float GetPlayerAttackInterval()
    {
        ItemsData weaponItem = profile.loadout != null ? profile.loadout.weapon : null;
        if (weaponItem != null && weaponItem.combatData != null && weaponItem.combatData.itemRole == CombatItemRole.Equipment)
        {
            return Mathf.Max(0.1f, weaponItem.combatData.attackIntervalSeconds);
        }

        return DefaultPlayerAttackInterval;
    }

    public string GetPlayerAttackBlockedReason()
    {
        if (profile.currentHp <= 0)
        {
            return "Heal before fighting.";
        }

        ItemsData weaponItem = profile.loadout != null ? profile.loadout.weapon : null;
        CombatItemDefinition weaponData = weaponItem != null ? weaponItem.combatData : null;

        if (weaponData != null &&
            weaponData.itemRole == CombatItemRole.Equipment &&
            weaponData.weaponAttackType == CombatAttackType.Ranged &&
            weaponData.requiresAmmo &&
            profile.loadout.ammo == null)
        {
            return "Equip ammo.";
        }

        return null;
    }

    CombatAttackType GetPlayerAttackType()
    {
        ItemsData weaponItem = profile.loadout != null ? profile.loadout.weapon : null;
        CombatItemDefinition weaponData = weaponItem != null ? weaponItem.combatData : null;
        if (weaponData != null && weaponData.itemRole == CombatItemRole.Equipment)
        {
            return weaponData.weaponAttackType;
        }

        return CombatAttackType.Melee;
    }

    CombatStatBonuses GetCombinedCombatBonuses()
    {
        CombatStatBonuses combined = new();
        if (profile.loadout != null)
        {
            foreach ((CombatEquipSlot slot, int utilityIndex, ItemsData itemData) in profile.loadout.Enumerate())
            {
                if (itemData == null || itemData.combatData == null || itemData.combatData.itemRole != CombatItemRole.Equipment)
                {
                    continue;
                }

                combined.Add(itemData.combatData.statBonuses);
            }
        }

        foreach (PotionEffectData potionEffect in EnumerateActivePotionEffects())
        {
            if (potionEffect == null)
            {
                continue;
            }

            potionEffect.EnsureInitialized();
            combined.Add(potionEffect.statBonuses);
        }

        return combined;
    }

    int GetEffectiveAccuracyLevel()
    {
        CombatAttackType attackType = GetPlayerAttackType();
        int baseLevel = attackType == CombatAttackType.Ranged ? profile.range.currentLevel : profile.attack.currentLevel;
        int potionBonus = attackType == CombatAttackType.Ranged
            ? GetPotionLevelBonus(CombatSkillType.Range)
            : GetPotionLevelBonus(CombatSkillType.Attack);

        return baseLevel + potionBonus + 8;
    }

    int GetEffectiveStrengthLevel()
    {
        CombatAttackType attackType = GetPlayerAttackType();
        int baseLevel = attackType == CombatAttackType.Ranged ? profile.range.currentLevel : profile.strength.currentLevel;
        int potionBonus = attackType == CombatAttackType.Ranged
            ? GetPotionLevelBonus(CombatSkillType.Range)
            : GetPotionLevelBonus(CombatSkillType.Strength);

        return baseLevel + potionBonus + 8;
    }

    int GetEffectiveDefenceLevel()
    {
        return profile.defence.currentLevel + GetPotionLevelBonus(CombatSkillType.Defence) + 8;
    }

    int GetPotionLevelBonus(CombatSkillType skillType)
    {
        int totalBonus = 0;
        foreach (PotionEffectData potionEffect in EnumerateActivePotionEffects())
        {
            if (potionEffect == null)
            {
                continue;
            }

            totalBonus += skillType switch
            {
                CombatSkillType.Attack => potionEffect.attackLevelBonus,
                CombatSkillType.Strength => potionEffect.strengthLevelBonus,
                CombatSkillType.Defence => potionEffect.defenceLevelBonus,
                CombatSkillType.Range => potionEffect.rangeLevelBonus,
                _ => 0
            };
        }

        return totalBonus;
    }

    System.Collections.Generic.IEnumerable<PotionEffectData> EnumerateActivePotionEffects()
    {
        float now = Time.unscaledTime;
        for (int i = 0; i < profile.activePotionEffects.Count; i++)
        {
            ActivePotionEffectState effectState = profile.activePotionEffects[i];
            if (effectState == null || effectState.sourceItem == null || effectState.expiresAt <= now)
            {
                continue;
            }

            CombatItemDefinition combatData = effectState.sourceItem.combatData;
            if (combatData == null || combatData.itemRole != CombatItemRole.Potion)
            {
                continue;
            }

            yield return combatData.potionEffect;
        }
    }
}
