using System;
using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdleCard : MonoBehaviour
{
    [Title("Data")]
    public bool newInstanceOnStart = true;
    [PropertyDropdown] public IdleData idleData;
    public IdleInstance currentInstance;

    [Title("UI")]
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
        IdleInstance newInstance = new IdleInstance(idleData);
        SetInstance(newInstance);
    }

    public void SetInstance(IdleInstance inst)
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

    void UpdateUI(IdleInstance inst)
    {
        if (inst == null || inst.idleData == null)
        {
            ResetUI();
            return;
        }

        displayNameText.text = inst.idleData.displayName;
        iconImage.sprite = inst.idleData.icon;

        // Subtitle text shows ex. 10 XP / 5 seconds
        subtitleText.text = $"{inst.idleData.xpReward} XP / {inst.idleData.interval} seconds";

        xpProgressText.text = $"{inst.currentXP} / {inst.idleData.maxXP} XP";

        float progress = inst.idleData.maxXP > 0 ? (float)inst.currentXP / inst.idleData.maxXP : 0f;
        xpProgressBar.fillAmount = progress;

        timerProgressBar.fillAmount = inst.idleData.interval > 0f ? inst.timer / inst.idleData.interval : 0f;

        levelText.text = $"Level: {inst.level}";
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
    }
}
