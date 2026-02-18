using System.Collections.Generic;
using UnityEngine;

public class JobInstance : MonoBehaviour
{
    public int currentLevel;
    public float currentXP;

    public JobData jobData;
    public List<IdleInstance> idleInstances = new();
}
