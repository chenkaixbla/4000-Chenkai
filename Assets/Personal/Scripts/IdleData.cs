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
    public Action onTimerFinish;
    [AssetPreview] public Sprite icon;

    public IdleFinishActionType finishType;
    [SerializeReference] public FinishAction finishAction;

    void OnEnable()
    {
        GenerateGUID();
    }

    void OnValidate()
    {
        if(finishType == IdleFinishActionType.GiveItem)
        {
            if(finishAction?.GetType() != typeof(FinishAction_GiveItem))
                finishAction = new FinishAction_GiveItem();
        }
        else if(finishType == IdleFinishActionType.GiveStat)
        {
            if(finishAction?.GetType() != typeof(FinishAction_GiveStat))
                finishAction = new FinishAction_GiveStat();
        }
    }

    [Button]
    void GenerateGUID()
    {
        guid = Guid.NewGuid().ToString();
    }
}
