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

        rootRect = (RectTransform)transform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image background = GetOrAddComponent<Image>(gameObject);
        background.color = new Color(0.07f, 0.08f, 0.10f, 0.98f);

        HorizontalLayoutGroup rootLayout = GetOrAddComponent<HorizontalLayoutGroup>(gameObject);
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 18f;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = true;

        CreateColumns();
        isBuilt = true;
    }

    void CreateColumns()
    {
        RectTransform leftColumn = CreatePanel(rootRect, 260f);
        CreateLabel(leftColumn, "Monsters", 30, TextAlignmentOptions.Center);
        monsterListContent = CreateScrollList(leftColumn, 0f);

        RectTransform centerColumn = CreatePanel(rootRect, -1f);
        CreateLabel(centerColumn, "Encounter", 30, TextAlignmentOptions.Center);
        playerHpText = CreateLabel(centerColumn, "Player HP: 0 / 0", 26, TextAlignmentOptions.Left);
        playerHpFill = CreateBar(centerColumn, new Color(0.22f, 0.74f, 0.30f, 1f));
        playerDerivedText = CreateLabel(centerColumn, string.Empty, 22, TextAlignmentOptions.TopLeft);
        playerTimerText = CreateLabel(centerColumn, string.Empty, 22, TextAlignmentOptions.Left);

        CreateSpacer(centerColumn, 12f);
        monsterNameText = CreateLabel(centerColumn, "No Monster Selected", 28, TextAlignmentOptions.Center);
        monsterHpText = CreateLabel(centerColumn, "Monster HP: 0 / 0", 26, TextAlignmentOptions.Left);
        monsterHpFill = CreateBar(centerColumn, new Color(0.78f, 0.22f, 0.24f, 1f));
        monsterStatsText = CreateLabel(centerColumn, string.Empty, 22, TextAlignmentOptions.TopLeft);
        monsterTimerText = CreateLabel(centerColumn, string.Empty, 22, TextAlignmentOptions.Left);

        CreateSpacer(centerColumn, 10f);
        RectTransform styleRow = CreateRow(centerColumn, 10f);
        CreateStyleButton(styleRow, CombatStyle.MeleeAccurate);
        CreateStyleButton(styleRow, CombatStyle.MeleeAggressive);
        CreateStyleButton(styleRow, CombatStyle.MeleeDefensive);
        CreateStyleButton(styleRow, CombatStyle.Ranged);

        RectTransform actionRow = CreateRow(centerColumn, 10f);
        foodButtonRefs = CreateButton(actionRow, "Use Food", () => manager.UseFood(), 54);
        potionButtonRefs = CreateButton(actionRow, "Use Potion", () => manager.UsePotion(), 54);
        stopButtonRefs = CreateButton(actionRow, "Stop", () => manager.StopCombat(), 54);

        statusText = CreateLabel(centerColumn, string.Empty, 22, TextAlignmentOptions.Left);
        effectStatusText = CreateLabel(centerColumn, string.Empty, 22, TextAlignmentOptions.Left);

        CreateSpacer(centerColumn, 8f);
        CreateLabel(centerColumn, "Combat Log", 26, TextAlignmentOptions.Center);
        logText = CreateLabel(centerColumn, "Combat log is empty.", 21, TextAlignmentOptions.TopLeft);
        LayoutElement logElement = logText.GetComponent<LayoutElement>();
        logElement.flexibleHeight = 1f;
        logElement.minHeight = 200f;

        RectTransform rightColumn = CreatePanel(rootRect, 330f);
        CreateLabel(rightColumn, "Loadout", 30, TextAlignmentOptions.Center);
        CreateSlotButton(rightColumn, CombatEquipSlot.Weapon);
        CreateSlotButton(rightColumn, CombatEquipSlot.Offhand);
        CreateSlotButton(rightColumn, CombatEquipSlot.Helmet);
        CreateSlotButton(rightColumn, CombatEquipSlot.Body);
        CreateSlotButton(rightColumn, CombatEquipSlot.Legs);
        CreateSlotButton(rightColumn, CombatEquipSlot.Gloves);
        CreateSlotButton(rightColumn, CombatEquipSlot.Boots);
        CreateSlotButton(rightColumn, CombatEquipSlot.Cape);
        CreateSlotButton(rightColumn, CombatEquipSlot.Ammo);
        CreateSlotButton(rightColumn, CombatEquipSlot.Food);
        CreateSlotButton(rightColumn, CombatEquipSlot.Potion);
        CreateSlotButton(rightColumn, CombatEquipSlot.Utility, 0);
        CreateSlotButton(rightColumn, CombatEquipSlot.Utility, 1);
        CreateSlotButton(rightColumn, CombatEquipSlot.Utility, 2);

        CreateSpacer(rightColumn, 8f);
        selectionTitleText = CreateLabel(rightColumn, "Selection", 26, TextAlignmentOptions.Center);
        selectionListContent = CreateScrollList(rightColumn, 220f);
    }

    void CreateStyleButton(RectTransform parent, CombatStyle style)
    {
        ButtonRefs refs = CreateButton(parent, GetStyleDisplayName(style), () => manager.SetCombatStyle(style), 46);
        styleButtons.Add(new StyleButtonBinding
        {
            style = style,
            refs = refs
        });
    }

    void CreateSlotButton(RectTransform parent, CombatEquipSlot slot, int utilityIndex = -1)
    {
        ButtonRefs refs = CreateButton(parent, CombatManager.GetSlotDisplayName(slot, utilityIndex), () =>
        {
            selectedSlot = slot;
            selectedUtilityIndex = utilityIndex;
            RefreshSlotButtons();
            RebuildSelectionList();
        }, 56);

        slotButtons.Add(new SlotButtonBinding
        {
            slot = slot,
            utilityIndex = utilityIndex,
            refs = refs
        });
    }

    RectTransform CreatePanel(Transform parent, float preferredWidth)
    {
        GameObject panelObject = new("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        panelObject.transform.SetParent(parent, false);

        Image image = panelObject.GetComponent<Image>();
        image.color = new Color(0.11f, 0.12f, 0.14f, 0.94f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement layoutElement = panelObject.GetComponent<LayoutElement>();
        if (preferredWidth > 0f)
        {
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.flexibleWidth = 0f;
        }
        else
        {
            layoutElement.flexibleWidth = 1f;
            layoutElement.minWidth = 420f;
        }

        layoutElement.flexibleHeight = 1f;
        return panelObject.GetComponent<RectTransform>();
    }

    RectTransform CreateRow(Transform parent, float spacing)
    {
        GameObject rowObject = new("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowObject.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 54f;
        return rowObject.GetComponent<RectTransform>();
    }

    RectTransform CreateScrollList(Transform parent, float preferredHeight)
    {
        GameObject root = new("Scroll Root", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
        root.transform.SetParent(parent, false);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.08f, 0.08f, 0.09f, 0.92f);

        LayoutElement layoutElement = root.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight > 0f ? preferredHeight : 300f;
        layoutElement.flexibleHeight = preferredHeight > 0f ? 0f : 1f;

        RectTransform viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<RectTransform>();
        viewport.SetParent(root.transform, false);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = Color.clear;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        RectTransform content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = root.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        return content;
    }

    Image CreateBar(Transform parent, Color fillColor)
    {
        GameObject barRoot = new("Bar", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        barRoot.transform.SetParent(parent, false);
        barRoot.GetComponent<Image>().color = new Color(0.20f, 0.20f, 0.22f, 1f);

        LayoutElement rootElement = barRoot.GetComponent<LayoutElement>();
        rootElement.preferredHeight = 22f;

        RectTransform fillRect = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        fillRect.SetParent(barRoot.transform, false);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillRect.GetComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;
        return fillImage;
    }

    TMP_Text CreateLabel(Transform parent, string value, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.text = value;
        text.alignment = alignment;
        text.enableWordWrapping = true;

        LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();
        layoutElement.minHeight = fontSize + 8f;
        return text;
    }

    ButtonRefs CreateButton(Transform parent, string value, UnityEngine.Events.UnityAction onClick, float height)
    {
        GameObject buttonObject = new("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.16f, 0.16f, 0.16f, 0.95f);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = height;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);

        RectTransform textRect = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
        textRect.SetParent(buttonObject.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMP_Text label = textRect.GetComponent<TMP_Text>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 20f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.text = value;

        return new ButtonRefs
        {
            button = button,
            background = background,
            label = label
        };
    }

    void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(parent, false);
        spacer.GetComponent<LayoutElement>().preferredHeight = height;
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

    static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        if (!gameObject.TryGetComponent(out T component))
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }
}
