using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Per-job job-bar config: which menu this job's button opens and whether the single job bar
/// is shown while viewing it. Edited via the custom Job_UI inspector table. A job with no entry
/// uses the default <see cref="Job_UI.menuToOpen"/> and shows the bar.
/// </summary>
[Serializable]
public class Job_BarEntry
{
    public Job_Data job;
    [Tooltip("Menu this job opens. Empty = use Job_UI's default menuToOpen.")]
    public string menuName;
    [Tooltip("Whether the job bar shows while this job is being viewed.")]
    public bool showJobBar = true;
}

/// <summary>
/// Spawns one <see cref="UI_Button"/> per job under a parent transform. Two job lists are
/// gathered in the editor (real + testing) from <see cref="Catalog_DataSettings"/>'s folders, and
/// the active one is chosen at runtime from <see cref="Catalog_DataSettings.dataSource"/> - so you
/// can flip Real/Testing in the Catalog Spreadsheet and just play, no re-gather needed.
///
/// Gathering uses the editor asset database (can't run in a build), so it's a one-time
/// editor step (the Refresh button); spawning happens at runtime from the serialized lists.
/// </summary>
[DisallowMultipleComponent]
public class Job_UI : MonoBehaviour
{
    [Title("Spawning")]
    [Required]
    [Tooltip("UI_Button prefab to spawn for each job.")]
    public UI_Button buttonPrefab;

    [Required]
    [Tooltip("Parent the spawned job buttons are placed under (e.g. a layout group).")]
    public Transform buttonParent;

    public bool buildOnStart = true;

    [Title("Navigation")]
    [Required]
    [Tooltip("Menu manager used to open a menu when a job button is clicked.")]
    public UI_Menu_Manager menuManager;

    [Dropdown(nameof(GetMenuNames))]
    [Tooltip("Default menu opened by a job button when its row in the table has no menu set.")]
    public string menuToOpen;

    [Tooltip("Optional: idle view to populate with the clicked job's idle cards.")]
    public Idle_UI idleUI;

    [Title("Job Bar")]
    [Tooltip("The single job bar shown while viewing a job: its title text = level, value text = XP.")]
    public UI_Bar jobBar;
    [Tooltip("Optional: root object toggled with the job view. If empty, the bar's own GameObject is used.")]
    public GameObject jobBarRoot;

    [Title("Jobs (gathered automatically on Build / Play)")]
    [MessageBox("Job lists fill in automatically when you press Build or enter Play.", nameof(HasNoJobs), MessageMode.None)]
    [ReadOnly, SerializeField]
    List<Job_Data> realJobs = new();

    [ReadOnly, SerializeField]
    List<Job_Data> testingJobs = new();

    // Per-job bar config (menu + show-bar), edited via the custom Job_UI inspector table
    // ([HideInInspector] so it doesn't also draw as a raw list).
    [HideInInspector, SerializeField]
    List<Job_BarEntry> jobBarEntries = new();

    // Only the buttons this script spawned, so a rebuild touches nothing else under the parent.
    readonly List<UI_Button> spawned = new();

    // The job currently being viewed (last clicked job button), and its bound runtime.
    Job_Data currentJob;
    Job_Runtime boundJob;

    bool HasNoJobs => (realJobs == null || realJobs.Count == 0) && (testingJobs == null || testingJobs.Count == 0);

    void Start()
    {
        if (buildOnStart)
            Build();
    }

    void OnEnable()
    {
        Catalog_DataSettings.OnDataSourceChanged += HandleDataSourceChanged;
        if (menuManager != null)
            menuManager.OnMenuChanged += HandleMenuChanged;
    }

    void OnDisable()
    {
        Catalog_DataSettings.OnDataSourceChanged -= HandleDataSourceChanged;
        if (menuManager != null)
            menuManager.OnMenuChanged -= HandleMenuChanged;
        UnbindJobDisplay();
    }

    // The active menu changed (e.g. opened the shop): re-evaluate whether the job bar shows.
    void HandleMenuChanged(string menuName) => UpdateJobBar();

    // Rebuild when the data source is flipped at runtime, so no manual Build is needed.
    void HandleDataSourceChanged()
    {
        if (Application.isPlaying)
            Build();
    }

    // The job list for the data source currently selected in Catalog_DataSettings.
    List<Job_Data> ActiveJobs()
    {
        return Catalog_DataSettings.ActiveDataSource == Catalog_DataSource.Testing ? testingJobs : realJobs;
    }

