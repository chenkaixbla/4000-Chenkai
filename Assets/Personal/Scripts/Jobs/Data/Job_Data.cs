using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public enum Job_Category
{
    Gathering,
    Production,
    Utility,
    Companion
}

[CreateAssetMenu(fileName = "Job_Data", menuName = "Game/Jobs/Job_Data")]
public class Job_Data : ScriptableObject
{
    [Title("General")]
    public string jobName;
    public Sprite jobIcon;
    public Job_Category jobCategory = Job_Category.Gathering;

    [Line]

    [Title("Idles")]
    public List<Idle_Data> idleDatas = new();
    public Idle_CardUI idleCardPrefabOverride;

    [Line]

    [Title("Start Conditions")]
    public List<ConditionRuleEntry> startConditions = new();

    public bool AreStartConditionsMet(InventoryManager inventory = null, Job_Instance existingInstance = null)
    {
        EnsureStartConditions();
        ConditionContext conditionContext = new ConditionContext(null, existingInstance, inventory);
        return ConditionRuleUtility.AreAllMet(startConditions, conditionContext);
    }

    public Idle_Data GetPrimaryIdleData()
    {
        if (idleDatas == null)
        {
            return null;
        }

        for (int i = 0; i < idleDatas.Count; i++)
        {
            if (idleDatas[i] != null)
            {
                return idleDatas[i];
            }
        }

        return null;
    }

    public List<Idle_Data> GetValidIdleDatas(List<Idle_Data> buffer = null)
    {
        List<Idle_Data> target = buffer ?? new List<Idle_Data>();
        target.Clear();

        if (idleDatas == null)
        {
            return target;
        }

        for (int i = 0; i < idleDatas.Count; i++)
        {
            Idle_Data idleData = idleDatas[i];
            if (idleData != null)
            {
                target.Add(idleData);
            }
        }

        return target;
    }

    void OnEnable()
    {
        EnsureStartConditions();
    }

    void OnValidate()
    {
        EnsureStartConditions();
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
}
