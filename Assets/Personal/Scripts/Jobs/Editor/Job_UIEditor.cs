using System.Collections.Generic;
using EditorAttributes.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom inspector for <see cref="Job_UI"/>. Inherits EditorAttributes' <see cref="EditorExtension"/>
/// (not plain Editor) so the script's EditorAttributes - including the <c>[Button] Build</c> - keep
/// rendering; we just append an IMGUI table below. The table lists the active data source's jobs
/// with, per job, the menu its button opens and whether the shared job bar shows while viewing it.
/// Jobs disabled on the Job_Manager are struck through. Auto-refreshes on select / data-source /
/// project change.
/// </summary>
[CustomEditor(typeof(Job_UI))]
public class Job_UIEditor : EditorExtension
{
    static readonly float[] Columns = { 0f, 150f, 64f }; // Job (flexible), Menu (fixed), Job Bar (fixed)

    List<Job_Data> _activeJobs = new();
    IMGUIContainer _table;

    protected override void OnEnable()
    {
        base.OnEnable(); // EditorExtension loads [Button] data here
        Catalog_DataSettings.OnDataSourceChanged += HandleChange;
        EditorApplication.projectChanged += HandleChange;
        RefreshActiveJobs();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
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
        // Full EditorAttributes inspector (fields, [ShowInInspector], buttons), then our table.
        VisualElement root = base.CreateInspectorGUI();

        _table = new IMGUIContainer(DrawJobBarTable);
        _table.style.marginTop = 6;
        root.Add(_table);
        return root;
    }

    void DrawJobBarTable()
    {
        EditorTableGUI.DrawTitle("Job Bar (per-job menu + visibility)");
        EditorTableGUI.DrawHeader(Columns, new[] { "Job", "Menu", "Job Bar" });

        if (_activeJobs.Count == 0)
        {
            EditorGUILayout.HelpBox("No jobs found for the active data source (set in the Catalog Spreadsheet).", MessageType.Info);
            return;
        }

        Job_UI jobUI = (Job_UI)target;
        Job_Manager jobManager = Object.FindFirstObjectByType<Job_Manager>();

        // Menu options: "(Default)" first (stored as empty), then every menu name.
        string[] menuNames = jobUI.GetMenuNames();
        string[] options = new string[menuNames.Length + 1];
        options[0] = "(Default)";
        for (int i = 0; i < menuNames.Length; i++)
            options[i + 1] = menuNames[i];

        for (int i = 0; i < _activeJobs.Count; i++)
        {
            Job_Data job = _activeJobs[i];
            if (job == null)
                continue;

            bool disabled = jobManager != null && !jobManager.IsJobEnabled(job);
            string label = string.IsNullOrWhiteSpace(job.jobName) ? job.name : job.jobName;

            Rect[] cells = EditorTableGUI.DrawRow(Columns, i);
            EditorTableGUI.LabelCell(cells[0], label);

            // Menu dropdown (index 0 = default / empty).
            string currentMenu = jobUI.GetJobMenu(job);
            int selected = IndexOfMenu(menuNames, currentMenu);
            int next = EditorGUI.Popup(cells[1], selected, options);
            if (next != selected)
            {
                Undo.RecordObject(jobUI, "Set Job Menu");
                jobUI.SetJobMenu(job, next == 0 ? string.Empty : menuNames[next - 1]);
                EditorUtility.SetDirty(jobUI);
            }

            // Show-job-bar toggle.
            bool enabled = jobUI.IsJobBarEnabled(job);
            bool nextEnabled = EditorGUI.Toggle(cells[2], enabled);
            if (nextEnabled != enabled)
            {
                Undo.RecordObject(jobUI, "Toggle Job Bar");
                jobUI.SetJobBarEnabled(job, nextEnabled);
                EditorUtility.SetDirty(jobUI);
            }

            if (disabled)
                EditorTableGUI.StrikeRow();
        }
    }

    // Popup index for a stored menu name: 0 = default/empty, else its position + 1.
    static int IndexOfMenu(string[] menuNames, string menu)
    {
        if (string.IsNullOrWhiteSpace(menu))
            return 0;

        for (int i = 0; i < menuNames.Length; i++)
        {
            if (string.Equals(menuNames[i]?.Trim(), menu.Trim(), System.StringComparison.OrdinalIgnoreCase))
                return i + 1;
        }

        return 0; // unknown menu (e.g. renamed) falls back to default
    }

    void RefreshActiveJobs()
    {
        _activeJobs = GameData_Editor.GatherActiveJobs();
    }
}
