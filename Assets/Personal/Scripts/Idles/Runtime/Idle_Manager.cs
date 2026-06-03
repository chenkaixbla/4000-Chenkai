using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

/// <summary>
/// Owns the live idle state for the whole game and drives the tick loop. One instance
/// per scene (<see cref="Instance"/>). Runtimes are created per job on first request and
/// persist for the manager's lifetime, so an idle keeps running (and its timer keeps
/// advancing) even when its card isn't on screen.
///
/// The view layer (<see cref="Idle_UI"/>) asks this manager for a job's runtimes and binds
/// cards to them; this manager just makes sure every running idle ticks each frame.
/// </summary>
[DisallowMultipleComponent]
public class Idle_Manager : MonoBehaviour
{
    public static Idle_Manager Instance { get; private set; }

    [Title("Runtime")]
    [ReadOnly, SerializeField]
    int runningCount;

    [ReadOnly, SerializeField]
    int trackedCount;

    // Persistent runtimes per job (the single source of truth).
    readonly Dictionary<Job_Data, List<Idle_Runtime>> runtimesByJob = new();

    // Flat list of every runtime, scanned each frame to tick the running ones.
    // Serialized + read-only so every instance's level/xp is inspectable live in play mode.
    [Title("Tracked Idle Instances")]
    [ReadOnly, SerializeField]
    List<Idle_Runtime> allRuntimes = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning($"[Idle_Manager] A second Idle_Manager '{name}' was found. There should be one per scene.", this);

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;
        int running = 0;

        for (int i = 0; i < allRuntimes.Count; i++)
        {
            Idle_Runtime runtime = allRuntimes[i];
            if (runtime == null || !runtime.isRunning)
                continue;

            int cycles = runtime.Tick(deltaTime);
            if (cycles > 0)
                AwardCycleXP(runtime, cycles);

            running++;
        }

        runningCount = running;
        trackedCount = allRuntimes.Count;
    }

    // Each completed cycle grants idle XP to the idle and job XP to its owning job.
    void AwardCycleXP(Idle_Runtime runtime, int cycles)
    {
        Idle_Data data = runtime.idleData;
        if (data == null)
            return;

        runtime.AddXP(data.idleXPReward * cycles);

        if (Job_Manager.Instance != null && runtime.ownerJobData != null)
            Job_Manager.Instance.AddXP(runtime.ownerJobData, data.jobXPReward * cycles);
    }

    /// <summary>
    /// Returns the persistent runtimes for a job, creating them on first request.
    /// </summary>
    public List<Idle_Runtime> GetRuntimes(Job_Data job)
    {
        if (job == null)
            return new List<Idle_Runtime>();

        if (runtimesByJob.TryGetValue(job, out List<Idle_Runtime> existing))
            return existing;

        List<Idle_Runtime> list = new List<Idle_Runtime>();
        List<Idle_Data> datas = job.GetValidIdleDatas();
        for (int i = 0; i < datas.Count; i++)
        {
            Idle_Runtime runtime = new Idle_Runtime(datas[i], job);
            list.Add(runtime);
            allRuntimes.Add(runtime);
        }

        runtimesByJob[job] = list;
        return list;
    }

    public void StartIdle(Idle_Runtime runtime) => runtime?.SetRunning(true);

    public void StopIdle(Idle_Runtime runtime) => runtime?.SetRunning(false);

    public void ToggleIdle(Idle_Runtime runtime) => runtime?.Toggle();

    /// <summary>Stops every tracked idle.</summary>
    public void StopAll()
    {
        for (int i = 0; i < allRuntimes.Count; i++)
            allRuntimes[i]?.SetRunning(false);
    }
}
