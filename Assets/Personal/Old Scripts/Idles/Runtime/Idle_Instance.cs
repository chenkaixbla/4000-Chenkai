using System;
using EditorAttributes;
using UnityEngine;

[Serializable]
public class Idle_Instance : IConditionProgressSource
{
    public Idle_Data idleData;
    public Job_Instance ownerJobInstance;

    [ReadOnly] public float timer;
    [ReadOnly] public int currentXP;
    [ReadOnly] public int maxXP;
    [ReadOnly] public int level = 1;

    [ReadOnly] public bool isRunning;
    [ReadOnly] public int completedCycles;

    public event Action OnUpdate;
    public event Action OnTimerFinish;
    public event Action<bool> OnRunningStateChanged;

    int IConditionProgressSource.CurrentLevel => level;
    int IConditionProgressSource.CurrentXP => currentXP;

    public Idle_Instance(Idle_Data data, Job_Instance ownerJob = null)
    {
        Initialize(data, ownerJob);
    }

    public void Initialize(Idle_Data data, Job_Instance ownerJob = null)
    {
        idleData = data;
        ownerJobInstance = ownerJob;
        timer = 0f;
        completedCycles = 0;

        level = Mathf.Max(1, level);
        currentXP = Mathf.Max(0, currentXP);
        CheckLevel();
    }

    public bool StartRunning(InventoryManager inventory = null)
    {
        if (idleData == null)
        {
            return false;
        }

        if (!AreStartConditionsMet(inventory))
        {
            return false;
        }

        if (!isRunning)
        {
            isRunning = true;
            OnRunningStateChanged?.Invoke(true);
        }

        return true;
    }

    public void StopRunning()
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        OnRunningStateChanged?.Invoke(false);
    }

    public void Tick(float deltaTime, InventoryManager inventory = null)
    {
        if (!isRunning || idleData == null)
        {
            OnUpdate?.Invoke();
            return;
        }

        if (!idleData.CanRunCycle(this, ownerJobInstance, inventory))
        {
            if (idleData.stopWhenCycleCannotRun)
            {
                StopRunning();
            }

            OnUpdate?.Invoke();
            return;
        }

        float safeInterval = Mathf.Max(0.1f, idleData.interval);
        timer += Mathf.Max(0f, deltaTime);

        while (timer >= safeInterval)
        {
            timer -= safeInterval;
            CompleteCycle(inventory);

            if (!isRunning)
            {
                break;
            }
        }

        OnUpdate?.Invoke();
    }

    public bool AreStartConditionsMet(InventoryManager inventory = null)
    {
        return idleData != null && idleData.AreStartConditionsMet(this, ownerJobInstance, inventory);
    }

    public float GetNormalizedProgress()
    {
        if (idleData == null || idleData.interval <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(timer / idleData.interval);
    }

    void CompleteCycle(InventoryManager inventory)
    {
        if (!idleData.TryExecuteCycle(this, ownerJobInstance, inventory))
        {
            if (idleData.stopWhenCycleCannotRun)
            {
                StopRunning();
            }

            return;
        }

        completedCycles++;
        currentXP += Mathf.Max(0, idleData.idleXPReward);
        ownerJobInstance?.AddXP(Mathf.Max(0, idleData.jobXPReward));

        CheckLevel();
        OnTimerFinish?.Invoke();

        if (!idleData.autoRestart)
        {
            StopRunning();
        }
    }

    void CheckLevel()
    {
        maxXP = Mathf.Max(1, XPUtility.GetMaxXPForLevel(level));
        while (currentXP >= maxXP)
        {
            currentXP -= maxXP;
            level++;
            maxXP = Mathf.Max(1, XPUtility.GetMaxXPForLevel(level));
        }
    }
}
