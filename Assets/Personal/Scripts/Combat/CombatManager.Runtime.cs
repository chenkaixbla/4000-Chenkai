using UnityEngine;

public partial class CombatManager
{
    bool UpdateTimedStatuses()
    {
        bool stateChanged = false;

        // Expire potion effects individually so concurrent effects can end independently.
        float now = Time.unscaledTime;
        for (int i = profile.activePotionEffects.Count - 1; i >= 0; i--)
        {
            ActivePotionEffectState effect = profile.activePotionEffects[i];
            if (effect == null || effect.sourceItem == null)
            {
                profile.activePotionEffects.RemoveAt(i);
                stateChanged = true;
                continue;
            }

            if (effect.expiresAt > now)
            {
                continue;
            }

            AddCombatLog($"Potion effect faded: {effect.sourceItem.displayName}.");
            profile.activePotionEffects.RemoveAt(i);
            stateChanged = true;
        }

        potionWasActive = profile.activePotionEffects.Count > 0;

        bool deathDebuffActive = IsDeathDebuffActive();
        if (deathDebuffWasActive && !deathDebuffActive)
        {
            AddCombatLog("Death debuff expired.");
            stateChanged = true;
        }

        deathDebuffWasActive = deathDebuffActive;
        return stateChanged;
    }

    void SpawnEncounter(MonsterData monsterData)
    {
        if (monsterData == null)
        {
            return;
        }

        encounter.monsterData = monsterData;
        encounter.currentMonsterHp = Mathf.Max(1, monsterData.maxHp);
        encounter.isRespawning = false;

        float now = Time.unscaledTime;
        encounter.playerAttackReadyTime = now + GetPlayerAttackInterval();
        encounter.monsterAttackReadyTime = now + Mathf.Max(0.1f, monsterData.attackInterval);
        AddCombatLog($"{monsterData.displayName} appears.");
    }

    void ResolvePlayerAttack()
    {
        if (encounter.monsterData == null)
        {
            return;
        }

        string blockedReason = GetPlayerAttackBlockedReason();
        if (!string.IsNullOrWhiteSpace(blockedReason))
        {
            return;
        }

        MonsterData monsterData = encounter.monsterData;
        CombatAttackType playerAttackType = GetPlayerAttackType();
        int attackRoll = GetPlayerAccuracyRating();
        int defenceRoll = monsterData.GetEvasionForAttack(playerAttackType);
        float hitChance = CombatMath.GetHitChance(attackRoll, defenceRoll);

        if (UnityEngine.Random.value > hitChance)
        {
            AddCombatLog($"You miss {monsterData.displayName}.");
            NotifyStateChanged();
            return;
        }

        int maxHit = GetPlayerMaxHit();
        int rawDamage = maxHit > 0 ? UnityEngine.Random.Range(1, maxHit + 1) : 0;
        int damage = CombatMath.ApplyDamageReduction(rawDamage, monsterData.damageReductionPercent);
        encounter.currentMonsterHp = Mathf.Max(0, encounter.currentMonsterHp - damage);
        AwardPlayerDamageExperience(damage);
        AddCombatLog($"You hit {monsterData.displayName} for {damage}.");

        if (encounter.currentMonsterHp <= 0)
        {
            HandleMonsterDefeated(monsterData);
        }

        NotifyStateChanged();
    }

    void ResolveMonsterAttack()
    {
        if (encounter.monsterData == null)
        {
            return;
        }

        MonsterData monsterData = encounter.monsterData;
        int playerEvasion = GetPlayerEvasionRating(monsterData.attackType);
        float hitChance = CombatMath.GetHitChance(Mathf.Max(0, monsterData.attackAccuracy), playerEvasion);

        if (UnityEngine.Random.value > hitChance)
        {
            AddCombatLog($"{monsterData.displayName} misses you.");
            NotifyStateChanged();
            return;
        }

        int rawDamage = monsterData.maxHit > 0 ? UnityEngine.Random.Range(1, monsterData.maxHit + 1) : 0;
        int damage = CombatMath.ApplyDamageReduction(rawDamage, GetPlayerDamageReductionPercent());
        profile.currentHp = Mathf.Max(0, profile.currentHp - damage);
        AddCombatLog($"{monsterData.displayName} hits you for {damage}.");

        if (profile.currentHp <= 0)
        {
            HandlePlayerDeath();
        }

        NotifyStateChanged();
    }

