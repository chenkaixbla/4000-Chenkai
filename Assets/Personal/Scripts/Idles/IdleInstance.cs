using System;
using EditorAttributes;
using UnityEngine;

[System.Serializable]
public class IdleInstance : IConditionProgressSource
{
    public IdleData idleData;
    public JobInstance ownerJobInstance;
    public float timer;
    public int currentXP;
    [ReadOnly] public int maxXP;
    public int level;

    public event Action OnUpdate;
    public event Action OnTimerFinish;

    public IdleInstance(IdleData data, JobInstance ownerJob = null)
    {
        Initialize(data, ownerJob);
    }

    public void Initialize(IdleData data, JobInstance ownerJob = null)
    {
        idleData = data;
        ownerJobInstance = ownerJob;
        level = Mathf.Max(0, level);
        currentXP = Mathf.Max(0, currentXP);

        if (idleData != null)
        {
            timer = 0;
        }

        CheckLevel();
    }

    public void DoUpdate()
    {
        if (idleData == null) return;

        if (!AreStartConditionsMet())
        {
            OnUpdate?.Invoke();
            return;
        }

        timer += Time.deltaTime;
        if (idleData.interval > 0f)
        {
            while (timer >= idleData.interval)
            {
                timer -= idleData.interval;
                GrantReward();
                CheckLevel();

                OnTimerFinish?.Invoke();
            }
        }

        OnUpdate?.Invoke();
    }

    public bool AreStartConditionsMet()
    {
        return idleData != null && idleData.AreStartConditionsMet(this, ownerJobInstance);
    }

    void GrantReward()
    {
        currentXP += idleData.idleXPReward;

        if (ownerJobInstance != null)
        {
            ownerJobInstance.AddXP(idleData.jobXPReward);
        }

        idleData.ApplyFinishActions(this);
    }
    
    void CheckLevel()
    {
        maxXP = XPUtility.GetMaxXPForLevel(level);
        if (maxXP <= 0)
        {
            return;
        }

        while (currentXP >= maxXP)
        {
            currentXP -= maxXP;
            level++;
            maxXP = XPUtility.GetMaxXPForLevel(level);

            if (maxXP <= 0)
            {
                break;
            }
        }
    }

    int IConditionProgressSource.CurrentLevel => level;
    int IConditionProgressSource.CurrentXP => currentXP;
}
