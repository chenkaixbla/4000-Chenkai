using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

/// <summary>The combat style used to attack. Each style has its own level (player &amp; monster).</summary>
public enum Combat_AttackType
{
    Melee,
    Ranged,
    Magic
}

/// <summary>
/// Data for one combat monster. Mirrors the player's combat model so the two fight on the
/// same terms: a monster has the same four levels (health / strength / defense / attack type)
/// whose sum is its combat level, plus the attributes that aren't derived from levels
/// (speed, crit). The derived stats - max HP, damage reduction, damage range - are NOT stored
/// here; the combat system computes them from these levels using the same formulas as the
/// player, so balancing a monster means setting its levels.
/// </summary>
[CreateAssetMenu(fileName = "Monster_Data", menuName = "Game/Combat/Monster_Data")]
public class Monster_Data : ScriptableObject
{
    [Title("General")]
    [AssetPreview(previewHeight: 96f)] public Sprite icon;
    public string monsterName;
    [TextArea(0, int.MaxValue)] public string description;

    [Title("Combat Levels")]
    [Range(1, 99)] public int healthLevel = 1;
    [Range(1, 99)] public int strengthLevel = 1;
    [Range(1, 99)] public int defenseLevel = 1;

    [Tooltip("Which style this monster attacks with. Drives its damage range alongside strength.")]
    public Combat_AttackType attackType = Combat_AttackType.Melee;
    [Range(1, 99)] public int attackTypeLevel = 1;

    [ReadOnly, SerializeField]
    [Tooltip("Auto-calculated sum of the four levels.")]
    int combatLevel = 4;

    [Title("Combat Attributes (not level-derived)")]
    [Tooltip("Actions per second - higher means the monster attacks more often.")]
    [Min(0.1f)] public float speed = 1f;

    [Tooltip("Chance (0-1) for an attack to land a critical hit.")]
    [Range(0f, 1f)] public float criticalHitRate = 0.05f;

    [Tooltip("Damage multiplier on a crit, as a percent. 200 = double damage.")]
    [Min(100)] public int criticalHitBonus = 200;

    [Title("Rewards (on defeat)")]
    public List<Reward> rewards = new();

    /// <summary>Combat level = sum of the four levels (mirrors the player).</summary>
    public int CombatLevel => healthLevel + strengthLevel + defenseLevel + attackTypeLevel;

    void OnValidate()
    {
        combatLevel = CombatLevel;
    }
}
