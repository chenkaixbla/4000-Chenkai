using System.Collections.Generic;
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

    sealed class StyleButtonBinding
    {
        public CombatStyle style;
        public ButtonRefs refs;
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

    readonly Dictionary<MonsterData, ButtonRefs> monsterButtons = new();
    readonly List<StyleButtonBinding> styleButtons = new();
    readonly List<SlotButtonBinding> slotButtons = new();

    RectTransform rootRect;
    RectTransform monsterListContent;
    RectTransform selectionListContent;
    TMP_Text playerHpText;
    Image playerHpFill;
    TMP_Text playerDerivedText;
    TMP_Text playerTimerText;
    TMP_Text monsterNameText;
    TMP_Text monsterHpText;
    Image monsterHpFill;
    TMP_Text monsterStatsText;
    TMP_Text monsterTimerText;
    TMP_Text statusText;
    TMP_Text effectStatusText;
    TMP_Text logText;
    TMP_Text selectionTitleText;
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
        RefreshStyleButtons();
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
            $"Style: {GetStyleDisplayName(manager.Profile.activeStyle)}\n" +
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

        foodButtonRefs.button.interactable = manager.CanUseFood();
        potionButtonRefs.button.interactable = manager.CanUsePotion();
        stopButtonRefs.button.interactable = manager.IsCombatRunning || manager.IsRespawning;

        IReadOnlyList<string> combatLog = manager.CombatLog;
        logText.text = combatLog.Count > 0 ? string.Join("\n", combatLog) : "Combat log is empty.";
    }

    void RebuildMonsterList()
    {
        ClearChildren(monsterListContent);
        monsterButtons.Clear();

        if (manager.MonsterDatas == null || manager.MonsterDatas.Count == 0)
        {
            CreateLabel(monsterListContent, "No monsters assigned.", 22, TextAlignmentOptions.Center);
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
            ButtonRefs refs = CreateButton(monsterListContent, string.Empty, () => manager.SelectMonster(capturedMonster), 58);
            refs.label.alignment = TextAlignmentOptions.MidlineLeft;
            refs.label.margin = new Vector4(16f, 0f, 16f, 0f);
            monsterButtons[capturedMonster] = refs;
        }

        RefreshMonsterButtonStates();
    }

    void RefreshMonsterButtonStates()
    {
        foreach (KeyValuePair<MonsterData, ButtonRefs> entry in monsterButtons)
        {
            MonsterData monsterData = entry.Key;
            ButtonRefs refs = entry.Value;
            bool isSelected = manager.SelectedMonster == monsterData;
            int defeats = manager.GetMonsterKillCount(monsterData);
            refs.label.text = $"{monsterData.displayName}\nDefeats: {defeats}";
            refs.background.color = isSelected ? new Color(0.32f, 0.42f, 0.18f, 0.95f) : new Color(0.15f, 0.15f, 0.18f, 0.95f);
        }
    }

    void RefreshStyleButtons()
    {
        for (int i = 0; i < styleButtons.Count; i++)
        {
            StyleButtonBinding binding = styleButtons[i];
            bool isActive = manager.Profile.activeStyle == binding.style;
            binding.refs.background.color = isActive ? new Color(0.18f, 0.42f, 0.52f, 0.95f) : new Color(0.16f, 0.16f, 0.16f, 0.95f);
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
            binding.refs.label.text = $"{CombatManager.GetSlotDisplayName(binding.slot, binding.utilityIndex)}\n{itemLabel}";

            bool isSelected = selectedSlot == binding.slot && selectedUtilityIndex == binding.utilityIndex;
            binding.refs.background.color = isSelected ? new Color(0.42f, 0.30f, 0.14f, 0.95f) : new Color(0.16f, 0.16f, 0.16f, 0.95f);
        }
    }

    void RebuildSelectionList()
    {
        ClearChildren(selectionListContent);
        selectionTitleText.text = $"Selection: {CombatManager.GetSlotDisplayName(selectedSlot, selectedUtilityIndex)}";

        if (selectedSlot == CombatEquipSlot.None)
        {
            CreateLabel(selectionListContent, "Select a slot to equip items.", 22, TextAlignmentOptions.Center);
            return;
        }

        CreateButton(selectionListContent, "Unequip", () => manager.EquipItem(selectedSlot, null, selectedUtilityIndex), 54);

        List<ItemsInstance> candidates = manager.GetCompatibleItemsForSlot(selectedSlot, selectedUtilityIndex);
        if (candidates.Count == 0)
        {
            CreateLabel(selectionListContent, "No compatible inventory items.", 22, TextAlignmentOptions.Center);
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
            CreateButton(
                selectionListContent,
                $"{capturedItem.displayName} x{capturedQuantity}",
                () => manager.EquipItem(selectedSlot, capturedItem, selectedUtilityIndex),
                54);
        }
    }

    static string GetStyleDisplayName(CombatStyle style)
    {
        return style switch
        {
            CombatStyle.MeleeAccurate => "Melee Accurate",
            CombatStyle.MeleeAggressive => "Melee Aggressive",
            CombatStyle.MeleeDefensive => "Melee Defensive",
            CombatStyle.Ranged => "Ranged",
            _ => style.ToString()
        };
    }
}
