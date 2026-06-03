using System;
using UnityEngine;

/// <summary>
/// Live progression state for one job: its level and XP. Generic and UI-agnostic - a job
/// list/header UI can read these and subscribe to <see cref="OnUpdated"/> to refresh.
/// Uses the same shared <see cref="XP_Utility"/> curve as idles.
/// </summary>
[Serializable]
public class Job_Runtime
{
    public Job_Data jobData;

    public int level = 1;
    public int currentXP;
    public int maxXP;

    /// <summary>Raised when level/XP changes.</summary>
    public event Action OnUpdated;

    public Job_Runtime(Job_Data data)
    {
        jobData = data;
        XP_Utility.AddXP(ref level, ref currentXP, ref maxXP, 0); // compute maxXP for level
    }

    /// <summary>Level/XP progress, 0..1 (for a job level bar).</summary>
    public float GetNormalizedLevelProgress()
    {
        return maxXP > 0 ? Mathf.Clamp01((float)currentXP / maxXP) : 0f;
    }

    public void AddXP(int amount)
    {
        if (amount <= 0)
            return;

        XP_Utility.AddXP(ref level, ref currentXP, ref maxXP, amount);
        OnUpdated?.Invoke();
    }
}
