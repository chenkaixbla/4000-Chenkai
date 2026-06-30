using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom inspector for <see cref="Idle_UI"/>. The normal fields draw with UI Toolkit
/// PropertyFields (so EditorAttributes still work), then an embedded IMGUI table lists the active
/// data source's jobs with the <see cref="Idle_Card"/> prefab used for each. Jobs disabled on the
/// Job_Manager are struck through. Auto-refreshes on select / data-source change / project change.
/// </summary>
[CustomEditor(typeof(Idle_UI))]
public class Idle_UIEditor : Editor
{
    static readonly float[] Columns = { 0f, 220f }; // Job (flexible), Idle Card (fixed)

    List<Job_Data> _activeJobs = new();
    IMGUIContainer _table;

    void OnEnable()
    {
        Catalog_DataSettings.OnDataSourceChanged += HandleChange;
        EditorApplication.projectChanged += HandleChange;
        RefreshActiveJobs();
    }

    void OnDisable()
    {
        Catalog_DataSettings.OnDataSourceChanged -= HandleChange;
        EditorApplication.projectChanged -= HandleChange;
    }

    void HandleChange()
    {
        RefreshActiveJobs();
        _table?.MarkDirtyRepaint();
    }

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();

        // Default fields via PropertyField (EditorAttributes render here), minus the per-job
        // card list, which the table edits.
        SerializedProperty p = serializedObject.GetIterator();
        if (p.NextVisible(true))
        {
            do
            {
                if (p.name == "m_Script" || p.name == "jobCards")
                    continue;

                root.Add(new PropertyField(p.Copy()));
            }
            while (p.NextVisible(false));
        }

        _table = new IMGUIContainer(DrawJobCardTable);
        _table.style.marginTop = 6;
        root.Add(_table);
        return root;
    }

    void DrawJobCardTable()
    {
        EditorTableGUI.DrawTitle("Job Cards");
        EditorTableGUI.DrawHeader(Columns, new[] { "Job", "Idle Card" });

        if (_activeJobs.Count == 0)
        {
            EditorGUILayout.HelpBox("No jobs found for the active data source (set in the Catalog Spreadsheet).", MessageType.Info);
            return;
        }

        Idle_UI idleUI = (Idle_UI)target;
        Job_Manager jobManager = Object.FindFirstObjectByType<Job_Manager>();

        for (int i = 0; i < _activeJobs.Count; i++)
        {
            Job_Data job = _activeJobs[i];
            if (job == null)
                continue;

            bool disabled = jobManager != null && !jobManager.IsJobEnabled(job);
            string label = string.IsNullOrWhiteSpace(job.jobName) ? job.name : job.jobName;

            Rect[] cells = EditorTableGUI.DrawRow(Columns, i);
            EditorTableGUI.LabelCell(cells[0], label);

            Idle_Card current = idleUI.GetJobCard(job);
            Idle_Card next = (Idle_Card)EditorGUI.ObjectField(cells[1], current, typeof(Idle_Card), false);
            if (next != current)
            {
                Undo.RecordObject(idleUI, "Set Job Card");
                idleUI.SetJobCard(job, next);
                EditorUtility.SetDirty(idleUI);
            }

            if (disabled)
                EditorTableGUI.StrikeRow();
        }
    }

    void RefreshActiveJobs()
    {
        _activeJobs = GameData_Editor.GatherActiveJobs();
    }
}
