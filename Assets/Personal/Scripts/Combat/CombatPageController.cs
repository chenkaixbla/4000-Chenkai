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

    sealed class SlotButtonBinding
    {
        public CombatEquipSlot slot;
        public int utilityIndex;
        public ButtonRefs refs;
    }

    CombatManager manager;
    bool isBuilt;
    CombatEquipSlot selectedSlot = CombatEquipSlot.Weapon;
    int selectedUtilityIndex = -1;

    readonly Dictionary<MonsterData, MonsterSelectionButtonElement> monsterButtons = new();
    readonly List<SlotButtonBinding> slotButtons = new();

    [Title("UI - Monster List Spawning")]
    // Spawn setup for the monster list (content parent + runtime button element prefab).
    [SerializeField] CombatSpawnListReferences monsterListSpawn = new();

    [Title("UI - Item Selection Spawning")]
    // Spawn setup for the slot-item selection list (content parent + runtime button element prefab).
    [SerializeField] CombatSpawnListReferences selectionListSpawn = new();

    [Title("UI - Player")]
    // Label displaying current and max player HP.
    [SerializeField] TMP_Text playerHpText;

    // Filled image used as the player's HP bar.
    [SerializeField] Image playerHpFill;

    // Multiline combat stat summary for the player.
    [SerializeField] TMP_Text playerDerivedText;

    // Timers for attack cooldown and consumable cooldown.
    [SerializeField] TMP_Text playerTimerText;

    [Title("UI - Monster")]
    // Label for currently selected or active monster name.
    [SerializeField] TMP_Text monsterNameText;

    // Label displaying current and max monster HP.
    [SerializeField] TMP_Text monsterHpText;

    // Filled image used as the monster HP bar.
    [SerializeField] Image monsterHpFill;

    // Multiline monster stat summary.
    [SerializeField] TMP_Text monsterStatsText;

    // Timer label for monster attack or respawn countdown.
    [SerializeField] TMP_Text monsterTimerText;

    [Title("UI - Status And Log")]
    // Overall encounter state text (idle, combat, respawning, dead).
    [SerializeField] TMP_Text statusText;

    // Active timed effects text (potion/debuff states).
    [SerializeField] TMP_Text effectStatusText;

    // Scrolling combat log text output.
    [SerializeField] TMP_Text logText;

    // Header text above the right-hand selection list.
    [SerializeField] TMP_Text selectionTitleText;

    [Title("UI - Action Buttons")]
    // Uses equipped food when available.
    [SerializeField] Button foodButton;

    // Uses equipped potion when available.
    [SerializeField] Button potionButton;

    // Stops combat and clears current encounter state.
    [SerializeField] Button stopButton;

    [Title("UI - Slot Buttons")]
    // Slot button elements authored in the scene (each element knows its slot/index and UI refs).
    [SerializeField] List<CombatSlotButtonElement> slotButtonElements = new();

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
        RefreshSlotButtons();
        RebuildSelectionList();
        RefreshDynamicState();
    }

    void RefreshDynamicState()
    {
        if (manager == null)
        {
            return;
        }

        int playerMaxHp = manager.GetPlayerMaxHp();
        playerHpText.text = $"Player HP: {manager.Profile.currentHp} / {playerMaxHp}";
        playerHpFill.fillAmount = playerMaxHp > 0 ? (float)manager.Profile.currentHp / playerMaxHp : 0f;

        string blockedReason = manager.GetPlayerAttackBlockedReason();
        playerDerivedText.text =
            $"Accuracy: {manager.GetPlayerAccuracyRating():N0}\n" +
            $"Max Hit: {manager.GetPlayerMaxHit()}\n" +
            $"Evasion (M/R/M): {manager.GetPlayerEvasionRating(CombatAttackType.Melee):N0} / {manager.GetPlayerEvasionRating(CombatAttackType.Ranged):N0} / {manager.GetPlayerEvasionRating(CombatAttackType.Magic):N0}\n" +
            $"Damage Reduction: {manager.GetPlayerDamageReductionPercent()}%\n" +
            $"Attack Interval: {manager.GetPlayerAttackInterval():0.0}s\n" +
            $"Levels  HP {manager.Profile.hitpoints.currentLevel}  ATK {manager.Profile.attack.currentLevel}  STR {manager.Profile.strength.currentLevel}  DEF {manager.Profile.defence.currentLevel}  RNG {manager.Profile.range.currentLevel}" +
            (string.IsNullOrWhiteSpace(blockedReason) ? string.Empty : $"\nBlocked: {blockedReason}");

        playerTimerText.text = $"Player Attack In: {manager.GetPlayerAttackRemainingSeconds():0.0}s   Food CD: {manager.GetFoodCooldownRemainingSeconds():0.0}s";

        MonsterData displayedMonster = manager.DisplayedMonster;
        if (displayedMonster == null)
        {
            monsterNameText.text = "No Monster Selected";
            monsterHpText.text = "Monster HP: 0 / 0";
            monsterHpFill.fillAmount = 0f;
            monsterStatsText.text = "Select a monster to start combat.";
            monsterTimerText.text = string.Empty;
        }
        else
        {
            int currentMonsterHp = manager.Encounter.monsterData != null ? manager.Encounter.currentMonsterHp : 0;
            monsterNameText.text = displayedMonster.displayName;
            monsterHpText.text = $"Monster HP: {currentMonsterHp} / {displayedMonster.maxHp}";
            monsterHpFill.fillAmount = displayedMonster.maxHp > 0 ? (float)currentMonsterHp / displayedMonster.maxHp : 0f;
            monsterStatsText.text =
                $"Attack Type: {displayedMonster.attackType}\n" +
                $"Accuracy: {displayedMonster.attackAccuracy:N0}\n" +
                $"Max Hit: {displayedMonster.maxHit}\n" +
                $"Evasion (M/R/M): {displayedMonster.meleeEvasion:N0} / {displayedMonster.rangedEvasion:N0} / {displayedMonster.magicEvasion:N0}\n" +
                $"Damage Reduction: {displayedMonster.damageReductionPercent}%\n" +
                $"First 10 Bonus Defeats: {Mathf.Clamp(10 - manager.GetMonsterKillCount(displayedMonster), 0, 10)} remaining";

            monsterTimerText.text = manager.IsRespawning
                ? $"Respawn In: {manager.GetRespawnRemainingSeconds():0.0}s"
                : $"Monster Attack In: {manager.GetMonsterAttackRemainingSeconds():0.0}s";
        }

        if (manager.Profile.currentHp <= 0)
        {
            statusText.text = "Status: Dead";
        }
        else if (manager.IsRespawning)
        {
            statusText.text = "Status: Waiting for respawn";
        }
        else if (manager.IsCombatRunning)
        {
            statusText.text = "Status: In combat";
        }
        else
        {
            statusText.text = "Status: Idle";
        }

        effectStatusText.text =
            $"Potion: {(manager.IsPotionActive() ? $"{manager.Profile.activePotionItem.displayName} ({manager.GetPotionRemainingSeconds():0.0}s)" : "Inactive")}\n" +
            $"Death Debuff: {(manager.IsDeathDebuffActive() ? $"{manager.GetDeathDebuffRemainingSeconds():0.0}s remaining" : "Inactive")}";

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

        IReadOnlyList<string> combatLog = manager.CombatLog;
        logText.text = combatLog.Count > 0 ? string.Join("\n", combatLog) : "Combat log is empty.";
    }

    void RebuildMonsterList()
    {
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
            MonsterSelectionButtonElement buttonElement = CreateSelectionButton(monsterListSpawn, string.Empty, () => manager.SelectMonster(capturedMonster), 58f);
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

    void RefreshSlotButtons()
    {
        for (int i = 0; i < slotButtons.Count; i++)
        {
            SlotButtonBinding binding = slotButtons[i];
            ItemsData equippedItem = manager.GetEquippedItem(binding.slot, binding.utilityIndex);
            int quantity = equippedItem != null && manager.inventory != null ? manager.inventory.GetQuantity(equippedItem) : 0;
            string itemLabel = equippedItem != null ? $"{equippedItem.displayName} x{quantity}" : "Empty";

            if (binding.refs.label != null)
            {
                binding.refs.label.text = $"{CombatManager.GetSlotDisplayName(binding.slot, binding.utilityIndex)}\n{itemLabel}";
            }

            bool isSelected = selectedSlot == binding.slot && selectedUtilityIndex == binding.utilityIndex;
            if (binding.refs.background != null)
            {
                binding.refs.background.color = isSelected ? new Color(0.42f, 0.30f, 0.14f, 0.95f) : new Color(0.16f, 0.16f, 0.16f, 0.95f);
            }
        }
    }

    void RebuildSelectionList()
    {
        ClearChildren(selectionListSpawn.Content);
        selectionTitleText.text = $"Selection: {CombatManager.GetSlotDisplayName(selectedSlot, selectedUtilityIndex)}";

        if (selectedSlot == CombatEquipSlot.None)
        {
            CreateSelectionInfoButton(selectionListSpawn, "Select a slot to equip items.", 54f);
            return;
        }

        CreateSelectionButton(selectionListSpawn, "Unequip", () => manager.EquipItem(selectedSlot, null, selectedUtilityIndex), 54f);

        List<ItemsInstance> candidates = manager.GetCompatibleItemsForSlot(selectedSlot, selectedUtilityIndex);
        if (candidates.Count == 0)
        {
            CreateSelectionInfoButton(selectionListSpawn, "No compatible inventory items.", 54f);
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
            CreateSelectionButton(
                selectionListSpawn,
                $"{capturedItem.displayName} x{capturedQuantity}",
                () => manager.EquipItem(selectedSlot, capturedItem, selectedUtilityIndex),
                54f);
        }
    }

}
