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
        int attackBonus = profile.activeStyle == CombatStyle.Ranged ? bonuses.rangedAccuracyBonus : bonuses.meleeAccuracyBonus;
        return CombatMath.GetAttackRoll(effectiveLevel, attackBonus);
    }

    public int GetPlayerMaxHit()
    {
        CombatStatBonuses bonuses = GetCombinedCombatBonuses();
        int effectiveLevel = GetEffectiveStrengthLevel();
        int strengthBonus = profile.activeStyle == CombatStyle.Ranged ? bonuses.rangedStrengthBonus : bonuses.meleeStrengthBonus;
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

        if (profile.activeStyle == CombatStyle.Ranged)
        {
            if (weaponData == null || weaponData.itemRole != CombatItemRole.Equipment || weaponData.weaponAttackType != CombatAttackType.Ranged)
            {
                return "Equip a ranged weapon.";
            }

            if (weaponData.requiresAmmo && profile.loadout.ammo == null)
            {
                return "Equip ammo.";
            }
        }
        else if (weaponData != null && weaponData.itemRole == CombatItemRole.Equipment && weaponData.weaponAttackType == CombatAttackType.Ranged)
        {
            return "Switch to Ranged style or equip a melee weapon.";
        }

        return null;
    }

    CombatAttackType GetPlayerAttackType()
    {
        return profile.activeStyle == CombatStyle.Ranged ? CombatAttackType.Ranged : CombatAttackType.Melee;
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

        if (IsPotionActive() && profile.activePotionItem != null && profile.activePotionItem.combatData != null)
        {
            PotionEffectData potionEffect = profile.activePotionItem.combatData.potionEffect;
            if (potionEffect != null)
            {
                potionEffect.EnsureInitialized();
                combined.Add(potionEffect.statBonuses);
            }
        }

        return combined;
    }

    int GetEffectiveAccuracyLevel()
    {
        int baseLevel = profile.activeStyle == CombatStyle.Ranged ? profile.range.currentLevel : profile.attack.currentLevel;
        int stanceBonus = profile.activeStyle switch
        {
            CombatStyle.MeleeAccurate => 3,
            CombatStyle.Ranged => 3,
            _ => 0
        };

        int potionBonus = profile.activeStyle == CombatStyle.Ranged
            ? GetPotionLevelBonus(CombatSkillType.Range)
            : GetPotionLevelBonus(CombatSkillType.Attack);

        return baseLevel + stanceBonus + potionBonus + 8;
    }

    int GetEffectiveStrengthLevel()
    {
        int baseLevel = profile.activeStyle == CombatStyle.Ranged ? profile.range.currentLevel : profile.strength.currentLevel;
        int stanceBonus = profile.activeStyle switch
        {
            CombatStyle.MeleeAggressive => 3,
            CombatStyle.Ranged => 3,
            _ => 0
        };

        int potionBonus = profile.activeStyle == CombatStyle.Ranged
            ? GetPotionLevelBonus(CombatSkillType.Range)
            : GetPotionLevelBonus(CombatSkillType.Strength);

        return baseLevel + stanceBonus + potionBonus + 8;
    }

    int GetEffectiveDefenceLevel()
    {
        int stanceBonus = profile.activeStyle == CombatStyle.MeleeDefensive ? 3 : 0;
        return profile.defence.currentLevel + stanceBonus + GetPotionLevelBonus(CombatSkillType.Defence) + 8;
    }

    int GetPotionLevelBonus(CombatSkillType skillType)
    {
        if (!IsPotionActive() || profile.activePotionItem == null || profile.activePotionItem.combatData == null)
        {
            return 0;
        }

        PotionEffectData potionEffect = profile.activePotionItem.combatData.potionEffect;
        if (potionEffect == null)
        {
            return 0;
        }

        return skillType switch
        {
            CombatSkillType.Attack => potionEffect.attackLevelBonus,
            CombatSkillType.Strength => potionEffect.strengthLevelBonus,
            CombatSkillType.Defence => potionEffect.defenceLevelBonus,
            CombatSkillType.Range => potionEffect.rangeLevelBonus,
            _ => 0
        };
    }

    static string GetStyleName(CombatStyle style)
    {
        return style switch
        {
            CombatStyle.MeleeAccurate => "Melee Accurate",
            CombatStyle.MeleeAggressive => "Melee Aggressive",
            CombatStyle.MeleeDefensive => "Melee Defensive",
            CombatStyle.Ranged => "Ranged",
            _ => style.ToString()
        };
    }
}
