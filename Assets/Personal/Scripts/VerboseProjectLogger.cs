using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EditorAttributes;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-10000)]
public class VerboseProjectLogger : MonoBehaviour
{
    enum LogStorageRoot
    {
        ProjectRoot,
        PersistentDataPath
    }

    struct PendingLogEntry
    {
        public LogType type;
        public string source;
        public string message;
        public string stackTrace;
    }

    static readonly object FileLock = new object();
    static readonly object PendingLock = new object();
    static readonly List<PendingLogEntry> PendingEntries = new List<PendingLogEntry>();

    const int MaxPendingEntries = 512;
    const bool DefaultEchoWithoutInstance = true;

    public static VerboseProjectLogger Instance { get; private set; }

    [Title("Play Session")]
    [SerializeField] bool clearLogFileOnPlay = true;

    [Title("Console Echo")]
    [SerializeField] bool echoLogToConsole = true;
    [SerializeField] bool echoWarningToConsole = true;
    [SerializeField] bool echoErrorToConsole = true;

    [Title("Output Settings")]
    [SerializeField] LogStorageRoot storageRoot = LogStorageRoot.ProjectRoot;
    [SerializeField] string relativeFolder = "Logs/Runtime";
    [SerializeField] string fileName = "verbose-log.txt";

    [Title("Runtime Debug")]
    [SerializeField, ReadOnly] string resolvedDirectory;
    [SerializeField, ReadOnly] string resolvedFilePath;
    [SerializeField, ReadOnly] string lastWriteError;

    bool outputReady;

    public static string CurrentLogFilePath => Instance != null ? Instance.resolvedFilePath : string.Empty;

    // Writes a normal log line to the project log file.
    public static void Log(string message)
    {
        Log("General", message);
    }

    // Writes a normal log line to the project log file with a custom source/category label.
    public static void Log(string source, string message)
    {
        AppendInternal(LogType.Log, source, message, string.Empty);
    }

    public static void LogWarning(string source, string message)
    {
        AppendInternal(LogType.Warning, source, message, string.Empty);
    }

    public static void LogWarning(string message)
    {
        LogWarning("General", message);
    }

    public static void LogError(string source, string message)
    {
        AppendInternal(LogType.Error, source, message, string.Empty);
    }

    public static void LogError(string message)
    {
        LogError("General", message);
    }

    // Backward-compatible aliases.
    public static void Append(string message)
    {
        Log(message);
    }

    public static void Append(string source, string message)
    {
        Log(source, message);
    }

    public static void AppendWarning(string source, string message)
    {
        LogWarning(source, message);
    }

    public static void AppendError(string source, string message)
    {
        LogError(source, message);
    }

