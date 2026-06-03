using System.Collections.Generic;
using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class CombatPageController : MonoBehaviour
{
    sealed class ButtonRefs
    {
        public Button button;
        public Image background;
        public TMP_Text label;
    }

    [System.Serializable]
    sealed class PlayerStatsDisplayGroup
    {
        public StatsDisplayText accuracy;
        public StatsDisplayText maxHit;
        public StatsDisplayText evasion;
        public StatsDisplayText damageReduction;
        public StatsDisplayText attackInterval;
        public StatsDisplayText levels;
        public StatsDisplayText blocked;
    }

    [System.Serializable]
    sealed class MonsterStatsDisplayGroup
    {
        public StatsDisplayText attackType;
        public StatsDisplayText accuracy;
        public StatsDisplayText maxHit;
        public StatsDisplayText evasion;
        public StatsDisplayText damageReduction;
        public StatsDisplayText firstTenBonusDefeats;
    }

    enum MonsterAttributeValueType
    {
        AttackType,
        Accuracy,
        MaxHit,
        DamageReductionPercent,
        MeleeEvasion,
        RangedEvasion,
        MagicEvasion,
        EvasionSummary,
        FirstTenBonusDefeats
    }

    [System.Serializable]
    sealed class MonsterAttributeDisplaySetting
    {
        // Target UI row this setting writes to.
        public Image_Text slot;

        // Which monster value is rendered in this row.
        public MonsterAttributeValueType valueType = MonsterAttributeValueType.AttackType;

        // Optional label shown before the value. Leave empty to render value only.
        public string labelPrefix = string.Empty;

        // Optional icon rendered in the row.
        public Sprite icon;
    }

    CombatManager manager;
    bool isBuilt;

    readonly Dictionary<MonsterData, MonsterSelectionButtonElement> monsterButtons = new();
    readonly List<ItemSlot> allLoadoutSlots = new();

    CombatEquipSlot selectedSlot = CombatEquipSlot.None;
    int selectedSlotIndex = -1;
    ItemSlot activeSelectionSourceSlot;

    [Title("UI - Monster Switcher")]
    // Button that toggles the monster switcher panel.
    [SerializeField] Button monsterSwitcherButton;

    // Toggleable root panel containing monster switcher list content.
    [SerializeField] GameObject monsterSwitcherPanelRoot;

    // Spawn setup for monster switcher entries.
    [SerializeField] CombatSpawnListReferences monsterListSpawn = new();

    [Title("UI - Monster Panel")]
    // Name text for currently selected/active monster.
    [SerializeField] TMP_Text monsterNameText;

    // Description text for currently selected/active monster.
    [SerializeField] TMP_Text monsterDescriptionText;

    // Unified monster attribute row settings (slot reference + value + label + icon).
    [SerializeField] List<MonsterAttributeDisplaySetting> monsterAttributeSettings = new()
    {
        new() { valueType = MonsterAttributeValueType.AttackType, labelPrefix = "Attack Type" },
        new() { valueType = MonsterAttributeValueType.Accuracy, labelPrefix = "Accuracy" },
        new() { valueType = MonsterAttributeValueType.MaxHit, labelPrefix = "Max Hit" },
        new() { valueType = MonsterAttributeValueType.FirstTenBonusDefeats, labelPrefix = "First 10 Bonus" }
    };

    // Attack/respawn timer fill for monster panel.
    [SerializeField] Image monsterAttackTimerFill;

    // Attack/respawn timer text for monster panel.
    [SerializeField] TMP_Text monsterAttackTimerText;

    // Health text for monster panel.
    [SerializeField] TMP_Text monsterHpText;

    // Health fill image for monster panel.
    [SerializeField] Image monsterHpFill;


    [Title("UI - Player Panel")]
    // Health text for player panel.
    [SerializeField] TMP_Text playerHpText;

    // Health fill image for player panel.
    [SerializeField] Image playerHpFill;

    // Player attack timer fill image.
    [SerializeField] Image playerAttackTimerFill;

    // Player attack timer text.
    [SerializeField] TMP_Text playerAttackTimerText;

    // Food item slot with optional auto-use toggle.
    [SerializeField] ItemSlot foodSlot;

    // Potion item slots (expected: 3) with optional auto-use toggles.
    [SerializeField] List<ItemSlot> potionSlots = new();


    [Title("UI - Equipment Panel")]
    // Equipment slots (weapon/offhand/armor/accessories) represented as ItemSlot components.
    [SerializeField] List<ItemSlot> equipmentSlots = new();

    [Title("UI - Item Selection Panel")]
    // Root object for item candidate selection panel.
    [SerializeField] GameObject itemSelectionPanelRoot;

    // Optional dedicated close button for item selection panel.
    [SerializeField] Button itemSelectionCloseButton;

    // Header text above candidate list.
    [SerializeField] TMP_Text selectionTitleText;

    // Spawn setup for candidate ItemSlot rows.
    [SerializeField] ItemSlotSpawnReferences itemSelectionSpawn = new();

    // Per-controller toggle for verbose diagnostic logs.
    [SerializeField] bool enableVerboseLogging = true;

    [Title("UI - Stats")]
    // Explicitly named player stats rows.
    [SerializeField] PlayerStatsDisplayGroup playerStats = new();

    // Explicitly named monster stats rows.
    [SerializeField] MonsterStatsDisplayGroup monsterStats = new();

    [Title("UI - Status And Log")]
    // Current encounter status text.
    [SerializeField] TMP_Text statusText;

    // Active effect/debuff summary text.
    [SerializeField] TMP_Text effectStatusText;

    // Combat log output text.
    [SerializeField] TMP_Text logText;

    [Title("UI - Manual Action Buttons")]
    // Manual food-use button.
    [SerializeField] Button foodButton;

    // Manual potion-use button.
    [SerializeField] Button potionButton;

    // Manual stop-combat button.
    [SerializeField] Button stopButton;

    ButtonRefs foodButtonRefs;
    ButtonRefs potionButtonRefs;
    ButtonRefs stopButtonRefs;

    public void Initialize(CombatManager combatManager)
    {
        if (manager == combatManager && isBuilt)
        {
            RefreshAll();
            return;
        }

        if (manager != null)
        {
            manager.StateChanged -= HandleStateChanged;
        }

        manager = combatManager;
        if (manager != null)
        {
            manager.StateChanged += HandleStateChanged;
        }

        EnsureBuilt();
        RefreshAll();
    }

    void OnDestroy()
    {
        if (manager != null)
        {
            manager.StateChanged -= HandleStateChanged;
        }

        UnbindSelectionCloseButton();

        UnbindLoadoutSlots();
    }

    void Update()
    {
        if (!isBuilt || !gameObject.activeInHierarchy || manager == null)
        {
            return;
        }

        RefreshDynamicState();
    }

    void HandleStateChanged()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        RefreshAll();
    }

    public void RefreshAll()
    {
        if (!isBuilt || manager == null)
        {
            return;
        }

        RebuildMonsterList();
        RefreshItemSlots();
        // Keep selection list rendering strictly tied to active panel context.
        RefreshSelectionListIfOpen();
        RefreshDynamicState();
    }

    void RefreshSelectionListIfOpen()
    {
        if (itemSelectionPanelRoot == null || !itemSelectionPanelRoot.activeSelf)
        {
            // Panel is not open, so force list to remain empty.
            ClearSelectionList();
            return;
        }

        RebuildSelectionList();
    }

    void ClearSelectionList()
    {
        if (itemSelectionSpawn?.Content == null)
        {
            return;
        }

        ClearChildren(itemSelectionSpawn.Content);

        if (selectionTitleText != null)
        {
            selectionTitleText.text = "Selection";
        }
    }

    void RefreshDynamicState()
    {
        if (manager == null)
        {
            return;
        }

        int playerMaxHp = manager.GetPlayerMaxHp();
        if (playerHpText != null)
        {
            playerHpText.text = $"Player HP: {manager.Profile.currentHp} / {playerMaxHp}";
        }

        if (playerHpFill != null)
        {
            playerHpFill.fillAmount = playerMaxHp > 0 ? (float)manager.Profile.currentHp / playerMaxHp : 0f;
        }

        float playerAttackRemaining = manager.GetPlayerAttackRemainingSeconds();
        float playerAttackInterval = Mathf.Max(0.001f, manager.GetPlayerAttackInterval());
        float playerAttackProgress = 1f - Mathf.Clamp01(playerAttackRemaining / playerAttackInterval);
        if (playerAttackTimerFill != null)
        {
            playerAttackTimerFill.fillAmount = playerAttackProgress;
        }

        if (playerAttackTimerText != null)
        {
            playerAttackTimerText.text = $"Attack: {playerAttackRemaining:0.0}s";
        }

        string blockedReason = manager.GetPlayerAttackBlockedReason();
        SetStatsEntry(playerStats?.accuracy, "Accuracy", manager.GetPlayerAccuracyRating().ToString("N0"));
        SetStatsEntry(playerStats?.maxHit, "Max Hit", manager.GetPlayerMaxHit().ToString());
        SetStatsEntry(
            playerStats?.evasion,
            "Evasion",
            $"{manager.GetPlayerEvasionRating(CombatAttackType.Melee):N0} / {manager.GetPlayerEvasionRating(CombatAttackType.Ranged):N0} / {manager.GetPlayerEvasionRating(CombatAttackType.Magic):N0}");
        SetStatsEntry(playerStats?.damageReduction, "Damage Reduction", $"{manager.GetPlayerDamageReductionPercent()}%");
        SetStatsEntry(playerStats?.attackInterval, "Attack Interval", $"{manager.GetPlayerAttackInterval():0.0}s");
        SetStatsEntry(
            playerStats?.levels,
            "Levels",
            $"HP {manager.Profile.hitpoints.currentLevel}  ATK {manager.Profile.attack.currentLevel}  STR {manager.Profile.strength.currentLevel}  DEF {manager.Profile.defence.currentLevel}  RNG {manager.Profile.range.currentLevel}");
        SetStatsEntry(playerStats?.blocked, "Blocked", string.IsNullOrWhiteSpace(blockedReason) ? string.Empty : blockedReason);

        MonsterData displayedMonster = manager.DisplayedMonster;
        if (displayedMonster == null)
        {
            if (monsterNameText != null)
            {
                monsterNameText.text = "No Monster Selected";
            }

            if (monsterDescriptionText != null)
            {
                monsterDescriptionText.text = string.Empty;
            }

            if (monsterHpText != null)
            {
                monsterHpText.text = "Monster HP: 0 / 0";
            }

            if (monsterHpFill != null)
            {
                monsterHpFill.fillAmount = 0f;
            }

            if (monsterAttackTimerFill != null)
            {
                monsterAttackTimerFill.fillAmount = 0f;
            }

            if (monsterAttackTimerText != null)
            {
                monsterAttackTimerText.text = string.Empty;
            }

            ClearMonsterStatsDisplay();
            ClearMonsterAttributeSlots();
        }
        else
        {
            int currentMonsterHp = manager.Encounter.monsterData != null ? manager.Encounter.currentMonsterHp : 0;
            if (monsterNameText != null)
            {
                monsterNameText.text = displayedMonster.displayName;
            }

            if (monsterDescriptionText != null)
            {
                monsterDescriptionText.text = displayedMonster.description ?? string.Empty;
            }

            if (monsterHpText != null)
            {
                monsterHpText.text = $"Monster HP: {currentMonsterHp} / {displayedMonster.maxHp}";
            }

            if (monsterHpFill != null)
            {
                monsterHpFill.fillAmount = displayedMonster.maxHp > 0 ? (float)currentMonsterHp / displayedMonster.maxHp : 0f;
            }

            bool isRespawning = manager.IsRespawning;
            float timerRemaining = isRespawning ? manager.GetRespawnRemainingSeconds() : manager.GetMonsterAttackRemainingSeconds();
            float timerDuration = isRespawning ? Mathf.Max(0.001f, manager.monsterRespawnDelay) : Mathf.Max(0.001f, displayedMonster.attackInterval);
            float timerProgress = 1f - Mathf.Clamp01(timerRemaining / timerDuration);

            if (monsterAttackTimerFill != null)
            {
                monsterAttackTimerFill.fillAmount = timerProgress;
            }

            if (monsterAttackTimerText != null)
            {
                monsterAttackTimerText.text = isRespawning
                    ? $"Respawn: {timerRemaining:0.0}s"
                    : $"Attack: {timerRemaining:0.0}s";
            }

            int firstTenRemaining = Mathf.Clamp(10 - manager.GetMonsterKillCount(displayedMonster), 0, 10);
            SetStatsEntry(monsterStats?.attackType, "Attack Type", displayedMonster.attackType.ToString());
            SetStatsEntry(monsterStats?.accuracy, "Accuracy", displayedMonster.attackAccuracy.ToString("N0"));
            SetStatsEntry(monsterStats?.maxHit, "Max Hit", displayedMonster.maxHit.ToString());
            SetStatsEntry(
                monsterStats?.evasion,
                "Evasion",
                $"{displayedMonster.meleeEvasion:N0} / {displayedMonster.rangedEvasion:N0} / {displayedMonster.magicEvasion:N0}");
            SetStatsEntry(monsterStats?.damageReduction, "Damage Reduction", $"{displayedMonster.damageReductionPercent}%");
            SetStatsEntry(monsterStats?.firstTenBonusDefeats, "First 10 Bonus Defeats", $"{firstTenRemaining} remaining");

            UpdateMonsterAttributeSlots(displayedMonster, firstTenRemaining);
        }

        if (manager.Profile.currentHp <= 0)
        {
            if (statusText != null)
            {
                statusText.text = "Status: Dead";
            }
        }
        else if (manager.IsRespawning)
        {
            if (statusText != null)
            {
                statusText.text = "Status: Waiting for respawn";
            }
        }
        else if (manager.IsCombatRunning)
        {
            if (statusText != null)
            {
                statusText.text = "Status: In combat";
            }
        }
        else if (statusText != null)
        {
            statusText.text = "Status: Idle";
        }

        if (effectStatusText != null)
        {
            effectStatusText.text =
                $"Potion Effects: {manager.GetActivePotionSummary()}\n" +
                $"Death Debuff: {(manager.IsDeathDebuffActive() ? $"{manager.GetDeathDebuffRemainingSeconds():0.0}s remaining" : "Inactive")}";
        }

        if (foodButtonRefs?.button != null)
        {
            foodButtonRefs.button.interactable = manager.CanUseFood();
        }

        if (potionButtonRefs?.button != null)
        {
            potionButtonRefs.button.interactable = manager.CanUsePotion();
        }

        if (stopButtonRefs?.button != null)
        {
            stopButtonRefs.button.interactable = manager.IsCombatRunning || manager.IsRespawning;
        }

        if (logText != null)
        {
            IReadOnlyList<string> combatLog = manager.CombatLog;
            logText.text = combatLog.Count > 0 ? string.Join("\n", combatLog) : "Combat log is empty.";
        }
    }

    void RebuildMonsterList()
    {
        if (monsterListSpawn?.Content == null)
        {
            return;
        }

        ClearChildren(monsterListSpawn.Content);
        monsterButtons.Clear();

        if (manager.MonsterDatas == null || manager.MonsterDatas.Count == 0)
        {
            CreateSelectionInfoButton(monsterListSpawn, "No monsters assigned.", 58f);
            return;
        }

        for (int i = 0; i < manager.MonsterDatas.Count; i++)
        {
            MonsterData monsterData = manager.MonsterDatas[i];
            if (monsterData == null)
            {
                continue;
            }

            MonsterData capturedMonster = monsterData;
            MonsterSelectionButtonElement buttonElement = CreateSelectionButton(monsterListSpawn, string.Empty, () => HandleMonsterSelected(capturedMonster), 58f);
            if (buttonElement == null)
            {
                continue;
            }

            buttonElement.SetLabelAlignment(TextAlignmentOptions.MidlineLeft);
            buttonElement.SetLabelMargin(new Vector4(16f, 0f, 16f, 0f));
            monsterButtons[capturedMonster] = buttonElement;
        }

        RefreshMonsterButtonStates();
    }

    void HandleMonsterSelected(MonsterData monsterData)
    {
        manager.SelectMonster(monsterData);
        activeSelectionSourceSlot = null;
        if (monsterSwitcherPanelRoot != null)
        {
            monsterSwitcherPanelRoot.SetActive(false);
        }
    }

    void RefreshMonsterButtonStates()
    {
        foreach (KeyValuePair<MonsterData, MonsterSelectionButtonElement> entry in monsterButtons)
        {
            MonsterData monsterData = entry.Key;
            MonsterSelectionButtonElement buttonElement = entry.Value;
            bool isSelected = manager.SelectedMonster == monsterData;
            int defeats = manager.GetMonsterKillCount(monsterData);

            buttonElement.SetLabel($"{monsterData.displayName}\nDefeats: {defeats}");
            buttonElement.SetBackgroundColor(isSelected ? new Color(0.32f, 0.42f, 0.18f, 0.95f) : new Color(0.15f, 0.15f, 0.18f, 0.95f));
        }
    }

    void RefreshItemSlots()
    {
        for (int i = 0; i < allLoadoutSlots.Count; i++)
        {
            ItemSlot slot = allLoadoutSlots[i];
            if (slot == null)
            {
                continue;
            }

            ItemsData equippedItem = manager.GetEquippedItem(slot.Slot, slot.SlotIndex);
            int quantity = equippedItem != null && manager.inventory != null ? manager.inventory.GetQuantity(equippedItem) : 0;
            string slotLabel = CombatManager.GetSlotDisplayName(slot.Slot, slot.SlotIndex);
            string itemLabel = equippedItem != null ? $"{equippedItem.displayName} x{quantity}" : "Empty";
            slot.BindItem(equippedItem, quantity, $"{slotLabel}\n{itemLabel}");

            if (slot.Slot == CombatEquipSlot.Food)
            {
                slot.SetAutoToggleVisible(true);
                slot.SetAutoToggleValue(manager.GetFoodAutoUseEnabled());
            }
            else if (slot.Slot == CombatEquipSlot.Potion)
            {
                slot.SetAutoToggleVisible(true);
                slot.SetAutoToggleValue(manager.GetPotionAutoUseEnabled(slot.SlotIndex));
            }
            else
            {
                slot.SetAutoToggleVisible(false);
            }
        }
    }

    void HandleItemSlotClicked(ItemSlot slot)
    {
        if (slot == null)
        {
            LogVerboseWarning("HandleItemSlotClicked received null slot.");
            return;
        }

        bool panelIsOpen = itemSelectionPanelRoot != null && itemSelectionPanelRoot.activeSelf;
        bool isSameSelectionSource = panelIsOpen &&
            activeSelectionSourceSlot == slot &&
            selectedSlot == slot.Slot &&
            selectedSlotIndex == slot.SlotIndex;

        // Clicking the same source slot again acts as a close toggle for the panel.
        if (isSameSelectionSource)
        {
            LogVerbose($"ItemSlot clicked again for active selection source. Closing panel for slot {slot.Slot} ({slot.SlotIndex}).");
            CloseSelectionPanel();
            return;
        }

        LogVerbose(
            $"ItemSlot clicked. uiName: {slot.gameObject.name}, slot: {slot.Slot}, slotIndex: {slot.SlotIndex}, boundItem: {slot.BoundItemData?.displayName ?? "None"}, boundQuantity: {slot.BoundQuantity}.");

        selectedSlot = slot.Slot;
        selectedSlotIndex = slot.SlotIndex;
        activeSelectionSourceSlot = slot;

        if (itemSelectionPanelRoot != null)
        {
            itemSelectionPanelRoot.SetActive(true);
            LogVerbose("Item selection panel opened from slot click.");
        }
        else
        {
            LogVerboseError("Cannot open item selection panel because itemSelectionPanelRoot is null.");
        }

        RebuildSelectionList();
    }

    void HandleItemSlotToggleChanged(ItemSlot slot, bool enabled)
    {
        if (slot == null || manager == null)
        {
            return;
        }

        if (slot.Slot == CombatEquipSlot.Food)
        {
            manager.SetFoodAutoUseEnabled(enabled);
            return;
        }

        if (slot.Slot == CombatEquipSlot.Potion)
        {
            manager.SetPotionAutoUseEnabled(slot.SlotIndex, enabled);
        }
    }

    void RebuildSelectionList()
    {
        if (itemSelectionSpawn?.Content == null)
        {
            LogVerboseWarning("RebuildSelectionList skipped because selection spawn content is missing.");
            return;
        }

        LogVerbose(
            $"RebuildSelectionList start. panelActive: {itemSelectionPanelRoot != null && itemSelectionPanelRoot.activeSelf}, selectedSlot: {selectedSlot}, selectedSlotIndex: {selectedSlotIndex}, activeSourceSlot: {activeSelectionSourceSlot?.gameObject.name ?? "None"}.");

        ClearChildren(itemSelectionSpawn.Content);

        bool panelIsOpen = itemSelectionPanelRoot != null && itemSelectionPanelRoot.activeSelf;
        if (selectionTitleText != null)
        {
            selectionTitleText.text = !panelIsOpen || activeSelectionSourceSlot == null
                ? "Selection"
                : $"Selection: {CombatManager.GetSlotDisplayName(selectedSlot, selectedSlotIndex)}";

            LogVerbose($"Selection title updated: {selectionTitleText.text}");
        }

        // The selection panel should only render concrete candidate item rows.
        // If no slot is actively selected, keep the panel list empty.
        if (!panelIsOpen || activeSelectionSourceSlot == null || selectedSlot == CombatEquipSlot.None)
        {
            LogVerbose("No active source slot while rebuilding selection list. Leaving candidate list empty.");
            return;
        }

        List<ItemsInstance> candidates = manager.GetCompatibleItemsForSlot(selectedSlot, selectedSlotIndex);
        LogVerbose(
            $"Compatible candidate query finished. slot: {selectedSlot}, slotIndex: {selectedSlotIndex}, candidateCount: {candidates.Count}.");

        if (candidates.Count == 0)
        {
            manager.LogItemSelectionCompatibilityBreakdown(selectedSlot, selectedSlotIndex);
            LogVerboseWarning("Selection list ended with no compatible inventory item rows. Candidate list remains empty.");
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            ItemsInstance candidate = candidates[i];
            if (candidate == null || candidate.itemData == null)
            {
                continue;
            }

            ItemsData capturedItem = candidate.itemData;
            int capturedQuantity = candidate.quantity;
            ItemSlot candidateSlot = CreateSelectionItemSlot();
            if (candidateSlot == null)
            {
                LogVerboseError($"Failed to create candidate row for {capturedItem.displayName}.");
                continue;
            }

            string label = $"{capturedItem.displayName} x{capturedQuantity}";
            LogVerbose($"Adding candidate row: {label}.");
            candidateSlot.BindItem(capturedItem, capturedQuantity, label);
            candidateSlot.SetCandidateSelectAction(() =>
            {
                LogVerbose($"Candidate selected: {capturedItem.displayName} for slot {selectedSlot} ({selectedSlotIndex}).");
                manager.EquipItem(selectedSlot, capturedItem, selectedSlotIndex);
                CloseSelectionPanel();
            });
            candidateSlot.SetAutoToggleVisible(false);
        }

        LogVerbose($"RebuildSelectionList complete. Spawned candidate rows: {candidates.Count}. Content child count: {itemSelectionSpawn.Content.childCount}.");
    }

    void CloseSelectionPanel()
    {
        LogVerbose(
            $"Closing selection panel. Previous selectedSlot: {selectedSlot}, selectedSlotIndex: {selectedSlotIndex}, activeSourceSlot: {activeSelectionSourceSlot?.gameObject.name ?? "None"}.");

        activeSelectionSourceSlot = null;
        selectedSlot = CombatEquipSlot.None;
        selectedSlotIndex = -1;

        if (itemSelectionPanelRoot != null)
        {
            itemSelectionPanelRoot.SetActive(false);
        }

        ClearSelectionList();
        RefreshItemSlots();
    }

    void LogVerbose(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.Log("CombatPageController", message);
    }

    void LogVerboseWarning(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.LogWarning("CombatPageController", message);
    }

    void LogVerboseError(string message)
    {
        if (!enableVerboseLogging)
        {
            return;
        }

        VerboseProjectLogger.LogError("CombatPageController", message);
    }

    void ToggleMonsterSwitcherPanel()
    {
        if (monsterSwitcherPanelRoot == null)
        {
            return;
        }

        bool nextState = !monsterSwitcherPanelRoot.activeSelf;
        monsterSwitcherPanelRoot.SetActive(nextState);
        if (nextState)
        {
            RebuildMonsterList();
        }
    }

    void UpdateMonsterAttributeSlots(MonsterData monsterData, int firstTenRemaining)
    {
        if (monsterAttributeSettings == null || monsterAttributeSettings.Count == 0)
        {
            return;
        }

        for (int i = 0; i < monsterAttributeSettings.Count; i++)
        {
            MonsterAttributeDisplaySetting rowSetting = monsterAttributeSettings[i];
            if (rowSetting == null || rowSetting.slot == null)
            {
                continue;
            }

            string valueText = GetMonsterAttributeValueText(monsterData, firstTenRemaining, rowSetting.valueType);
            rowSetting.slot.Set(rowSetting.icon, BuildMonsterAttributeRowText(rowSetting, valueText));
        }
    }

    string BuildMonsterAttributeRowText(MonsterAttributeDisplaySetting rowSetting, string valueText)
    {
        string label = rowSetting != null && !string.IsNullOrWhiteSpace(rowSetting.labelPrefix)
            ? rowSetting.labelPrefix.Trim()
            : string.Empty;

        if (string.IsNullOrWhiteSpace(label))
        {
            return valueText ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(valueText))
        {
            return label;
        }

        return $"{label}: {valueText}";
    }

    string GetMonsterAttributeValueText(MonsterData monsterData, int firstTenRemaining, MonsterAttributeValueType valueType)
    {
        if (monsterData == null)
        {
            return string.Empty;
        }

        return valueType switch
        {
            MonsterAttributeValueType.AttackType => monsterData.attackType.ToString(),
            MonsterAttributeValueType.Accuracy => monsterData.attackAccuracy.ToString("N0"),
            MonsterAttributeValueType.MaxHit => monsterData.maxHit.ToString(),
            MonsterAttributeValueType.DamageReductionPercent => $"{monsterData.damageReductionPercent}%",
            MonsterAttributeValueType.MeleeEvasion => monsterData.meleeEvasion.ToString("N0"),
            MonsterAttributeValueType.RangedEvasion => monsterData.rangedEvasion.ToString("N0"),
            MonsterAttributeValueType.MagicEvasion => monsterData.magicEvasion.ToString("N0"),
            MonsterAttributeValueType.EvasionSummary => $"{monsterData.meleeEvasion:N0} / {monsterData.rangedEvasion:N0} / {monsterData.magicEvasion:N0}",
            MonsterAttributeValueType.FirstTenBonusDefeats => firstTenRemaining.ToString(),
            _ => string.Empty
        };
    }

    void ClearMonsterAttributeSlots()
    {
        if (monsterAttributeSettings == null)
        {
            return;
        }

        for (int i = 0; i < monsterAttributeSettings.Count; i++)
        {
            MonsterAttributeDisplaySetting rowSetting = monsterAttributeSettings[i];
            if (rowSetting == null || rowSetting.slot == null)
            {
                continue;
            }

            rowSetting.slot.Set(null, string.Empty);
        }
    }

    void SetStatsEntry(StatsDisplayText display, string label, string value)
    {
        if (display == null)
        {
            return;
        }

        display.Set(label, value);
        display.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
    }

    void ClearMonsterStatsDisplay()
    {
        SetStatsEntry(monsterStats?.attackType, "Attack Type", string.Empty);
        SetStatsEntry(monsterStats?.accuracy, "Accuracy", string.Empty);
        SetStatsEntry(monsterStats?.maxHit, "Max Hit", string.Empty);
        SetStatsEntry(monsterStats?.evasion, "Evasion", string.Empty);
        SetStatsEntry(monsterStats?.damageReduction, "Damage Reduction", string.Empty);
        SetStatsEntry(monsterStats?.firstTenBonusDefeats, "First 10 Bonus Defeats", string.Empty);
    }
}
