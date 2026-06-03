using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Spawns one <see cref="UI_Button"/> per job under a parent transform. The job list
/// is gathered automatically from the Catalog (the same Job_Data assets the catalog
/// spreadsheet edits), so the number of buttons and their text/icon always match the
/// jobs that exist - no manual button placement.
///
/// Workflow: the list is gathered in the editor (auto on add, or the Refresh button)
/// and saved with the scene, then spawned at runtime. Building the assets at runtime
/// can't use the editor's asset database, so the gather step happens in-editor.
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

    [Title("Jobs (gathered from the Catalog)")]
    [MessageBox("No jobs gathered yet. Press 'Refresh Jobs From Catalog'.", nameof(HasNoJobs), MessageMode.Warning)]
    [ReadOnly, SerializeField]
    List<Job_Data> jobs = new();

    readonly List<UI_Button> spawned = new();

    bool HasNoJobs => jobs == null || jobs.Count == 0;

    void Start()
    {
        if (buildOnStart)
            Build();
    }

    /// <summary>Clears any spawned buttons and creates one per gathered job.</summary>
    [Button]
    public void Build()
    {
        ClearSpawned();

        if (buttonPrefab == null || buttonParent == null)
        {
            Debug.LogWarning("[Job_UI] Assign a button prefab and a parent before building.", this);
            return;
        }

        for (int i = 0; i < jobs.Count; i++)
        {
            Job_Data job = jobs[i];
            if (job == null)
                continue;

            UI_Button button = Instantiate(buttonPrefab, buttonParent, false);
            button.name = string.IsNullOrWhiteSpace(job.jobName) ? $"JobButton_{i}" : $"JobButton_{job.jobName}";
            button.SetText(job.jobName);
            button.SetImage(job.jobIcon);

            if (button.button != null && menuManager != null && !string.IsNullOrWhiteSpace(menuToOpen))
            {
                string menu = menuToOpen;
                button.button.onClick.AddListener(() => menuManager.Show(menu));
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
    [Button]
    [Tooltip("Pulls every Job_Data from the Catalog's jobs folder into the list.")]
    void RefreshJobsFromCatalog()
    {
        jobs.Clear();

        string folder = ResolveJobsFolder();
        bool validFolder = !string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder);

        string[] guids = validFolder
            ? AssetDatabase.FindAssets("t:Job_Data", new[] { folder })
            : AssetDatabase.FindAssets("t:Job_Data");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Job_Data job = AssetDatabase.LoadAssetAtPath<Job_Data>(path);
            if (job != null)
                jobs.Add(job);
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"[Job_UI] Gathered {jobs.Count} job(s) from the Catalog.", this);
    }

    static string ResolveJobsFolder()
    {
        string[] settingsGuids = AssetDatabase.FindAssets("t:Catalog_DataSettings");
        if (settingsGuids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(settingsGuids[0]);
        Catalog_DataSettings settings = AssetDatabase.LoadAssetAtPath<Catalog_DataSettings>(path);
        return settings != null ? settings.jobsDataFolder : null;
    }
#endif
}
