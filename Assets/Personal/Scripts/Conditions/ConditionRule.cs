using System;
using System.Collections.Generic;
using UnityEngine;

public enum ConditionRuleType
{
    Level,
    XP,
    ItemQuantity
}

public enum ConditionComparisonType
{
    GreaterThanOrEqual,
    GreaterThan,
    Equal,
    LessThanOrEqual,
    LessThan
}

public enum ProgressionConditionTarget
{
    CurrentIdle,
    CurrentJob,
    SpecificIdle,
    SpecificJob
}

public interface IConditionProgressSource
{
    int CurrentLevel { get; }
    int CurrentXP { get; }
}

[Serializable]
public class ConditionRuleEntry
{
    public ConditionRuleType conditionType;
    [SerializeReference] public ConditionRule condition;
}

public sealed class ConditionContext
{
    public static readonly ConditionContext Empty = new();

    public IdleInstance IdleInstance { get; }
    public JobInstance JobInstance { get; }

    public ConditionContext(IdleInstance idleInstance = null, JobInstance jobInstance = null)
    {
        IdleInstance = idleInstance;
        JobInstance = jobInstance ?? idleInstance?.ownerJobInstance;
    }
}

[Serializable]
public abstract class ConditionRule
{
    public abstract ConditionRuleType RuleType { get; }

    public abstract bool IsMet(ConditionContext context);
}

[Serializable]
public class ConditionRule_Level : ConditionRule
{
    public ProgressionConditionTarget target = ProgressionConditionTarget.CurrentIdle;
    public IdleData idleData;
    public JobData jobData;
    public ConditionComparisonType comparison = ConditionComparisonType.GreaterThanOrEqual;
    public int requiredLevel;

    public override ConditionRuleType RuleType => ConditionRuleType.Level;

    public override bool IsMet(ConditionContext context)
    {
        IConditionProgressSource source = ConditionRuntimeLookup.ResolveProgressSource(target, context, idleData, jobData);
        return source != null && ConditionRuleUtility.Compare(source.CurrentLevel, requiredLevel, comparison);
    }
}

[Serializable]
public class ConditionRule_XP : ConditionRule
{
    public ProgressionConditionTarget target = ProgressionConditionTarget.CurrentIdle;
    public IdleData idleData;
    public JobData jobData;
    public ConditionComparisonType comparison = ConditionComparisonType.GreaterThanOrEqual;
    public int requiredXP;

    public override ConditionRuleType RuleType => ConditionRuleType.XP;

    public override bool IsMet(ConditionContext context)
    {
        IConditionProgressSource source = ConditionRuntimeLookup.ResolveProgressSource(target, context, idleData, jobData);
        return source != null && ConditionRuleUtility.Compare(source.CurrentXP, requiredXP, comparison);
    }
}

[Serializable]
public class ConditionRule_ItemQuantity : ConditionRule
{
    public ItemsData itemData;
    public ConditionComparisonType comparison = ConditionComparisonType.GreaterThanOrEqual;
    public int quantity = 1;

    public override ConditionRuleType RuleType => ConditionRuleType.ItemQuantity;

    public override bool IsMet(ConditionContext context)
    {
        if (itemData == null)
        {
            return false;
        }

        int currentQuantity = InventoryManager.Instance != null ? InventoryManager.Instance.GetQuantity(itemData) : 0;
        return ConditionRuleUtility.Compare(currentQuantity, Mathf.Max(0, quantity), comparison);
    }
}

public static class ConditionRuleUtility
{
    public static void EnsureConditionRuleType(ConditionRuleEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.condition != null && entry.condition.RuleType == entry.conditionType)
        {
            return;
        }

        entry.condition = CreateConditionRule(entry.conditionType);
    }

    public static ConditionRule CreateConditionRule(ConditionRuleType conditionType)
    {
        switch (conditionType)
        {
            case ConditionRuleType.Level:
                return new ConditionRule_Level();
            case ConditionRuleType.XP:
                return new ConditionRule_XP();
            case ConditionRuleType.ItemQuantity:
                return new ConditionRule_ItemQuantity();
            default:
                return null;
        }
    }

    public static bool Compare(int currentValue, int targetValue, ConditionComparisonType comparison)
    {
        switch (comparison)
        {
            case ConditionComparisonType.GreaterThanOrEqual:
                return currentValue >= targetValue;
            case ConditionComparisonType.GreaterThan:
                return currentValue > targetValue;
            case ConditionComparisonType.Equal:
                return currentValue == targetValue;
            case ConditionComparisonType.LessThanOrEqual:
                return currentValue <= targetValue;
            case ConditionComparisonType.LessThan:
                return currentValue < targetValue;
            default:
                return false;
        }
    }

}

public static class ConditionRuntimeLookup
{
    public static IConditionProgressSource ResolveProgressSource(ProgressionConditionTarget target, ConditionContext context, IdleData idleData, JobData jobData)
    {
        switch (target)
        {
            case ProgressionConditionTarget.CurrentIdle:
                return context?.IdleInstance;
            case ProgressionConditionTarget.CurrentJob:
                return context?.JobInstance;
            case ProgressionConditionTarget.SpecificIdle:
                return FindIdleInstance(idleData);
            case ProgressionConditionTarget.SpecificJob:
                return FindJobInstance(jobData);
            default:
                return null;
        }
    }

    public static IdleInstance FindIdleInstance(IdleData idleData)
    {
        if (idleData == null)
        {
            return null;
        }

        IReadOnlyList<JobInstance> activeJobs = JobInstance.ActiveInstances;
        for (int i = 0; i < activeJobs.Count; i++)
        {
            JobInstance jobInstance = activeJobs[i];
            if (jobInstance == null || jobInstance.idleInstances == null)
            {
                continue;
            }

            for (int j = 0; j < jobInstance.idleInstances.Count; j++)
            {
                IdleInstance idleInstance = jobInstance.idleInstances[j];
                if (idleInstance != null && idleInstance.idleData == idleData)
                {
                    return idleInstance;
                }
            }
        }

        return null;
    }

    public static JobInstance FindJobInstance(JobData jobData)
    {
        if (jobData == null)
        {
            return null;
        }

        IReadOnlyList<JobInstance> activeJobs = JobInstance.ActiveInstances;
        for (int i = 0; i < activeJobs.Count; i++)
        {
            JobInstance jobInstance = activeJobs[i];
            if (jobInstance != null && jobInstance.jobData == jobData)
            {
                return jobInstance;
            }
        }

        return null;
    }
}
