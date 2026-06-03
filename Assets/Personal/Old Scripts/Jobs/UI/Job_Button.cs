using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Job_Button : MonoBehaviour
{
    [ReadOnly, SerializeField] Job_Data assignedJobData;

    [Title("UI")]
    public Button button;
    public TMP_Text displayNameText;
    public Image iconImage;
    public Job_ButtonView view;

    Job_Manager manager;

    public Job_Data AssignedJobData => assignedJobData;

    void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }

    void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }

    public void Bind(Job_Data jobData, Job_Manager ownerManager, bool unlocked)
    {
        assignedJobData = jobData;
        manager = ownerManager;

        string jobName = jobData != null && !string.IsNullOrWhiteSpace(jobData.jobName) ? jobData.jobName : "-";

        if (displayNameText != null)
        {
            displayNameText.text = jobName;
        }

        if (iconImage != null)
        {
            iconImage.sprite = jobData != null ? jobData.jobIcon : null;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (view != null)
        {
            view.Bind(jobData, unlocked);
        }

        if (button != null)
        {
            button.interactable = unlocked && jobData != null;
        }
    }

    void OnButtonClick()
    {
        if (assignedJobData == null || manager == null)
        {
            return;
        }

        manager.StartJob(assignedJobData);
    }
}
