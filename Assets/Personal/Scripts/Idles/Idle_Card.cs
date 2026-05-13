using System;
using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Idle_Card : MonoBehaviour
{
    [Title("Data")]
    public bool newInstanceOnStart = true;
    [PropertyDropdown] public Idle_Data idleData;
    public Idle_Instance currentInstance;

    [Title("UI")]
    public GameObject lockRoot;
    public TMP_Text displayNameText;
    public TMP_Text subtitleText;
    public Image iconImage;
    public Image timerProgressBar;
    public Image xpProgressBar;
    public TMP_Text xpProgressText;
    public TMP_Text levelText;

    Action onInstanceUpdate;

    void Start()
    {
        ResetUI();

        if (newInstanceOnStart && idleData != null)
        {
            CreateNewInstance();
        }
    }

    [Button]
    public void CreateNewInstance()
    {
        if (idleData == null)
        {
            return;
        }

        if (!idleData.AreStartConditionsMet())
        {
            Debug.Log($"Idle '{idleData.displayName}' cannot start because its conditions are not met.");
            ShowLockedState(idleData, 0, 0);
            return;
        }

        Idle_Instance newInstance = new Idle_Instance(idleData);
        SetInstance(newInstance);
    }

    public void SetInstance(Idle_Instance inst)
    {
        UnsubscribeCurrentInstance();

        currentInstance = inst;
        if (currentInstance == null)
        {
            ResetUI();
            return;
        }

        onInstanceUpdate = HandleInstanceUpdated;
        currentInstance.OnUpdate += onInstanceUpdate;
        UpdateUI(currentInstance);
    }

    void OnDisable()
    {
        UnsubscribeCurrentInstance();
    }

    void HandleInstanceUpdated()
    {
        if (currentInstance != null)
        {
            UpdateUI(currentInstance);
        }
    }

    void UnsubscribeCurrentInstance()
    {
        if (currentInstance != null && onInstanceUpdate != null)
        {
            currentInstance.OnUpdate -= onInstanceUpdate;
            onInstanceUpdate = null;
        }
    }

    void UpdateUI(Idle_Instance inst)
    {
        if (inst == null || inst.idleData == null)
        {
            ResetUI();
            return;
        }

        displayNameText.text = inst.idleData.displayName;
        iconImage.sprite = inst.idleData.icon;

        if (!inst.AreStartConditionsMet())
        {
            ShowLockedState(inst.idleData, inst.level, inst.currentXP);
            return;
        }

        int maxXP = GetMaxXP(inst);

        subtitleText.text = $"{inst.idleData.idleXPReward} XP / {inst.idleData.interval} seconds";

        xpProgressText.text = maxXP > 0
            ? $"{inst.currentXP} / {maxXP} XP"
            : $"{inst.currentXP} XP";

        float progress = maxXP > 0 ? (float)inst.currentXP / maxXP : 0f;
        xpProgressBar.fillAmount = progress;

        timerProgressBar.fillAmount = inst.idleData.interval > 0f ? inst.timer / inst.idleData.interval : 0f;

        levelText.text = $"Level: {inst.level}";

        lockRoot?.SetActive(false);
    }

    void ResetUI()
    {
        displayNameText.text = "No Idle";
        subtitleText.text = "";
        iconImage.sprite = null;
        timerProgressBar.fillAmount = 0f;
        xpProgressText.text = "";
        xpProgressBar.fillAmount = 0f;
        levelText.text = "";
        lockRoot?.SetActive(false);

    }

    void ShowLockedState(Idle_Data data, int level, int currentXP)
    {
        if (data == null)
        {
            ResetUI();
            return;
        }
        

        displayNameText.text = data.displayName;
        subtitleText.text = "Locked";
        iconImage.sprite = data.icon;
        timerProgressBar.fillAmount = 0f;
        xpProgressText.text = "";
        xpProgressBar.fillAmount = 0;
        levelText.text = $"Locked - Level: {level}";

        lockRoot?.SetActive(true);
    }

    int GetMaxXP(Idle_Instance inst)
    {
        return XPUtility.GetMaxXPForLevel(inst.level);
    }
}
