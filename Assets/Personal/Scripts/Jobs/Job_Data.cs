using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "JobData", menuName = "Game/JobData")]
public class Job_Data : ScriptableObject
{
    public string jobName;
    public Sprite jobIcon;

    [Line]
    
    public List<Idle_Data> idleDatas = new();
}
