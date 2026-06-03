using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Idle_CardUI : MonoBehaviour
{
    [Title("UI")]
    public Button toggleButton;
    public TMP_Text nameText;
    public TMP_Text xpPerSecondText;
    public TMP_Text rewardText;
    public TMP_Text levelText;
    public Image levelBarFill;
    public TMP_Text timerText;
    public Image timerBarFill;
    public GameObject runningRoot;
    public GameObject stoppedRoot;

    [ReadOnly, System.NonSerialized] Idle_Instance boundIdle;
    [ReadOnly, SerializeField] bool isActiveSelection;

    Idle_Manager ownerManager;

    public Idle_Instance BoundIdle => boundIdle;

    public void Bind(Idle_Instance idleInstance)
    {
        Unbind();

        boundIdle = idleInstance;
        if (boundIdle == null)
        {
            ClearView();
            return;
        }

        boundIdle.OnUpdate += Refresh;
        boundIdle.OnTimerFinish += Refresh;
        boundIdle.OnRunningStateChanged += HandleRunningStateChanged;
        Refresh();
    }

    public void Unbind()
    {
        if (boundIdle != null)
        {
            boundIdle.OnUpdate -= Refresh;
            boundIdle.OnTimerFinish -= Refresh;
            boundIdle.OnRunningStateChanged -= HandleRunningStateChanged;
        }

        boundIdle = null;
        isActiveSelection = false;
        UpdateRunningStateVisual(false);
    }

    public void ConfigureToggle(Idle_Manager manager)
    {
        ownerManager = manager;

        if (toggleButton == null)
        {
            toggleButton = GetComponent<Button>();
        }

        if (toggleButton == null)
        {
            return;
        }

        toggleButton.onClick.RemoveListener(OnToggleClicked);
        toggleButton.onClick.AddListener(OnToggleClicked);
    }

    public void SetSelectionState(bool isSelected)
    {
        isActiveSelection = isSelected;
        UpdateRunningStateVisual(boundIdle != null && boundIdle.isRunning);
    }

    public void Refresh()
    {
        if (boundIdle == null || boundIdle.idleData == null)
        {
            ClearView();
            return;
        }

        Idle_Data data = boundIdle.idleData;

        if (nameText != null)
        {
            nameText.text = string.IsNullOrWhiteSpace(data.displayName) ? data.name : data.displayName;
        }

        if (xpPerSecondText != null)
        {
            xpPerSecondText.text = $"Idle XP/s: {data.GetIdleXpPerSecond():0.##} | Job XP/s: {data.GetJobXpPerSecond():0.##}";
        }

        if (rewardText != null)
        {
            rewardText.text = $"Rewards: {data.GetRewardSummary()}";
        }

        if (levelText != null)
        {
            levelText.text = $"Idle Lv {boundIdle.level}";
        }

        if (levelBarFill != null)
        {
            int safeMaxXp = Mathf.Max(1, boundIdle.maxXP);
            levelBarFill.fillAmount = Mathf.Clamp01((float)boundIdle.currentXP / safeMaxXp);
        }

        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = boundIdle.GetNormalizedProgress();
        }

        if (timerText != null)
        {
            float safeDuration = Mathf.Max(0.1f, data.interval);
            float remaining = Mathf.Max(0f, safeDuration - boundIdle.timer);
            timerText.text = $"{remaining:0.0}s";
        }

        UpdateRunningStateVisual(boundIdle.isRunning);
    }

    void ClearView()
    {
        if (nameText != null)
        {
            nameText.text = "-";
        }

        if (xpPerSecondText != null)
        {
            xpPerSecondText.text = string.Empty;
        }

        if (rewardText != null)
        {
            rewardText.text = "Rewards: None";
        }

        if (levelText != null)
        {
            levelText.text = string.Empty;
        }

        if (levelBarFill != null)
        {
            levelBarFill.fillAmount = 0f;
        }

        if (timerText != null)
        {
            timerText.text = string.Empty;
        }

        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = 0f;
        }

        UpdateRunningStateVisual(false);
    }

    void HandleRunningStateChanged(bool isRunning)
    {
        UpdateRunningStateVisual(isRunning);
    }

    void UpdateRunningStateVisual(bool isRunning)
    {
        if (runningRoot != null)
        {
            runningRoot.SetActive(isRunning && isActiveSelection);
        }

        if (stoppedRoot != null)
        {
            stoppedRoot.SetActive(!isRunning || !isActiveSelection);
        }
    }

    void OnToggleClicked()
    {
        if (ownerManager == null || boundIdle == null)
        {
            return;
        }

        ownerManager.ToggleIdle(boundIdle);
    }

    void OnDisable()
    {
        Unbind();

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(OnToggleClicked);
        }
    }
}
