using System;
using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The base view for one idle action. Bind an <see cref="Idle_Runtime"/> and the card
/// keeps itself in sync via events. It handles the common fields (name, icon, info,
/// progress, running/stopped state) and forwards the lifecycle to any
/// <see cref="Idle_Card_Extension"/> components on the same prefab - so special cards add
/// their own visuals without this script knowing about them.
///
/// Reusable by design: <see cref="Bind"/>/<see cref="Unbind"/> let one card serve many
/// runtimes, which is what makes pooling (no destroy/instantiate on job switch) possible.
/// </summary>
[DisallowMultipleComponent]
public class Idle_Card : MonoBehaviour
{
    [Title("Common UI (all optional)")]
    public TMP_Text nameText;
    public Image iconImage;
    [Tooltip("Free-form line for xp/s, rewards, etc.")]
    public TMP_Text infoText;

    [Tooltip("Level / XP bar. Its title shows \"Level: ##\" and its value shows XP current/max.")]
    public UI_Bar levelBar;
    [Tooltip("Action timer bar.")]
    public UI_Bar timerBar;

    public Button toggleButton;

    [NonSerialized] Idle_Runtime bound;
    Idle_Card_Extension[] extensions;
    bool initialized;

    /// <summary>The runtime currently shown by this card, or null.</summary>
    public Idle_Runtime Bound => bound;

    /// <summary>Raised when the toggle button is clicked. The manager decides what happens.</summary>
    public event Action<Idle_Card> ToggleRequested;

    void Awake() => Initialize();

    void Initialize()
    {
        if (initialized)
            return;

        extensions = GetComponents<Idle_Card_Extension>();
        if (toggleButton != null)
            toggleButton.onClick.AddListener(RaiseToggle);

        initialized = true;
    }

    /// <summary>Shows the given runtime and subscribes to its updates.</summary>
    public void Bind(Idle_Runtime runtime)
    {
        Initialize();
        Unbind();

        bound = runtime;
        if (bound == null)
        {
            ClearView();
            return;
        }

        bound.OnUpdated += Refresh;
        bound.OnCycleCompleted += Refresh;

        for (int i = 0; i < extensions.Length; i++)
            extensions[i]?.OnBind(bound);

        Refresh();
    }

    /// <summary>Detaches from the current runtime (called before reuse / on pool return).</summary>
    public void Unbind()
    {
        if (bound != null)
        {
            bound.OnUpdated -= Refresh;
            bound.OnCycleCompleted -= Refresh;
        }

        if (extensions != null)
        {
            for (int i = 0; i < extensions.Length; i++)
                extensions[i]?.OnUnbind();
        }

        bound = null;
    }

    public void Refresh()
    {
        if (bound == null || bound.idleData == null)
        {
            ClearView();
            return;
        }

        Idle_Data data = bound.idleData;

        if (nameText != null)
            nameText.text = string.IsNullOrWhiteSpace(data.displayName) ? data.name : data.displayName;

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;

        if (infoText != null)
            infoText.text = $"Idle XP/s {data.GetIdleXpPerSecond():0.##}   Job XP/s {data.GetJobXpPerSecond():0.##}";

        if (levelBar != null)
        {
            if (levelBar.titleText != null)
                levelBar.titleText.text = $"Level: {bound.level}";

            levelBar.maxValue = bound.maxXP;
            levelBar.SetValue(bound.currentXP);
        }

        if (timerBar != null)
            timerBar.SetFill(bound.GetNormalizedProgress());

        for (int i = 0; i < extensions.Length; i++)
            extensions[i]?.OnRefresh(bound);
    }

    void ClearView()
    {
        if (nameText != null) nameText.text = "-";
        if (infoText != null) infoText.text = string.Empty;

        if (levelBar != null)
        {
            if (levelBar.titleText != null) levelBar.titleText.text = string.Empty;
            levelBar.SetFill(0f);
        }

        if (timerBar != null) timerBar.SetFill(0f);
    }

    void RaiseToggle() => ToggleRequested?.Invoke(this);

    void Reset()
    {
        if (toggleButton == null)
            toggleButton = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
    }
}
