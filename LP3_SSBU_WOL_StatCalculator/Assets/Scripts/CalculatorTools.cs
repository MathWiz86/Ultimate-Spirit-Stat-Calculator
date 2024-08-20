// Copyright (c) Craig Williams, MathWiz86

using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// A helper class with tools useful for the stat calculator.
/// </summary>
public static class CalculatorTools
{
    /// <summary>A simple index to return when no data is set.</summary>
    public static readonly int NoneIndex = -1;

    /// <summary>
    /// Sanitizes a battle name so that it can be used for data indexing.
    /// </summary>
    /// <param name="inName">The given battle name.</param>
    /// <returns>Returns the cleaned up name.</returns>
    public static string SanitizeBattleName(string inName)
    {
        if (string.IsNullOrEmpty(inName))
            return string.Empty;

        inName = inName.ToLowerInvariant();

        // Edge cases for special characters. i.e. Pokémon Trainer and Mòrag.
        inName = inName.Replace('é', 'e');
        inName = inName.Replace('ò', 'o');

        return inName;
    }

    /// <summary>
    /// Creates a data path, with the given base name.
    /// </summary>
    /// <param name="baseFileName">The base file name.</param>
    /// <returns>Returns the finalized path.</returns>
    public static string CreateDataPath(string baseFileName)
    {
        if (!baseFileName.EndsWith(SaveDataManager.DataFileExtension))
            baseFileName = $"{baseFileName}{SaveDataManager.DataFileExtension}";

        string path = Path.Combine(SaveDataManager.DataDirectory, baseFileName);

        // Sanity check.
        if (!path.EndsWith(SaveDataManager.DataFileExtension))
            path = $"{path}{SaveDataManager.DataFileExtension}";

        return path;
    }

    /// <summary>
    /// Creates a save data file name, with the given base name. This has no directories.
    /// </summary>
    /// <param name="baseFileName">The base file name.</param>
    /// <returns>Returns the finalized path.</returns>
    public static string CreateSaveDataFileName(string baseFileName)
    {
        string path = baseFileName;

        if (!path.StartsWith(SaveDataManager.SaveDataFileHeader))
            path = $"{SaveDataManager.SaveDataFileHeader}{path}";

        if (!path.EndsWith(SaveDataManager.DataFileExtension))
            path = $"{path}{SaveDataManager.DataFileExtension}";

        return path;
    }

    /// <summary>
    /// Creates a save data path, with the given base name.
    /// </summary>
    /// <param name="baseFileName">The base file name.</param>
    /// <returns>Returns the finalized path.</returns>
    public static string CreateSaveDataPath(string baseFileName)
    {
        return Path.Combine(SaveDataManager.SaveDataDirectory, CreateSaveDataFileName(baseFileName));
    }

    /// <summary>
    /// Gets the display name of the given file. Essentially, chops off the file header and extension.
    /// </summary>
    /// <param name="inFileName">The processed file name.</param>
    /// <returns>Returns the display name.</returns>
    public static string GetSaveDataDisplayName(string inFileName)
    {
        string finalName = inFileName.Replace(SaveDataManager.SaveDataFileHeader, "");

        if (finalName.EndsWith(SaveDataManager.DataFileExtension))
            finalName = finalName.Remove(finalName.LastIndexOf(SaveDataManager.DataFileExtension, StringComparison.Ordinal));

        return finalName;
    }

    /// <summary>
    /// Destroys all child game objects attached to the given parent.
    /// </summary>
    /// <param name="inTransform">The root to remove children from.</param>
    public static void DestroyAllChildren(Transform inTransform)
    {
        if (!inTransform)
            return;

        foreach (Transform child in inTransform.GetComponentsInChildren<Transform>())
        {
            if (child == inTransform)
                continue;

            UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Checks to see if two floats are within a tolerant equality of each other.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <param name="tolerance">How much leeway is allowed between the two values.</param>
    /// <returns>Returns if the two values are within the given tolerance.</returns>
    public static bool IsNearlyEqual(float a, float b, float tolerance = 0.0001f)
    {
        return Math.Abs(a - b) < tolerance;
    }

    /// <summary>
    /// Parses out just numeric text from the given string, and turns it into an integer.
    /// </summary>
    /// <param name="inStr">The string to check.</param>
    /// <param name="foundValue">The resulting value.</param>
    /// <returns>Returns if a value was found.</returns>
    public static bool ParseOutNumberFromString(string inStr, out int foundValue)
    {
        inStr = Regex.Replace(inStr, @"[^\d]", "");
        return int.TryParse(inStr, out foundValue);
    }
}