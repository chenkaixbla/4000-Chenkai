using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using UnityEngine.Serialization;

public class Job_Manager : MonoBehaviour
{
    public List<Job_Data> jobDatas = new();

    [Line]

    public Transform jobContainer;
    public Job_Button jobButtonPrefab;

    [Line]

    public Idle_Manager idleManager;

    void Awake()
    {
        idleManager ??= FindFirstObjectByType<Idle_Manager>();
    }

    void Start()
    {
        SpawnAllJobs();
    }

    void SpawnAllJobs()
    {
        if (jobButtonPrefab == null || jobContainer == null)
        {
            Debug.LogWarning($"{nameof(Job_Manager)} is missing required references.", this);
            return;
        }

        foreach (Job_Data data in jobDatas)
        {
            if (data == null)
            {
                continue;
            }

            Job_Button element = Instantiate(jobButtonPrefab, jobContainer);
            element.gameObject.name = data.jobName + " Element";

            if (element.displayNameText != null)
            {
                element.displayNameText.text = data.jobName;
            }

            if (element.iconImage != null)
            {
                element.iconImage.sprite = data.jobIcon;
            }

            Job_Instance instance = ResolveOrCreateJobInstance(element);
            if (instance == null)
            {
                continue;
            }

            instance.jobData = data;
            SetupIdleInstances(instance);

            if (element.button != null)
            {
                element.button.onClick.AddListener( () => OnClickJob(instance) );
            }
        }
    }

    void OnClickJob(Job_Instance instance)
    {

    }

    void SetupIdleInstances(Job_Instance jobInstance)
    {
        if (jobInstance == null || jobInstance.jobData == null)
        {
            return;
        }

        if (jobInstance.idleInstances == null)
        {
            jobInstance.idleInstances = new List<Idle_Instance>();
        }
        else
        {
            jobInstance.idleInstances.Clear();
        }

        if (jobInstance.jobData.idleDatas == null)
        {
            return;
        }

        foreach (Idle_Data idleData in jobInstance.jobData.idleDatas)
        {
            if (idleData == null)
            {
                continue;
            }

            Idle_Instance idleInstance = new Idle_Instance(idleData, jobInstance);
            jobInstance.idleInstances.Add(idleInstance);    // Add to job instance's list

            if (idleManager != null)
            {
                idleManager.AddInstance(idleInstance);    // Add to manager's list for updating
            }
        }
    }

    Job_Instance ResolveOrCreateJobInstance(Job_Button element)
    {
        if (element == null)
        {
            return null;
        }

        Job_Instance instance = element.jobInstance;
        if (instance == null)
        {
            instance = element.GetComponent<Job_Instance>();
            element.jobInstance = instance;
        }

        if (instance == null)
        {
            instance = element.gameObject.AddComponent<Job_Instance>();
            element.jobInstance = instance;
        }

        return instance;
    }
}
