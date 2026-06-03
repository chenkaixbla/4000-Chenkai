using System;
using UnityEngine;

/// <summary>
/// The live runtime state of a single idle action. This is intentionally generic and
/// UI-agnostic: it wraps an <see cref="Idle_Data"/> and tracks per-play state (running,
/// timer, completed cycles), raising events a card can subscribe to. Special card visuals
/// never change this class - they read from it.
///
/// NOTE: the economy (costs/rewards) and start conditions are still not implemented; only
/// the timer, cycle count and XP/level are live. The name 'Idle_Runtime' is temporary and
/// can be consolidated to 'Idle_Instance' (now free) whenever convenient.
/// </summary>
[Serializable]
public class Idle_Runtime
{
    public Idle_Data idleData;

    /// <summary>The job this idle belongs to (for awarding job XP). Set by Idle_Manager.</summary>
    public Job_Data ownerJobData;

    public bool isRunning;
    public float timer;
    public int completedCycles;

    // Level/XP state. maxXP is computed from the level curve on construction.
    public int level = 1;
    public int currentXP;
    public int maxXP;

    /// <summary>Raised whenever state changes enough to warrant a UI refresh.</summary>
    public event Action OnUpdated;

    /// <summary>Raised each time one interval completes.</summary>
    public event Action OnCycleCompleted;

    public Idle_Runtime(Idle_Data data, Job_Data ownerJob = null)
    {
        idleData = data;
        ownerJobData = ownerJob;
        XP_Utility.AddXP(ref level, ref currentXP, ref maxXP, 0); // compute maxXP for level
    }

    public float Interval => idleData != null ? Mathf.Max(0.1f, idleData.interval) : 1f;

    /// <summary>Action timer progress, 0..1 (for the timer bar).</summary>
    public float GetNormalizedProgress()
    {
        return idleData == null ? 0f : Mathf.Clamp01(timer / Interval);
    }

    /// <summary>Level/XP progress, 0..1 (for the level bar).</summary>
    public float GetNormalizedLevelProgress()
    {
        return maxXP > 0 ? Mathf.Clamp01((float)currentXP / maxXP) : 0f;
    }

    public void SetRunning(bool running)
    {
        if (isRunning == running)
            return;

        isRunning = running;
        if (!running)
            timer = 0f;

        OnUpdated?.Invoke();
    }

    public void Toggle() => SetRunning(!isRunning);

    /// <summary>
    /// Advances the timer and returns how many cycles completed this tick. The owning
    /// Idle_Manager uses the return value to award idle + job XP. XP/economy are awarded by
    /// the manager, not here, so this stays a pure state holder.
    /// </summary>
    public int Tick(float deltaTime)
    {
        if (!isRunning || idleData == null)
            return 0;

        timer += Mathf.Max(0f, deltaTime);

        int cycles = 0;
        while (timer >= Interval)
        {
            timer -= Interval;
            completedCycles++;
            cycles++;
            OnCycleCompleted?.Invoke();
        }

        OnUpdated?.Invoke();
        return cycles;
    }

    /// <summary>Adds idle XP and levels up using the shared curve, then refreshes the UI.</summary>
    public void AddXP(int amount)
    {
        XP_Utility.AddXP(ref level, ref currentXP, ref maxXP, amount);
        OnUpdated?.Invoke();
    }
}
