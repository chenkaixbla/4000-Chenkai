using System.Collections.Generic;
using EditorAttributes;
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
/// Card look per job: resolved as <see cref="jobCards"/> entry, else
/// <c>Job_Data.idleCardPrefabOverride</c>, else <see cref="defaultCardPrefab"/>. When a
/// <see cref="gridLayout"/> is assigned, its cell size is set to match the spawned card's
/// size, so different jobs can use differently-sized cards.
/// </summary>
[System.Serializable]
public class Idle_JobCard
{
    public Job_Data job;
    public Idle_Card cardPrefab;
}

[DisallowMultipleComponent]
public class Idle_UI : Singleton<Idle_UI>
{
    [Title("Spawning")]
    [Required]
    [Tooltip("The basic idle card used when a job has no entry/override.")]
    public Idle_Card defaultCardPrefab;

    [Required]
    [Tooltip("Parent the spawned idle cards are placed under (e.g. a layout group).")]
    public Transform cardParent;

    [Tooltip("Optional: grid whose cell size is set to match each job's card prefab size.")]
    public GridLayoutGroup gridLayout;

    // Per-job card prefab, edited via the custom Idle_UI inspector table (excluded from the
    // default fields). Resolution order: this entry, else Job_Data.idleCardPrefabOverride,
    // else defaultCardPrefab.
    [SerializeField]
    List<Idle_JobCard> jobCards = new();

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

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        GameObject root = new GameObject("PooledIdleCards");
        root.transform.SetParent(transform, false);
        root.SetActive(false);
        poolRoot = root.transform;
    }

    /// <summary>Shows the idle cards for the given job, reusing pooled cards.</summary>
    public void ShowJob(Job_Data job)
    {
        ReturnActiveCards();

        activeJob = job != null ? job.jobName : string.Empty;
        if (job == null)
            return;

        Idle_Card prefab = ResolveCardPrefab(job);
        if (prefab == null)
        {
            Debug.LogWarning($"[Idle_UI] No card prefab for job '{job.jobName}' (set a default or a job override).", this);
            return;
        }

        ApplyGridCellSize(prefab);

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

    // Card prefab for a job: jobCards entry first, then the job's own override, then the default.
    Idle_Card ResolveCardPrefab(Job_Data job)
    {
        for (int i = 0; i < jobCards.Count; i++)
        {
            if (jobCards[i] != null && jobCards[i].job == job && jobCards[i].cardPrefab != null)
                return jobCards[i].cardPrefab;
        }

        return job.idleCardPrefabOverride != null ? job.idleCardPrefabOverride : defaultCardPrefab;
    }

    /// <summary>The card prefab mapped to this job in <see cref="jobCards"/>, or null. (Editor use.)</summary>
    public Idle_Card GetJobCard(Job_Data job)
    {
        for (int i = 0; i < jobCards.Count; i++)
        {
            if (jobCards[i] != null && jobCards[i].job == job)
                return jobCards[i].cardPrefab;
        }

        return null;
    }

    /// <summary>Sets (or clears, if null) the card prefab mapped to a job. (Editor use.)</summary>
    public void SetJobCard(Job_Data job, Idle_Card card)
    {
        if (job == null)
            return;

        for (int i = 0; i < jobCards.Count; i++)
        {
            if (jobCards[i] == null || jobCards[i].job != job)
                continue;

            if (card == null)
                jobCards.RemoveAt(i);
            else
                jobCards[i].cardPrefab = card;
            return;
        }

        if (card != null)
            jobCards.Add(new Idle_JobCard { job = job, cardPrefab = card });
    }

    // Matches the grid's cell size to the card prefab's authored size, so each job's cards
    // render at their own size. Pooling is unaffected (the pool is keyed by prefab).
    void ApplyGridCellSize(Idle_Card prefab)
    {
        if (gridLayout == null || prefab == null)
            return;

        if (prefab.transform is RectTransform prefabRect)
        {
            Vector2 size = prefabRect.rect.size;
            if (size.x <= 0f || size.y <= 0f)
                size = prefabRect.sizeDelta;

            gridLayout.cellSize = size;
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
