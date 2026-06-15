using EditorAttributes;
using UnityEngine;

/// <summary>Which data set the game reads from.</summary>
public enum Game_DataSource
{
    Real,
    Testing
}

/// <summary>
/// Scene-wide game controller. For now its only job is to pick the active data set
/// (real vs testing) so other systems can gather from one place instead of hard-coding
/// a folder. One instance per scene, exposed via <see cref="Instance"/>.
///
/// Future systems (job/item/idle gathering, etc.) read <see cref="ActiveDataFolder"/>
/// or the per-type folder helpers rather than referencing a fixed path.
/// </summary>
[DisallowMultipleComponent]
public class Game_Manager : MonoBehaviour
{
    public static Game_Manager Instance { get; private set; }

    /// <summary>Raised when this manager's data-source config changes in the inspector.</summary>
    public static event System.Action OnDataSourceChanged;

    [Title("Data Source")]
    [Tooltip("Switch which data set the game captures from. Future gathering code reads this.")]
    public Game_DataSource dataSource = Game_DataSource.Real;

    [Title("Folders")]
    [Tooltip("Root folder for the real data set (project-relative, e.g. Assets/Personal/Data).")]
    public string realDataFolder = "Assets/Personal/Data";

    [Tooltip("Root folder for the testing data set.")]
    public string testingDataFolder = "Assets/Personal/Testing Data";

    /// <summary>Root data folder for the currently selected source.</summary>
    public string ActiveDataFolder => dataSource == Game_DataSource.Testing ? testingDataFolder : realDataFolder;

    public string JobsFolder => $"{ActiveDataFolder}/Jobs";
    public string ItemsFolder => $"{ActiveDataFolder}/Items";
    public string IdlesFolder => $"{ActiveDataFolder}/Idles";

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning($"[Game_Manager] A second Game_Manager '{name}' was found. There should be one per scene.", this);

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Fires when the data source (or folders) are edited in the inspector. Consumers that
    // subscribe at runtime (e.g. Job_UI) rebuild themselves so flipping the enum is live.
    void OnValidate()
    {
        OnDataSourceChanged?.Invoke();
    }
}
