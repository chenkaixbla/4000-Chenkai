using System.Collections.Generic;
using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the idle view for a selected job: spawns one <see cref="Idle_Card"/> per idle
/// and binds it to that idle's <see cref="Idle_Runtime"/>. One instance per scene.
///
/// Performance is handled automatically:
/// - Cards are pooled (keyed by prefab) and created lazily - switching jobs reuses cards
///   instead of destroying/instantiating. The pool is set up at runtime; nothing to wire.
/// - Runtimes persist per job, so a job keeps its state when you switch away and back.
///
/// Card look per job: <see cref="defaultCardPrefab"/> is the basic card; a job can override
/// it with <c>Job_Data.idleCardPrefabOverride</c> for special formatting. Resolution is
/// 'job override, else default' (per-job + default).
/// </summary>
[DisallowMultipleComponent]
public class Idle_UI : MonoBehaviour
{
    public static Idle_UI Instance { get; private set; }

    [Title("Spawning")]
    [Required]
    [Tooltip("The basic idle card used when a job has no override.")]
    public Idle_Card defaultCardPrefab;

    [Required]
    [Tooltip("Parent the spawned idle cards are placed under (e.g. a layout group).")]
    public Transform cardParent;

    [Title("Current Job Display (all optional)")]
    [Tooltip("Level label for the job being viewed. Formatted \"Level: ##\".")]
    public TMP_Text jobLevelText;
    [Tooltip("Experience label for the job being viewed. Formatted \"current/max\".")]
    public TMP_Text jobExperienceText;
    [Tooltip("Fill image for the job level / XP bar.")]
    public Image jobLevelBarFill;

    [Title("Runtime")]
    [ReadOnly, SerializeField]
    string activeJob;

    // Pool keyed by the prefab reference, so basic and special cards pool separately.
    readonly Dictionary<Idle_Card, Stack<Idle_Card>> pool = new();
    readonly Dictionary<Idle_Card, Idle_Card> prefabOfInstance = new();
    Transform poolRoot;

    // Fallback runtimes per job, only used when there's no Idle_Manager in the scene.
    // When a manager exists it owns the runtimes (and ticks them); this stays empty.
    readonly Dictionary<Job_Data, List<Idle_Runtime>> fallbackRuntimes = new();

    readonly List<Idle_Card> activeCards = new();

    // The job whose level/xp is currently shown in the Current Job Display.
    Job_Runtime boundJob;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning($"[Idle_UI] A second Idle_UI '{name}' was found. There should be one per scene.", this);

        Instance = this;

        GameObject root = new GameObject("PooledIdleCards");
        root.transform.SetParent(transform, false);
        root.SetActive(false);
        poolRoot = root.transform;
    }

    void OnDestroy()
    {
        UnbindJobDisplay();

        if (Instance == this)
            Instance = null;
    }

    /// <summary>Shows the idle cards for the given job, reusing pooled cards.</summary>
    public void ShowJob(Job_Data job)
    {
        ReturnActiveCards();
        BindJobDisplay(job);

        activeJob = job != null ? job.jobName : string.Empty;
        if (job == null)
            return;

        Idle_Card prefab = job.idleCardPrefabOverride != null ? job.idleCardPrefabOverride : defaultCardPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[Idle_UI] No card prefab for job '{job.jobName}' (set a default or a job override).", this);
            return;
        }

        List<Idle_Runtime> runtimes = ResolveRuntimes(job);
        for (int i = 0; i < runtimes.Count; i++)
        {
            Idle_Card card = Spawn(prefab);
            card.transform.SetParent(cardParent, false);
            card.transform.SetSiblingIndex(i);
            card.ToggleRequested += HandleToggle;
            card.Bind(runtimes[i]);
            activeCards.Add(card);
        }
    }

    void HandleToggle(Idle_Card card)
    {
        if (card.Bound == null)
            return;

        if (Idle_Manager.Instance != null)
            Idle_Manager.Instance.ToggleIdle(card.Bound);
        else
            card.Bound.Toggle();
    }

    // --- Current Job Display (level / xp for the job being viewed) ---

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
        if (boundJob == null)
            return;

        if (jobLevelText != null)
            jobLevelText.text = $"Level: {boundJob.level}";

        if (jobExperienceText != null)
            jobExperienceText.text = $"{boundJob.currentXP}/{boundJob.maxXP}";

        if (jobLevelBarFill != null)
            jobLevelBarFill.fillAmount = boundJob.GetNormalizedLevelProgress();
    }

    void ClearJobDisplay()
    {
        if (jobLevelText != null) jobLevelText.text = string.Empty;
        if (jobExperienceText != null) jobExperienceText.text = string.Empty;
        if (jobLevelBarFill != null) jobLevelBarFill.fillAmount = 0f;
    }

    // Runtimes come from the Idle_Manager (it owns them and ticks them). If no manager is
    // in the scene, fall back to local runtimes so cards still display - they just won't tick.
    List<Idle_Runtime> ResolveRuntimes(Job_Data job)
    {
        if (Idle_Manager.Instance != null)
            return Idle_Manager.Instance.GetRuntimes(job);

        if (fallbackRuntimes.TryGetValue(job, out List<Idle_Runtime> existing))
            return existing;

        List<Idle_Runtime> list = new List<Idle_Runtime>();
        List<Idle_Data> datas = job.GetValidIdleDatas();
        for (int i = 0; i < datas.Count; i++)
            list.Add(new Idle_Runtime(datas[i]));

        fallbackRuntimes[job] = list;
        return list;
    }

    void ReturnActiveCards()
    {
        for (int i = 0; i < activeCards.Count; i++)
        {
            Idle_Card card = activeCards[i];
            if (card == null)
                continue;

            card.ToggleRequested -= HandleToggle;
            card.Unbind();
            Return(card);
        }

        activeCards.Clear();
    }

    // --- Pooling (keyed by prefab) ---

    Idle_Card Spawn(Idle_Card prefab)
    {
        if (!pool.TryGetValue(prefab, out Stack<Idle_Card> stack))
        {
            stack = new Stack<Idle_Card>();
            pool[prefab] = stack;
        }

        Idle_Card card = null;
        while (stack.Count > 0 && card == null)
            card = stack.Pop();

        if (card == null)
        {
            card = Instantiate(prefab);
            prefabOfInstance[card] = prefab;
        }

        card.gameObject.SetActive(true);
        return card;
    }

    void Return(Idle_Card card)
    {
        card.gameObject.SetActive(false);
        card.transform.SetParent(poolRoot, false);

        if (prefabOfInstance.TryGetValue(card, out Idle_Card prefab) && pool.TryGetValue(prefab, out Stack<Idle_Card> stack))
            stack.Push(card);
        else
            Destroy(card.gameObject);
    }
}
