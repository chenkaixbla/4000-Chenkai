using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class Menu_ViewPanelEntry
{
    public Menu_ViewPanelId panelId = Menu_ViewPanelId.None;
    public GameObject panelRoot;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    public bool Matches(string panelName)
    {
        if (string.IsNullOrWhiteSpace(panelName))
        {
            return false;
        }

        return string.Equals(panelId.ToString(), panelName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public void SetVisible(bool isVisible)
    {
        if (panelRoot == null)
        {
            return;
        }

        panelRoot.SetActive(isVisible);
    }
}

[DisallowMultipleComponent]
public class Menu_MainViewManager : MonoBehaviour
{
    [Title("View Panel List")]
    public List<Menu_ViewPanelEntry> panelEntries = new();
    public Menu_ViewPanelId defaultPanel = Menu_ViewPanelId.Idles;
    public bool showDefaultPanelOnStart = true;

    [ReadOnly, SerializeField] Menu_ViewPanelId activePanel = Menu_ViewPanelId.None;

    [SerializeField, HideInInspector, FormerlySerializedAs("viewPanels")]
    List<Menu_ViewPanel> legacyViewPanels = new();

    public Menu_ViewPanelId ActivePanel => activePanel;

    void Awake()
    {
        MigrateLegacyPanelsIfNeeded();
        PruneNullPanels();
    }

    void Start()
    {
        PruneNullPanels();

        if (showDefaultPanelOnStart)
        {
            ShowPanel(defaultPanel);
            return;
        }

        SetActivePanelFromCurrentState();
    }

    public void ShowPanel(Menu_ViewPanelId panelId)
    {
        PruneNullPanels();

        bool found = false;
        for (int i = 0; i < panelEntries.Count; i++)
        {
            Menu_ViewPanelEntry panel = panelEntries[i];
            bool shouldShow = panel != null && panel.panelId == panelId;
            panel?.SetVisible(shouldShow);
            found |= shouldShow;
        }

        activePanel = found ? panelId : Menu_ViewPanelId.None;
        if (!found)
        {
            Debug.LogWarning($"{nameof(Menu_MainViewManager)} could not find panel id '{panelId}'.", this);
        }
    }

    public void ShowPanel(string panelName)
    {
        PruneNullPanels();
        bool found = false;

        for (int i = 0; i < panelEntries.Count; i++)
        {
            Menu_ViewPanelEntry panel = panelEntries[i];
            bool shouldShow = panel != null && panel.Matches(panelName);
            panel?.SetVisible(shouldShow);

            if (shouldShow)
            {
                activePanel = panel.panelId;
                found = true;
            }
        }

        if (!found)
        {
            activePanel = Menu_ViewPanelId.None;
            Debug.LogWarning($"{nameof(Menu_MainViewManager)} could not find panel '{panelName}'.", this);
        }
    }

    public string[] GetPanelNames()
    {
        PruneNullPanels();
        string[] names = new string[panelEntries.Count];

        for (int i = 0; i < panelEntries.Count; i++)
        {
            Menu_ViewPanelEntry panel = panelEntries[i];
            names[i] = panel != null ? panel.panelId.ToString() : string.Empty;
        }

        return names;
    }

    void SetActivePanelFromCurrentState()
    {
        activePanel = Menu_ViewPanelId.None;

        Menu_ViewPanelEntry firstVisible = null;
        for (int i = 0; i < panelEntries.Count; i++)
        {
            Menu_ViewPanelEntry panel = panelEntries[i];
            if (panel == null || !panel.IsVisible)
            {
                continue;
            }

            if (firstVisible == null)
            {
                firstVisible = panel;
                continue;
            }

            panel.SetVisible(false);
        }

        if (firstVisible != null)
        {
            activePanel = firstVisible.panelId;
        }
    }

    void PruneNullPanels()
    {
        panelEntries.RemoveAll(entry => entry == null || entry.panelRoot == null);
    }

    void OnValidate()
    {
        MigrateLegacyPanelsIfNeeded();
        PruneNullPanels();
    }

    void MigrateLegacyPanelsIfNeeded()
    {
        if (panelEntries.Count > 0 || legacyViewPanels == null || legacyViewPanels.Count == 0)
        {
            return;
        }

        for (int i = 0; i < legacyViewPanels.Count; i++)
        {
            Menu_ViewPanel legacyPanel = legacyViewPanels[i];
            if (legacyPanel == null)
            {
                continue;
            }

            Menu_ViewPanelEntry entry = new Menu_ViewPanelEntry
            {
                panelId = legacyPanel.panelId,
                panelRoot = legacyPanel.gameObject
            };

            panelEntries.Add(entry);
        }

        legacyViewPanels.Clear();
    }
}
