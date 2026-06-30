using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

/// <summary>One job paired with whether it's allowed to spawn into the game.</summary>
[Serializable]
public class Job_Toggle
{
    public Job_Data job;
    public bool enabled = true;
}

/// <summary>
/// Owns the live progression (level + XP) for every job. One instance per scene
/// (<see cref="Instance"/>). A job's <see cref="Job_Runtime"/> is created on first request
/// and persists for the manager's lifetime. Idle_Manager calls <see cref="AddXP"/> here when
/// an idle completes a cycle, so a job levels up from the idles run under it.
///
/// Also holds per-job enable toggles (see the custom inspector) that <see cref="Job_UI"/>
/// reads to decide which jobs to spawn.
/// </summary>
[DisallowMultipleComponent]
public class Job_Manager : Singleton<Job_Manager>
{
    // Per-job enable flags, edited via the custom Job_Manager inspector (auto-listed toggles).
    [SerializeField]
    List<Job_Toggle> jobToggles = new();

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

    /// <summary>True if the job is allowed to spawn. Jobs not in the toggle list default to enabled.</summary>
    public bool IsJobEnabled(Job_Data job)
    {
        if (job == null)
            return false;

        for (int i = 0; i < jobToggles.Count; i++)
        {
            if (jobToggles[i] != null && jobToggles[i].job == job)
                return jobToggles[i].enabled;
        }

        return true;
    }

    /// <summary>Sets a job's enabled flag, adding a toggle entry if there isn't one yet.</summary>
    public void SetJobEnabled(Job_Data job, bool enabled)
    {
        if (job == null)
            return;

        for (int i = 0; i < jobToggles.Count; i++)
        {
            if (jobToggles[i] != null && jobToggles[i].job == job)
            {
                jobToggles[i].enabled = enabled;
                return;
            }
        }

        jobToggles.Add(new Job_Toggle { job = job, enabled = enabled });
    }
}
