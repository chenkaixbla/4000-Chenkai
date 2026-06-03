using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

[DisallowMultipleComponent]
public class Job_Manager : MonoBehaviour
{
    [Title("Buttons")]
    public Job_Button jobButtonPrefab;
    public Transform jobButtonsContainer;
    public bool clearButtonsOnRebuild = true;

    [Line]

    [Title("References")]
    public Job_ListPanel jobListPanel;
    public Idle_Manager idleManager;
    public InventoryManager inventoryManager;
    public Menu_MainViewManager menuViewManager;

    [Line]

    [Title("Flow")]
    public bool rebuildButtonsOnStart = true;
    public Menu_ViewPanelId panelToShowOnStartJob = Menu_ViewPanelId.Idles;

    [ReadOnly, System.NonSerialized] Job_Instance activeJobInstance;
    [ReadOnly, System.NonSerialized] List<Job_Instance> runtimeJobInstances = new();

    readonly Dictionary<Job_Data, Job_Instance> runtimeLookup = new();
    readonly List<Job_Button> spawnedButtons = new();

    public event Action<Job_Instance> OnJobStarted;
    public event Action<Job_Instance> OnJobStopped;

    public Job_Instance ActiveJobInstance => activeJobInstance;

    void Awake()
    {
        idleManager ??= FindFirstObjectByType<Idle_Manager>();
        inventoryManager ??= InventoryManager.Instance;
        menuViewManager ??= FindFirstObjectByType<Menu_MainViewManager>();
        jobListPanel ??= FindFirstObjectByType<Job_ListPanel>();
    }

    void Start()
    {
        if (rebuildButtonsOnStart)
        {
            RebuildJobButtons();
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < runtimeJobInstances.Count; i++)
        {
            runtimeJobInstances[i]?.UnregisterActive();
        }
    }

    [Button]
    public void RebuildJobButtons()
    {
        if (clearButtonsOnRebuild)
        {
            ClearSpawnedButtons();
        }

        if (jobButtonPrefab == null || jobButtonsContainer == null)
        {
            return;
        }

        IReadOnlyList<Job_Data> jobs = jobListPanel != null ? jobListPanel.Jobs : null;
        if (jobs == null)
        {
            return;
        }

        for (int i = 0; i < jobs.Count; i++)
        {
            Job_Data jobData = jobs[i];
            if (jobData == null)
            {
                continue;
            }

            Job_Button button = Instantiate(jobButtonPrefab, jobButtonsContainer);
            button.gameObject.name = $"{jobData.jobName} Button";

            bool unlocked = jobData.AreStartConditionsMet(inventoryManager, ResolveExistingInstance(jobData));
            button.Bind(jobData, this, unlocked);
            spawnedButtons.Add(button);
        }
    }

    public bool StartJob(Job_Data jobData)
    {
        if (jobData == null)
        {
            Debug.LogWarning($"{nameof(Job_Manager)} cannot start a null job.", this);
            return false;
        }

        Job_Instance instance = ResolveOrCreateInstance(jobData);
        if (!jobData.AreStartConditionsMet(inventoryManager, instance))
        {
            return false;
        }

        if (activeJobInstance != null && activeJobInstance != instance)
        {
            StopJob();
        }

        activeJobInstance = instance;
        activeJobInstance.RegisterActive();

        if (idleManager != null)
        {
            idleManager.ShowJob(activeJobInstance);
        }

        menuViewManager?.ShowPanel(panelToShowOnStartJob);
        OnJobStarted?.Invoke(activeJobInstance);
        return true;
    }

    public void StopJob()
    {
        if (activeJobInstance == null)
        {
            return;
        }

        Job_Instance previous = activeJobInstance;
        idleManager?.StopIdle();

        activeJobInstance = null;
        OnJobStopped?.Invoke(previous);
    }

    public bool TryStartJobByIndex(int index)
    {
        IReadOnlyList<Job_Data> jobs = jobListPanel != null ? jobListPanel.Jobs : null;
        if (jobs == null || index < 0 || index >= jobs.Count)
        {
            return false;
        }

        return StartJob(jobs[index]);
    }

    void ClearSpawnedButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            Job_Button button = spawnedButtons[i];
            if (button == null)
            {
                continue;
            }

            Destroy(button.gameObject);
        }

        spawnedButtons.Clear();
    }

    Job_Instance ResolveOrCreateInstance(Job_Data jobData)
    {
        if (runtimeLookup.TryGetValue(jobData, out Job_Instance existing) && existing != null)
        {
            return existing;
        }

        Job_Instance created = new Job_Instance(jobData);
        runtimeLookup[jobData] = created;
        runtimeJobInstances.Add(created);
        return created;
    }

    Job_Instance ResolveExistingInstance(Job_Data jobData)
    {
        if (jobData == null)
        {
            return null;
        }

        runtimeLookup.TryGetValue(jobData, out Job_Instance existing);
        return existing;
    }
}
