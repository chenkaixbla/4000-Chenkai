using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Job_ListPanel : MonoBehaviour
{
    public List<Job_Data> jobs = new();

    public IReadOnlyList<Job_Data> Jobs => jobs;
}
