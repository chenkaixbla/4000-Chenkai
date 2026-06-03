using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public enum Idle_Kind
{
    Woodcutting,
    Mining,
    Crafting,
    Pet,
    Cooking,
    Custom
}

[Serializable]
public class IdleFinishActionEntry
{
    public IdleFinishActionType finishType;
    [SerializeReference] public FinishAction finishAction;
}

[CreateAssetMenu(fileName = "Idle_Data", menuName = "Game/Idles/Idle_Data")]
public class Idle_Data : ScriptableObject
{
    [Title("General")]
    [AssetPreview(previewHeight: 96f)] public Sprite icon;
    public string guid;
    public string displayName;
    public Idle_Kind idleKind = Idle_Kind.Woodcutting;

    [Title("Timing")]
    [Min(0.1f)] public float interval = 3f;
    public bool autoRestart = true;
    public bool stopWhenCycleCannotRun = true;

    [Title("Progression")]
    [Min(0)] public int idleXPReward = 10;
    [Min(0)] public int jobXPReward = 5;

    [Title("Cycle Economy")]
    public List<Item_Amount> cycleCosts = new();
    public List<Item_Amount> cycleRewards = new();

    [Title("Conditions & Actions")]
    public List<ConditionRuleEntry> startConditions = new();
    public List<IdleFinishActionEntry> finishActions = new();

    [SerializeField, HideInInspector] IdleFinishActionType finishType;
    [SerializeReference, HideInInspector] FinishAction finishAction;

    void OnEnable()
    {
        EnsureGuid();
        EnsureStartConditions();
        EnsureFinishActions();
    }

    void OnValidate()
    {
        EnsureGuid();
        EnsureStartConditions();
        EnsureFinishActions();
    }

    [Button]
    void GenerateGUID()
    {
        guid = Guid.NewGuid().ToString();
    }

    public bool AreStartConditionsMet(Idle_Instance idleInstance = null, Job_Instance jobInstance = null, InventoryManager inventory = null)
    {
        EnsureStartConditions();
        ConditionContext context = new ConditionContext(idleInstance, jobInstance, inventory);
        return ConditionRuleUtility.AreAllMet(startConditions, context);
    }

    public bool CanRunCycle(Idle_Instance idleInstance, Job_Instance jobInstance, InventoryManager inventory)
    {
        if (!AreStartConditionsMet(idleInstance, jobInstance, inventory))
        {
            return false;
        }

        InventoryManager resolvedInventory = inventory != null ? inventory : InventoryManager.Instance;
        return resolvedInventory == null || resolvedInventory.CanConsume(cycleCosts);
    }

    public bool TryExecuteCycle(Idle_Instance idleInstance, Job_Instance jobInstance, InventoryManager inventory)
    {
        InventoryManager resolvedInventory = inventory != null ? inventory : InventoryManager.Instance;
        if (!CanRunCycle(idleInstance, jobInstance, resolvedInventory))
        {
            return false;
        }

        if (resolvedInventory != null)
        {
            if (!resolvedInventory.TryConsume(cycleCosts))
            {
                return false;
            }

            resolvedInventory.Grant(cycleRewards);
        }

        ApplyFinishActions(idleInstance, resolvedInventory);
        return true;
    }

    public float GetIdleXpPerSecond()
    {
        return interval > 0f ? idleXPReward / interval : 0f;
    }

    public float GetJobXpPerSecond()
    {
        return interval > 0f ? jobXPReward / interval : 0f;
    }

    public string GetRewardSummary()
    {
        return FormatItemAmounts(cycleRewards);
    }

    public string GetCostSummary()
    {
        return FormatItemAmounts(cycleCosts);
    }

    void ApplyFinishActions(Idle_Instance idleInstance, InventoryManager inventory)
    {
        if (idleInstance == null || finishActions == null)
        {
            return;
        }

        for (int i = 0; i < finishActions.Count; i++)
        {
            IdleFinishActionEntry entry = finishActions[i];
            entry?.finishAction?.Apply(idleInstance, inventory);
        }
    }

    void EnsureGuid()
    {
        if (string.IsNullOrWhiteSpace(guid))
        {
            GenerateGUID();
        }
    }

    void EnsureStartConditions()
    {
        if (startConditions == null)
        {
            startConditions = new List<ConditionRuleEntry>();
        }

        for (int i = 0; i < startConditions.Count; i++)
        {
            ConditionRuleEntry entry = startConditions[i];
            if (entry == null)
            {
                entry = new ConditionRuleEntry();
                startConditions[i] = entry;
            }

            ConditionRuleUtility.EnsureConditionRuleType(entry);
        }
    }

    void EnsureFinishActions()
    {
        if (finishActions == null)
        {
            finishActions = new List<IdleFinishActionEntry>();
        }

        if (finishActions.Count == 0 && finishAction != null)
        {
            finishActions.Add(new IdleFinishActionEntry
            {
                finishType = finishAction.ActionType,
                finishAction = finishAction
            });
        }

        for (int i = 0; i < finishActions.Count; i++)
        {
            IdleFinishActionEntry entry = finishActions[i];
            if (entry == null)
            {
                entry = new IdleFinishActionEntry();
                finishActions[i] = entry;
            }

            EnsureFinishActionType(entry);
        }

        finishAction = null;
    }

    void EnsureFinishActionType(IdleFinishActionEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.finishAction != null && entry.finishAction.ActionType == entry.finishType)
        {
            return;
        }

        entry.finishAction = entry.finishType switch
        {
            IdleFinishActionType.GiveItem => new FinishAction_GiveItem(),
            IdleFinishActionType.GiveStat => new FinishAction_GiveStat(),
            _ => null
        };
    }

    static string FormatItemAmounts(IReadOnlyList<Item_Amount> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            return "None";
        }

        List<string> labels = new();
        for (int i = 0; i < entries.Count; i++)
        {
            Item_Amount entry = entries[i];
            if (entry == null || entry.itemData == null)
            {
                continue;
            }

            string itemName = !string.IsNullOrWhiteSpace(entry.itemData.displayName) ? entry.itemData.displayName : entry.itemData.name;
            labels.Add($"{itemName} x{entry.SafeQuantity}");
        }

        return labels.Count == 0 ? "None" : string.Join(", ", labels);
    }
}
