using UnityEngine;

/// <summary>
/// Shared XP/level math for every progression source (idles and jobs use the same curve).
/// The curve is the classic RuneScape/Melvor-style formula recovered from the old project.
/// </summary>
public static class XP_Utility
{
    /// <summary>XP required to advance FROM <paramref name="level"/> to the next level.</summary>
    public static int GetMaxXPForLevel(int level)
    {
        return (int)((level - 1 + 300 * Mathf.Pow(2, (level - 1) / 6.1f)) / 4);
    }

    /// <summary>
    /// Adds <paramref name="amount"/> XP and levels up as many times as the running total
    /// allows, rolling the remainder into the next level. Pass amount 0 to just (re)compute
    /// <paramref name="maxXP"/> for the current level (e.g. on construction).
    /// </summary>
    public static void AddXP(ref int level, ref int currentXP, ref int maxXP, int amount)
    {
        if (amount > 0)
            currentXP += amount;

        maxXP = Mathf.Max(1, GetMaxXPForLevel(level));
        while (currentXP >= maxXP)
        {
            currentXP -= maxXP;
            level++;
            maxXP = Mathf.Max(1, GetMaxXPForLevel(level));
        }
    }
}