    [Button]
    public void ClearLogFileNow()
    {
        EnsureOutputPathInitialized();
        lock (FileLock)
        {
            try
            {
                File.WriteAllText(resolvedFilePath, string.Empty, Encoding.UTF8);
                lastWriteError = string.Empty;
            }
            catch (Exception exception)
            {
                lastWriteError = exception.Message;
            }
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureOutputPathInitialized();

        if (clearLogFileOnPlay)
        {
            ClearLogFileNow();
        }

        WriteEntry(LogType.Log, "Logger", "=== Play Session Started ===", string.Empty, includeStackTrace: false, echoToConsole: false);
        FlushPendingEntries();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            WriteEntry(LogType.Log, "Logger", "=== Play Session Ended ===", string.Empty, includeStackTrace: false, echoToConsole: false);
            Instance = null;
        }
    }

    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(relativeFolder))
        {
            relativeFolder = "Logs/Runtime";
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "verbose-log.txt";
        }

        if (!fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".txt";
        }

        outputReady = false;
        EnsureOutputPathInitialized();
    }

    static void AppendInternal(LogType logType, string source, string message, string stackTrace)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        VerboseProjectLogger logger = Instance;
        if (logger != null)
        {
            logger.WriteEntry(logType, source, message, stackTrace, includeStackTrace: true, echoToConsole: logger.ShouldEchoToConsole(logType));
            return;
        }

        if (DefaultEchoWithoutInstance)
        {
            WriteToUnityConsole(logType, source, message);
        }

        // If scripts log before the logger wakes up, we queue entries and flush once ready.
        lock (PendingLock)
        {
            if (PendingEntries.Count >= MaxPendingEntries)
            {
                PendingEntries.RemoveAt(0);
            }

            PendingEntries.Add(new PendingLogEntry
            {
                type = logType,
                source = source,
                message = message,
                stackTrace = stackTrace
            });
        }
    }

    void FlushPendingEntries()
    {
        List<PendingLogEntry> snapshot = null;

        lock (PendingLock)
        {
            if (PendingEntries.Count == 0)
            {
                return;
            }

            snapshot = new List<PendingLogEntry>(PendingEntries);
            PendingEntries.Clear();
        }

        for (int i = 0; i < snapshot.Count; i++)
        {
            PendingLogEntry entry = snapshot[i];
            WriteEntry(
                entry.type,
                entry.source,
                entry.message,
                entry.stackTrace,
                includeStackTrace: true,
                echoToConsole: ShouldEchoToConsole(entry.type));
        }
    }

    void EnsureOutputPathInitialized()
    {
        if (outputReady)
        {
            return;
        }

        string baseDirectory = ResolveBaseDirectory();
        string cleanRelativeFolder = relativeFolder.Replace('\\', '/').Trim();
        string file = fileName.Trim();

        if (string.IsNullOrWhiteSpace(file))
        {
            file = "verbose-log.txt";
        }

        string outputDirectory = string.IsNullOrWhiteSpace(cleanRelativeFolder)
            ? baseDirectory
            : Path.Combine(baseDirectory, cleanRelativeFolder);

        resolvedDirectory = Path.GetFullPath(outputDirectory);
        resolvedFilePath = Path.Combine(resolvedDirectory, file);

        try
        {
            Directory.CreateDirectory(resolvedDirectory);
            if (!File.Exists(resolvedFilePath))
            {
                File.WriteAllText(resolvedFilePath, string.Empty, Encoding.UTF8);
            }

            outputReady = true;
            lastWriteError = string.Empty;
        }
        catch (Exception exception)
        {
            outputReady = false;
            lastWriteError = exception.Message;
        }
    }

    string ResolveBaseDirectory()
    {
        if (storageRoot == LogStorageRoot.ProjectRoot)
        {
            try
            {
                DirectoryInfo parentDirectory = Directory.GetParent(Application.dataPath);
                if (parentDirectory != null)
                {
                    return parentDirectory.FullName;
                }
            }
            catch
            {
                // Fallback below if project-root resolution is unavailable.
            }
        }

        return Application.persistentDataPath;
    }

    void WriteEntry(LogType logType, string source, string message, string stackTrace, bool includeStackTrace, bool echoToConsole)
    {
        EnsureOutputPathInitialized();
        if (!outputReady)
        {
            if (echoToConsole)
            {
                WriteToUnityConsole(logType, source, message);
            }

            return;
        }

        string line = BuildFormattedLogEntry(logType, source, message, stackTrace, includeStackTrace);

        lock (FileLock)
        {
            try
            {
                File.AppendAllText(resolvedFilePath, line, Encoding.UTF8);
                lastWriteError = string.Empty;
            }
            catch (Exception exception)
            {
                lastWriteError = exception.Message;
            }
        }

        if (echoToConsole)
        {
            WriteToUnityConsole(logType, source, message);
        }
    }

    bool ShouldEchoToConsole(LogType logType)
    {
        return logType switch
        {
            LogType.Warning => echoWarningToConsole,
            LogType.Error => echoErrorToConsole,
            LogType.Assert => echoErrorToConsole,
            LogType.Exception => echoErrorToConsole,
            _ => echoLogToConsole
        };
    }

    static void WriteToUnityConsole(LogType logType, string source, string message)
    {
        string cleanSource = string.IsNullOrWhiteSpace(source) ? "General" : source;
        string finalMessage = $"[{cleanSource}] {message}";

        switch (logType)
        {
            case LogType.Warning:
                Debug.LogWarning(finalMessage);
                break;
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                Debug.LogError(finalMessage);
                break;
            default:
                Debug.Log(finalMessage);
                break;
        }
    }

    string BuildFormattedLogEntry(LogType logType, string source, string message, string stackTrace, bool includeStackTrace)
    {
        StringBuilder builder = new StringBuilder(512);
        builder.Append('[')
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append("] [")
            .Append(logType)
            .Append("] [")
            .Append(string.IsNullOrWhiteSpace(source) ? "Unknown" : source)
            .Append("] ")
            .AppendLine(message ?? string.Empty);

        if (includeStackTrace && !string.IsNullOrWhiteSpace(stackTrace))
        {
            builder.AppendLine(stackTrace);
        }

        return builder.ToString();
    }
}
