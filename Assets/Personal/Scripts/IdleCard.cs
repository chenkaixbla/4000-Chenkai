using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdleCard : MonoBehaviour
{
    public IdleInstance currentInstance;

    [Title("UI")]
    public TMP_Text displayNameText;
    public TMP_Text subtitleText;
    public Image iconImage;
    public Image timerProgressBar;
    public Image xpProgressBar;
    public TMP_Text xpProgressText;
    public TMP_Text levelText;

    public IdleData idleData;

    void Start()
    {
        ResetUI();
    }

    [Button]
    public void CreateNewInstance()
    {
        IdleInstance newInstance = new IdleInstance(idleData);
        SetInstance(newInstance);
    }

    void Update()
    {
        currentInstance?.DoUpdate();
    }

    public void SetInstance(IdleInstance inst)
    {
        currentInstance = inst;
        currentInstance.OnUpdate += () => UpdateUI(inst);
        UpdateUI(inst);
    }

    void UpdateUI(IdleInstance inst)
    {
        displayNameText.text = inst.idleData.displayName;
        iconImage.sprite = inst.idleData.icon;

        // Subtitle text shows ex. 10 XP / 5 seconds
        subtitleText.text = $"{inst.idleData.xpReward} XP / {inst.idleData.interval} seconds";

        xpProgressText.text = $"{inst.currentXP} / {inst.idleData.maxXP} XP";

        float progress = (float)inst.currentXP / inst.idleData.maxXP;
        xpProgressBar.fillAmount = progress;

        timerProgressBar.fillAmount = inst.timer / inst.idleData.interval;

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
