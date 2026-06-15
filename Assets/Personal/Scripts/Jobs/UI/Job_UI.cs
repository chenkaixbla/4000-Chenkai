using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Spawns one <see cref="UI_Button"/> per job under a parent transform. Two job lists are
/// gathered in the editor (real + testing) from <see cref="Game_Manager"/>'s folders, and
/// the active one is chosen at runtime from <see cref="Game_Manager.dataSource"/> - so you
/// can flip Real/Testing on the Game_Manager and just play, no re-gather needed.
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
    [Tooltip("Menu opened when any spawned job button is clicked.")]
    public string menuToOpen;

    [Tooltip("Optional: idle view to populate with the clicked job's idle cards.")]
    public Idle_UI idleUI;

    [Title("Jobs (gathered automatically on Build / Play)")]
    [MessageBox("Job lists fill in automatically when you press Build or enter Play.", nameof(HasNoJobs), MessageMode.None)]
    [ReadOnly, SerializeField]
    List<Job_Data> realJobs = new();

    [ReadOnly, SerializeField]
    List<Job_Data> testingJobs = new();

    // Only the buttons this script spawned, so a rebuild touches nothing else under the parent.
    readonly List<UI_Button> spawned = new();

    bool HasNoJobs => (realJobs == null || realJobs.Count == 0) && (testingJobs == null || testingJobs.Count == 0);

    void Start()
    {
        if (buildOnStart)
            Build();
    }

    void OnEnable()
    {
        Game_Manager.OnDataSourceChanged += HandleDataSourceChanged;
    }

    void OnDisable()
    {
        Game_Manager.OnDataSourceChanged -= HandleDataSourceChanged;
    }

    // Rebuild when the data source is flipped at runtime, so no manual Build is needed.
    void HandleDataSourceChanged()
    {
        if (Application.isPlaying)
            Build();
    }

    // The job list for the data source currently selected on the Game_Manager.
    List<Job_Data> ActiveJobs()
    {
        Game_Manager gm = Game_Manager.Instance != null ? Game_Manager.Instance : FindFirstObjectByType<Game_Manager>();
        Game_DataSource source = gm != null ? gm.dataSource : Game_DataSource.Real;
        return source == Game_DataSource.Testing ? testingJobs : realJobs;
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

            UI_Button button = Instantiate(buttonPrefab, buttonParent, false);
            button.name = string.IsNullOrWhiteSpace(job.jobName) ? $"JobButton_{i}" : $"JobButton_{job.jobName}";
            button.SetText(job.jobName);
            button.SetImage(job.jobIcon);

            if (button.button != null)
            {
                Job_Data clicked = job;
                string menu = menuToOpen;
                button.button.onClick.AddListener(() =>
                {
                    if (menuManager != null && !string.IsNullOrWhiteSpace(menu))
                        menuManager.Show(menu);

                    if (idleUI != null)
                        idleUI.ShowJob(clicked);
                });
            }

            spawned.Add(button);
        }
    }

    /// <summary>Menu names from the referenced manager - feeds the [Dropdown].</summary>
    string[] GetMenuNames()
    {
        UI_Menu_Manager manager = menuManager != null ? menuManager : FindFirstObjectByType<UI_Menu_Manager>();
        return manager != null ? manager.GetMenuNames() : new[] { string.Empty };
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
    // Refreshes both cached lists from Game_Manager's folders. Runs inside Build so the
    // designer never has to gather manually.
    void GatherJobsIntoCaches()
    {
        Game_Manager gm = Game_Manager.Instance != null ? Game_Manager.Instance : FindFirstObjectByType<Game_Manager>();
        string realFolder = $"{(gm != null ? gm.realDataFolder : "Assets/Personal/Data")}/Jobs";
        string testFolder = $"{(gm != null ? gm.testingDataFolder : "Assets/Personal/Testing Data")}/Jobs";

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