    void AwardPlayerDamageExperience(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        int previousMaxHp = GetPlayerMaxHp();
        int combatXp = damage * 4;
        int hitpointsXp = Mathf.FloorToInt(damage * 1.3f);
        CombatAttackType attackType = GetPlayerAttackType();

        if (attackType == CombatAttackType.Ranged)
        {
            profile.range.AddXP(combatXp);
        }
        else
        {
            // Without combat styles, split melee XP across attack/strength/defence while preserving total XP.
            int splitXp = combatXp / 3;
            int remainder = combatXp % 3;
            profile.attack.AddXP(splitXp + (remainder > 0 ? 1 : 0));
            profile.strength.AddXP(splitXp + (remainder > 1 ? 1 : 0));
            profile.defence.AddXP(splitXp);
        }

        profile.hitpoints.AddXP(hitpointsXp);
        int newMaxHp = GetPlayerMaxHp();
        if (newMaxHp > previousMaxHp && profile.currentHp > 0)
        {
            profile.currentHp = Mathf.Clamp(profile.currentHp + (newMaxHp - previousMaxHp), 0, newMaxHp);
        }
        else
        {
            profile.currentHp = Mathf.Clamp(profile.currentHp, 0, newMaxHp);
        }
    }

    void HandleMonsterDefeated(MonsterData monsterData)
    {
        if (monsterData == null)
        {
            return;
        }

        int defeatCount = IncrementMonsterKillCount(monsterData);
        AddCombatLog($"{monsterData.displayName} defeated. Total defeats: {defeatCount}.");
        GrantDrops(monsterData.normalDrops, false);

        if (defeatCount <= 10)
        {
            GrantDrops(monsterData.firstTenBonusDrops, true);
        }

        encounter.Clear();
        if (autoCombatEnabled && selectedMonster == monsterData && profile.currentHp > 0)
        {
            encounter.isRespawning = true;
            encounter.respawnReadyTime = Time.unscaledTime + Mathf.Max(0.1f, monsterRespawnDelay);
        }
    }

    void HandlePlayerDeath()
    {
        autoCombatEnabled = false;
        encounter.Clear();
        profile.currentHp = 0;
        profile.deathDebuffExpiresAt = Time.unscaledTime + Mathf.Max(0f, deathDebuffDurationSeconds);
        AddCombatLog("You died. Combat stopped and idles suffer a temporary loot penalty.");
    }

    void GrantDrops(System.Collections.Generic.List<MonsterDropEntry> drops, bool isBonusDrop)
    {
        if (drops == null || inventory == null)
        {
            return;
        }

        for (int i = 0; i < drops.Count; i++)
        {
            MonsterDropEntry entry = drops[i];
            if (entry == null || entry.itemData == null)
            {
                continue;
            }

            if (UnityEngine.Random.value > Mathf.Clamp01(entry.dropChance))
            {
                continue;
            }

            int quantity = entry.RollQuantity();
            if (quantity <= 0)
            {
                continue;
            }

            inventory.AddItem(entry.itemData, quantity);
            AddCombatLog(isBonusDrop
                ? $"Bonus drop: {entry.itemData.displayName} x{quantity}"
                : $"Loot: {entry.itemData.displayName} x{quantity}");
        }
    }

    int IncrementMonsterKillCount(MonsterData monsterData)
    {
        if (monsterData == null || string.IsNullOrWhiteSpace(monsterData.guid))
        {
            return 0;
        }

        int currentCount = GetMonsterKillCount(monsterData) + 1;
        monsterKillCounts[monsterData.guid] = currentCount;
        return currentCount;
    }
}