    /// <summary>Clears any spawned buttons and creates one per job in the active data source.</summary>
    [Button]
    public void Build()
    {
        ClearSpawned();

        if (buttonPrefab == null || buttonParent == null)
        {
            Debug.LogWarning("[Job_UI] Assign a button prefab and a parent before building.", this);
            return;
        }

#if UNITY_EDITOR
        GatherJobsIntoCaches();
#endif

        List<Job_Data> jobs = ActiveJobs();
        for (int i = 0; i < jobs.Count; i++)
        {
            Job_Data job = jobs[i];
            if (job == null)
                continue;

            // Skip jobs the Job_Manager has disabled.
            if (Job_Manager.Instance != null && !Job_Manager.Instance.IsJobEnabled(job))
                continue;

            UI_Button button = Instantiate(buttonPrefab, buttonParent, false);
            button.name = string.IsNullOrWhiteSpace(job.jobName) ? $"JobButton_{i}" : $"JobButton_{job.jobName}";
            button.SetText(job.jobName);
            button.SetImage(job.jobIcon);

            if (button.button != null)
            {
                Job_Data clicked = job;
                button.button.onClick.AddListener(() => OnJobButtonClicked(clicked));
            }

            spawned.Add(button);
        }
    }

    // Clicking a job button: remember it, open its menu and idle view, then refresh the bar.
    void OnJobButtonClicked(Job_Data job)
    {
        currentJob = job;

        string menu = GetEffectiveMenu(job);
        if (menuManager != null && !string.IsNullOrWhiteSpace(menu))
            menuManager.Show(menu); // raises OnMenuChanged -> UpdateJobBar

        if (idleUI != null)
            idleUI.ShowJob(job);

        UpdateJobBar(); // also covers the no-menu-manager case
    }

    // --- Job bar (one shared bar, shown only while a job's view is active) ---

    // Shows + syncs the bar when the active menu matches the current job's menu and the bar is
    // enabled for it; hides it otherwise (e.g. after switching to a non-job menu like the shop).
    void UpdateJobBar()
    {
        string active = menuManager != null ? menuManager.ActiveMenu : null;

        bool onJobView = currentJob != null
            && IsJobBarEnabled(currentJob)
            && !string.IsNullOrWhiteSpace(active)
            && string.Equals(active.Trim(), GetEffectiveMenu(currentJob)?.Trim(), StringComparison.OrdinalIgnoreCase);

        if (onJobView)
        {
            SetJobBarActive(true);
            BindJobDisplay(currentJob);
        }
        else
        {
            UnbindJobDisplay();
            SetJobBarActive(false);
        }
    }

    void SetJobBarActive(bool active)
    {
        GameObject root = jobBarRoot != null ? jobBarRoot : (jobBar != null ? jobBar.gameObject : null);
        if (root != null && root.activeSelf != active)
            root.SetActive(active);
    }

    // --- Job display (level / xp for the selected job) ---

    void BindJobDisplay(Job_Data job)
    {
        UnbindJobDisplay();

        if (job == null || Job_Manager.Instance == null)
        {
            ClearJobDisplay();
            return;
        }

        boundJob = Job_Manager.Instance.GetRuntime(job);
        if (boundJob == null)
        {
            ClearJobDisplay();
            return;
        }

        boundJob.OnUpdated += RefreshJobDisplay;
        RefreshJobDisplay();
    }

    void UnbindJobDisplay()
    {
        if (boundJob != null)
            boundJob.OnUpdated -= RefreshJobDisplay;

        boundJob = null;
    }

    void RefreshJobDisplay()
    {
        if (boundJob == null || jobBar == null)
            return;

        if (jobBar.titleText != null)
            jobBar.titleText.text = $"Level: {boundJob.level}";

        jobBar.maxValue = boundJob.maxXP;
        jobBar.SetValue(boundJob.currentXP);
    }

    void ClearJobDisplay()
    {
        if (jobBar == null)
            return;

        if (jobBar.titleText != null) jobBar.titleText.text = string.Empty;
        jobBar.SetFill(0f);
    }

