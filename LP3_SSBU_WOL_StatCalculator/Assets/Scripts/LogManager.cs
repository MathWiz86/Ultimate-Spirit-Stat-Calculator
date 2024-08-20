// Copyright (c) Craig Williams, MathWiz86

using System;
using System.IO;
using UnityEngine;

/// <summary>
/// A log type. Helps categorize logs.
/// </summary>
public enum LogType
{
    Log,
    Warning,
    Error,
    Fatal,
    Verbose // Editor only logs.
}

/// <summary>
/// A simple log manager to write messages to a file.
/// </summary>
public class LogManager : MonoBehaviour
{
    /// <summary>The max number of log files to keep before deleting the oldest.</summary>
    private const int MaxLogFiles = 5;

    /// <summary>A custom file extension to separate out log files from other files.</summary>
    private static readonly string LogFileExtension = ".calclog";

    /// <summary>The log manager singleton.</summary>
    private static LogManager _manager;

    /// <summary>The main log directory.</summary>
    private string LogDirectory { get { return $@"{SaveDataManager.DataDirectory}_logs\"; } }

    /// <summary>The header of every log file.</summary>
    private string LogFileNameBase { get { return "SpiritCalc_Log_"; } }

#if UNITY_EDITOR
    /// <summary>Allows verbose logging in the editor.</summary>
    [SerializeField] private bool allowVerboseLogs = true;
#endif

    /// <summary>The current path to the active log. Lines in the current session are logged here.</summary>
    private string _currentLogPath;

    private void Awake()
    {
        if (_manager != null)
            return;
        _manager = this;

        UpdateCurrentLogFiles();
    }

    /// <summary>
    /// Writes the given log line to the file.
    /// </summary>
    /// <param name="type">The type of the log.</param>
    /// <param name="log">The log text.</param>
    /// <returns>Returns if a line was successfully written to the file.</returns>
    public static bool WriteLogLine(LogType type, string log)
    {
        return _manager && _manager.WriteLine(type, log);
    }

    /// <summary>
    /// Validates the given <see cref="statement"/> is true. If not, logs the provided line.
    /// </summary>
    /// <param name="statement">The statement to check.</param>
    /// <param name="type">The type of the log.</param>
    /// <param name="log">The log text to write if false.</param>
    /// <returns>Returns if the <see cref="statement"/> is true or false.</returns>
    public static bool ValidateOrLog(bool statement, LogType type, string log)
    {
        if (!statement)
            WriteLogLine(type, log);

        return statement;
    }

    /// <summary>
    /// If there are more than <see cref="MaxLogFiles"/>, the oldest is deleted. After this, a new log file is created.
    /// </summary>
    private void UpdateCurrentLogFiles()
    {
        if (!string.IsNullOrEmpty(_currentLogPath) && File.Exists(_currentLogPath))
            return;

        DirectoryInfo logDir;

        try
        {
            logDir = Directory.CreateDirectory(LogDirectory);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to create logging file. Exception: {e}");
            return;
        }

        // Delete files over the limit. We need at least one file of space before our max.
        int fileCount = 0;
        FileInfo[] files = logDir.GetFiles($"{LogFileNameBase}*{LogFileExtension}");
        for (int i = files.Length - 1; i >= 0; --i)
        {
            if (++fileCount < MaxLogFiles)
                continue;

            files[i].Delete();
        }

        // Create a new file.
        _currentLogPath = Path.Combine(LogDirectory, $"{LogFileNameBase}{DateTime.Now:yyyy-MM-dd_HH-mm-ss-f}{LogFileExtension}");
        FileStream fileStream = File.Create(_currentLogPath);
        fileStream.Close();

        // Log a success.
        if (!File.Exists(_currentLogPath))
            return;

        WriteLogLine(LogType.Log, "New Spirit Calculator Log Started");
    }

    /// <summary>
    /// Writes the given log line to the file.
    /// </summary>
    /// <param name="type">The type of the log.</param>
    /// <param name="log">The log text.</param>
    /// <returns>Returns if a line was successfully written to the file.</returns>
    private bool WriteLine(LogType type, string log)
    {
#if UNITY_EDITOR
        // Just act like we wrote the verbose line if we didn't actually write it.
        if (type == LogType.Verbose && !allowVerboseLogs)
            return true;
#endif

        if (string.IsNullOrEmpty(log) || string.IsNullOrWhiteSpace(log))
            return false;

        string finalLogLine = $"[{DateTime.Now:yyyy-MM-dd_HH-mm-ss-f} - {type.ToString()}]: {log}";
        Debug.Log(finalLogLine);

        // If we were unable to create a log, we at least wrote it to the console.
        if (string.IsNullOrEmpty(_currentLogPath))
            return true;

        try
        {
            using (StreamWriter writer = new StreamWriter(_currentLogPath, true))
            {
                writer.WriteLine(finalLogLine);
                writer.Close();
            }
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.Log($"Failed to write log line! {e}");
#endif
            return false;
        }

        if (type == LogType.Fatal)
            Application.Quit();

        return true;
    }
}