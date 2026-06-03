using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

[System.Serializable]
public class Job_Instance : IConditionProgressSource
{
    static readonly List<Job_Instance> activeInstances = new();

    public static IReadOnlyList<Job_Instance> ActiveInstances => activeInstances;

    [ReadOnly] public int currentLevel = 1;
    [ReadOnly] public int currentXP;
    [ReadOnly] public int maxXP;

    public Job_Data jobData;
    public List<Idle_Instance> idleInstances = new();
    [ReadOnly] public Idle_Instance activeIdleInstance;

    int IConditionProgressSource.CurrentLevel => currentLevel;
    int IConditionProgressSource.CurrentXP => currentXP;

    public Job_Instance(Job_Data data = null)
    {
        Initialize(data);
    }

    public void Initialize(Job_Data data)
    {
        jobData = data;
        idleInstances ??= new List<Idle_Instance>();

        currentLevel = Mathf.Max(1, currentLevel);
        currentXP = Mathf.Max(0, currentXP);
        CheckLevel();
    }

    public void RegisterActive()
    {
        if (!activeInstances.Contains(this))
        {
            activeInstances.Add(this);
        }
    }

    public void UnregisterActive()
    {
        activeInstances.Remove(this);
    }

    public Idle_Data GetPrimaryIdleData()
    {
        return jobData != null ? jobData.GetPrimaryIdleData() : null;
    }

    public Idle_Instance ResolveOrCreateIdleInstance(Idle_Data idleData)
    {
        if (idleData == null)
        {
            return null;
        }

        idleInstances ??= new List<Idle_Instance>();

        for (int i = 0; i < idleInstances.Count; i++)
        {
            Idle_Instance existing = idleInstances[i];
            if (existing != null && existing.idleData == idleData)
            {
                return existing;
            }
        }

        Idle_Instance created = new Idle_Instance(idleData, this);
        idleInstances.Add(created);
        return created;
    }

    public void SetActiveIdle(Idle_Instance idleInstance)
    {
        activeIdleInstance = idleInstance;
    }

    public void AddXP(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentXP += amount;
        CheckLevel();
    }

    void CheckLevel()
    {
        maxXP = Mathf.Max(1, XPUtility.GetMaxXPForLevel(currentLevel));
        while (currentXP >= maxXP)
        {
            currentXP -= maxXP;
            currentLevel++;
            maxXP = Mathf.Max(1, XPUtility.GetMaxXPForLevel(currentLevel));
        }
    }
}
