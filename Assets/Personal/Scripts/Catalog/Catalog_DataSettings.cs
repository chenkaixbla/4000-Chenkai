using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Which data set the catalog and game read from.</summary>
public enum Catalog_DataSource
{
    Real,
    Testing
}

/// <summary>
/// Single source of truth for where job/item/idle/monster data lives. Holds the active data
/// source (Real vs Testing) and the two root folders. Gathering scans a root folder filtered by
/// type (Job_Data / ItemsData / Idle_Data / Monster_Data), recursing into any subfolders - so
/// subfolders are optional dev-only organization, never required. The per-type folder properties
/// are just the conventional subfolder paths (used as a hint for where to create new assets). The
/// Catalog spreadsheet edits this asset (data source dropdown + folder settings) and every
/// gathering script reads it through <see cref="Active"/> - flipping Real/Testing drives everything.
///
/// (This replaced the old Game_Manager scene component; the rule is the same, the home moved.)
/// </summary>
[CreateAssetMenu(fileName = "Catalog_DataSettings", menuName = "Game/Catalog/Data Settings")]
public class Catalog_DataSettings : ScriptableObject
{
    /// <summary>Raised when the active data source (or its folders) changes.</summary>
    public static event Action OnDataSourceChanged;

    public Catalog_DataSource dataSource = Catalog_DataSource.Real;

    [Tooltip("Root folder for the real data set (e.g. Assets/Personal/Data). Jobs/Items/Idles/Monsters live in subfolders.")]
    public string realDataFolder = "Assets/Personal/Data";

    [Tooltip("Root folder for the testing data set. Same subfolder layout as the real one.")]
    public string testingDataFolder = "Assets/Personal/Testing Data";

    /// <summary>Root data folder for the currently selected source. Gathering scans this by type.</summary>
    public string ActiveDataFolder => dataSource == Catalog_DataSource.Testing ? testingDataFolder : realDataFolder;

    // Conventional per-type subfolder paths. Optional organization - used only as the default
    // create location for new assets; loading reads the root (ActiveDataFolder) and recurses.
    public string JobsFolder => $"{ActiveDataFolder}/Jobs";
    public string ItemsFolder => $"{ActiveDataFolder}/Items";
    public string IdlesFolder => $"{ActiveDataFolder}/Idles";
    public string MonstersFolder => $"{ActiveDataFolder}/Monsters";

    // The single project-wide settings asset, so runtime gathering doesn't need a scene object.
    static Catalog_DataSettings _active;

    /// <summary>The project's Catalog_DataSettings asset (found once, cached). Null if none exists.</summary>
    public static Catalog_DataSettings Active
    {
        get
        {
            if (_active != null)
                return _active;

#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:Catalog_DataSettings");
            if (guids.Length > 0)
                _active = AssetDatabase.LoadAssetAtPath<Catalog_DataSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
#endif
            if (_active == null)
                _active = Resources.Load<Catalog_DataSettings>("Catalog_DataSettings");

            return _active;
        }
    }

    /// <summary>The active source's data source, or Real if no settings asset exists.</summary>
    public static Catalog_DataSource ActiveDataSource => Active != null ? Active.dataSource : Catalog_DataSource.Real;

    void OnEnable() => _active = this;

    // Inspector edits to the asset (folders, etc.) notify consumers, mirroring the old manager.
    void OnValidate() => RaiseChanged();

    /// <summary>Sets the active data source and notifies consumers (used by the spreadsheet dropdown).</summary>
    public void SetDataSource(Catalog_DataSource source)
    {
        if (dataSource == source)
            return;

        dataSource = source;
        RaiseChanged();
    }

    public static void RaiseChanged() => OnDataSourceChanged?.Invoke();
}
