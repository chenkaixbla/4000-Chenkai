using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A single named menu panel. Pair a unique <see cref="menuName"/> with the
/// GameObject that holds the menu's UI. The manager keeps exactly one of these
/// active at a time.
/// </summary>
[Serializable]
public class UI_Menu_Entry
{
    [Tooltip("Unique name used to open this menu. Buttons and code reference the menu by this name.")]
    public string menuName;

    [Required]
    [Tooltip("The panel GameObject to toggle. It is the root of this menu's UI inside the menu container.")]
    public GameObject panel;

    [TypeFilter(typeof(Button), typeof(UI_Button))]
    [Tooltip("Optional: the button that opens this menu. Accepts a native Button or a UI_Button. Auto-wired at runtime.")]
    public UnityEngine.Object openButton;

    public bool IsValid => panel != null && !string.IsNullOrWhiteSpace(menuName);

    /// <summary>The clickable Button behind the assigned opener (native Button or UI_Button), or null.</summary>
    public Button ResolveOpenButton()
    {
        return openButton switch
        {
            UI_Button uiButton => uiButton.button,
            Button button => button,
            _ => null
        };
    }

    public bool Matches(string otherName)
    {
        return !string.IsNullOrWhiteSpace(otherName)
            && string.Equals(menuName?.Trim(), otherName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public void SetActive(bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}

/// <summary>
/// Scene-wide controller for the menu area. Holds a manual list of named menu
/// panels and shows one at a time via <see cref="SetActive"/>. There is meant to
/// be a single instance per scene, exposed through <see cref="Instance"/> so any
/// other script (buttons, gameplay events) can switch menus by name.
///
/// Setup is intentionally manual/artist-friendly: drop your panels into the scene,
/// add each one to the <see cref="menus"/> list with a unique name. Everything else
/// (which panel is on, default on start, lookup by name) is handled automatically.
/// </summary>
[DisallowMultipleComponent]
public class UI_Menu_Manager : MonoBehaviour
{
    /// <summary>The active manager in the current scene. Set in Awake.</summary>
    public static UI_Menu_Manager Instance { get; private set; }

    [Title("Menus")]
    [Tooltip("Each entry pairs a unique name with the panel it shows. Only one panel is active at a time.")]
    public List<UI_Menu_Entry> menus = new();

    [Title("Startup")]
    [Dropdown(nameof(GetMenuNames))]
    [Tooltip("Menu shown when the scene starts (if 'Show Default On Start' is on).")]
    public string defaultMenu;

    public bool showDefaultOnStart = true;

    [Title("Runtime")]
    [ReadOnly, SerializeField]
    string activeMenu;

    /// <summary>Name of the menu currently shown, or empty if none.</summary>
    public string ActiveMenu => activeMenu;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning($"[UI_Menu_Manager] A second manager '{name}' was found. There should be one per scene.", this);

        Instance = this;
        WireOpenButtons();
    }

    /// <summary>
    /// Auto-hooks each menu's opener button (native Button or UI_Button) so clicking it
    /// shows that menu. Lets you wire menu navigation entirely from this one manager.
    /// </summary>
    void WireOpenButtons()
    {
        for (int i = 0; i < menus.Count; i++)
        {
            UI_Menu_Entry entry = menus[i];
            Button button = entry?.ResolveOpenButton();
            if (button == null)
                continue;

            string menuName = entry.menuName;
            button.onClick.AddListener(() => Show(menuName));
        }
    }

    void Start()
    {
        if (showDefaultOnStart && !string.IsNullOrWhiteSpace(defaultMenu))
            Show(defaultMenu);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Shows the menu with the given name and hides every other menu.
    /// Pass an empty/unknown name to simply hide everything.
    /// </summary>
    public void Show(string menuName)
    {
        bool found = false;

        for (int i = 0; i < menus.Count; i++)
        {
            UI_Menu_Entry entry = menus[i];
            if (entry == null)
                continue;

            bool shouldShow = entry.Matches(menuName);
            entry.SetActive(shouldShow);
            found |= shouldShow;
        }

        activeMenu = found ? menuName : string.Empty;

        if (!found && !string.IsNullOrWhiteSpace(menuName))
            Debug.LogWarning($"[UI_Menu_Manager] No menu named '{menuName}'.", this);
    }

    /// <summary>Hides every menu panel.</summary>
    public void HideAll()
    {
        for (int i = 0; i < menus.Count; i++)
            menus[i]?.SetActive(false);

        activeMenu = string.Empty;
    }

    public bool HasMenu(string menuName)
    {
        for (int i = 0; i < menus.Count; i++)
        {
            if (menus[i] != null && menus[i].Matches(menuName))
                return true;
        }

        return false;
    }

    /// <summary>
    /// All menu names in order. Used by EditorAttributes [Dropdown] fields here and
    /// on other scripts (e.g. UI_Menu_Button) so menus can be picked without typos.
    /// </summary>
    public string[] GetMenuNames()
    {
        string[] names = new string[menus.Count];
        for (int i = 0; i < menus.Count; i++)
            names[i] = menus[i] != null ? menus[i].menuName : string.Empty;

        return names;
    }

    void OnValidate()
    {
        // Warn about duplicate names so dropdown selection stays unambiguous.
        for (int i = 0; i < menus.Count; i++)
        {
            if (menus[i] == null || string.IsNullOrWhiteSpace(menus[i].menuName))
                continue;

            for (int j = i + 1; j < menus.Count; j++)
            {
                if (menus[j] != null && menus[i].Matches(menus[j].menuName))
                {
                    Debug.LogWarning($"[UI_Menu_Manager] Duplicate menu name '{menus[i].menuName}'. Names must be unique.", this);
                    return;
                }
            }
        }
    }
}
