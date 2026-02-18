using System;
using EditorAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "IdleData", menuName = "Game/IdleData")]
public class IdleData : ScriptableObject
{
    public string guid;
    public string displayName;
    public float interval;
    public int xpReward;
    public int maxXP;
    [AssetPreview] public Sprite icon;

    public IdleFinishActionType finishType;
    [SerializeReference] public FinishAction finishAction;

    void OnEnable()
    {
        EnsureGuid();
    }

    void OnValidate()
    {
        EnsureGuid();
        EnsureFinishActionType();
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

    void EnsureFinishActionType()
    {
        if (finishType == IdleFinishActionType.GiveItem)
        {
            if (finishAction is not FinishAction_GiveItem)
            {
                finishAction = new FinishAction_GiveItem();
            }
        }
        else if (finishType == IdleFinishActionType.GiveStat)
        {
            if (finishAction is not FinishAction_GiveStat)
            {
                finishAction = new FinishAction_GiveStat();
            }
        }
    }
}
