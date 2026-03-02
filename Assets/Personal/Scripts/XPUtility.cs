using UnityEngine;

public static class XPUtility
{
    public static int GetMaxXPForLevel(int level)
    {
        return (int)((level - 1 + 300 * Mathf.Pow(2, (level - 1) / 6.1f)) / 4);
    }
}
