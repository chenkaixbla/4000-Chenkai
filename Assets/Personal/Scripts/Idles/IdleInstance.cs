using System;
using UnityEngine;

[System.Serializable]
public class IdleInstance
{
    public IdleData idleData;
    public float timer;
    public int currentXP;
    public int level;

    public event Action OnUpdate;
    public event Action OnTimerFinish;

    public IdleInstance(IdleData data)
    {
        Initialize(data);
    }

    public void Initialize(IdleData data)
    {
        idleData = data;

        if (idleData != null)
        {
            timer = 0;
        }
    }

    public void DoUpdate()
    {
        if (idleData == null) return;

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

    void GrantReward()
    {
        currentXP += idleData.xpReward;
        idleData.finishAction?.Apply(this);
    }
    
    void CheckLevel()
    {
        if (idleData.maxXP <= 0)
        {
            return;
        }

        while (currentXP >= idleData.maxXP)
        {
            currentXP -= idleData.maxXP;
            level++;
        }
    }
}
