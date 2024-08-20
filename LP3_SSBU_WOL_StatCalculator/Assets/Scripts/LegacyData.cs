// Copyright (c) Craig Williams, MathWiz86

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

/// <summary>
/// A manager for any legacy save data. If possible, save data created in older versions of the calculator can be reprocessed into new versions.
/// Place any legacy saves in the '_data/_legacy' folder of the calculator. If you are using this yourself, create a new <see cref="CalculatorSaveData_Legacy_Base"/>
/// inheriting class, which then attempts to reformat the data.
/// </summary>
public static class LegacyDataManager
{
    /// <summary>
    /// Base class for preparing legacy save data. This is only so that <see cref="LegacyData{T}"/> can be added to collections.
    /// </summary>
    private abstract class LegacyData
    {
        /// <summary>
        /// Attempts to create some <see cref="CalculatorSaveData_Legacy_Base"/> data for the given file.
        /// </summary>
        /// <param name="inFile">The file to parse.</param>
        /// <returns>Returns a created <see cref="CalculatorSaveData_Legacy_Base"/>, if able.</returns>
        public abstract CalculatorSaveData_Legacy_Base TryCreateLegacyData(FileInfo inFile);
    }

    /// <summary>
    /// A container for creating legacy save data.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="CalculatorSaveData_Legacy_Base"/> to be created.</typeparam>
    private sealed class LegacyData<T> : LegacyData where T : CalculatorSaveData_Legacy_Base
    {
        public override CalculatorSaveData_Legacy_Base TryCreateLegacyData(FileInfo inFile)
        {
            if (inFile == null)
                return null;

            string fileData;

            try
            {
                using StreamReader reader = new StreamReader(inFile.FullName);
                fileData = reader.ReadToEnd();
                reader.Close();
            }
            catch (Exception e)
            {
                LogManager.WriteLogLine(LogType.Error, $"Failed to read legacy save file at path [{inFile.FullName}]. Exception: [{e}]");
                return null;
            }

            if (string.IsNullOrWhiteSpace(fileData))
                return null;

            // Separate the try-catch, as this may fail to deserialize.
            try
            {
                // If we can deserialize properly, and the data is valid for this particular legacy data, return this save.
                T legacySave = JsonConvert.DeserializeObject<T>(fileData);
                if (legacySave != null && !legacySave.IsValidLegacyData(fileData))
                    return null;

                return legacySave;
            }
            catch
            {
                // It's okay to fail to create the legacy save. This file simply wasn't for this type.
                return null;
            }
        }
    }