    /// <summary>Menu names from the referenced manager - feeds the [Dropdown] and the editor table.</summary>
    public string[] GetMenuNames()
    {
        UI_Menu_Manager manager = menuManager != null ? menuManager : FindFirstObjectByType<UI_Menu_Manager>();
        return manager != null ? manager.GetMenuNames() : new[] { string.Empty };
    }

    // --- Per-job bar config (read/written by the custom Job_UI editor table) ---

    /// <summary>The menu actually opened for a job: its row's menu, else the default menuToOpen.</summary>
    public string GetEffectiveMenu(Job_Data job)
    {
        string row = GetJobMenu(job);
        return string.IsNullOrWhiteSpace(row) ? menuToOpen : row;
    }

    /// <summary>The raw menu set for a job in its row (empty = use the default). (Editor use.)</summary>
    public string GetJobMenu(Job_Data job)
    {
        Job_BarEntry entry = FindEntry(job);
        return entry != null ? entry.menuName : string.Empty;
    }

    /// <summary>Sets (or clears) the menu for a job's row. (Editor use.)</summary>
    public void SetJobMenu(Job_Data job, string menu)
    {
        if (job == null)
            return;

        GetOrCreateEntry(job).menuName = string.IsNullOrWhiteSpace(menu) ? string.Empty : menu;
        PruneEntry(job);
    }

    /// <summary>Whether the job bar shows for a job. Jobs without a row default to true.</summary>
    public bool IsJobBarEnabled(Job_Data job)
    {
        Job_BarEntry entry = FindEntry(job);
        return entry == null || entry.showJobBar;
    }

    /// <summary>Sets whether the job bar shows for a job. (Editor use.)</summary>
    public void SetJobBarEnabled(Job_Data job, bool enabled)
    {
        if (job == null)
            return;

        GetOrCreateEntry(job).showJobBar = enabled;
        PruneEntry(job);
    }

    Job_BarEntry FindEntry(Job_Data job)
    {
        if (job == null)
            return null;

        for (int i = 0; i < jobBarEntries.Count; i++)
        {
            if (jobBarEntries[i] != null && jobBarEntries[i].job == job)
                return jobBarEntries[i];
        }

        return null;
    }

    Job_BarEntry GetOrCreateEntry(Job_Data job)
    {
        Job_BarEntry entry = FindEntry(job);
        if (entry == null)
        {
            entry = new Job_BarEntry { job = job };
            jobBarEntries.Add(entry);
        }

        return entry;
    }

    // Drops an entry that's back to all-defaults (no menu override, bar shown) to keep the list tidy.
    void PruneEntry(Job_Data job)
    {
        for (int i = 0; i < jobBarEntries.Count; i++)
        {
            if (jobBarEntries[i] == null || jobBarEntries[i].job != job)
                continue;

            if (string.IsNullOrWhiteSpace(jobBarEntries[i].menuName) && jobBarEntries[i].showJobBar)
                jobBarEntries.RemoveAt(i);
            return;
        }
    }

    // Destroys only the buttons this script spawned - other children of buttonParent are left alone.
    void ClearSpawned()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] == null)
                continue;

            if (Application.isPlaying)
                Destroy(spawned[i].gameObject);
            else
                DestroyImmediate(spawned[i].gameObject);
        }

        spawned.Clear();
    }

#if UNITY_EDITOR
    // Refreshes both cached lists from Catalog_DataSettings' folders. Runs inside Build so the
    // designer never has to gather manually.
    void GatherJobsIntoCaches()
    {
        // Search each source root by type; subfolders are optional dev organization (FindAssets recurses).
        Catalog_DataSettings settings = Catalog_DataSettings.Active;
        string realFolder = settings != null ? settings.realDataFolder : "Assets/Personal/Data";
        string testFolder = settings != null ? settings.testingDataFolder : "Assets/Personal/Testing Data";

        realJobs = GatherJobs(realFolder);
        testingJobs = GatherJobs(testFolder);

        if (!Application.isPlaying)
            EditorUtility.SetDirty(this);
    }

    static List<Job_Data> GatherJobs(string folder)
    {
        List<Job_Data> result = new List<Job_Data>();
        if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"[Job_UI] Jobs folder '{folder}' is not a valid project folder.");
            return result;
        }

        string[] guids = AssetDatabase.FindAssets("t:Job_Data", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Job_Data job = AssetDatabase.LoadAssetAtPath<Job_Data>(path);
            if (job != null)
                result.Add(job);
        }

        return result;
    }
#endif
}
