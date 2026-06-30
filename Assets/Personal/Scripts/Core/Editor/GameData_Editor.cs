using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-side data gathering that respects the Catalog_DataSettings source-of-truth rule: it reads
/// the active data source's folders rather than scanning the whole project. Shared by manager editors.
/// </summary>
public static class GameData_Editor
{
    /// <summary>Jobs in the active data source (via Catalog_DataSettings), so no real/testing duplicates.</summary>
    public static List<Job_Data> GatherActiveJobs()
    {
        List<Job_Data> jobs = new List<Job_Data>();

        Catalog_DataSettings settings = Catalog_DataSettings.Active;
        string folder = settings != null ? settings.ActiveDataFolder : "Assets/Personal/Data";

        string[] guids = AssetDatabase.IsValidFolder(folder)
            ? AssetDatabase.FindAssets("t:Job_Data", new[] { folder })
            : AssetDatabase.FindAssets("t:Job_Data");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Job_Data job = AssetDatabase.LoadAssetAtPath<Job_Data>(path);
            if (job != null)
                jobs.Add(job);
        }

        return jobs;
    }
}
