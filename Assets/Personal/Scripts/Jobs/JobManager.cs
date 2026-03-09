using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public class JobManager : MonoBehaviour
{
    public Transform jobContainer;
    public JobElement jobElementPrefab;

    [Line]

    public IdleCardsView idleCardsView;
    public IdleManager idleManager;
    public CardViewManager cardViewManager;
    public int idleViewIndex = 0;

    public List<JobData> jobDatas = new();

    ScrollViewData viewData;

    void Start()
    {
        viewData = cardViewManager.GetScrollViewData(idleViewIndex);
        SpawnAllJobs();
    }

    void SpawnAllJobs()
    {
        foreach (JobData data in jobDatas)
        {
            JobElement element = Instantiate(jobElementPrefab, jobContainer);
            print(element == null);
            element.gameObject.name = data.jobName + " Element";
            element.displayNameText.text = data.jobName;
            element.iconImage.sprite = data.jobIcon;

            JobInstance instance = element.jobInstance;
            if(instance != null)
            {
                instance.jobData = data;
                SetupIdleInstances(instance);

                element.button.onClick.AddListener( () => OnClickJob(idleCardsView, instance) );
            }
        }
    }

    void OnClickJob(IdleCardsView cardsView, JobInstance instance)
    {
        cardViewManager.ShowScrollView(idleViewIndex);
        cardsView.UpdateView(instance);
    }

    void SetupIdleInstances(JobInstance jobInstance)
    {
        foreach (IdleData idleData in jobInstance.jobData.idleDatas)
        {
            IdleInstance idleInstance = new IdleInstance(idleData, jobInstance);
            jobInstance.idleInstances.Add(idleInstance);    // Add to job instance's list
            idleManager.AddInstance(idleInstance);    // Add to manager's list for updating
        }
    }
}