    /// <summary>The directory to place legacy save data.</summary>
    private static string LegacyDataDirectory { get { return $@"{SaveDataManager.DataDirectory}_legacy\"; } }

    /// <summary>The different types of <see cref="LegacyData{T}"/>. Create a new entry for each ty pe of <see cref="CalculatorSaveData_Legacy_Base"/>.</summary>
    private static readonly List<LegacyData> _legacyDataEntries = new List<LegacyData> { new LegacyData<CalculatorSaveData_Legacy_V0_0>() };

    /// <summary>
    /// Gathers all legacy save data files, and attempts to convert them into new save data.
    /// </summary>
    public static void UpdateLegacyData()
    {
        // Ensure the directory exists.
        DirectoryInfo legacyDir;
        try
        {
            legacyDir = Directory.CreateDirectory(LegacyDataDirectory);
        }
        catch (Exception e)
        {
            LogManager.WriteLogLine(LogType.Error, $"Unable to get or create legacy data directory at path [{LegacyDataDirectory}]. Exception: [{e}]");
            return;
        }

        // For each file, attempt to get legacy data for it, and convert it to updated data.
        IEnumerable<FileInfo> files = legacyDir.EnumerateFiles();
        foreach (FileInfo file in files)
        {
            CalculatorSaveData_Legacy_Base legacySave = TryGetLegacyData(file);
            if (legacySave == null)
                continue;

            CalculatorSaveData convertedData = legacySave.ConvertToNewSaveData();
            if (convertedData == null)
            {
                LogManager.WriteLogLine(LogType.Error, $"Failed to convert legacy save data [{file.Name}] of type [{legacySave.GetType()}].");
                continue;
            }

            convertedData.FileName = $"legacy_{CalculatorTools.GetSaveDataDisplayName(file.Name)}";
            LogManager.WriteLogLine(LogType.Log, $"Successfully converted legacy save data for file [{file.Name}]");
            SaveDataManager.WriteSaveData(convertedData);
        }
    }

    /// <summary>
    /// Attempts to create <see cref="CalculatorSaveData_Legacy_Base"/> save data for the given file.
    /// </summary>
    /// <param name="inFile">The file to parse.</param>
    /// <returns>Returns the created <see cref="CalculatorSaveData_Legacy_Base"/> save data.</returns>
    private static CalculatorSaveData_Legacy_Base TryGetLegacyData(FileInfo inFile)
    {
        if (_legacyDataEntries == null)
            return null;

        // Go through each entry, and attempt to parse the file into its associated legacy data.
        foreach (LegacyData legacyData in _legacyDataEntries)
        {
            CalculatorSaveData_Legacy_Base legacySave = legacyData.TryCreateLegacyData(inFile);
            if (legacySave == null)
                continue;

            LogManager.WriteLogLine(LogType.Log, $"Successfully loaded legacy save data at file [{inFile.Name}] as [{legacySave.GetType()}].");
            return legacySave;
        }

        LogManager.WriteLogLine(LogType.Warning, $"Failed to find legacy save data converter for file [{inFile.Name}]");
        return null;
    }
}

/// <summary>
/// A base class for legacy <see cref="CalculatorSaveData"/> created for older versions of the calculator. Inherit this, and add it as an extra type in
/// <see cref="LegacyDataManager._legacyDataEntries"/>.
/// </summary>
public abstract class CalculatorSaveData_Legacy_Base
{
    /// <summary>See: <see cref="CalculatorSaveData._saveVersion"/></summary>
    [JsonProperty] protected int _saveVersion;

    /// <summary>
    /// Checks if this legacy save data is actually valid for the JSON file parsed.
    /// </summary>
    /// <param name="dataString">The original data from the file, if necessary.</param>
    /// <returns>Returns if this data can convert the current file.</returns>
    public abstract bool IsValidLegacyData(string dataString);

    /// <summary>
    /// Converts the stored legacy data into updated <see cref="CalculatorSaveData"/>.
    /// </summary>
    /// <returns></returns>
    public abstract CalculatorSaveData ConvertToNewSaveData();
}

/// <summary>
/// Legacy version of <see cref="CalculatorSaveData"/>.
/// This represents files created in the pre-public version of the calculator, used during the run of the original Let's Play.
/// </summary>
[Serializable]
public class CalculatorSaveData_Legacy_V0_0 : CalculatorSaveData_Legacy_Base
{
    /// <summary>
    /// Legacy version of <see cref="BattleEntry"/>.
    /// This represents battle entries for <see cref="CalculatorSaveData_Legacy_V0_0"/>.
    /// </summary>
    [Serializable]
    private struct BattleEntry_Legacy
    {
        /// <summary>The type of the battle.</summary>
        public BattleType Type;

        /// <summary>The index of the winning player. Originally an enum of size 4, with 0 being 'None'.</summary>
        public int WinningPlayer;

        /// <summary>The index of the winning player. This can get reset to invalid.</summary>
        public bool bSharedBattle;

        /// <summary>The number of losses each player had. The key was originally an enum, with 0 being 'None'.</summary>
        public Dictionary<string, int> PlayerLosses;
    }

    /// <summary>The number of values in the original player enum.</summary>
    private const int PlayerEnumCount = 4;

    /// <summary>See: <see cref="CalculatorSaveData.lastAddedBattle"/></summary>
    [JsonProperty] private string LastAddedBattle = string.Empty;

    /// <summary>The original dictionary of <see cref="BattleEntry"/> data. The enum has been replaced with a string value.</summary>
    [JsonProperty] private Dictionary<string, BattleEntry_Legacy> BattleStats;

    public override bool IsValidLegacyData(string dataString)
    {
        return _saveVersion == 0; // This is intended for pre-release save data.
    }

    public override CalculatorSaveData ConvertToNewSaveData()
    {
        CalculatorSaveData data = new CalculatorSaveData();

        data.lastAddedBattle = LastAddedBattle;

        // Add all players, under generic names.
        List<string> players = new List<string>();
        for (int i = 0; i < PlayerEnumCount - 1; ++i)
        {
            players.Add($"Player {i + 1}");
        }

        data.SetPlayers(players);
        
        BattleStats ??= new Dictionary<string, BattleEntry_Legacy>();
        foreach (KeyValuePair<string, BattleEntry_Legacy> legacyEntry in BattleStats)
        {
            BattleEntry newEntry = new BattleEntry();
            newEntry.Validate();

            newEntry.type = legacyEntry.Value.Type;
            newEntry.isBattleShared = legacyEntry.Value.bSharedBattle;

            // Index 0 is 'None', so we decrement by 1.
            newEntry.winningPlayer = legacyEntry.Value.WinningPlayer > 0 ? legacyEntry.Value.WinningPlayer - 1 : CalculatorTools.NoneIndex;

            // Ensure losses for all players are transferred, ensuring there's entries for everyone.
            int highestLossCount = 0;
            Dictionary<int, int> convertedLosses = ConvertLosses(legacyEntry.Value.PlayerLosses);
            for (int i = 0; i < PlayerEnumCount - 1; ++i)
            {
                // Apply losses for every player, even if they didn't have any before, to ensure accurate counts.
                convertedLosses.TryGetValue(i, out int losses);
                newEntry.UpdateLoss(i, newEntry.isBattleShared ? 0 : losses);

                highestLossCount = Math.Max(highestLossCount, losses);
            }

            // Apply losses for a shared battle. In this version, this was the highest player loss count.
            if (newEntry.isBattleShared)
                newEntry.UpdateLoss(CalculatorTools.NoneIndex, highestLossCount);

            // Add the new entry.
            newEntry.Validate();
            data.AddOrUpdateBattleEntry(legacyEntry.Key, newEntry);
        }

        return data;
    }

    /// <summary>
    /// Converts the losses stored in <see cref="BattleEntry_Legacy"/> to a proper player index map.
    /// </summary>
    /// <param name="inLosses">The original loss dictionary.</param>
    /// <returns>Returns the converted collection.</returns>
    private Dictionary<int, int> ConvertLosses(Dictionary<string, int> inLosses)
    {
        Dictionary<int, int> fixedLosses = new Dictionary<int, int>();
        foreach (KeyValuePair<string, int> loss in inLosses)
        {
            int key = CalculatorTools.NoneIndex;

            // Go based on the original initials of the players.
            if (loss.Key.StartsWith('M'))
                key = 0;
            else if (loss.Key.StartsWith('S'))
                key = 1;
            else if (loss.Key.StartsWith('P'))
                key = 2;

            if (key >= 0)
                fixedLosses.Add(key, loss.Value);
        }

        return fixedLosses;
    }
}