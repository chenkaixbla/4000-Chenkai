using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Job_Instance : MonoBehaviour, IConditionProgressSource
{
    private static readonly List<Job_Instance> _activeInstances = new();

    public static IReadOnlyList<Job_Instance> ActiveInstances => _activeInstances;

    public int currentLevel;
    public int currentXP;
    public int maxXP;

    public Job_Data jobData;
    public List<Idle_Instance> idleInstances = new();

    int IConditionProgressSource.CurrentLevel => currentLevel;
    int IConditionProgressSource.CurrentXP => currentXP;

    void OnEnable()
    {
        SyncProgression();

        if (!_activeInstances.Contains(this))
        {
            _activeInstances.Add(this);
        }
    }

    void OnDisable()
    {
        _activeInstances.Remove(this);
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

    void SyncProgression()
    {
        currentLevel = Mathf.Max(0, currentLevel);
        currentXP = Mathf.Max(0, currentXP);
        CheckLevel();
    }

    void CheckLevel()
    {
        maxXP = XPUtility.GetMaxXPForLevel(currentLevel);
        if (maxXP <= 0)
        {
            return;
        }

        while (currentXP >= maxXP)
        {
            currentXP -= maxXP;
            currentLevel++;
            maxXP = XPUtility.GetMaxXPForLevel(currentLevel);

            if (maxXP <= 0)
            {
                break;
            }
        }
    }
}
