using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public class JobManager : MonoBehaviour
{
    [Title("UI")]
    public Transform jobElementContainer;
    public JobElement jobElementPrefab;

    [Title("References")]
    public IdleCardsView idleCardsView;
    public IdleManager idleManager;

    public List<JobData> jobDatas = new();

    void Start()
    {
        SpawnAllJobs();
    }

    void SpawnAllJobs()
    {
        foreach (JobData data in jobDatas)
        {
            JobElement element = Instantiate(jobElementPrefab, jobElementContainer);
            element.gameObject.name = data.jobName + " Element";
            element.displayNameText.text = data.jobName;
            element.iconImage.sprite = data.jobIcon;

            JobInstance instance = element.jobInstance;
            if(instance != null)
            {
                instance.jobData = data;
                SetupIdleInstances(instance);

                element.button.onClick.AddListener(() => idleCardsView.UpdateView(instance));
            }
        }
    }

    void SetupIdleInstances(JobInstance jobInstance)
    {
        foreach (IdleData idleData in jobInstance.jobData.idleDatas)
        {
            IdleInstance idleInstance = new IdleInstance(idleData);
            jobInstance.idleInstances.Add(idleInstance);    // Add to job instance's list
            idleManager.idleInstances.Add(idleInstance);    // Add to manager's list for updating
        }
    }
}
