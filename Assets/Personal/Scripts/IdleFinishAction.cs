using UnityEngine;

public enum IdleFinishActionType
{
    GiveItem,
    GiveStat
}

[System.Serializable]
public class FinishAction
{
}

[System.Serializable]
public class FinishAction_GiveItem : FinishAction
{
    public ItemsData itemsData;
    public int quantity;
}

[System.Serializable]
public class FinishAction_GiveStat : FinishAction
{
    public int attackDamageOffset;
}
