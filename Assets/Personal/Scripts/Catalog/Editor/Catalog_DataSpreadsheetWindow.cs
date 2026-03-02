using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Catalog_DataSpreadsheetWindow : EditorWindow
{
    private enum Catalog_Tab
    {
        Jobs,
        Items,
        Idles
    }

    private enum Catalog_ItemFilter
    {
        All = -1,
        Resource = (int)Catalog_ItemType.Resource,
        Weapon = (int)Catalog_ItemType.Weapon,
        Armor = (int)Catalog_ItemType.Armor,
        Food = (int)Catalog_ItemType.Food
    }

    private enum JobIdleRowAction
    {
        None,
        Remove,
        Move
    }

    private const string SettingsAssetPath = "Assets/Personal/Scripts/Catalog/Catalog_DataSettings.asset";
    private const string StretchTablesPrefKey = "Catalog_DataSpreadsheetWindow.StretchTables";
    private const float ObjectCellWidth = 220f;
    private static readonly float[] JobsTableBaseWidths = { 30f, ObjectCellWidth, 160f, 90f, 80f, 80f, 72f, 24f };
    private static readonly float[] JobIdleTableBaseWidths = { 30f, ObjectCellWidth, 150f, 80f, 80f, 80f, 90f, 300f, 46f, 24f };
    private static readonly float[] ItemsTableBaseWidths = { 30f, ObjectCellWidth, 100f, 70f, 150f, 260f, 80f, 90f, 24f };
    private static readonly float[] IdleAssetsTableBaseWidths = { 30f, ObjectCellWidth, 140f, 75f, 75f, 75f, 85f, 220f, 200f, 24f };

    private readonly List<JobData> _jobRows = new();
    private readonly List<ItemsData> _itemRows = new();
    private readonly List<IdleData> _idleRows = new();
    private readonly Dictionary<int, IdleData> _pendingIdleAdds = new();
    private readonly HashSet<int> _selectedJobIds = new();
    private readonly HashSet<int> _selectedItemIds = new();
    private readonly HashSet<int> _selectedIdleAssetIds = new();
    private readonly Dictionary<int, HashSet<int>> _selectedIdleIds = new();
    private readonly HashSet<int> _expandedJobRows = new();
    private readonly Dictionary<int, HashSet<int>> _expandedJobIdleFinishActionRows = new();
    private readonly HashSet<int> _expandedIdleAssetFinishActionRows = new();

    private Catalog_DataSettings _settings;
    private JobData _idleCategoryFilterJob;

    private Catalog_Tab _activeTab;
    private Catalog_ItemFilter _itemFilter = Catalog_ItemFilter.All;
    private Vector2 _jobsTableScroll;
    private Vector2 _itemsScroll;
    private Vector2 _idleScroll;
    private bool _stretchTables = true;

    private JobData _pendingJobToAdd;
    private ItemsData _pendingItemToAdd;
    private IdleData _pendingIdleToAdd;

    [MenuItem("Tools/Catalog/Data Spreadsheet")]
    public static void OpenWindow()
    {
        Catalog_DataSpreadsheetWindow window = GetWindow<Catalog_DataSpreadsheetWindow>("Catalog Spreadsheet");
        window.minSize = new Vector2(1200f, 500f);
        window.Show();
    }

    public static void RefreshAllOpenWindows()
    {
        Catalog_DataSpreadsheetWindow[] windows = Resources.FindObjectsOfTypeAll<Catalog_DataSpreadsheetWindow>();
        foreach (Catalog_DataSpreadsheetWindow window in windows)
        {
            window.RefreshRowsFromFolders();
            window.Repaint();
        }
    }

    private void OnEnable()
    {
        LoadOrCreateSettings();
        _stretchTables = EditorPrefs.GetBool(StretchTablesPrefKey, true);
        RefreshRowsFromFolders();
    }

    private void OnProjectChange()
    {
        RefreshRowsFromFolders();
        Repaint();
    }

    private void OnFocus()
    {
        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();

        switch (_activeTab)
        {
            case Catalog_Tab.Jobs:
                DrawJobsCategory();
                break;
            case Catalog_Tab.Items:
                _itemsScroll = EditorGUILayout.BeginScrollView(_itemsScroll, !_stretchTables, true);
                DrawItemsCategory();
                EditorGUILayout.EndScrollView();
                break;
            case Catalog_Tab.Idles:
                _idleScroll = EditorGUILayout.BeginScrollView(_idleScroll, !_stretchTables, true);
                DrawIdleCategory();
                EditorGUILayout.EndScrollView();
                break;
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        _activeTab = (Catalog_Tab)GUILayout.Toolbar((int)_activeTab, new[] { "Jobs", "Items", "Idles" }, EditorStyles.toolbarButton);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Settings", EditorStyles.toolbarButton, GUILayout.Width(70f)))
        {
            Catalog_DataSettingsWindow.OpenWindow();
        }

        bool nextStretchTables = GUILayout.Toggle(_stretchTables, "Stretch Tables", EditorStyles.toolbarButton, GUILayout.Width(98f));
        if (nextStretchTables != _stretchTables)
        {
            _stretchTables = nextStretchTables;
            EditorPrefs.SetBool(StretchTablesPrefKey, _stretchTables);
            Repaint();
        }

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
        {
            RefreshRowsFromFolders();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawJobsCategory()
    {
        float tableWidth = Mathf.Max(320f, position.width - 24f);
        _jobsTableScroll = EditorGUILayout.BeginScrollView(_jobsTableScroll, !_stretchTables, true);
        float[] jobColumnWidths = GetTableColumnWidths(tableWidth, JobsTableBaseWidths);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Jobs Category", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _pendingJobToAdd = (JobData)EditorGUILayout.ObjectField("Add Existing Job", _pendingJobToAdd, typeof(JobData), false);
        using (new EditorGUI.DisabledScope(_pendingJobToAdd == null))
        {
            if (GUILayout.Button("Add", GUILayout.Width(70f)))
            {
                AddUnique(_jobRows, _pendingJobToAdd);
                _pendingJobToAdd = null;
            }
        }

        if (GUILayout.Button("New JobData", GUILayout.Width(110f)))
        {
            JobData created = CreateDataAsset<JobData>(GetSettingsFolderPath(_settings?.jobsDataFolder), "JobData");
            AddUnique(_jobRows, created);
        }
        EditorGUILayout.EndHorizontal();

        DrawDropZone<JobData>(
            "Drop JobData assets here",
            droppedJob =>
            {
                AddUnique(_jobRows, droppedJob);
            });

        DrawJobsBulkActions();
        DrawJobsHeader(jobColumnWidths);

        int removeIndex = -1;
        for (int i = 0; i < _jobRows.Count; i++)
        {
            if (DrawJobRow(i, jobColumnWidths, tableWidth))
            {
                removeIndex = i;
            }
        }

        if (removeIndex >= 0)
        {
            _jobRows.RemoveAt(removeIndex);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawJobsHeader(float[] widths)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        DrawHeaderCell("Sel", widths[0]);
        DrawHeaderCell("JobData", widths[1]);
        DrawHeaderCell("Job Name", widths[2]);
        DrawHeaderCell("Icon", widths[3]);
        DrawHeaderCell("Max XP", widths[4]);
        DrawHeaderCell("Idle Count", widths[5]);
        DrawHeaderCell("Expand", widths[6]);
        DrawHeaderCell("X", widths[7]);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawJobsBulkActions()
    {
        PruneSelection(_selectedJobIds, _jobRows);

        int selectedCount = _selectedJobIds.Count;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Select All", GUILayout.Width(84f)))
        {
            foreach (JobData jobData in _jobRows)
            {
                if (jobData != null)
                {
                    _selectedJobIds.Add(jobData.GetInstanceID());
                }
            }
        }

        if (GUILayout.Button("Clear", GUILayout.Width(56f)))
        {
            _selectedJobIds.Clear();
        }

        using (new EditorGUI.DisabledScope(selectedCount == 0))
        {
            if (GUILayout.Button("Delete Selected", GUILayout.Width(112f)))
            {
                List<JobData> selectedJobs = GetSelectedObjects(_jobRows, _selectedJobIds);
                if (DeleteAssetsWithConfirmation(selectedJobs, "JobData"))
                {
                    HashSet<int> deletedIds = new();
                    foreach (JobData jobData in selectedJobs)
                    {
                        if (jobData != null)
                        {
                            deletedIds.Add(jobData.GetInstanceID());
                        }
                    }

                    _jobRows.RemoveAll(jobData => jobData == null || deletedIds.Contains(jobData.GetInstanceID()));
                    _selectedJobIds.ExceptWith(deletedIds);

                    foreach (int deletedId in deletedIds)
                    {
                        _expandedJobRows.Remove(deletedId);
                        _pendingIdleAdds.Remove(deletedId);
                        _selectedIdleIds.Remove(deletedId);
                        _expandedJobIdleFinishActionRows.Remove(deletedId);
                    }
                }
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"Selected: {selectedCount}", GUILayout.Width(90f));

        EditorGUILayout.EndHorizontal();
    }

    private bool DrawJobRow(int rowIndex, float[] widths, float tableWidth)
    {
        JobData row = _jobRows[rowIndex];
        int rowId = row != null ? row.GetInstanceID() : 0;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        bool selected = row != null && _selectedJobIds.Contains(rowId);
        bool nextSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(widths[0]));
        if (row != null && nextSelected != selected)
        {
            SetSelection(_selectedJobIds, rowId, nextSelected);
        }

        EditorGUI.BeginChangeCheck();
        JobData changedRow = (JobData)EditorGUILayout.ObjectField(row, typeof(JobData), false, GUILayout.Width(widths[1]));
        if (EditorGUI.EndChangeCheck())
        {
            if (row != null && changedRow != row)
            {
                _selectedJobIds.Remove(rowId);
                _expandedJobRows.Remove(rowId);
                _pendingIdleAdds.Remove(row.GetInstanceID());
                _selectedIdleIds.Remove(row.GetInstanceID());
                _expandedJobIdleFinishActionRows.Remove(row.GetInstanceID());
            }

            _jobRows[rowIndex] = changedRow;
            row = changedRow;
            rowId = row != null ? row.GetInstanceID() : 0;
        }

        if (row == null)
        {
            float emptyWidth = widths[2] + widths[3] + widths[4] + widths[5];
            GUILayout.Label("Missing", GUILayout.Width(emptyWidth));
            GUILayout.Space(widths[6]);
            bool removeEmpty = GUILayout.Button("X", GUILayout.Width(widths[7]));
            EditorGUILayout.EndHorizontal();
            return removeEmpty;
        }

        SerializedObject serializedRow = new SerializedObject(row);
        serializedRow.Update();

        DrawCompactProperty(serializedRow.FindProperty("jobName"), widths[2]);
        DrawCompactProperty(serializedRow.FindProperty("jobIcon"), widths[3]);
        DrawCompactProperty(serializedRow.FindProperty("maxXP"), widths[4]);

        SerializedProperty idleDatasProperty = serializedRow.FindProperty("idleDatas");
        EditorGUILayout.LabelField(idleDatasProperty != null ? idleDatasProperty.arraySize.ToString() : "-", GUILayout.Width(widths[5]));

        bool expanded = _expandedJobRows.Contains(rowId);
        if (GUILayout.Button(expanded ? "Hide" : "Show", GUILayout.Width(widths[6])))
        {
            if (expanded)
            {
                _expandedJobRows.Remove(rowId);
            }
            else
            {
                _expandedJobRows.Add(rowId);
            }
        }

        bool removeRow = GUILayout.Button("X", GUILayout.Width(widths[7]));
        if (removeRow)
        {
            removeRow = DeleteAssetsWithConfirmation(new List<JobData> { row }, "JobData");
            if (removeRow)
            {
                _selectedJobIds.Remove(rowId);
                _expandedJobRows.Remove(rowId);
                _pendingIdleAdds.Remove(rowId);
                _selectedIdleIds.Remove(rowId);
                _expandedJobIdleFinishActionRows.Remove(rowId);
            }
        }

        if (serializedRow.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(row);
        }

        EditorGUILayout.EndHorizontal();

        if (_expandedJobRows.Contains(rowId))
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(24f);
            EditorGUILayout.BeginVertical();
            DrawIdleTable(row, Mathf.Max(260f, tableWidth - 36f));
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        return removeRow;
    }

    private void DrawIdleTable(JobData jobData, float tableWidth)
    {
        EnsureIdleList(jobData);
        HashSet<int> selectedIdleIds = GetIdleSelectionSet(jobData);
        PruneIdleSelection(jobData, selectedIdleIds);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("IdleData Table", EditorStyles.boldLabel);

        int jobId = jobData.GetInstanceID();
        _pendingIdleAdds.TryGetValue(jobId, out IdleData pendingIdleData);

        EditorGUILayout.BeginHorizontal();
        pendingIdleData = (IdleData)EditorGUILayout.ObjectField("Add Existing Idle", pendingIdleData, typeof(IdleData), false);
        using (new EditorGUI.DisabledScope(pendingIdleData == null))
        {
            if (GUILayout.Button("Add", GUILayout.Width(70f)))
            {
                AddIdleToJob(jobData, pendingIdleData);
                pendingIdleData = null;
            }
        }

        if (GUILayout.Button("New IdleData", GUILayout.Width(110f)))
        {
            IdleData created = CreateDataAsset<IdleData>(GetSettingsFolderPath(_settings?.idleDataFolder), "IdleData");
            AddIdleToJob(jobData, created);
        }
        EditorGUILayout.EndHorizontal();

        _pendingIdleAdds[jobId] = pendingIdleData;

        DrawDropZone<IdleData>(
            "Drop IdleData assets here for this job",
            droppedIdleData =>
            {
                AddIdleToJob(jobData, droppedIdleData);
            });

        DrawIdleBulkActions(jobData, selectedIdleIds);
        float[] columnWidths = GetTableColumnWidths(tableWidth, JobIdleTableBaseWidths);
        DrawIdleHeader(columnWidths);

        int removeIndex = -1;
        int moveFromIndex = -1;
        int moveToIndex = -1;
        for (int i = 0; i < jobData.idleDatas.Count; i++)
        {
            JobIdleRowAction rowAction = DrawIdleRow(jobData, i, selectedIdleIds, columnWidths, out int targetIndex);
            if (rowAction == JobIdleRowAction.Remove)
            {
                removeIndex = i;
                break;
            }

            if (rowAction == JobIdleRowAction.Move)
            {
                moveFromIndex = i;
                moveToIndex = targetIndex;
                break;
            }
        }

        if (moveFromIndex >= 0)
        {
            MoveIdleRow(jobData, moveFromIndex, moveToIndex);
        }
        else if (removeIndex >= 0)
        {
            IdleData removedIdleData = jobData.idleDatas[removeIndex];
            if (removedIdleData != null)
            {
                selectedIdleIds.Remove(removedIdleData.GetInstanceID());
            }

            Undo.RecordObject(jobData, "Remove IdleData");
            jobData.idleDatas.RemoveAt(removeIndex);
            EditorUtility.SetDirty(jobData);
            GetJobIdleFinishActionRows(jobData).Clear();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawIdleHeader(float[] widths)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        DrawHeaderCell("Sel", widths[0]);
        DrawHeaderCell("IdleData", widths[1]);
        DrawHeaderCell("Display Name", widths[2]);
        DrawHeaderCell("Interval", widths[3]);
        DrawHeaderCell("XP Reward", widths[4]);
        DrawHeaderCell("Max XP", widths[5]);
        DrawHeaderCell("Icon", widths[6]);
        DrawHeaderCell("Rules", widths[7]);
        DrawHeaderCell("Move", widths[8]);
        DrawHeaderCell("X", widths[9]);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawIdleBulkActions(JobData jobData, HashSet<int> selectedIdleIds)
    {
        int selectedCount = selectedIdleIds.Count;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Select All", GUILayout.Width(84f)))
        {
            foreach (IdleData idleData in jobData.idleDatas)
            {
                if (idleData != null)
                {
                    selectedIdleIds.Add(idleData.GetInstanceID());
                }
            }
        }

        if (GUILayout.Button("Clear", GUILayout.Width(56f)))
        {
            selectedIdleIds.Clear();
        }

        using (new EditorGUI.DisabledScope(selectedCount == 0))
        {
            if (GUILayout.Button("Remove Selected", GUILayout.Width(116f)))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Remove Selected IdleData",
                    $"Remove {selectedCount} selected IdleData reference(s) from '{jobData.name}'?",
                    "Remove",
                    "Cancel");

                if (confirmed)
                {
                    Undo.RecordObject(jobData, "Remove Selected IdleData");
                    jobData.idleDatas.RemoveAll(idleData => idleData != null && selectedIdleIds.Contains(idleData.GetInstanceID()));
                    EditorUtility.SetDirty(jobData);
                    GetJobIdleFinishActionRows(jobData).Clear();
                    selectedIdleIds.Clear();
                }
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"Selected: {selectedCount}", GUILayout.Width(90f));

        EditorGUILayout.EndHorizontal();
    }

    private JobIdleRowAction DrawIdleRow(JobData jobData, int index, HashSet<int> selectedIdleIds, float[] widths, out int targetIndex)
    {
        targetIndex = index;
        HashSet<int> expandedFinishActions = GetJobIdleFinishActionRows(jobData);
        IdleData row = jobData.idleDatas[index];
        int rowId = row != null ? row.GetInstanceID() : 0;
        int finishActionRowKey = index;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        bool selected = row != null && selectedIdleIds.Contains(rowId);
        bool nextSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(widths[0]));
        if (row != null && nextSelected != selected)
        {
            SetSelection(selectedIdleIds, rowId, nextSelected);
        }

        EditorGUI.BeginChangeCheck();
        IdleData changedRow = (IdleData)EditorGUILayout.ObjectField(row, typeof(IdleData), false, GUILayout.Width(widths[1]));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(jobData, "Change IdleData Reference");
            jobData.idleDatas[index] = changedRow;
            EditorUtility.SetDirty(jobData);
            if (row != null && changedRow != row)
            {
                selectedIdleIds.Remove(rowId);
                expandedFinishActions.Remove(finishActionRowKey);
            }
            row = changedRow;
            rowId = row != null ? row.GetInstanceID() : 0;
        }

        if (row == null)
        {
            float emptyWidth = 0f;
            for (int i = 2; i < widths.Length - 1; i++)
            {
                emptyWidth += widths[i];
            }

            GUILayout.Label("Missing", GUILayout.Width(emptyWidth));
            bool removeNull = GUILayout.Button("X", GUILayout.Width(widths[9]));
            EditorGUILayout.EndHorizontal();
            expandedFinishActions.Remove(finishActionRowKey);
            return removeNull ? JobIdleRowAction.Remove : JobIdleRowAction.None;
        }

        SerializedObject serializedRow = new SerializedObject(row);
        serializedRow.Update();

        DrawCompactProperty(serializedRow.FindProperty("displayName"), widths[2]);
        DrawCompactProperty(serializedRow.FindProperty("interval"), widths[3]);
        DrawCompactProperty(serializedRow.FindProperty("xpReward"), widths[4]);
        DrawCompactProperty(serializedRow.FindProperty("maxXP"), widths[5]);
        DrawCompactProperty(serializedRow.FindProperty("icon"), widths[6]);

        SerializedProperty finishActionsProperty = serializedRow.FindProperty("finishActions");
        SerializedProperty startConditionsProperty = serializedRow.FindProperty("startConditions");
        bool isExpanded = expandedFinishActions.Contains(finishActionRowKey);
        EditorGUILayout.BeginHorizontal(GUILayout.Width(widths[7]));
        bool nextExpanded = isExpanded;
        if (GUILayout.Button(isExpanded ? "v" : ">", EditorStyles.miniButton, GUILayout.Width(16f)))
        {
            nextExpanded = !isExpanded;
        }

        EditorGUILayout.LabelField(GetIdleRulesLabel(finishActionsProperty, startConditionsProperty), GUILayout.Width(Mathf.Max(0f, widths[7] - 16f)));
        EditorGUILayout.EndHorizontal();

        if (nextExpanded != isExpanded)
        {
            if (nextExpanded)
            {
                expandedFinishActions.Add(finishActionRowKey);
            }
            else
            {
                expandedFinishActions.Remove(finishActionRowKey);
            }
        }

        bool moveUp = false;
        bool moveDown = false;
        using (new EditorGUILayout.HorizontalScope(GUILayout.Width(widths[8])))
        {
            using (new EditorGUI.DisabledScope(index <= 0))
            {
                moveUp = GUILayout.Button("^", EditorStyles.miniButtonLeft);
            }

            using (new EditorGUI.DisabledScope(index >= jobData.idleDatas.Count - 1))
            {
                moveDown = GUILayout.Button("v", EditorStyles.miniButtonRight);
            }
        }

        bool removeRow = GUILayout.Button("X", GUILayout.Width(widths[9]))
            && EditorUtility.DisplayDialog(
                "Remove IdleData Reference",
                $"Remove '{row.name}' from '{jobData.name}' idle list?",
                "Remove",
                "Cancel");

        if (removeRow)
        {
            selectedIdleIds.Remove(rowId);
            expandedFinishActions.Remove(finishActionRowKey);
        }

        bool drawFinishActionDetails = expandedFinishActions.Contains(finishActionRowKey);
        if (drawFinishActionDetails)
        {
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(GetFinishActionIndentWidth(widths, 7));
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(Mathf.Max(180f, widths[7] + widths[8] + widths[9]))))
            {
                DrawIdleRulesDetails(finishActionsProperty, startConditionsProperty);
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.EndHorizontal();
        }

        if (serializedRow.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(row);
        }

        if (removeRow)
        {
            return JobIdleRowAction.Remove;
        }

        if (moveUp)
        {
            targetIndex = index - 1;
            return JobIdleRowAction.Move;
        }

        if (moveDown)
        {
            targetIndex = index + 1;
            return JobIdleRowAction.Move;
        }

        return JobIdleRowAction.None;
    }

    private void DrawItemsCategory()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Items Category", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _itemFilter = (Catalog_ItemFilter)EditorGUILayout.EnumPopup("Item Filter", _itemFilter, GUILayout.Width(250f));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _pendingItemToAdd = (ItemsData)EditorGUILayout.ObjectField("Add Existing Item", _pendingItemToAdd, typeof(ItemsData), false);
        using (new EditorGUI.DisabledScope(_pendingItemToAdd == null))
        {
            if (GUILayout.Button("Add", GUILayout.Width(70f)))
            {
                AddUnique(_itemRows, _pendingItemToAdd);
                _pendingItemToAdd = null;
            }
        }

        if (GUILayout.Button("New ItemsData", GUILayout.Width(110f)))
        {
            ItemsData created = CreateDataAsset<ItemsData>(GetSettingsFolderPath(_settings?.itemsDataFolder), "ItemsData");
            AddUnique(_itemRows, created);
        }
        EditorGUILayout.EndHorizontal();

        DrawDropZone<ItemsData>(
            "Drop ItemsData assets here",
            droppedItem =>
            {
                AddUnique(_itemRows, droppedItem);
            });

        DrawItemsBulkActions();
        float[] itemColumnWidths = GetTableColumnWidths(position.width - 28f, ItemsTableBaseWidths);
        DrawItemsHeader(itemColumnWidths);

        int removeIndex = -1;
        for (int i = 0; i < _itemRows.Count; i++)
        {
            ItemsData row = _itemRows[i];
            if (row != null && !PassesItemFilter(row))
            {
                continue;
            }

            if (DrawItemRow(i, itemColumnWidths))
            {
                removeIndex = i;
            }
        }

        if (removeIndex >= 0)
        {
            _itemRows.RemoveAt(removeIndex);
        }
    }

    private void DrawItemsHeader(float[] widths)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        DrawHeaderCell("Sel", widths[0]);
        DrawHeaderCell("ItemsData", widths[1]);
        DrawHeaderCell("Type", widths[2]);
        DrawHeaderCell("ID", widths[3]);
        DrawHeaderCell("Display Name", widths[4]);
        DrawHeaderCell("Description", widths[5]);
        DrawHeaderCell("Price", widths[6]);
        DrawHeaderCell("Icon", widths[7]);
        DrawHeaderCell("X", widths[8]);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawItemsBulkActions()
    {
        PruneSelection(_selectedItemIds, _itemRows);

        int selectedCount = _selectedItemIds.Count;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Select All", GUILayout.Width(84f)))
        {
            foreach (ItemsData itemData in _itemRows)
            {
                if (itemData != null)
                {
                    _selectedItemIds.Add(itemData.GetInstanceID());
                }
            }
        }

        if (GUILayout.Button("Clear", GUILayout.Width(56f)))
        {
            _selectedItemIds.Clear();
        }

        using (new EditorGUI.DisabledScope(selectedCount == 0))
        {
            if (GUILayout.Button("Delete Selected", GUILayout.Width(112f)))
            {
                List<ItemsData> selectedItems = GetSelectedObjects(_itemRows, _selectedItemIds);
                if (DeleteAssetsWithConfirmation(selectedItems, "ItemsData"))
                {
                    HashSet<int> deletedIds = new();
                    foreach (ItemsData itemData in selectedItems)
                    {
                        if (itemData != null)
                        {
                            deletedIds.Add(itemData.GetInstanceID());
                        }
                    }

                    _itemRows.RemoveAll(itemData => itemData == null || deletedIds.Contains(itemData.GetInstanceID()));
                    _selectedItemIds.ExceptWith(deletedIds);
                }
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"Selected: {selectedCount}", GUILayout.Width(90f));

        EditorGUILayout.EndHorizontal();
    }

    private bool DrawItemRow(int rowIndex, float[] widths)
    {
        ItemsData row = _itemRows[rowIndex];
        int rowId = row != null ? row.GetInstanceID() : 0;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        bool selected = row != null && _selectedItemIds.Contains(rowId);
        bool nextSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(widths[0]));
        if (row != null && nextSelected != selected)
        {
            SetSelection(_selectedItemIds, rowId, nextSelected);
        }

        EditorGUI.BeginChangeCheck();
        ItemsData changedRow = (ItemsData)EditorGUILayout.ObjectField(row, typeof(ItemsData), false, GUILayout.Width(widths[1]));
        if (EditorGUI.EndChangeCheck())
        {
            if (row != null && changedRow != row)
            {
                _selectedItemIds.Remove(rowId);
            }

            _itemRows[rowIndex] = changedRow;
            row = changedRow;
            rowId = row != null ? row.GetInstanceID() : 0;
        }

        if (row == null)
        {
            float emptyWidth = widths[2] + widths[3] + widths[4] + widths[5] + widths[6] + widths[7];
            GUILayout.Label("Missing", GUILayout.Width(emptyWidth));
            bool removeEmpty = GUILayout.Button("X", GUILayout.Width(widths[8]));
            EditorGUILayout.EndHorizontal();
            return removeEmpty;
        }

        SerializedObject serializedRow = new SerializedObject(row);
        serializedRow.Update();

        DrawCompactProperty(serializedRow.FindProperty("itemType"), widths[2]);
        DrawCompactProperty(serializedRow.FindProperty("itemID"), widths[3]);
        DrawCompactProperty(serializedRow.FindProperty("displayName"), widths[4]);
        DrawCompactProperty(serializedRow.FindProperty("itemDescriptions"), widths[5]);
        DrawCompactProperty(serializedRow.FindProperty("price"), widths[6]);
        DrawCompactProperty(serializedRow.FindProperty("icon"), widths[7]);

        bool removeRow = GUILayout.Button("X", GUILayout.Width(widths[8]));
        if (removeRow)
        {
            removeRow = DeleteAssetsWithConfirmation(new List<ItemsData> { row }, "ItemsData");
            if (removeRow)
            {
                _selectedItemIds.Remove(rowId);
            }
        }

        if (serializedRow.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(row);
        }

        EditorGUILayout.EndHorizontal();
        return removeRow;
    }

    private void DrawIdleCategory()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Idle Category", EditorStyles.boldLabel);

        DrawIdleCategoryFilter();

        EditorGUILayout.BeginHorizontal();
        _pendingIdleToAdd = (IdleData)EditorGUILayout.ObjectField("Add Existing Idle", _pendingIdleToAdd, typeof(IdleData), false);
        using (new EditorGUI.DisabledScope(_pendingIdleToAdd == null))
        {
            if (GUILayout.Button("Add", GUILayout.Width(70f)))
            {
                AddUnique(_idleRows, _pendingIdleToAdd);
                _pendingIdleToAdd = null;
            }
        }

        if (GUILayout.Button("New IdleData", GUILayout.Width(110f)))
        {
            IdleData created = CreateDataAsset<IdleData>(GetSettingsFolderPath(_settings?.idleDataFolder), "IdleData");
            AddUnique(_idleRows, created);
        }
        EditorGUILayout.EndHorizontal();

        DrawDropZone<IdleData>(
            "Drop IdleData assets here",
            droppedIdleData =>
            {
                AddUnique(_idleRows, droppedIdleData);
            });

        DrawIdleAssetsBulkActions();
        float[] idleAssetColumnWidths = GetTableColumnWidths(position.width - 28f, IdleAssetsTableBaseWidths);
        DrawIdleAssetsHeader(idleAssetColumnWidths);

        int removeIndex = -1;
        for (int i = 0; i < _idleRows.Count; i++)
        {
            IdleData row = _idleRows[i];
            if (row != null && !PassesIdleJobFilter(row))
            {
                continue;
            }

            if (DrawIdleAssetRow(i, idleAssetColumnWidths))
            {
                removeIndex = i;
            }
        }

        if (removeIndex >= 0)
        {
            _idleRows.RemoveAt(removeIndex);
        }
    }

    private void DrawIdleCategoryFilter()
    {
        List<JobData> availableJobs = new();
        foreach (JobData jobData in _jobRows)
        {
            if (jobData != null)
            {
                availableJobs.Add(jobData);
            }
        }

        availableJobs.Sort((a, b) =>
        {
            string left = string.IsNullOrWhiteSpace(a.jobName) ? a.name : a.jobName;
            string right = string.IsNullOrWhiteSpace(b.jobName) ? b.name : b.jobName;
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        });

        List<string> filterNames = new() { "All Jobs" };
        foreach (JobData jobData in availableJobs)
        {
            filterNames.Add(string.IsNullOrWhiteSpace(jobData.jobName) ? jobData.name : jobData.jobName);
        }

        int currentIndex = 0;
        if (_idleCategoryFilterJob != null)
        {
            int foundIndex = availableJobs.IndexOf(_idleCategoryFilterJob);
            if (foundIndex >= 0)
            {
                currentIndex = foundIndex + 1;
            }
            else
            {
                _idleCategoryFilterJob = null;
            }
        }

        int nextIndex = EditorGUILayout.Popup("Job Filter", currentIndex, filterNames.ToArray(), GUILayout.Width(300f));
        _idleCategoryFilterJob = nextIndex > 0 ? availableJobs[nextIndex - 1] : null;
    }

    private void DrawIdleAssetsBulkActions()
    {
        PruneSelection(_selectedIdleAssetIds, _idleRows);

        int selectedCount = _selectedIdleAssetIds.Count;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Select All", GUILayout.Width(84f)))
        {
            foreach (IdleData idleData in _idleRows)
            {
                if (idleData != null && PassesIdleJobFilter(idleData))
                {
                    _selectedIdleAssetIds.Add(idleData.GetInstanceID());
                }
            }
        }

        if (GUILayout.Button("Clear", GUILayout.Width(56f)))
        {
            _selectedIdleAssetIds.Clear();
        }

        using (new EditorGUI.DisabledScope(selectedCount == 0))
        {
            if (GUILayout.Button("Delete Selected", GUILayout.Width(112f)))
            {
                List<IdleData> selectedIdles = GetSelectedObjects(_idleRows, _selectedIdleAssetIds);
                if (DeleteAssetsWithConfirmation(selectedIdles, "IdleData"))
                {
                    HashSet<int> deletedIds = new();
                    foreach (IdleData idleData in selectedIdles)
                    {
                        if (idleData != null)
                        {
                            deletedIds.Add(idleData.GetInstanceID());
                        }
                    }

                    _idleRows.RemoveAll(idleData => idleData == null || deletedIds.Contains(idleData.GetInstanceID()));
                    _selectedIdleAssetIds.ExceptWith(deletedIds);
                    _expandedIdleAssetFinishActionRows.ExceptWith(deletedIds);

                    foreach (HashSet<int> idleSelectionSet in _selectedIdleIds.Values)
                    {
                        idleSelectionSet.ExceptWith(deletedIds);
                    }
                }
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"Selected: {selectedCount}", GUILayout.Width(90f));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawIdleAssetsHeader(float[] widths)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        DrawHeaderCell("Sel", widths[0]);
        DrawHeaderCell("IdleData", widths[1]);
        DrawHeaderCell("Display Name", widths[2]);
        DrawHeaderCell("Interval", widths[3]);
        DrawHeaderCell("XP Reward", widths[4]);
        DrawHeaderCell("Max XP", widths[5]);
        DrawHeaderCell("Icon", widths[6]);
        DrawHeaderCell("Rules", widths[7]);
        DrawHeaderCell("Jobs", widths[8]);
        DrawHeaderCell("X", widths[9]);
        EditorGUILayout.EndHorizontal();
    }

    private bool DrawIdleAssetRow(int rowIndex, float[] widths)
    {
        IdleData row = _idleRows[rowIndex];
        int rowId = row != null ? row.GetInstanceID() : 0;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        bool selected = row != null && _selectedIdleAssetIds.Contains(rowId);
        bool nextSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(widths[0]));
        if (row != null && nextSelected != selected)
        {
            SetSelection(_selectedIdleAssetIds, rowId, nextSelected);
        }

        EditorGUI.BeginChangeCheck();
        IdleData changedRow = (IdleData)EditorGUILayout.ObjectField(row, typeof(IdleData), false, GUILayout.Width(widths[1]));
        if (EditorGUI.EndChangeCheck())
        {
            if (row != null && changedRow != row)
            {
                _selectedIdleAssetIds.Remove(rowId);
                _expandedIdleAssetFinishActionRows.Remove(rowId);
            }

            _idleRows[rowIndex] = changedRow;
            row = changedRow;
            rowId = row != null ? row.GetInstanceID() : 0;
        }

        if (row == null)
        {
            float emptyWidth = 0f;
            for (int i = 2; i < widths.Length - 1; i++)
            {
                emptyWidth += widths[i];
            }

            GUILayout.Label("Missing", GUILayout.Width(emptyWidth));
            bool removeEmpty = GUILayout.Button("X", GUILayout.Width(widths[9]));
            EditorGUILayout.EndHorizontal();
            _expandedIdleAssetFinishActionRows.Remove(rowId);
            return removeEmpty;
        }

        SerializedObject serializedRow = new SerializedObject(row);
        serializedRow.Update();

        DrawCompactProperty(serializedRow.FindProperty("displayName"), widths[2]);
        DrawCompactProperty(serializedRow.FindProperty("interval"), widths[3]);
        DrawCompactProperty(serializedRow.FindProperty("xpReward"), widths[4]);
        DrawCompactProperty(serializedRow.FindProperty("maxXP"), widths[5]);
        DrawCompactProperty(serializedRow.FindProperty("icon"), widths[6]);
        SerializedProperty finishActionsProperty = serializedRow.FindProperty("finishActions");
        SerializedProperty startConditionsProperty = serializedRow.FindProperty("startConditions");
        bool isExpanded = _expandedIdleAssetFinishActionRows.Contains(rowId);
        EditorGUILayout.BeginHorizontal(GUILayout.Width(widths[7]));
        bool nextExpanded = isExpanded;
        if (GUILayout.Button(isExpanded ? "v" : ">", EditorStyles.miniButton, GUILayout.Width(16f)))
        {
            nextExpanded = !isExpanded;
        }

        EditorGUILayout.LabelField(GetIdleRulesLabel(finishActionsProperty, startConditionsProperty), GUILayout.Width(Mathf.Max(0f, widths[7] - 16f)));
        EditorGUILayout.EndHorizontal();

        if (nextExpanded != isExpanded)
        {
            SetSelection(_expandedIdleAssetFinishActionRows, rowId, nextExpanded);
        }

        EditorGUILayout.LabelField(GetIdleMembershipLabel(row), GUILayout.Width(widths[8]));

        bool removeRow = GUILayout.Button("X", GUILayout.Width(widths[9]));
        if (removeRow)
        {
            removeRow = DeleteAssetsWithConfirmation(new List<IdleData> { row }, "IdleData");
            if (removeRow)
            {
                _selectedIdleAssetIds.Remove(rowId);
                _expandedIdleAssetFinishActionRows.Remove(rowId);
                foreach (HashSet<int> idleSelectionSet in _selectedIdleIds.Values)
                {
                    idleSelectionSet.Remove(rowId);
                }
            }
        }

        bool drawFinishActionDetails = _expandedIdleAssetFinishActionRows.Contains(rowId);
        if (drawFinishActionDetails)
        {
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(GetFinishActionIndentWidth(widths, 7));
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(Mathf.Max(180f, widths[7] + widths[8] + widths[9]))))
            {
                DrawIdleRulesDetails(finishActionsProperty, startConditionsProperty);
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.EndHorizontal();
        }

        if (serializedRow.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(row);
        }

        return removeRow;
    }

    private void RefreshRowsFromFolders()
    {
        LoadOrCreateSettings();

        HashSet<int> previousExpandedJobRows = new(_expandedJobRows);
        Dictionary<int, HashSet<int>> previousExpandedJobIdleFinishActionRows = new();
        foreach (KeyValuePair<int, HashSet<int>> pair in _expandedJobIdleFinishActionRows)
        {
            previousExpandedJobIdleFinishActionRows[pair.Key] = new HashSet<int>(pair.Value);
        }

        HashSet<int> previousExpandedIdleAssetFinishActionRows = new(_expandedIdleAssetFinishActionRows);

        string jobsFolder = GetSettingsFolderPath(_settings?.jobsDataFolder);
        string itemsFolder = GetSettingsFolderPath(_settings?.itemsDataFolder);
        string idleFolder = GetSettingsFolderPath(_settings?.idleDataFolder);

        _jobRows.Clear();
        _itemRows.Clear();
        _idleRows.Clear();
        _selectedJobIds.Clear();
        _selectedItemIds.Clear();
        _selectedIdleAssetIds.Clear();
        _selectedIdleIds.Clear();
        _expandedJobRows.Clear();
        _expandedJobIdleFinishActionRows.Clear();
        _expandedIdleAssetFinishActionRows.Clear();
        _pendingIdleAdds.Clear();

        _jobRows.AddRange(LoadAssets<JobData>(jobsFolder));
        _itemRows.AddRange(LoadAssets<ItemsData>(itemsFolder));
        _idleRows.AddRange(LoadAssets<IdleData>(idleFolder));

        Dictionary<int, JobData> jobsById = new();
        foreach (JobData jobData in _jobRows)
        {
            if (jobData != null)
            {
                jobsById[jobData.GetInstanceID()] = jobData;
            }
        }

        foreach (int expandedJobId in previousExpandedJobRows)
        {
            if (jobsById.ContainsKey(expandedJobId))
            {
                _expandedJobRows.Add(expandedJobId);
            }
        }

        foreach (KeyValuePair<int, HashSet<int>> pair in previousExpandedJobIdleFinishActionRows)
        {
            if (!jobsById.TryGetValue(pair.Key, out JobData jobData) || jobData == null)
            {
                continue;
            }

            HashSet<int> restoredRows = new(pair.Value);
            int idleCount = jobData.idleDatas != null ? jobData.idleDatas.Count : 0;
            restoredRows.RemoveWhere(index => index < 0 || index >= idleCount);

            if (restoredRows.Count > 0)
            {
                _expandedJobIdleFinishActionRows[pair.Key] = restoredRows;
            }
        }

        HashSet<int> validIdleIds = new();
        foreach (IdleData idleData in _idleRows)
        {
            if (idleData != null)
            {
                validIdleIds.Add(idleData.GetInstanceID());
            }
        }

        foreach (int expandedIdleId in previousExpandedIdleAssetFinishActionRows)
        {
            if (validIdleIds.Contains(expandedIdleId))
            {
                _expandedIdleAssetFinishActionRows.Add(expandedIdleId);
            }
        }

        if (_idleCategoryFilterJob != null && !_jobRows.Contains(_idleCategoryFilterJob))
        {
            _idleCategoryFilterJob = null;
        }
    }

    private static List<T> LoadAssets<T>(string folderPath) where T : ScriptableObject
    {
        string[] guids = AssetDatabase.IsValidFolder(folderPath)
            ? AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath })
            : AssetDatabase.FindAssets($"t:{typeof(T).Name}");

        List<T> assets = new();
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                assets.Add(asset);
            }
        }

        assets.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        return assets;
    }

    private void LoadOrCreateSettings()
    {
        if (_settings != null)
        {
            return;
        }

        string[] existingSettings = AssetDatabase.FindAssets("t:Catalog_DataSettings");
        if (existingSettings.Length > 0)
        {
            string existingPath = AssetDatabase.GUIDToAssetPath(existingSettings[0]);
            _settings = AssetDatabase.LoadAssetAtPath<Catalog_DataSettings>(existingPath);
            if (_settings != null)
            {
                return;
            }
        }

        EnsureFolderPath("Assets/Personal/Scripts/Catalog");

        _settings = CreateInstance<Catalog_DataSettings>();
        string path = AssetDatabase.GenerateUniqueAssetPath(SettingsAssetPath);
        AssetDatabase.CreateAsset(_settings, path);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolderPath(string folderPath)
    {
        string normalizedPath = GetSettingsFolderPath(folderPath);
        if (AssetDatabase.IsValidFolder(normalizedPath))
        {
            return;
        }

        string[] parts = normalizedPath.Split('/');
        if (parts.Length == 0)
        {
            return;
        }

        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static T CreateDataAsset<T>(string folderPath, string defaultName) where T : ScriptableObject
    {
        string path = AssetDatabase.IsValidFolder(folderPath) ? folderPath : "Assets";
        T created = CreateInstance<T>();
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{defaultName}.asset");
        AssetDatabase.CreateAsset(created, assetPath);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(created);
        Selection.activeObject = created;
        return created;
    }

    private static string GetSettingsFolderPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Assets";
        }

        return path.Replace('\\', '/').TrimEnd('/');
    }

    private static void DrawDropZone<T>(string text, Action<T> onDrop) where T : UnityEngine.Object
    {
        Rect dropRect = GUILayoutUtility.GetRect(0f, 28f, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, text, EditorStyles.helpBox);

        Event currentEvent = Event.current;
        if (!dropRect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        switch (currentEvent.type)
        {
            case EventType.DragUpdated:
            {
                if (ContainsType<T>(DragAndDrop.objectReferences))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
                currentEvent.Use();
                break;
            }
            case EventType.DragPerform:
            {
                DragAndDrop.AcceptDrag();
                foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject is T validObject)
                    {
                        onDrop?.Invoke(validObject);
                    }
                }
                currentEvent.Use();
                break;
            }
        }
    }

    private static bool ContainsType<T>(UnityEngine.Object[] objects) where T : UnityEngine.Object
    {
        foreach (UnityEngine.Object obj in objects)
        {
            if (obj is T)
            {
                return true;
            }
        }

        return false;
    }

    private float[] GetTableColumnWidths(float totalWidth, float[] baseWidths)
    {
        float[] widths = new float[baseWidths.Length];
        Array.Copy(baseWidths, widths, baseWidths.Length);

        if (!_stretchTables)
        {
            return widths;
        }

        float usableWidth = Mathf.Max(240f, totalWidth - 12f);
        float baseWidthTotal = 0f;
        for (int i = 0; i < baseWidths.Length; i++)
        {
            baseWidthTotal += baseWidths[i];
        }

        if (baseWidthTotal <= 0f)
        {
            return widths;
        }

        float widthScale = usableWidth / baseWidthTotal;
        for (int i = 0; i < widths.Length; i++)
        {
            widths[i] = baseWidths[i] * widthScale;
        }

        return widths;
    }

    private HashSet<int> GetJobIdleFinishActionRows(JobData jobData)
    {
        int jobId = jobData.GetInstanceID();
        if (!_expandedJobIdleFinishActionRows.TryGetValue(jobId, out HashSet<int> expandedRows))
        {
            expandedRows = new HashSet<int>();
            _expandedJobIdleFinishActionRows[jobId] = expandedRows;
        }

        return expandedRows;
    }

    private static float GetFinishActionIndentWidth(float[] widths, int finishActionColumnIndex)
    {
        float width = 0f;
        for (int i = 0; i < finishActionColumnIndex && i < widths.Length; i++)
        {
            width += widths[i];
        }

        return width;
    }

    private static string GetFinishActionsLabel(SerializedProperty finishActionsProperty)
    {
        if (finishActionsProperty == null || !finishActionsProperty.isArray)
        {
            return "-";
        }

        int count = finishActionsProperty.arraySize;
        if (count <= 0)
        {
            return "None";
        }

        if (count == 1)
        {
            SerializedProperty firstActionEntry = finishActionsProperty.GetArrayElementAtIndex(0);
            SerializedProperty finishTypeProperty = firstActionEntry?.FindPropertyRelative("finishType");
            if (finishTypeProperty != null
                && finishTypeProperty.propertyType == SerializedPropertyType.Enum
                && finishTypeProperty.enumValueIndex >= 0
                && finishTypeProperty.enumValueIndex < finishTypeProperty.enumDisplayNames.Length)
            {
                return finishTypeProperty.enumDisplayNames[finishTypeProperty.enumValueIndex];
            }

            return "1 Action";
        }

        return $"{count} Actions";
    }

    private static string GetConditionRulesLabel(SerializedProperty conditionsProperty)
    {
        if (conditionsProperty == null || !conditionsProperty.isArray)
        {
            return "-";
        }

        int count = conditionsProperty.arraySize;
        if (count <= 0)
        {
            return "None";
        }

        if (count == 1)
        {
            SerializedProperty firstConditionEntry = conditionsProperty.GetArrayElementAtIndex(0);
            SerializedProperty conditionTypeProperty = firstConditionEntry?.FindPropertyRelative("conditionType");
            if (conditionTypeProperty != null
                && conditionTypeProperty.propertyType == SerializedPropertyType.Enum
                && conditionTypeProperty.enumValueIndex >= 0
                && conditionTypeProperty.enumValueIndex < conditionTypeProperty.enumDisplayNames.Length)
            {
                return conditionTypeProperty.enumDisplayNames[conditionTypeProperty.enumValueIndex];
            }

            return "1 Condition";
        }

        return $"{count} Conditions";
    }

    private static string GetIdleRulesLabel(SerializedProperty finishActionsProperty, SerializedProperty conditionsProperty)
    {
        bool hasActions = finishActionsProperty != null && finishActionsProperty.isArray && finishActionsProperty.arraySize > 0;
        bool hasConditions = conditionsProperty != null && conditionsProperty.isArray && conditionsProperty.arraySize > 0;

        if (!hasActions && !hasConditions)
        {
            return "None";
        }

        if (!hasActions)
        {
            return GetConditionRulesLabel(conditionsProperty);
        }

        if (!hasConditions)
        {
            return GetFinishActionsLabel(finishActionsProperty);
        }

        return $"{finishActionsProperty.arraySize}A / {conditionsProperty.arraySize}C";
    }

    private static void DrawFinishActionsDetails(SerializedProperty finishActionsProperty)
    {
        EditorGUILayout.LabelField("Finish Actions", EditorStyles.miniBoldLabel);

        if (finishActionsProperty == null || !finishActionsProperty.isArray)
        {
            EditorGUILayout.HelpBox("Finish actions list is not available.", MessageType.Info);
            return;
        }

        int removeIndex = -1;
        for (int i = 0; i < finishActionsProperty.arraySize; i++)
        {
            SerializedProperty actionEntry = finishActionsProperty.GetArrayElementAtIndex(i);
            if (actionEntry == null)
            {
                continue;
            }

            SerializedProperty finishTypeProperty = actionEntry.FindPropertyRelative("finishType");
            SerializedProperty finishActionProperty = actionEntry.FindPropertyRelative("finishAction");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Action {i + 1}", EditorStyles.miniBoldLabel, GUILayout.Width(52f));
                if (finishTypeProperty != null)
                {
                    EditorGUILayout.PropertyField(finishTypeProperty, GUIContent.none);
                }
                else
                {
                    GUILayout.Label("-");
                }

                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    removeIndex = i;
                }

                EditorGUILayout.EndHorizontal();
                DrawManagedReferenceChildren(finishActionProperty, "No finish action assigned for this entry.");
            }
        }

        if (removeIndex >= 0)
        {
            finishActionsProperty.DeleteArrayElementAtIndex(removeIndex);
        }

        if (GUILayout.Button("Add Action", GUILayout.Width(88f)))
        {
            int newIndex = finishActionsProperty.arraySize;
            finishActionsProperty.arraySize++;

            SerializedProperty newActionEntry = finishActionsProperty.GetArrayElementAtIndex(newIndex);
            if (newActionEntry != null)
            {
                SerializedProperty newFinishTypeProperty = newActionEntry.FindPropertyRelative("finishType");
                if (newFinishTypeProperty != null && newFinishTypeProperty.propertyType == SerializedPropertyType.Enum)
                {
                    newFinishTypeProperty.enumValueIndex = 0;
                }

                SerializedProperty newFinishActionProperty = newActionEntry.FindPropertyRelative("finishAction");
                if (newFinishActionProperty != null)
                {
                    newFinishActionProperty.managedReferenceValue = new FinishAction_GiveItem();
                }
            }
        }
    }

    private static void DrawConditionRulesDetails(SerializedProperty conditionsProperty)
    {
        EditorGUILayout.LabelField("Start Conditions", EditorStyles.miniBoldLabel);

        if (conditionsProperty == null || !conditionsProperty.isArray)
        {
            EditorGUILayout.HelpBox("Start conditions list is not available.", MessageType.Info);
            return;
        }

        int removeIndex = -1;
        for (int i = 0; i < conditionsProperty.arraySize; i++)
        {
            SerializedProperty conditionEntry = conditionsProperty.GetArrayElementAtIndex(i);
            if (conditionEntry == null)
            {
                continue;
            }

            SerializedProperty conditionTypeProperty = conditionEntry.FindPropertyRelative("conditionType");
            SerializedProperty conditionProperty = conditionEntry.FindPropertyRelative("condition");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Rule {i + 1}", EditorStyles.miniBoldLabel, GUILayout.Width(52f));
                if (conditionTypeProperty != null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(conditionTypeProperty, GUIContent.none);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EnsureConditionRuleType(conditionTypeProperty, conditionProperty);
                    }
                }
                else
                {
                    GUILayout.Label("-");
                }

                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    removeIndex = i;
                }

                EditorGUILayout.EndHorizontal();
                EnsureConditionRuleType(conditionTypeProperty, conditionProperty);
                DrawManagedReferenceChildren(conditionProperty, "No condition rule assigned for this entry.");
            }
        }

        if (removeIndex >= 0)
        {
            conditionsProperty.DeleteArrayElementAtIndex(removeIndex);
        }

        if (GUILayout.Button("Add Condition", GUILayout.Width(104f)))
        {
            int newIndex = conditionsProperty.arraySize;
            conditionsProperty.arraySize++;

            SerializedProperty newConditionEntry = conditionsProperty.GetArrayElementAtIndex(newIndex);
            if (newConditionEntry != null)
            {
                SerializedProperty newConditionTypeProperty = newConditionEntry.FindPropertyRelative("conditionType");
                if (newConditionTypeProperty != null && newConditionTypeProperty.propertyType == SerializedPropertyType.Enum)
                {
                    newConditionTypeProperty.enumValueIndex = 0;
                }

                SerializedProperty newConditionProperty = newConditionEntry.FindPropertyRelative("condition");
                if (newConditionProperty != null)
                {
                    newConditionProperty.managedReferenceValue = ConditionRuleUtility.CreateConditionRule(ConditionRuleType.Level);
                }
            }
        }
    }

    private static void DrawIdleRulesDetails(SerializedProperty finishActionsProperty, SerializedProperty conditionsProperty)
    {
        DrawFinishActionsDetails(finishActionsProperty);
        EditorGUILayout.Space(4f);
        DrawConditionRulesDetails(conditionsProperty);
    }

    private static void EnsureConditionRuleType(SerializedProperty conditionTypeProperty, SerializedProperty conditionProperty)
    {
        if (conditionTypeProperty == null
            || conditionTypeProperty.propertyType != SerializedPropertyType.Enum
            || conditionProperty == null)
        {
            return;
        }

        ConditionRuleType selectedType = (ConditionRuleType)conditionTypeProperty.enumValueIndex;
        if (conditionProperty.managedReferenceValue is ConditionRule existingCondition && existingCondition.RuleType == selectedType)
        {
            return;
        }

        conditionProperty.managedReferenceValue = ConditionRuleUtility.CreateConditionRule(selectedType);
    }

    private static void DrawManagedReferenceChildren(SerializedProperty managedReferenceProperty, string emptyMessage)
    {
        if (managedReferenceProperty == null || managedReferenceProperty.managedReferenceValue == null)
        {
            EditorGUILayout.HelpBox(emptyMessage, MessageType.Info);
            return;
        }

        int parentDepth = managedReferenceProperty.depth;
        bool previousExpandedState = managedReferenceProperty.isExpanded;
        managedReferenceProperty.isExpanded = true;

        SerializedProperty iterator = managedReferenceProperty.Copy();
        SerializedProperty end = iterator.GetEndProperty();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            if (iterator.depth == parentDepth + 1)
            {
                EditorGUILayout.PropertyField(iterator, true);
            }

            enterChildren = false;
        }

        managedReferenceProperty.isExpanded = previousExpandedState;
    }

    private static void DrawHeaderCell(string text, float width)
    {
        GUILayout.Label(text, EditorStyles.miniBoldLabel, GUILayout.Width(width));
    }

    private static void DrawCompactProperty(SerializedProperty property, float width)
    {
        if (property == null)
        {
            GUILayout.Label("-", GUILayout.Width(width));
            return;
        }

        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                property.intValue = EditorGUILayout.IntField(property.intValue, GUILayout.Width(width));
                break;
            case SerializedPropertyType.Float:
                property.floatValue = EditorGUILayout.FloatField(property.floatValue, GUILayout.Width(width));
                break;
            case SerializedPropertyType.Boolean:
                property.boolValue = EditorGUILayout.Toggle(property.boolValue, GUILayout.Width(width));
                break;
            case SerializedPropertyType.Enum:
                property.enumValueIndex = EditorGUILayout.Popup(property.enumValueIndex, property.enumDisplayNames, GUILayout.Width(width));
                break;
            case SerializedPropertyType.String:
                property.stringValue = EditorGUILayout.DelayedTextField(property.stringValue, GUILayout.Width(width));
                break;
            case SerializedPropertyType.ObjectReference:
                property.objectReferenceValue = EditorGUILayout.ObjectField(property.objectReferenceValue, typeof(UnityEngine.Object), false, GUILayout.Width(width));
                break;
            default:
                EditorGUILayout.PropertyField(property, GUIContent.none, false, GUILayout.Width(width));
                break;
        }
    }

    private static void SetSelection(HashSet<int> selectionSet, int id, bool isSelected)
    {
        if (id == 0)
        {
            return;
        }

        if (isSelected)
        {
            selectionSet.Add(id);
        }
        else
        {
            selectionSet.Remove(id);
        }
    }

    private static void PruneSelection<T>(HashSet<int> selectionSet, List<T> objects) where T : UnityEngine.Object
    {
        HashSet<int> validIds = new();
        foreach (T obj in objects)
        {
            if (obj != null)
            {
                validIds.Add(obj.GetInstanceID());
            }
        }

        selectionSet.IntersectWith(validIds);
    }

    private static List<T> GetSelectedObjects<T>(List<T> rows, HashSet<int> selectedIds) where T : UnityEngine.Object
    {
        List<T> selectedObjects = new();
        foreach (T row in rows)
        {
            if (row != null && selectedIds.Contains(row.GetInstanceID()))
            {
                selectedObjects.Add(row);
            }
        }

        return selectedObjects;
    }

    private static bool DeleteAssetsWithConfirmation<T>(List<T> assets, string label) where T : ScriptableObject
    {
        List<T> uniqueAssets = new();
        HashSet<string> uniquePaths = new();
        foreach (T asset in assets)
        {
            if (asset == null)
            {
                continue;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            if (uniquePaths.Add(path))
            {
                uniqueAssets.Add(asset);
            }
        }

        if (uniqueAssets.Count == 0)
        {
            return false;
        }

        string message = uniqueAssets.Count == 1
            ? $"Delete '{uniqueAssets[0].name}'?\n\nThe asset will be moved to Trash."
            : $"Delete {uniqueAssets.Count} {label} assets?\n\nThe assets will be moved to Trash.";

        bool confirmed = EditorUtility.DisplayDialog($"Delete {label}", message, "Delete", "Cancel");
        if (!confirmed)
        {
            return false;
        }

        int deletedCount = 0;
        foreach (T asset in uniqueAssets)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrWhiteSpace(path) && AssetDatabase.MoveAssetToTrash(path))
            {
                deletedCount++;
            }
        }

        if (deletedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        if (deletedCount != uniqueAssets.Count)
        {
            EditorUtility.DisplayDialog(
                "Delete Incomplete",
                $"Deleted {deletedCount} of {uniqueAssets.Count} selected {label} assets.",
                "OK");
        }

        return deletedCount > 0;
    }

    private HashSet<int> GetIdleSelectionSet(JobData jobData)
    {
        int jobId = jobData.GetInstanceID();
        if (!_selectedIdleIds.TryGetValue(jobId, out HashSet<int> selectionSet))
        {
            selectionSet = new HashSet<int>();
            _selectedIdleIds[jobId] = selectionSet;
        }

        return selectionSet;
    }

    private static void PruneIdleSelection(JobData jobData, HashSet<int> selectedIds)
    {
        HashSet<int> validIds = new();
        foreach (IdleData idleData in jobData.idleDatas)
        {
            if (idleData != null)
            {
                validIds.Add(idleData.GetInstanceID());
            }
        }

        selectedIds.IntersectWith(validIds);
    }

    private static void AddUnique<T>(List<T> list, T item) where T : UnityEngine.Object
    {
        if (item == null || list.Contains(item))
        {
            return;
        }

        list.Add(item);
    }

    private void EnsureIdleList(JobData jobData)
    {
        if (jobData.idleDatas != null)
        {
            return;
        }

        Undo.RecordObject(jobData, "Initialize IdleData List");
        jobData.idleDatas = new List<IdleData>();
        EditorUtility.SetDirty(jobData);
    }

    private void AddIdleToJob(JobData jobData, IdleData idleData)
    {
        if (jobData == null || idleData == null)
        {
            return;
        }

        EnsureIdleList(jobData);
        if (jobData.idleDatas.Contains(idleData))
        {
            return;
        }

        Undo.RecordObject(jobData, "Add IdleData");
        jobData.idleDatas.Add(idleData);
        EditorUtility.SetDirty(jobData);
    }

    private void MoveIdleRow(JobData jobData, int fromIndex, int toIndex)
    {
        if (jobData == null || jobData.idleDatas == null)
        {
            return;
        }

        if (fromIndex < 0 || toIndex < 0 || fromIndex >= jobData.idleDatas.Count || toIndex >= jobData.idleDatas.Count || fromIndex == toIndex)
        {
            return;
        }

        Undo.RecordObject(jobData, "Reorder IdleData");
        IdleData movedRow = jobData.idleDatas[fromIndex];
        jobData.idleDatas.RemoveAt(fromIndex);
        jobData.idleDatas.Insert(toIndex, movedRow);
        EditorUtility.SetDirty(jobData);

        HashSet<int> expandedRows = GetJobIdleFinishActionRows(jobData);
        if (expandedRows.Count == 0)
        {
            return;
        }

        HashSet<int> remappedRows = new();
        foreach (int rowIndex in expandedRows)
        {
            if (rowIndex == fromIndex)
            {
                remappedRows.Add(toIndex);
            }
            else if (fromIndex < toIndex && rowIndex > fromIndex && rowIndex <= toIndex)
            {
                remappedRows.Add(rowIndex - 1);
            }
            else if (fromIndex > toIndex && rowIndex >= toIndex && rowIndex < fromIndex)
            {
                remappedRows.Add(rowIndex + 1);
            }
            else
            {
                remappedRows.Add(rowIndex);
            }
        }

        expandedRows.Clear();
        expandedRows.UnionWith(remappedRows);
    }

    private bool PassesItemFilter(ItemsData itemData)
    {
        if (_itemFilter == Catalog_ItemFilter.All)
        {
            return true;
        }

        return itemData.itemType == (Catalog_ItemType)(int)_itemFilter;
    }

    private bool PassesIdleJobFilter(IdleData idleData)
    {
        if (_idleCategoryFilterJob == null)
        {
            return true;
        }

        return _idleCategoryFilterJob.idleDatas != null && _idleCategoryFilterJob.idleDatas.Contains(idleData);
    }

    private string GetIdleMembershipLabel(IdleData idleData)
    {
        List<string> jobNames = new();
        foreach (JobData jobData in _jobRows)
        {
            if (jobData == null || jobData.idleDatas == null || !jobData.idleDatas.Contains(idleData))
            {
                continue;
            }

            string jobName = string.IsNullOrWhiteSpace(jobData.jobName) ? jobData.name : jobData.jobName;
            jobNames.Add(jobName);
        }

        return jobNames.Count == 0 ? "-" : string.Join(", ", jobNames);
    }
}
