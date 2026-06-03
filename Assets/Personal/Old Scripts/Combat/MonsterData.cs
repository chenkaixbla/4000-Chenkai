using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

[Serializable]
public class MonsterDropEntry
{
    public ItemsData itemData;
    [Range(0f, 1f)] public float dropChance = 1f;
    [Min(1)] public int minQuantity = 1;
    [Min(1)] public int maxQuantity = 1;

    public int RollQuantity()
    {
        int minimum = Mathf.Max(1, minQuantity);
        int maximum = Mathf.Max(minimum, maxQuantity);
        return UnityEngine.Random.Range(minimum, maximum + 1);
    }
}

[CreateAssetMenu(fileName = "MonsterData", menuName = "Game/Combat/MonsterData")]
public class MonsterData : ScriptableObject
{
    [Title("General")]
    public string guid;
    public string displayName;
    [TextArea(2, 6)] public string description;
    [AssetPreview] public Sprite icon;

    [Title("Combat")]
    [Min(1)] public int maxHp = 10;
    [Min(0.1f)] public float attackInterval = 2.4f;
    public CombatAttackType attackType = CombatAttackType.Melee;
    [Min(0)] public int attackAccuracy = 64;
    [Min(0)] public int maxHit = 1;
    [Range(0, 80)] public int damageReductionPercent = 0;
    [Min(0)] public int meleeEvasion = 64;
    [Min(0)] public int rangedEvasion = 64;
    [Min(0)] public int magicEvasion = 64;

    [Title("Drops")]
    public List<MonsterDropEntry> normalDrops = new();
    public List<MonsterDropEntry> firstTenBonusDrops = new();

    void OnEnable()
    {
        EnsureInitialized();
    }

    void OnValidate()
    {
        EnsureInitialized();
    }

    public int GetEvasionForAttack(CombatAttackType attackType)
    {
        return attackType switch
        {
            CombatAttackType.Melee => Mathf.Max(0, meleeEvasion),
            CombatAttackType.Ranged => Mathf.Max(0, rangedEvasion),
            CombatAttackType.Magic => Mathf.Max(0, magicEvasion),
            _ => 0
        };
    }

    void EnsureInitialized()
    {
        if (string.IsNullOrWhiteSpace(guid))
        {
            guid = Guid.NewGuid().ToString();
        }

        maxHp = Mathf.Max(1, maxHp);
        attackInterval = Mathf.Max(0.1f, attackInterval);
        maxHit = Mathf.Max(0, maxHit);
        attackAccuracy = Mathf.Max(0, attackAccuracy);
        meleeEvasion = Mathf.Max(0, meleeEvasion);
        rangedEvasion = Mathf.Max(0, rangedEvasion);
        magicEvasion = Mathf.Max(0, magicEvasion);
        damageReductionPercent = Mathf.Clamp(damageReductionPercent, 0, 80);

        EnsureDropList(normalDrops);
        EnsureDropList(firstTenBonusDrops);
    }

    static void EnsureDropList(List<MonsterDropEntry> drops)
    {
        if (drops == null)
        {
            return;
        }

        for (int i = 0; i < drops.Count; i++)
        {
            MonsterDropEntry entry = drops[i];
            if (entry == null)
            {
                drops[i] = new MonsterDropEntry();
                continue;
            }

            entry.dropChance = Mathf.Clamp01(entry.dropChance);
            entry.minQuantity = Mathf.Max(1, entry.minQuantity);
            entry.maxQuantity = Mathf.Max(entry.minQuantity, entry.maxQuantity);
        }
    }
}
