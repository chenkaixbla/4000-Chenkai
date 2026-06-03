using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

[DisallowMultipleComponent]
public class Idle_Manager : MonoBehaviour
{
    [Title("References")]
    public Job_Manager jobManager;
    public InventoryManager inventoryManager;
    public Pool_ObjectPooling objectPooling;

    [Title("Card Spawning")]
    public Transform idleCardsParent;
    public Idle_CardUI defaultIdleCardPrefab;

    [Title("Behavior")]
    public bool startIdleImmediately = true;

    [ReadOnly, System.NonSerialized] Job_Instance activeJobInstance;
    [ReadOnly, System.NonSerialized] Idle_Instance activeIdleInstance;
    [ReadOnly, System.NonSerialized] List<Idle_CardUI> spawnedCards = new();

    public event Action<Idle_Instance> OnActiveIdleChanged;

    public Job_Instance ActiveJobInstance => activeJobInstance;
    public Idle_Instance ActiveIdleInstance => activeIdleInstance;

    void Awake()
    {
        jobManager ??= FindFirstObjectByType<Job_Manager>();
        inventoryManager ??= InventoryManager.Instance;
        objectPooling ??= FindFirstObjectByType<Pool_ObjectPooling>();
    }

    void Update()
    {
        if (activeIdleInstance == null || !activeIdleInstance.isRunning)
        {
            return;
        }

        activeIdleInstance.Tick(Time.deltaTime, inventoryManager);
    }

    public bool ShowJob(Job_Instance jobInstance)
    {
        if (jobInstance == null)
        {
            StopIdle();
            return false;
        }

        List<Idle_Instance> idleInstances = ResolveIdleInstances(jobInstance);
        if (idleInstances.Count == 0)
        {
            Debug.LogWarning($"Job '{jobInstance.jobData?.jobName}' has no Idle_Data assigned.", this);
            StopIdle();
            return false;
        }

        if (activeIdleInstance != null)
        {
            activeIdleInstance.StopRunning();
        }

        activeJobInstance = jobInstance;
        SpawnCards(activeJobInstance, idleInstances);

        if (startIdleImmediately && idleInstances.Count > 0)
        {
            SetActiveIdle(idleInstances[0], true);
        }
        else
        {
            SetActiveIdle(null, false);
        }

        return true;
    }

    public void ToggleIdle(Idle_Instance idleInstance)
    {
        if (idleInstance == null || activeJobInstance == null)
        {
            return;
        }

        if (activeIdleInstance == idleInstance && idleInstance.isRunning)
        {
            SetActiveIdle(null, false);
            return;
        }

        SetActiveIdle(idleInstance, true);
    }

    public void StopIdle()
    {
        SetActiveIdle(null, false);
        activeJobInstance = null;
        ClearCards();
    }

    void SetActiveIdle(Idle_Instance nextIdle, bool startRunning)
    {
        if (activeIdleInstance != null && activeIdleInstance != nextIdle)
        {
            activeIdleInstance.StopRunning();
        }

        activeIdleInstance = nextIdle;
        if (activeJobInstance != null)
        {
            activeJobInstance.SetActiveIdle(activeIdleInstance);
        }

        if (activeIdleInstance != null && startRunning)
        {
            activeIdleInstance.StartRunning(inventoryManager);
        }

        for (int i = 0; i < spawnedCards.Count; i++)
        {
            Idle_CardUI card = spawnedCards[i];
            if (card == null)
            {
                continue;
            }

            bool selected = card.BoundIdle == activeIdleInstance;
            card.SetSelectionState(selected);
            card.Refresh();
        }

        if (activeIdleInstance == null)
        {
            OnActiveIdleChanged?.Invoke(null);
            return;
        }

        OnActiveIdleChanged?.Invoke(activeIdleInstance);
    }

    List<Idle_Instance> ResolveIdleInstances(Job_Instance jobInstance)
    {
        List<Idle_Instance> instances = new();
        if (jobInstance == null || jobInstance.jobData == null)
        {
            return instances;
        }

        List<Idle_Data> validIdleDatas = jobInstance.jobData.GetValidIdleDatas();
        for (int i = 0; i < validIdleDatas.Count; i++)
        {
            Idle_Instance idleInstance = jobInstance.ResolveOrCreateIdleInstance(validIdleDatas[i]);
            if (idleInstance != null)
            {
                instances.Add(idleInstance);
            }
        }

        return instances;
    }

    void SpawnCards(Job_Instance jobInstance, List<Idle_Instance> idleInstances)
    {
        ClearCards();

        if (idleInstances == null || idleInstances.Count == 0)
        {
            return;
        }

        Idle_CardUI prefab = jobInstance != null && jobInstance.jobData != null && jobInstance.jobData.idleCardPrefabOverride != null
            ? jobInstance.jobData.idleCardPrefabOverride
            : defaultIdleCardPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"{nameof(Idle_Manager)} has no idle card prefab for job '{jobInstance?.jobData?.jobName}'.", this);
            return;
        }

        string poolKey = GetPoolKey(jobInstance, prefab);
        if (objectPooling != null)
        {
            objectPooling.Prewarm(prefab, idleInstances.Count, poolKey);
        }

        Transform parent = idleCardsParent != null ? idleCardsParent : transform;
        for (int i = 0; i < idleInstances.Count; i++)
        {
            Idle_Instance idleInstance = idleInstances[i];
            Idle_CardUI card = SpawnCard(prefab, parent, poolKey);
            if (card == null)
            {
                continue;
            }

            card.Bind(idleInstance);
            card.ConfigureToggle(this);
            card.SetSelectionState(false);
            spawnedCards.Add(card);
        }
    }

    Idle_CardUI SpawnCard(Idle_CardUI prefab, Transform parent, string poolKey)
    {
        if (prefab == null)
        {
            return null;
        }

        if (objectPooling == null)
        {
            return Instantiate(prefab, parent);
        }

        return objectPooling.Spawn(prefab, parent, poolKey);
    }

    void ClearCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            Idle_CardUI card = spawnedCards[i];
            if (card == null)
            {
                continue;
            }

            if (objectPooling != null)
            {
                objectPooling.Return(card);
                continue;
            }

            card.Unbind();
            Destroy(card.gameObject);
        }

        spawnedCards.Clear();
    }

    static string GetPoolKey(Job_Instance jobInstance, Idle_CardUI prefab)
    {
        if (jobInstance == null || jobInstance.jobData == null || jobInstance.jobData.idleCardPrefabOverride != prefab)
        {
            return null;
        }

        return $"job:{jobInstance.jobData.GetInstanceID()}";
    }

    void OnDisable()
    {
        ClearCards();
    }
}
