using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

[Serializable]
public class IdleFinishActionEntry
{
    public IdleFinishActionType finishType;
    [SerializeReference] public FinishAction finishAction;
}

[CreateAssetMenu(fileName = "IdleData", menuName = "Game/IdleData")]
public class IdleData : ScriptableObject
{
    public string guid;
    public string displayName;
    public float interval;
    public int xpReward;
    public int maxXP;
    [AssetPreview] public Sprite icon;

    public List<IdleFinishActionEntry> finishActions = new();

    [SerializeField, HideInInspector] private IdleFinishActionType finishType;
    [SerializeReference, HideInInspector] private FinishAction finishAction;

    void OnEnable()
    {
        EnsureGuid();
        EnsureFinishActions();
    }

    void OnValidate()
    {
        EnsureGuid();
        EnsureFinishActions();
    }

    [Button]
    void GenerateGUID()
    {
        guid = Guid.NewGuid().ToString();
    }

    void EnsureGuid()
    {
        if (string.IsNullOrWhiteSpace(guid))
        {
            GenerateGUID();
        }
    }

    public void ApplyFinishActions(IdleInstance idleInstance)
    {
        if (idleInstance == null)
        {
            return;
        }

        if (finishActions == null || finishActions.Count == 0)
        {
            if (finishAction != null)
            {
                EnsureFinishActions();
            }

            if (finishActions == null || finishActions.Count == 0)
            {
                return;
            }
        }

        foreach (IdleFinishActionEntry entry in finishActions)
        {
            entry?.finishAction?.Apply(idleInstance);
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

        if (entry.finishType == IdleFinishActionType.GiveItem)
        {
            if (entry.finishAction is not FinishAction_GiveItem)
            {
                entry.finishAction = new FinishAction_GiveItem();
            }
        }
        else if (entry.finishType == IdleFinishActionType.GiveStat)
        {
            if (entry.finishAction is not FinishAction_GiveStat)
            {
                entry.finishAction = new FinishAction_GiveStat();
            }
        }
    }
}
