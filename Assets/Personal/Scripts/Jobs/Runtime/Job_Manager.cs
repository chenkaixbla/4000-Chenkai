using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

/// <summary>
/// Owns the live progression (level + XP) for every job. One instance per scene
/// (<see cref="Instance"/>). A job's <see cref="Job_Runtime"/> is created on first request
/// and persists for the manager's lifetime. Idle_Manager calls <see cref="AddXP"/> here when
/// an idle completes a cycle, so a job levels up from the idles run under it.
/// </summary>
[DisallowMultipleComponent]
public class Job_Manager : MonoBehaviour
{
    public static Job_Manager Instance { get; private set; }

    [Title("Runtime")]
    [ReadOnly, SerializeField]
    int trackedJobs;

    // Serialized + read-only so every job's level/xp is inspectable live in play mode.
    [Title("Tracked Job Instances")]
    [ReadOnly, SerializeField]
    List<Job_Runtime> jobRuntimes = new();

    readonly Dictionary<Job_Data, Job_Runtime> runtimeByJob = new();

    /// <summary>All job runtimes created so far.</summary>
    public IReadOnlyCollection<Job_Runtime> Runtimes => runtimeByJob.Values;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning($"[Job_Manager] A second Job_Manager '{name}' was found. There should be one per scene.", this);

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Returns the persistent runtime for a job, creating it on first request.</summary>
    public Job_Runtime GetRuntime(Job_Data job)
    {
        if (job == null)
            return null;

        if (runtimeByJob.TryGetValue(job, out Job_Runtime existing))
            return existing;

        Job_Runtime runtime = new Job_Runtime(job);
        runtimeByJob[job] = runtime;
        jobRuntimes.Add(runtime);
        trackedJobs = runtimeByJob.Count;
        return runtime;
    }

    public void AddXP(Job_Data job, int amount)
    {
        Job_Runtime runtime = GetRuntime(job);
        runtime?.AddXP(amount);
    }
}
