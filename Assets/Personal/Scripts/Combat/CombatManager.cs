using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using UnityEngine.UI;

public partial class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    const int MaxCombatLogEntries = 18;
    const float DefaultPlayerAttackInterval = 3f;

    [Title("Dependencies")]
    public InventoryManager inventory;
    public CardViewManager cardViewManager;
    public Button combatButton;
    public CombatPageController pageController;
    public int combatViewIndex = 4;

    [Title("Data")]
    public List<MonsterData> monsterDatas = new();

    [Title("Configuration")]
    [Min(0.1f)] public float monsterRespawnDelay = 2f;
    [Range(0f, 1f)] public float idleDropPenaltyMultiplier = 0.75f;
    [Min(0f)] public float deathDebuffDurationSeconds = 600f;
    [Min(0f)] public float foodCooldownSeconds = 5f;

    [Title("Runtime")]
    public CombatProfile profile = new();
    [SerializeField] CombatEncounterState encounter = new();

    readonly Dictionary<string, int> monsterKillCounts = new();
    readonly List<string> combatLog = new();

    MonsterData selectedMonster;
    bool autoCombatEnabled;
    bool deathDebuffWasActive;
    bool potionWasActive;

    public event Action StateChanged;

    public IReadOnlyList<MonsterData> MonsterDatas => monsterDatas;
    public IReadOnlyList<string> CombatLog => combatLog;
    public CombatProfile Profile => profile;
    public CombatEncounterState Encounter => encounter;
    public MonsterData SelectedMonster => selectedMonster;
    public MonsterData DisplayedMonster => encounter.monsterData != null ? encounter.monsterData : selectedMonster;
    public bool IsCombatRunning => autoCombatEnabled && encounter.monsterData != null;
    public bool IsRespawning => encounter.isRespawning;

    void Awake()
    {
        Instance = this;
        profile ??= new CombatProfile();
        profile.EnsureInitialized();
        encounter ??= new CombatEncounterState();
    }

    void Start()
    {
        if (inventory == null)
        {
            inventory = InventoryManager.Instance;
        }

        if (inventory != null)
        {
            inventory.OnInventoryChanged -= HandleInventoryChanged;
            inventory.OnInventoryChanged += HandleInventoryChanged;
        }

        if (combatButton != null)
        {
            combatButton.onClick.RemoveListener(ShowCombatPage);
            combatButton.onClick.AddListener(ShowCombatPage);
        }

        pageController?.Initialize(this);
        ValidateLoadout();
        NotifyStateChanged();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (inventory != null)
        {
            inventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        if (combatButton != null)
        {
            combatButton.onClick.RemoveListener(ShowCombatPage);
        }
    }

    void Update()
    {
        profile.EnsureInitialized();

        float now = Time.unscaledTime;
        bool stateChanged = UpdateTimedStatuses();

        if (!autoCombatEnabled || selectedMonster == null || profile.currentHp <= 0)
        {
            if (stateChanged)
            {
                NotifyStateChanged();
            }

            return;
        }

        if (encounter.isRespawning)
        {
            if (now >= encounter.respawnReadyTime)
            {
                SpawnEncounter(selectedMonster);
                stateChanged = true;
            }

            if (stateChanged)
            {
                NotifyStateChanged();
            }

            return;
        }

        if (encounter.monsterData == null)
        {
            SpawnEncounter(selectedMonster);
            NotifyStateChanged();
            return;
        }

        int safety = 8;
        while (safety-- > 0 && autoCombatEnabled && encounter.monsterData != null && !encounter.isRespawning)
        {
            bool processedAction = false;

            now = Time.unscaledTime;
            if (now >= encounter.playerAttackReadyTime)
            {
                encounter.playerAttackReadyTime = now + GetPlayerAttackInterval();
                ResolvePlayerAttack();
                processedAction = true;

                if (encounter.monsterData == null || encounter.isRespawning || !autoCombatEnabled)
                {
                    break;
                }
            }

            now = Time.unscaledTime;
            if (encounter.monsterData != null && now >= encounter.monsterAttackReadyTime)
            {
                encounter.monsterAttackReadyTime = now + Mathf.Max(0.1f, encounter.monsterData.attackInterval);
                ResolveMonsterAttack();
                processedAction = true;

                if (profile.currentHp <= 0 || !autoCombatEnabled)
                {
                    break;
                }
            }

            if (!processedAction)
            {
                break;
            }
        }

        if (stateChanged)
        {
            NotifyStateChanged();
        }
    }

    public void ShowCombatPage()
    {
        if (cardViewManager != null)
        {
            cardViewManager.ShowScrollView(combatViewIndex);
        }

        pageController?.RefreshAll();
        NotifyStateChanged();
    }

    public void SelectMonster(MonsterData monsterData)
    {
        selectedMonster = monsterData;
        encounter.Clear();

        if (monsterData == null)
        {
            autoCombatEnabled = false;
            AddCombatLog("No monster selected.");
            NotifyStateChanged();
            return;
        }

        if (profile.currentHp <= 0)
        {
            autoCombatEnabled = false;
            AddCombatLog("Heal before entering combat.");
            NotifyStateChanged();
            return;
        }

        autoCombatEnabled = true;
        AddCombatLog($"Targeting {monsterData.displayName}.");
        SpawnEncounter(monsterData);
        NotifyStateChanged();
    }

    public void StopCombat()
    {
        if (!autoCombatEnabled && encounter.monsterData == null && !encounter.isRespawning)
        {
            return;
        }

        autoCombatEnabled = false;
        encounter.Clear();
        AddCombatLog("Combat stopped.");
        NotifyStateChanged();
    }

    public void SetCombatStyle(CombatStyle style)
    {
        if (profile.activeStyle == style)
        {
            return;
        }

        profile.activeStyle = style;
        AddCombatLog($"Style changed to {GetStyleName(style)}.");
        NotifyStateChanged();
    }

    public int GetMonsterKillCount(MonsterData monsterData)
    {
        if (monsterData == null || string.IsNullOrWhiteSpace(monsterData.guid))
        {
            return 0;
        }

        return monsterKillCounts.TryGetValue(monsterData.guid, out int count) ? count : 0;
    }

    public float GetPlayerAttackRemainingSeconds()
    {
        if (encounter.monsterData == null || encounter.isRespawning)
        {
            return 0f;
        }

        return Mathf.Max(0f, encounter.playerAttackReadyTime - Time.unscaledTime);
    }

    public float GetMonsterAttackRemainingSeconds()
    {
        if (encounter.monsterData == null || encounter.isRespawning)
        {
            return 0f;
        }

        return Mathf.Max(0f, encounter.monsterAttackReadyTime - Time.unscaledTime);
    }

    public float GetRespawnRemainingSeconds()
    {
        return encounter.isRespawning ? Mathf.Max(0f, encounter.respawnReadyTime - Time.unscaledTime) : 0f;
    }

    public float GetFoodCooldownRemainingSeconds()
    {
        return Mathf.Max(0f, profile.foodCooldownEndsAt - Time.unscaledTime);
    }

    public float GetPotionRemainingSeconds()
    {
        if (!IsPotionActive())
        {
            return 0f;
        }

        return Mathf.Max(0f, profile.potionExpiresAt - Time.unscaledTime);
    }

    public float GetDeathDebuffRemainingSeconds()
    {
        if (!IsDeathDebuffActive())
        {
            return 0f;
        }

        return Mathf.Max(0f, profile.deathDebuffExpiresAt - Time.unscaledTime);
    }

    public bool IsPotionActive()
    {
        return profile.activePotionItem != null && Time.unscaledTime < profile.potionExpiresAt;
    }

    public bool IsDeathDebuffActive()
    {
        return Time.unscaledTime < profile.deathDebuffExpiresAt;
    }

    public static float GetIdleItemDropChanceMultiplier()
    {
        return Instance != null && Instance.IsDeathDebuffActive() ? Instance.idleDropPenaltyMultiplier : 1f;
    }

    void AddCombatLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        combatLog.Add(message);
        while (combatLog.Count > MaxCombatLogEntries)
        {
            combatLog.RemoveAt(0);
        }
    }

    void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
