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
        Idles,
        Monsters
    }

    private enum Catalog_ItemFilter
    {
        All = -1,
        Resource = (int)Catalog_ItemType.Resource,
        Weapon = (int)Catalog_ItemType.Weapon,
        Armor = (int)Catalog_ItemType.Armor,
        Food = (int)Catalog_ItemType.Food,
        Potion = (int)Catalog_ItemType.Potion,
        Utility = (int)Catalog_ItemType.Utility
    }

    private enum JobIdleRowAction
    {
        None,
        Remove,
        Move
    }

    private enum Jobs_Sort { Name, Category, MaxLevel, IdleCount }
    private enum Items_Sort { Name, ID, Type, Price }
    private enum Idles_Sort { Name, Interval, JobXP, Kind }
    private enum Monsters_Sort { Name, CombatLevel, AttackType, Speed }

    private const string SettingsAssetPath = "Assets/Personal/Scripts/Catalog/Catalog_DataSettings.asset";
    private const string StretchTablesPrefKey = "Catalog_DataSpreadsheetWindow.StretchTables";
    private const float ObjectCellWidth = 220f;
    private static readonly float[] JobsTableBaseWidths = { 30f, ObjectCellWidth, 160f, 90f, 80f, 80f, 70f, 24f };
    private static readonly float[] JobIdleTableBaseWidths = { 30f, ObjectCellWidth, 150f, 80f, 80f, 80f, 90f, 46f, 24f };
    private static readonly float[] ItemsTableBaseWidths = { 30f, ObjectCellWidth, 100f, 70f, 150f, 260f, 80f, 90f, 24f };
    private static readonly float[] IdleAssetsTableBaseWidths = { 30f, ObjectCellWidth, 140f, 75f, 95f, 85f, 200f, 70f, 24f };
    private static readonly float[] MonstersTableBaseWidths = { 30f, ObjectCellWidth, 150f, 80f, 90f, 70f, 70f, 24f };

    private readonly List<Job_Data> _jobRows = new();
    private readonly List<ItemsData> _itemRows = new();
    private readonly List<Idle_Data> _idleRows = new();
    private readonly List<Monster_Data> _monsterRows = new();
    private readonly Dictionary<int, Idle_Data> _pendingIdleAdds = new();
    private readonly HashSet<int> _selectedJobIds = new();
    private readonly HashSet<int> _selectedItemIds = new();
    private readonly HashSet<int> _selectedIdleAssetIds = new();
    private readonly HashSet<int> _selectedMonsterIds = new();
    private readonly Dictionary<int, HashSet<int>> _selectedIdleIds = new();
    private readonly HashSet<int> _expandedJobRows = new();

    private Catalog_DataSettings _settings;
    private Job_Data _idleCategoryFilterJob;

    private Catalog_Tab _activeTab;
    private Catalog_ItemFilter _itemFilter = Catalog_ItemFilter.All;
    private Jobs_Sort _jobSort = Jobs_Sort.Name;
    private Items_Sort _itemSort = Items_Sort.Name;
    private Idles_Sort _idleSort = Idles_Sort.Name;
    private Monsters_Sort _monsterSort = Monsters_Sort.Name;
    private bool _jobSortDesc;
    private bool _itemSortDesc;
    private bool _idleSortDesc;
    private bool _monsterSortDesc;
    private Vector2 _jobsTableScroll;
    private Vector2 _itemsScroll;
    private Vector2 _idleScroll;
    private Vector2 _monstersScroll;
    private bool _stretchTables = true;

    private Job_Data _pendingJobToAdd;
    private ItemsData _pendingItemToAdd;
    private Idle_Data _pendingIdleToAdd;
    private Monster_Data _pendingMonsterToAdd;

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
            case Catalog_Tab.Monsters:
                _monstersScroll = EditorGUILayout.BeginScrollView(_monstersScroll, !_stretchTables, true);
                DrawMonstersCategory();
                EditorGUILayout.EndScrollView();
                break;
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        _activeTab = (Catalog_Tab)GUILayout.Toolbar((int)_activeTab, new[] { "Jobs", "Items", "Idles", "Monsters" }, EditorStyles.toolbarButton);

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
        GUILayout.Label("Sort By", GUILayout.Width(46f));
        EditorGUI.BeginChangeCheck();
        _jobSort = (Jobs_Sort)EditorGUILayout.EnumPopup(_jobSort, GUILayout.Width(120f));
        bool jobSortChanged = EditorGUI.EndChangeCheck();
        bool jobDesc = DrawSortDirection(_jobSortDesc);
        if (jobDesc != _jobSortDesc) { _jobSortDesc = jobDesc; jobSortChanged = true; }
        if (jobSortChanged) SortJobs();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _pendingJobToAdd = (Job_Data)EditorGUILayout.ObjectField("Add Existing Job", _pendingJobToAdd, typeof(Job_Data), false);
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
            Job_Data created = CreateDataAsset<Job_Data>(GetSettingsFolderPath(_settings?.jobsDataFolder), "JobData");
            AddUnique(_jobRows, created);
        }
        EditorGUILayout.EndHorizontal();

        DrawDropZone<Job_Data>(
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
        DrawHeaderCell("Max Level", widths[4]);
        DrawHeaderCell("Idle Count", widths[5]);
        DrawHeaderCell("Rewards", widths[6]);
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
            foreach (Job_Data jobData in _jobRows)
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
                List<Job_Data> selectedJobs = GetSelectedObjects(_jobRows, _selectedJobIds);
                if (DeleteAssetsWithConfirmation(selectedJobs, "JobData"))
                {
                    HashSet<int> deletedIds = new();
                    foreach (Job_Data jobData in selectedJobs)
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
        Job_Data row = _jobRows[rowIndex];
        int rowId = row != null ? row.GetInstanceID() : 0;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        bool selected = row != null && _selectedJobIds.Contains(rowId);
        bool nextSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(widths[0]));
        if (row != null && nextSelected != selected)
        {
            SetSelection(_selectedJobIds, rowId, nextSelected);
        }

        EditorGUI.BeginChangeCheck();
        Job_Data changedRow = (Job_Data)EditorGUILayout.ObjectField(row, typeof(Job_Data), false, GUILayout.Width(widths[1]));
        if (EditorGUI.EndChangeCheck())
        {
            if (row != null && changedRow != row)
            {
                _selectedJobIds.Remove(rowId);
                _expandedJobRows.Remove(rowId);
                _pendingIdleAdds.Remove(row.GetInstanceID());
                _selectedIdleIds.Remove(row.GetInstanceID());
            }

            _jobRows[rowIndex] = changedRow;
            row = changedRow;
            rowId = row != null ? row.GetInstanceID() : 0;
        }

        if (row == null)
        {
            float emptyWidth = widths[2] + widths[3] + widths[4] + widths[5] + widths[6];
            GUILayout.Label("Missing", GUILayout.Width(emptyWidth));
            bool removeEmpty = GUILayout.Button("X", GUILayout.Width(widths[7]));
            EditorGUILayout.EndHorizontal();
            return removeEmpty;
        }

        SerializedObject serializedRow = new SerializedObject(row);
        serializedRow.Update();

        DrawCompactProperty(serializedRow.FindProperty("jobName"), widths[2]);
        DrawCompactProperty(serializedRow.FindProperty("jobIcon"), widths[3]);
        DrawCompactProperty(serializedRow.FindProperty("maxLevel"), widths[4]);

        SerializedProperty idleDatasProperty = serializedRow.FindProperty("idleDatas");
        EditorGUILayout.LabelField(idleDatasProperty != null ? idleDatasProperty.arraySize.ToString() : "-", GUILayout.Width(widths[5]));

        DrawOpenButton(row, widths[6]);

        bool removeRow = GUILayout.Button("X", GUILayout.Width(widths[7]));
        if (removeRow)
        {
            removeRow = DeleteAssetsWithConfirmation(new List<Job_Data> { row }, "JobData");
            if (removeRow)
            {
                _selectedJobIds.Remove(rowId);
                _expandedJobRows.Remove(rowId);
                _pendingIdleAdds.Remove(rowId);
                _selectedIdleIds.Remove(rowId);
            }
        }

        if (serializedRow.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(row);
        }

        EditorGUILayout.EndHorizontal();

        return removeRow;
    }

    private void DrawIdleTable(Job_Data jobData, float tableWidth)
    {
        EnsureIdleList(jobData);
        HashSet<int> selectedIdleIds = GetIdleSelectionSet(jobData);
        PruneIdleSelection(jobData, selectedIdleIds);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Idle_Data Table", EditorStyles.boldLabel);

        int jobId = jobData.GetInstanceID();
        _pendingIdleAdds.TryGetValue(jobId, out Idle_Data pendingIdleData);

        EditorGUILayout.BeginHorizontal();
        pendingIdleData = (Idle_Data)EditorGUILayout.ObjectField("Add Existing Idle", pendingIdleData, typeof(Idle_Data), false);
        using (new EditorGUI.DisabledScope(pendingIdleData == null))
        {
            if (GUILayout.Button("Add", GUILayout.Width(70f)))
            {
                AddIdleToJob(jobData, pendingIdleData);
                pendingIdleData = null;
            }
        }

        if (GUILayout.Button("New Idle_Data", GUILayout.Width(110f)))
        {
            Idle_Data created = CreateDataAsset<Idle_Data>(GetSettingsFolderPath(_settings?.idleDataFolder), "Idle_Data");
            AddIdleToJob(jobData, created);
        }
        EditorGUILayout.EndHorizontal();

        _pendingIdleAdds[jobId] = pendingIdleData;

        DrawDropZone<Idle_Data>(
            "Drop Idle_Data assets here for this job",
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
            Idle_Data removedIdleData = jobData.idleDatas[removeIndex];
            if (removedIdleData != null)
            {
                selectedIdleIds.Remove(removedIdleData.GetInstanceID());
            }

            Undo.RecordObject(jobData, "Remove Idle_Data");
            jobData.idleDatas.RemoveAt(removeIndex);
            EditorUtility.SetDirty(jobData);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawIdleHeader(float[] widths)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        DrawHeaderCell("Sel", widths[0]);
        DrawHeaderCell("Idle_Data", widths[1]);
        DrawHeaderCell("Display Name", widths[2]);
        DrawHeaderCell("Interval", widths[3]);
        DrawHeaderCell("XP Reward", widths[4]);
        DrawHeaderCell("Max XP", widths[5]);
        DrawHeaderCell("Icon", widths[6]);
        DrawHeaderCell("Move", widths[7]);
        DrawHeaderCell("X", widths[8]);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawIdleBulkActions(Job_Data jobData, HashSet<int> selectedIdleIds)
    {
        int selectedCount = selectedIdleIds.Count;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Select All", GUILayout.Width(84f)))
        {
            foreach (Idle_Data idleData in jobData.idleDatas)
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
                    "Remove Selected Idle_Data",
                    $"Remove {selectedCount} selected Idle_Data reference(s) from '{jobData.name}'?",
                    "Remove",
                    "Cancel");

                if (confirmed)
                {
                    Undo.RecordObject(jobData, "Remove Selected Idle_Data");
                    jobData.idleDatas.RemoveAll(idleData => idleData != null && selectedIdleIds.Contains(idleData.GetInstanceID()));
                    EditorUtility.SetDirty(jobData);
                    selectedIdleIds.Clear();
                }
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"Selected: {selectedCount}", GUILayout.Width(90f));

        EditorGUILayout.EndHorizontal();
    }

    private JobIdleRowAction DrawIdleRow(Job_Data jobData, int index, HashSet<int> selectedIdleIds, float[] widths, out int targetIndex)
    {
        targetIndex = index;
        Idle_Data row = jobData.idleDatas[index];
        int rowId = row != null ? row.GetInstanceID() : 0;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        bool selected = row != null && selectedIdleIds.Contains(rowId);
        bool nextSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(widths[0]));
        if (row != null && nextSelected != selected)
        {
            SetSelection(selectedIdleIds, rowId, nextSelected);
        }

        EditorGUI.BeginChangeCheck();
        Idle_Data changedRow = (Idle_Data)EditorGUILayout.ObjectField(row, typeof(Idle_Data), false, GUILayout.Width(widths[1]));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(jobData, "Change Idle_Data Reference");
            jobData.idleDatas[index] = changedRow;
            EditorUtility.SetDirty(jobData);
            if (row != null && changedRow != row)
            {
                selectedIdleIds.Remove(rowId);
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
            bool removeNull = GUILayout.Button("X", GUILayout.Width(widths[8]));
            EditorGUILayout.EndHorizontal();
            return removeNull ? JobIdleRowAction.Remove : JobIdleRowAction.None;
        }

        SerializedObject serializedRow = new SerializedObject(row);
        serializedRow.Update();

        DrawCompactProperty(serializedRow.FindProperty("displayName"), widths[2]);
        DrawCompactProperty(serializedRow.FindProperty("interval"), widths[3]);
        DrawCompactProperty(serializedRow.FindProperty("idleXPReward"), widths[4]);
        DrawCompactProperty(serializedRow.FindProperty("maxXP"), widths[5]);
        DrawCompactProperty(serializedRow.FindProperty("icon"), widths[6]);

        bool moveUp = false;
        bool moveDown = false;
        using (new EditorGUILayout.HorizontalScope(GUILayout.Width(widths[7])))
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

        bool removeRow = GUILayout.Button("X", GUILayout.Width(widths[8]))
            && EditorUtility.DisplayDialog(
                "Remove Idle_Data Reference",
                $"Remove '{row.name}' from '{jobData.name}' idle list?",
                "Remove",
                "Cancel");

        if (removeRow)
        {
            selectedIdleIds.Remove(rowId);
        }

        EditorGUILayout.EndHorizontal();

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
        GUILayout.Space(12f);
        GUILayout.Label("Sort By", GUILayout.Width(46f));
        EditorGUI.BeginChangeCheck();
        _itemSort = (Items_Sort)EditorGUILayout.EnumPopup(_itemSort, GUILayout.Width(120f));
        bool itemSortChanged = EditorGUI.EndChangeCheck();
        bool itemDesc = DrawSortDirection(_itemSortDesc);
        if (itemDesc != _itemSortDesc) { _itemSortDesc = itemDesc; itemSortChanged = true; }
        if (itemSortChanged) SortItems();
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

        EditorGUILayout.EndHorizontal();

        if (serializedRow.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(row);
        }

        return removeRow;
    }

    private void DrawIdleCategory()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Idle Category", EditorStyles.boldLabel);

        DrawIdleCategoryFilter();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Sort By", GUILayout.Width(46f));
        EditorGUI.BeginChangeCheck();
        _idleSort = (Idles_Sort)EditorGUILayout.EnumPopup(_idleSort, GUILayout.Width(120f));
        bool idleSortChanged = EditorGUI.EndChangeCheck();
        bool idleDesc = DrawSortDirection(_idleSortDesc);
        if (idleDesc != _idleSortDesc) { _idleSortDesc = idleDesc; idleSortChanged = true; }
        if (idleSortChanged) SortIdles();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _pendingIdleToAdd = (Idle_Data)EditorGUILayout.ObjectField("Add Existing Idle", _pendingIdleToAdd, typeof(Idle_Data), false);
        using (new EditorGUI.DisabledScope(_pendingIdleToAdd == null))
        {
            if (GUILayout.Button("Add", GUILayout.Width(70f)))
            {
                AddUnique(_idleRows, _pendingIdleToAdd);
                _pendingIdleToAdd = null;
            }
        }

        if (GUILayout.Button("New Idle_Data", GUILayout.Width(110f)))
        {
            Idle_Data created = CreateDataAsset<Idle_Data>(GetSettingsFolderPath(_settings?.idleDataFolder), "Idle_Data");
            AddUnique(_idleRows, created);
        }
        EditorGUILayout.EndHorizontal();

        DrawDropZone<Idle_Data>(
            "Drop Idle_Data assets here",
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
            Idle_Data row = _idleRows[i];
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
        List<Job_Data> availableJobs = new();
        foreach (Job_Data jobData in _jobRows)
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
        foreach (Job_Data jobData in availableJobs)
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
            foreach (Idle_Data idleData in _idleRows)
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
                List<Idle_Data> selectedIdles = GetSelectedObjects(_idleRows, _selectedIdleAssetIds);
                if (DeleteAssetsWithConfirmation(selectedIdles, "Idle_Data"))
                {
                    HashSet<int> deletedIds = new();
                    foreach (Idle_Data idleData in selectedIdles)
                    {
                        if (idleData != null)
                        {
                            deletedIds.Add(idleData.GetInstanceID());
                        }
                    }

                    _idleRows.RemoveAll(idleData => idleData == null || deletedIds.Contains(idleData.GetInstanceID()));
                    _selectedIdleAssetIds.ExceptWith(deletedIds);

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
        DrawHeaderCell("Idle_Data", widths[1]);
        DrawHeaderCell("Display Name", widths[2]);
        DrawHeaderCell("Interval", widths[3]);
        DrawHeaderCell("Job XP Reward", widths[4]);
        DrawHeaderCell("Icon", widths[5]);
        DrawHeaderCell("Jobs", widths[6]);
        DrawHeaderCell("Rewards", widths[7]);
        DrawHeaderCell("X", widths[8]);
        EditorGUILayout.EndHorizontal();
    }

    private bool DrawIdleAssetRow(int rowIndex, float[] widths)
    {
        Idle_Data row = _idleRows[rowIndex];
        int rowId = row != null ? row.GetInstanceID() : 0;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        bool selected = row != null && _selectedIdleAssetIds.Contains(rowId);
        bool nextSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(widths[0]));
        if (row != null && nextSelected != selected)
        {
            SetSelection(_selectedIdleAssetIds, rowId, nextSelected);
        }

        EditorGUI.BeginChangeCheck();
        Idle_Data changedRow = (Idle_Data)EditorGUILayout.ObjectField(row, typeof(Idle_Data), false, GUILayout.Width(widths[1]));
        if (EditorGUI.EndChangeCheck())
        {
            if (row != null && changedRow != row)
            {
                _selectedIdleAssetIds.Remove(rowId);
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
            bool removeEmpty = GUILayout.Button("X", GUILayout.Width(widths[8]));
            EditorGUILayout.EndHorizontal();
            return removeEmpty;
        }

        SerializedObject serializedRow = new SerializedObject(row);
        serializedRow.Update();

        DrawCompactProperty(serializedRow.FindProperty("displayName"), widths[2]);
        DrawCompactProperty(serializedRow.FindProperty("interval"), widths[3]);
        DrawCompactProperty(serializedRow.FindProperty("jobXPReward"), widths[4]);
        DrawCompactProperty(serializedRow.FindProperty("icon"), widths[5]);

        EditorGUILayout.LabelField(GetIdleMembershipLabel(row), GUILayout.Width(widths[6]));

        DrawOpenButton(row, widths[7]);

        bool removeRow = GUILayout.Button("X", GUILayout.Width(widths[8]));
        if (removeRow)
        {
            removeRow = DeleteAssetsWithConfirmation(new List<Idle_Data> { row }, "Idle_Data");
            if (removeRow)
            {
                _selectedIdleAssetIds.Remove(rowId);
                foreach (HashSet<int> idleSelectionSet in _selectedIdleIds.Values)
                {
                    idleSelectionSet.Remove(rowId);
                }
            }
        }

        EditorGUILayout.EndHorizontal();

        if (serializedRow.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(row);
        }

        return removeRow;
    }

    private void DrawMonstersCategory()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Monsters Category", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Sort By", GUILayout.Width(46f));
        EditorGUI.BeginChangeCheck();
        _monsterSort = (Monsters_Sort)EditorGUILayout.EnumPopup(_monsterSort, GUILayout.Width(120f));
        bool monsterSortChanged = EditorGUI.EndChangeCheck();
        bool monsterDesc = DrawSortDirection(_monsterSortDesc);
        if (monsterDesc != _monsterSortDesc) { _monsterSortDesc = monsterDesc; monsterSortChanged = true; }
        if (monsterSortChanged) SortMonsters();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _pendingMonsterToAdd = (Monster_Data)EditorGUILayout.ObjectField("Add Existing Monster", _pendingMonsterToAdd, typeof(Monster_Data), false);
        using (new EditorGUI.DisabledScope(_pendingMonsterToAdd == null))
        {
            if (GUILayout.Button("Add", GUILayout.Width(70f)))
            {
                AddUnique(_monsterRows, _pendingMonsterToAdd);
                _pendingMonsterToAdd = null;
            }
        }

        if (GUILayout.Button("New Monster_Data", GUILayout.Width(130f)))
        {
            Monster_Data created = CreateDataAsset<Monster_Data>(GetSettingsFolderPath(_settings?.monstersDataFolder), "Monster_Data");
            AddUnique(_monsterRows, created);
        }
        EditorGUILayout.EndHorizontal();

        DrawDropZone<Monster_Data>(
            "Drop Monster_Data assets here",
            droppedMonster =>
            {
                AddUnique(_monsterRows, droppedMonster);
            });

        DrawMonstersBulkActions();
        float[] monsterColumnWidths = GetTableColumnWidths(position.width - 28f, MonstersTableBaseWidths);
        DrawMonstersHeader(monsterColumnWidths);

        int removeIndex = -1;
        for (int i = 0; i < _monsterRows.Count; i++)
        {
            if (DrawMonsterRow(i, monsterColumnWidths))
            {
                removeIndex = i;
            }
        }

        if (removeIndex >= 0)
        {
            _monsterRows.RemoveAt(removeIndex);
        }
    }

    private void DrawMonstersHeader(float[] widths)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        DrawHeaderCell("Sel", widths[0]);
        DrawHeaderCell("Monster_Data", widths[1]);
        DrawHeaderCell("Name", widths[2]);
        DrawHeaderCell("Combat Lv", widths[3]);
        DrawHeaderCell("Atk Type", widths[4]);
        DrawHeaderCell("Speed", widths[5]);
        DrawHeaderCell("Rewards", widths[6]);
        DrawHeaderCell("X", widths[7]);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawMonstersBulkActions()
    {
        PruneSelection(_selectedMonsterIds, _monsterRows);

        int selectedCount = _selectedMonsterIds.Count;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Select All", GUILayout.Width(84f)))
        {
            foreach (Monster_Data monsterData in _monsterRows)
            {
                if (monsterData != null)
                {
                    _selectedMonsterIds.Add(monsterData.GetInstanceID());
                }
            }
        }

        if (GUILayout.Button("Clear", GUILayout.Width(56f)))
        {
            _selectedMonsterIds.Clear();
        }

        using (new EditorGUI.DisabledScope(selectedCount == 0))
        {
            if (GUILayout.Button("Delete Selected", GUILayout.Width(112f)))
            {
                List<Monster_Data> selectedMonsters = GetSelectedObjects(_monsterRows, _selectedMonsterIds);
                if (DeleteAssetsWithConfirmation(selectedMonsters, "Monster_Data"))
                {
                    HashSet<int> deletedIds = new();
                    foreach (Monster_Data monsterData in selectedMonsters)
                    {
                        if (monsterData != null)
                        {
                            deletedIds.Add(monsterData.GetInstanceID());
                        }
                    }

                    _monsterRows.RemoveAll(monsterData => monsterData == null || deletedIds.Contains(monsterData.GetInstanceID()));
                    _selectedMonsterIds.ExceptWith(deletedIds);
                }
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"Selected: {selectedCount}", GUILayout.Width(90f));

        EditorGUILayout.EndHorizontal();
    }

    private bool DrawMonsterRow(int rowIndex, float[] widths)
    {
        Monster_Data row = _monsterRows[rowIndex];
        int rowId = row != null ? row.GetInstanceID() : 0;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        bool selected = row != null && _selectedMonsterIds.Contains(rowId);
        bool nextSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(widths[0]));
        if (row != null && nextSelected != selected)
        {
            SetSelection(_selectedMonsterIds, rowId, nextSelected);
        }

        EditorGUI.BeginChangeCheck();
        Monster_Data changedRow = (Monster_Data)EditorGUILayout.ObjectField(row, typeof(Monster_Data), false, GUILayout.Width(widths[1]));
        if (EditorGUI.EndChangeCheck())
        {
            if (row != null && changedRow != row)
            {
                _selectedMonsterIds.Remove(rowId);
            }

            _monsterRows[rowIndex] = changedRow;
            row = changedRow;
            rowId = row != null ? row.GetInstanceID() : 0;
        }

        if (row == null)
        {
            float emptyWidth = widths[2] + widths[3] + widths[4] + widths[5] + widths[6];
            GUILayout.Label("Missing", GUILayout.Width(emptyWidth));
            bool removeEmpty = GUILayout.Button("X", GUILayout.Width(widths[7]));
            EditorGUILayout.EndHorizontal();
            return removeEmpty;
        }

        SerializedObject serializedRow = new SerializedObject(row);
        serializedRow.Update();

        DrawCompactProperty(serializedRow.FindProperty("monsterName"), widths[2]);
        EditorGUILayout.LabelField(row.CombatLevel.ToString(), GUILayout.Width(widths[3]));
        DrawCompactProperty(serializedRow.FindProperty("attackType"), widths[4]);
        DrawCompactProperty(serializedRow.FindProperty("speed"), widths[5]);

        DrawOpenButton(row, widths[6]);

        bool removeRow = GUILayout.Button("X", GUILayout.Width(widths[7]));
        if (removeRow)
        {
            removeRow = DeleteAssetsWithConfirmation(new List<Monster_Data> { row }, "Monster_Data");
            if (removeRow)
            {
                _selectedMonsterIds.Remove(rowId);
            }
        }

        EditorGUILayout.EndHorizontal();

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

        string jobsFolder = GetSettingsFolderPath(_settings?.jobsDataFolder);
        string itemsFolder = GetSettingsFolderPath(_settings?.itemsDataFolder);
        string idleFolder = GetSettingsFolderPath(_settings?.idleDataFolder);
        string monstersFolder = GetSettingsFolderPath(_settings?.monstersDataFolder);

        _jobRows.Clear();
        _itemRows.Clear();
        _idleRows.Clear();
        _monsterRows.Clear();
        _selectedJobIds.Clear();
        _selectedItemIds.Clear();
        _selectedIdleAssetIds.Clear();
        _selectedMonsterIds.Clear();
        _selectedIdleIds.Clear();
        _expandedJobRows.Clear();
        _pendingIdleAdds.Clear();

        _jobRows.AddRange(LoadAssets<Job_Data>(jobsFolder));
        _itemRows.AddRange(LoadAssets<ItemsData>(itemsFolder));
        _idleRows.AddRange(LoadAssets<Idle_Data>(idleFolder));
        _monsterRows.AddRange(LoadAssets<Monster_Data>(monstersFolder));

        SortJobs();
        SortItems();
        SortIdles();
        SortMonsters();

        Dictionary<int, Job_Data> jobsById = new();
        foreach (Job_Data jobData in _jobRows)
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

    private static void DrawHeaderCell(string text, float width)
    {
        GUILayout.Label(text, EditorStyles.miniBoldLabel, GUILayout.Width(width));
    }

    // --- Sorting (per tab; null/missing rows sink to the bottom, name is the tiebreaker) ---

    private static bool DrawSortDirection(bool descending)
    {
        return GUILayout.Toggle(descending, descending ? "Desc ▼" : "Asc ▲", EditorStyles.miniButton, GUILayout.Width(70f));
    }

    private void SortJobs() => _jobRows.Sort(CompareJobs);
    private void SortItems() => _itemRows.Sort(CompareItems);
    private void SortIdles() => _idleRows.Sort(CompareIdles);
    private void SortMonsters() => _monsterRows.Sort(CompareMonsters);

    private int CompareJobs(Job_Data a, Job_Data b)
    {
        if (a == b) return 0;
        if (a == null) return 1;
        if (b == null) return -1;
        int result = _jobSort switch
        {
            Jobs_Sort.Category => a.jobCategory.CompareTo(b.jobCategory),
            Jobs_Sort.MaxLevel => a.maxLevel.CompareTo(b.maxLevel),
            Jobs_Sort.IdleCount => (a.idleDatas?.Count ?? 0).CompareTo(b.idleDatas?.Count ?? 0),
            _ => 0
        };
        if (result == 0) result = CompareText(JobName(a), JobName(b));
        return _jobSortDesc ? -result : result;
    }

    private int CompareItems(ItemsData a, ItemsData b)
    {
        if (a == b) return 0;
        if (a == null) return 1;
        if (b == null) return -1;
        int result = _itemSort switch
        {
            Items_Sort.ID => a.itemID.CompareTo(b.itemID),
            Items_Sort.Type => a.itemType.CompareTo(b.itemType),
            Items_Sort.Price => a.price.CompareTo(b.price),
            _ => 0
        };
        if (result == 0) result = CompareText(ItemName(a), ItemName(b));
        return _itemSortDesc ? -result : result;
    }

    private int CompareIdles(Idle_Data a, Idle_Data b)
    {
        if (a == b) return 0;
        if (a == null) return 1;
        if (b == null) return -1;
        int result = _idleSort switch
        {
            Idles_Sort.Interval => a.interval.CompareTo(b.interval),
            Idles_Sort.JobXP => a.jobXPReward.CompareTo(b.jobXPReward),
            Idles_Sort.Kind => a.idleKind.CompareTo(b.idleKind),
            _ => 0
        };
        if (result == 0) result = CompareText(IdleName(a), IdleName(b));
        return _idleSortDesc ? -result : result;
    }

    private int CompareMonsters(Monster_Data a, Monster_Data b)
    {
        if (a == b) return 0;
        if (a == null) return 1;
        if (b == null) return -1;
        int result = _monsterSort switch
        {
            Monsters_Sort.CombatLevel => a.CombatLevel.CompareTo(b.CombatLevel),
            Monsters_Sort.AttackType => a.attackType.CompareTo(b.attackType),
            Monsters_Sort.Speed => a.speed.CompareTo(b.speed),
            _ => 0
        };
        if (result == 0) result = CompareText(MonsterName(a), MonsterName(b));
        return _monsterSortDesc ? -result : result;
    }

    private static int CompareText(string a, string b) => string.Compare(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    private static string JobName(Job_Data j) => j == null ? string.Empty : (string.IsNullOrWhiteSpace(j.jobName) ? j.name : j.jobName);
    private static string ItemName(ItemsData i) => i == null ? string.Empty : (string.IsNullOrWhiteSpace(i.displayName) ? i.name : i.displayName);
    private static string IdleName(Idle_Data i) => i == null ? string.Empty : (string.IsNullOrWhiteSpace(i.displayName) ? i.name : i.displayName);
    private static string MonsterName(Monster_Data m) => m == null ? string.Empty : (string.IsNullOrWhiteSpace(m.monsterName) ? m.name : m.monsterName);

    // Rewards are too complex to edit inline; this just selects + pings the asset so you
    // edit its rewards in the inspector.
    private static void DrawOpenButton(UnityEngine.Object asset, float width)
    {
        using (new EditorGUI.DisabledScope(asset == null))
        {
            if (GUILayout.Button("Open", GUILayout.Width(width)))
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }
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
                GUILayout.Label("-", GUILayout.Width(width));
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

    private HashSet<int> GetIdleSelectionSet(Job_Data jobData)
    {
        int jobId = jobData.GetInstanceID();
        if (!_selectedIdleIds.TryGetValue(jobId, out HashSet<int> selectionSet))
        {
            selectionSet = new HashSet<int>();
            _selectedIdleIds[jobId] = selectionSet;
        }

        return selectionSet;
    }

    private static void PruneIdleSelection(Job_Data jobData, HashSet<int> selectedIds)
    {
        HashSet<int> validIds = new();
        foreach (Idle_Data idleData in jobData.idleDatas)
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

    private void EnsureIdleList(Job_Data jobData)
    {
        if (jobData.idleDatas != null)
        {
            return;
        }

        Undo.RecordObject(jobData, "Initialize Idle_Data List");
        jobData.idleDatas = new List<Idle_Data>();
        EditorUtility.SetDirty(jobData);
    }

    private void AddIdleToJob(Job_Data jobData, Idle_Data idleData)
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

        Undo.RecordObject(jobData, "Add Idle_Data");
        jobData.idleDatas.Add(idleData);
        EditorUtility.SetDirty(jobData);
    }

    private void MoveIdleRow(Job_Data jobData, int fromIndex, int toIndex)
    {
        if (jobData == null || jobData.idleDatas == null)
        {
            return;
        }

        if (fromIndex < 0 || toIndex < 0 || fromIndex >= jobData.idleDatas.Count || toIndex >= jobData.idleDatas.Count || fromIndex == toIndex)
        {
            return;
        }

        Undo.RecordObject(jobData, "Reorder Idle_Data");
        Idle_Data movedRow = jobData.idleDatas[fromIndex];
        jobData.idleDatas.RemoveAt(fromIndex);
        jobData.idleDatas.Insert(toIndex, movedRow);
        EditorUtility.SetDirty(jobData);
    }

    private bool PassesItemFilter(ItemsData itemData)
    {
        if (_itemFilter == Catalog_ItemFilter.All)
        {
            return true;
        }

        return itemData.itemType == (Catalog_ItemType)(int)_itemFilter;
    }

    private bool PassesIdleJobFilter(Idle_Data idleData)
    {
        if (_idleCategoryFilterJob == null)
        {
            return true;
        }

        return _idleCategoryFilterJob.idleDatas != null && _idleCategoryFilterJob.idleDatas.Contains(idleData);
    }

    private string GetIdleMembershipLabel(Idle_Data idleData)
    {
        List<string> jobNames = new();
        foreach (Job_Data jobData in _jobRows)
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
