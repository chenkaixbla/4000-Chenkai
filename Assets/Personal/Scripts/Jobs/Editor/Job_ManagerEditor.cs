using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom inspector for <see cref="Job_Manager"/>. The normal fields are drawn with UI Toolkit
/// PropertyFields (so EditorAttributes still work), and an embedded IMGUI table auto-lists the
/// active data source's jobs as "name + enabled toggle" rows (read by Job_UI). Disabled jobs are
/// struck through. The list refreshes on select / data-source change / project change.
/// </summary>
[CustomEditor(typeof(Job_Manager))]
public class Job_ManagerEditor : Editor
{
    static readonly float[] Columns = { 0f, 64f }; // Job (flexible), Enabled (fixed)

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

        // Default fields via PropertyField (EditorAttributes render here), minus the internal
        // toggle list, which the table edits.
        SerializedProperty p = serializedObject.GetIterator();
        if (p.NextVisible(true))
        {
            do
            {
                if (p.name == "m_Script" || p.name == "jobToggles")
                    continue;

                root.Add(new PropertyField(p.Copy()));
            }
            while (p.NextVisible(false));
        }

        _table = new IMGUIContainer(DrawJobTable);
        _table.style.marginTop = 6;
        root.Add(_table);
        return root;
    }

    void DrawJobTable()
    {
        EditorTableGUI.DrawTitle("Jobs (enable to spawn)");
        EditorTableGUI.DrawHeader(Columns, new[] { "Job", "Enabled" });

        if (_activeJobs.Count == 0)
        {
            EditorGUILayout.HelpBox("No jobs found for the active data source (set in the Catalog Spreadsheet).", MessageType.Info);
            return;
        }

        Job_Manager manager = (Job_Manager)target;
        for (int i = 0; i < _activeJobs.Count; i++)
        {
            Job_Data job = _activeJobs[i];
            if (job == null)
                continue;

            bool enabled = manager.IsJobEnabled(job);
            string label = string.IsNullOrWhiteSpace(job.jobName) ? job.name : job.jobName;

            Rect[] cells = EditorTableGUI.DrawRow(Columns, i);
            EditorTableGUI.LabelCell(cells[0], label);

            bool next = EditorGUI.Toggle(cells[1], enabled);
            if (next != enabled)
            {
                Undo.RecordObject(manager, "Toggle Job Enabled");
                manager.SetJobEnabled(job, next);
                EditorUtility.SetDirty(manager);
            }

            if (!next)
                EditorTableGUI.StrikeRow();
        }
    }

    void RefreshActiveJobs()
    {
        _activeJobs = GameData_Editor.GatherActiveJobs();
    }
}
