using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Job_ButtonView : MonoBehaviour
{
    public TMP_Text displayNameText;
    public Image iconImage;
    public GameObject lockedRoot;
    public GameObject unlockedRoot;

    public void Bind(Job_Data jobData, bool unlocked)
    {
        if (displayNameText != null)
        {
            displayNameText.text = jobData != null && !string.IsNullOrWhiteSpace(jobData.jobName)
                ? jobData.jobName
                : "-";
        }

        if (iconImage != null)
        {
            iconImage.sprite = jobData != null ? jobData.jobIcon : null;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (lockedRoot != null)
        {
            lockedRoot.SetActive(!unlocked);
        }

        if (unlockedRoot != null)
        {
            unlockedRoot.SetActive(unlocked);
        }
    }
}
