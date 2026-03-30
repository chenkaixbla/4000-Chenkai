using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class CombatPageController
{
    void EnsureBuilt()
    {
        if (isBuilt)
        {
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        // All action buttons are now manually authored in the scene; we only wire listeners.
        foodButtonRefs = CacheButtonRefs(foodButton, () => manager.UseFood());
        potionButtonRefs = CacheButtonRefs(potionButton, () => manager.UsePotion());
        stopButtonRefs = CacheButtonRefs(stopButton, () => manager.StopCombat());

        RebuildSlotButtonBindings();
        isBuilt = true;
    }

    bool ValidateReferences()
    {
        bool hasAllReferences = true;

        hasAllReferences &= ValidateReference(monsterListSpawn.Content, nameof(monsterListSpawn) + ".Content");
        hasAllReferences &= ValidateReference(monsterListSpawn.ElementPrefab, nameof(monsterListSpawn) + ".ElementPrefab");
        hasAllReferences &= ValidateReference(selectionListSpawn.Content, nameof(selectionListSpawn) + ".Content");
        hasAllReferences &= ValidateReference(selectionListSpawn.ElementPrefab, nameof(selectionListSpawn) + ".ElementPrefab");

        hasAllReferences &= ValidateReference(playerHpText, nameof(playerHpText));
        hasAllReferences &= ValidateReference(playerHpFill, nameof(playerHpFill));
        hasAllReferences &= ValidateReference(playerDerivedText, nameof(playerDerivedText));
        hasAllReferences &= ValidateReference(playerTimerText, nameof(playerTimerText));

        hasAllReferences &= ValidateReference(monsterNameText, nameof(monsterNameText));
        hasAllReferences &= ValidateReference(monsterHpText, nameof(monsterHpText));
        hasAllReferences &= ValidateReference(monsterHpFill, nameof(monsterHpFill));
        hasAllReferences &= ValidateReference(monsterStatsText, nameof(monsterStatsText));
        hasAllReferences &= ValidateReference(monsterTimerText, nameof(monsterTimerText));

        hasAllReferences &= ValidateReference(statusText, nameof(statusText));
        hasAllReferences &= ValidateReference(effectStatusText, nameof(effectStatusText));
        hasAllReferences &= ValidateReference(logText, nameof(logText));
        hasAllReferences &= ValidateReference(selectionTitleText, nameof(selectionTitleText));

        hasAllReferences &= ValidateReference(foodButton, nameof(foodButton));
        hasAllReferences &= ValidateReference(potionButton, nameof(potionButton));
        hasAllReferences &= ValidateReference(stopButton, nameof(stopButton));

        if (slotButtonElements == null || slotButtonElements.Count == 0)
        {
            Debug.LogError("CombatPageController requires at least one slot button element.", this);
            hasAllReferences = false;
        }
        else
        {
            for (int i = 0; i < slotButtonElements.Count; i++)
            {
                CombatSlotButtonElement element = slotButtonElements[i];
                if (element == null)
                {
                    Debug.LogError($"CombatPageController slotButtonElements has a null entry at index {i}.", this);
                    hasAllReferences = false;
                }
            }
        }

        return hasAllReferences;
    }

    bool ValidateReference(Object target, string fieldName)
    {
        if (target != null)
        {
            return true;
        }

        Debug.LogError($"CombatPageController missing required reference: {fieldName}", this);
        return false;
    }

    ButtonRefs CacheButtonRefs(Button button, UnityEngine.Events.UnityAction onClick)
    {
        ButtonRefs refs = new()
        {
            button = button,
            background = button.targetGraphic as Image
        };

        refs.background ??= button.GetComponent<Image>();
        refs.label = button.GetComponentInChildren<TMP_Text>(true);

        button.onClick.RemoveAllListeners();
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        return refs;
    }

    void RebuildSlotButtonBindings()
    {
        slotButtons.Clear();
        for (int i = 0; i < slotButtonElements.Count; i++)
        {
            CombatSlotButtonElement element = slotButtonElements[i];
            if (element == null)
            {
                continue;
            }

            Button slotButton = element.Button;
            if (slotButton == null)
            {
                Debug.LogError("CombatPageController slot button element is missing its Button reference.", element);
                continue;
            }

            CombatEquipSlot capturedSlot = element.Slot;
            int capturedUtilityIndex = element.UtilityIndex;
            ButtonRefs refs = CacheButtonRefs(slotButton, () =>
            {
                selectedSlot = capturedSlot;
                selectedUtilityIndex = capturedUtilityIndex;
                RefreshSlotButtons();
                RebuildSelectionList();
            });

            if (element.Background != null)
            {
                refs.background = element.Background;
            }

            if (element.Label != null)
            {
                refs.label = element.Label;
            }

            slotButtons.Add(new SlotButtonBinding
            {
                slot = capturedSlot,
                utilityIndex = capturedUtilityIndex,
                refs = refs
            });
        }
    }

    MonsterSelectionButtonElement CreateSelectionButton(CombatSpawnListReferences spawnReferences, string value, UnityEngine.Events.UnityAction onClick, float height)
    {
        if (spawnReferences == null || spawnReferences.ElementPrefab == null || spawnReferences.Content == null)
        {
            return null;
        }

        MonsterSelectionButtonElement element = Instantiate(spawnReferences.ElementPrefab, spawnReferences.Content, false);
        element.gameObject.SetActive(true);
        element.Configure(value, onClick, true, height);
        return element;
    }

    MonsterSelectionButtonElement CreateSelectionInfoButton(CombatSpawnListReferences spawnReferences, string value, float height)
    {
        MonsterSelectionButtonElement element = CreateSelectionButton(spawnReferences, value, null, height);
        if (element == null)
        {
            return null;
        }

        element.SetInteractable(false);
        element.SetLabelAlignment(TextAlignmentOptions.Center);
        return element;
    }

    void ClearChildren(RectTransform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }
}
