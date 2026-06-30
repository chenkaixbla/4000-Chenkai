using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Shows the craftables of one crafting job: spawns an <see cref="Item_Slot"/> (icon + name)
/// under a content parent for every item produced by that job's crafting idles.
///
/// The job is chosen by name from a <see cref="craftingJobName"/> dropdown that auto-lists the
/// active <see cref="Catalog_DataSettings"/> source's jobs. Like Job_UI, both job lists (real +
/// testing) are cached in the editor so the chosen job can be resolved by name at runtime - flip
/// the data source in the Catalog Spreadsheet and just play, no re-gather needed.
/// </summary>
[DisallowMultipleComponent]
public class Crafting_UI : MonoBehaviour
{
    [Title("Spawning")]
    [Required]
    [Tooltip("Item slot prefab spawned per craftable (only its icon + name are used here).")]
    public Item_Slot itemSlotPrefab;

    [Required]
    [Tooltip("Parent the spawned craftable slots are placed under (e.g. a layout group).")]
    public Transform contentParent;

    public bool buildOnStart = true;

    [Line]

    [HelpBox("Which job's craftables to show. Names come from the active Catalog data source.", MessageMode.None)]
    [Dropdown(nameof(GetJobNames))]
    public string craftingJobName;

    // Both job lists cached in the editor (from Catalog_DataSettings' folders) so the chosen job
    // can be resolved by name at runtime, where the asset database isn't available.
    [Title("Jobs")]
    [HelpBox("Filled automatically on Build / Play.", MessageMode.None, drawAbove: true)]
    [ReadOnly, SerializeField]
    List<Job_Data> realJobs = new();

    [ReadOnly, SerializeField]
    List<Job_Data> testingJobs = new();

    // Only the slots this script spawned, so a rebuild leaves other children of the parent alone.
    readonly List<Item_Slot> spawned = new();

    void Start()
    {
        if (buildOnStart)
            Build();
    }

    void OnEnable() => Catalog_DataSettings.OnDataSourceChanged += HandleDataSourceChanged;

    void OnDisable() => Catalog_DataSettings.OnDataSourceChanged -= HandleDataSourceChanged;

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

    // The active-source job whose name matches the dropdown selection.
    Job_Data ResolveCraftingJob()
    {
        List<Job_Data> jobs = ActiveJobs();
        for (int i = 0; i < jobs.Count; i++)
        {
            if (jobs[i] != null && jobs[i].jobName == craftingJobName)
                return jobs[i];
        }

        return null;
    }

    /// <summary>Clears spawned slots and creates one per craftable of the selected crafting job.</summary>
    [Button]
    public void Build()
    {
        ClearSpawned();

        if (itemSlotPrefab == null || contentParent == null)
        {
            Debug.LogWarning("[Crafting_UI] Assign an item slot prefab and a content parent before building.", this);
            return;
        }

#if UNITY_EDITOR
        GatherJobsIntoCaches();
#endif

        Job_Data job = ResolveCraftingJob();
        if (job == null)
        {
            Debug.LogWarning($"[Crafting_UI] No job named '{craftingJobName}' in the active data source.", this);
            return;
        }

        List<ItemsData> craftables = GatherCraftables(job);
        for (int i = 0; i < craftables.Count; i++)
        {
            ItemsData item = craftables[i];
            if (item == null)
                continue;

            Item_Slot slot = Instantiate(itemSlotPrefab, contentParent, false);
            slot.name = string.IsNullOrWhiteSpace(item.displayName) ? $"Craftable_{i}" : $"Craftable_{item.displayName}";
            slot.Bind(item);
            spawned.Add(slot);
        }
    }

    // Produced items from every crafting idle on the job, deduped and kept in order.
    static List<ItemsData> GatherCraftables(Job_Data job)
    {
        List<ItemsData> result = new List<ItemsData>();
        if (job == null || job.idleDatas == null)
            return result;

        for (int i = 0; i < job.idleDatas.Count; i++)
        {
            if (job.idleDatas[i] is not Idle_Data_Crafting crafting)
                continue;

            for (int p = 0; p < crafting.produces.Count; p++)
            {
                ItemsData item = crafting.produces[p] != null ? crafting.produces[p].item : null;
                if (item != null && !result.Contains(item))
                    result.Add(item);
            }
        }

        return result;
    }

    // Destroys only the slots this script spawned - other children of contentParent are left alone.
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

    /// <summary>Job names for the [Dropdown], from the active Catalog source (auto-refreshes).</summary>
    public string[] GetJobNames()
    {
        List<Job_Data> jobs;

#if UNITY_EDITOR
        // Gather fresh from the active source so the dropdown stays current without a manual refresh.
        Catalog_DataSettings settings = Catalog_DataSettings.Active;
        jobs = GatherJobs(settings != null ? settings.ActiveDataFolder : "Assets/Personal/Data");
#else
        jobs = ActiveJobs();
#endif

        List<string> names = new List<string>();
        for (int i = 0; i < jobs.Count; i++)
        {
            if (jobs[i] != null && !string.IsNullOrWhiteSpace(jobs[i].jobName))
                names.Add(jobs[i].jobName);
        }

        return names.Count > 0 ? names.ToArray() : new[] { string.Empty };
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
            return result;

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
