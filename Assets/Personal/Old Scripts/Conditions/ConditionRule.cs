using System;
using System.Collections.Generic;
using EditorAttributes;
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

    public Idle_Instance Idle_Instance { get; }
    public Job_Instance JobInstance { get; }
    public InventoryManager Inventory { get; }

    public ConditionContext(Idle_Instance idleInstance = null, Job_Instance jobInstance = null, InventoryManager inventoryManager = null)
    {
        Idle_Instance = idleInstance;
        JobInstance = jobInstance ?? idleInstance?.ownerJobInstance;
        Inventory = inventoryManager ?? InventoryManager.Instance;
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

    [ShowField(nameof(target), ProgressionConditionTarget.SpecificIdle)]
    public Idle_Data idleData;

    [ShowField(nameof(target), ProgressionConditionTarget.SpecificJob)]
    public Job_Data jobData;

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

    [ShowField(nameof(target), ProgressionConditionTarget.SpecificIdle)]
    public Idle_Data idleData;

    [ShowField(nameof(target), ProgressionConditionTarget.SpecificJob)]
    public Job_Data jobData;

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

        InventoryManager inventory = context != null && context.Inventory != null
            ? context.Inventory
            : InventoryManager.Instance;

        int currentQuantity = inventory != null ? inventory.GetQuantity(itemData) : 0;
        return ConditionRuleUtility.Compare(currentQuantity, Mathf.Max(0, quantity), comparison);
    }
}

public static class ConditionRuleUtility
{
    public static bool IsMet(ConditionRuleEntry entry, ConditionContext context)
    {
        if (entry == null)
        {
            return false;
        }

        EnsureConditionRuleType(entry);
        return entry.condition != null && entry.condition.IsMet(context ?? ConditionContext.Empty);
    }

    public static bool AreAllMet(IReadOnlyList<ConditionRuleEntry> entries, ConditionContext context)
    {
        if (entries == null || entries.Count == 0)
        {
            return true;
        }

        ConditionContext evaluationContext = context ?? ConditionContext.Empty;
        for (int i = 0; i < entries.Count; i++)
        {
            if (!IsMet(entries[i], evaluationContext))
            {
                return false;
            }
        }

        return true;
    }

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
        return conditionType switch
        {
            ConditionRuleType.Level => new ConditionRule_Level(),
            ConditionRuleType.XP => new ConditionRule_XP(),
            ConditionRuleType.ItemQuantity => new ConditionRule_ItemQuantity(),
            _ => null
        };
    }

    public static bool Compare(int currentValue, int targetValue, ConditionComparisonType comparison)
    {
        return comparison switch
        {
            ConditionComparisonType.GreaterThanOrEqual => currentValue >= targetValue,
            ConditionComparisonType.GreaterThan => currentValue > targetValue,
            ConditionComparisonType.Equal => currentValue == targetValue,
            ConditionComparisonType.LessThanOrEqual => currentValue <= targetValue,
            ConditionComparisonType.LessThan => currentValue < targetValue,
            _ => false
        };
    }

    public static string GetRequirementLabel(ConditionRuleEntry entry)
    {
        if (entry == null)
        {
            return "?";
        }

        EnsureConditionRuleType(entry);
        if (entry.condition == null)
        {
            return "?";
        }

        if (entry.condition is ConditionRule_Level levelRule)
        {
            return FormatRequirementValue(Mathf.Max(0, levelRule.requiredLevel), levelRule.comparison);
        }

        if (entry.condition is ConditionRule_XP xpRule)
        {
            return FormatRequirementValue(Mathf.Max(0, xpRule.requiredXP), xpRule.comparison);
        }

        if (entry.condition is ConditionRule_ItemQuantity itemQuantityRule)
        {
            return FormatRequirementValue(Mathf.Max(0, itemQuantityRule.quantity), itemQuantityRule.comparison);
        }

        return "?";
    }

    public static Sprite GetRequirementIcon(ConditionRuleEntry entry)
    {
        if (entry == null)
        {
            return null;
        }

        EnsureConditionRuleType(entry);
        if (entry.condition == null)
        {
            return null;
        }

        if (entry.condition is ConditionRule_Level levelRule)
        {
            return GetProgressTargetIcon(levelRule.target, levelRule.idleData, levelRule.jobData);
        }

        if (entry.condition is ConditionRule_XP xpRule)
        {
            return GetProgressTargetIcon(xpRule.target, xpRule.idleData, xpRule.jobData);
        }

        if (entry.condition is ConditionRule_ItemQuantity itemQuantityRule && itemQuantityRule.itemData != null)
        {
            return itemQuantityRule.itemData.icon;
        }

        return null;
    }

    static string FormatRequirementValue(int value, ConditionComparisonType comparison)
    {
        return comparison switch
        {
            ConditionComparisonType.GreaterThanOrEqual => value.ToString(),
            ConditionComparisonType.Equal => value.ToString(),
            ConditionComparisonType.GreaterThan => $">{value}",
            ConditionComparisonType.LessThanOrEqual => $"<={value}",
            ConditionComparisonType.LessThan => $"<{value}",
            _ => value.ToString()
        };
    }

    static Sprite GetProgressTargetIcon(ProgressionConditionTarget target, Idle_Data idleData, Job_Data jobData)
    {
        return target switch
        {
            ProgressionConditionTarget.SpecificIdle => idleData != null ? idleData.icon : null,
            ProgressionConditionTarget.SpecificJob => jobData != null ? jobData.jobIcon : null,
            _ => null
        };
    }
}

public static class ConditionRuntimeLookup
{
    public static IConditionProgressSource ResolveProgressSource(ProgressionConditionTarget target, ConditionContext context, Idle_Data idleData, Job_Data jobData)
    {
        return target switch
        {
            ProgressionConditionTarget.CurrentIdle => context?.Idle_Instance,
            ProgressionConditionTarget.CurrentJob => context?.JobInstance,
            ProgressionConditionTarget.SpecificIdle => FindIdleInstance(idleData),
            ProgressionConditionTarget.SpecificJob => FindJobInstance(jobData),
            _ => null
        };
    }

    public static Idle_Instance FindIdleInstance(Idle_Data idleData)
    {
        if (idleData == null)
        {
            return null;
        }

        IReadOnlyList<Job_Instance> activeJobs = Job_Instance.ActiveInstances;
        for (int i = 0; i < activeJobs.Count; i++)
        {
            Job_Instance jobInstance = activeJobs[i];
            if (jobInstance == null || jobInstance.idleInstances == null)
            {
                continue;
            }

            for (int j = 0; j < jobInstance.idleInstances.Count; j++)
            {
                Idle_Instance idleInstance = jobInstance.idleInstances[j];
                if (idleInstance != null && idleInstance.idleData == idleData)
                {
                    return idleInstance;
                }
            }
        }

        return null;
    }

    public static Job_Instance FindJobInstance(Job_Data jobData)
    {
        if (jobData == null)
        {
            return null;
        }

        IReadOnlyList<Job_Instance> activeJobs = Job_Instance.ActiveInstances;
        for (int i = 0; i < activeJobs.Count; i++)
        {
            Job_Instance jobInstance = activeJobs[i];
            if (jobInstance != null && jobInstance.jobData == jobData)
            {
                return jobInstance;
            }
        }

        return null;
    }
}
