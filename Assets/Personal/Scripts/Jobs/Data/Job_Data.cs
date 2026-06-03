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
    [Min(1)] public int maxLevel = 100;

    [Line]

    [Title("Idles")]
    public List<Idle_Data> idleDatas = new();
    [Tooltip("Optional: special idle-card prefab for this job's idles. Empty = use Idle_UI's default card.")]
    public Idle_Card idleCardPrefabOverride;

    public Idle_Data GetPrimaryIdleData()
    {
        if (idleDatas == null)
            return null;

        for (int i = 0; i < idleDatas.Count; i++)
        {
            if (idleDatas[i] != null)
                return idleDatas[i];
        }

        return null;
    }

    public List<Idle_Data> GetValidIdleDatas(List<Idle_Data> buffer = null)
    {
        List<Idle_Data> target = buffer ?? new List<Idle_Data>();
        target.Clear();

        if (idleDatas == null)
            return target;

        for (int i = 0; i < idleDatas.Count; i++)
        {
            Idle_Data idleData = idleDatas[i];
            if (idleData != null)
                target.Add(idleData);
        }

        return target;
    }
}
