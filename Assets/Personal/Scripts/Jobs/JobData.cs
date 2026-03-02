using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "JobData", menuName = "Game/JobData")]
public class JobData : ScriptableObject
{
    public string jobName;
    public Sprite jobIcon;

    public List<IdleData> idleDatas = new();
}
