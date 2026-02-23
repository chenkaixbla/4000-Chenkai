using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class Catalog_DataSettingsWindow : EditorWindow
{
    private const string SettingsAssetPath = "Assets/Personal/Scripts/Catalog/Catalog_DataSettings.asset";

    private Catalog_DataSettings _settings;

    [MenuItem("Tools/Catalog/Spreadsheet Settings")]
    public static void OpenWindow()
    {
        Catalog_DataSettingsWindow window = GetWindow<Catalog_DataSettingsWindow>("Catalog Settings");
        window.minSize = new Vector2(520f, 170f);
        window.Show();
    }

    private void OnEnable()
    {
        LoadOrCreateSettings();
    }

    private void OnGUI()
    {
        if (_settings == null)
        {
            LoadOrCreateSettings();
        }

        if (_settings == null)
        {
            EditorGUILayout.HelpBox("Unable to load catalog settings.", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Catalog Data Folders", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        string jobsDataFolder = DrawFolderSettingField("Jobs Data Folder", _settings.jobsDataFolder);
        string itemsDataFolder = DrawFolderSettingField("Items Data Folder", _settings.itemsDataFolder);
        string idleDataFolder = DrawFolderSettingField("Idle Data Folder", _settings.idleDataFolder);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_settings, "Update Catalog Settings");
            _settings.jobsDataFolder = NormalizePath(jobsDataFolder);
            _settings.itemsDataFolder = NormalizePath(itemsDataFolder);
            _settings.idleDataFolder = NormalizePath(idleDataFolder);
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets();
            Catalog_DataSpreadsheetWindow.RefreshAllOpenWindows();
        }

        DrawFolderValidation(_settings.jobsDataFolder, "Jobs Data folder path is invalid. Catalog will use global search.");
        DrawFolderValidation(_settings.itemsDataFolder, "Items Data folder path is invalid. Catalog will use global search.");
        DrawFolderValidation(_settings.idleDataFolder, "Idle Data folder path is invalid. Catalog will use global search.");

        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Spreadsheet"))
        {
            Catalog_DataSpreadsheetWindow.RefreshAllOpenWindows();
        }

        if (GUILayout.Button("Ping Settings Asset"))
        {
            EditorGUIUtility.PingObject(_settings);
            Selection.activeObject = _settings;
        }
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawFolderValidation(string folderPath, string message)
    {
        if (!AssetDatabase.IsValidFolder(NormalizePath(folderPath)))
        {
            EditorGUILayout.HelpBox(message, MessageType.Info);
        }
    }

    private static string DrawFolderSettingField(string label, string currentPath)
    {
        EditorGUILayout.BeginHorizontal();

        string value = EditorGUILayout.DelayedTextField(label, NormalizePath(currentPath));
        if (GUILayout.Button("Browse", GUILayout.Width(64f)))
        {
            if (TryPickProjectFolder(label, value, out string selectedPath))
            {
                value = selectedPath;
                GUI.changed = true;
            }
        }

        EditorGUILayout.EndHorizontal();
        return value;
    }

    private static bool TryPickProjectFolder(string label, string currentPath, out string selectedPath)
    {
        string normalizedCurrentPath = NormalizePath(currentPath);
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        string startFolder = Application.dataPath;
        if (AssetDatabase.IsValidFolder(normalizedCurrentPath))
        {
            startFolder = Path.Combine(projectRoot, normalizedCurrentPath);
        }

        string pickedFolder = EditorUtility.OpenFolderPanel($"Select {label}", startFolder, string.Empty);
        if (string.IsNullOrWhiteSpace(pickedFolder))
        {
            selectedPath = normalizedCurrentPath;
            return false;
        }

        string projectRelativePath = FileUtil.GetProjectRelativePath(pickedFolder).Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(projectRelativePath) || !projectRelativePath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("Invalid Folder", "Select a folder inside this project's Assets directory.", "OK");
            selectedPath = normalizedCurrentPath;
            return false;
        }

        selectedPath = NormalizePath(projectRelativePath);
        return true;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Assets";
        }

        return path.Replace('\\', '/').TrimEnd('/');
    }

    private void LoadOrCreateSettings()
    {
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
        string normalizedPath = NormalizePath(folderPath);
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
}
