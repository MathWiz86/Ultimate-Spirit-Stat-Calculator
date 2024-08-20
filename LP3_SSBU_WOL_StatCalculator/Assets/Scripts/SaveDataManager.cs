// Copyright (c) Craig Williams, MathWiz86

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Save data used for individual <see cref="CalculatorSaveData"/>.
/// </summary>
[Serializable]
public class SaveDataSettings : ICalculatorData
{
    /// <summary>The names of players. Does not include 'None'.</summary>
    public List<string> players = new List<string>();

    public bool Validate()
    {
        bool isValid = true;

        if (players == null || players.Count == 0)
        {
            players = new List<string> { "Player 1", "Player 2", "Player 3" };
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Gets the <see cref="players"/> count.
    /// </summary>
    /// <returns>Returns the player count.</returns>
    public int GetPlayerCount()
    {
        return players?.Count ?? 0;
    }
}

/// <summary>
/// Settings to display by default when creating new user save data.
/// </summary>
[Serializable]
public class DefaultSaveCreationSettings : ICalculatorData
{
    /// <summary>The default <see cref="SaveDataSettings"/> for the file.</summary>
    public SaveDataSettings defaultSettings;

    // Bosses to add when creating new World of Light save data
    public List<string> bosses;

    public bool Validate()
    {
        bool isValid = true;

        if (defaultSettings == null)
        {
            defaultSettings = new SaveDataSettings();
            isValid = false;
        }

        if (bosses == null)
        {
            bosses = new List<string>
                     {
                         "giga bowser",
                         "galleom",
                         "rathalos",
                         "master hand", // True Master Hand in the Final Realm
                         "master hand (light realm)",
                         "master hand (final realm)", // Fake Master Hand in the Final Realm
                         "master hand gauntlet", // Master Hand vs. 50 Fighters
                         "crazy hand", // True Crazy Hand in the Final Realm
                         "crazy hand (sacred land)",
                         "crazy hand (mysterious dimension)",
                         "crazy hand (dracula's castle)",
                         "crazy hand (final realm)", // Fake Crazy Hand in the Final Realm
                         "ganon",
                         "marx",
                         "dracula",
                         "galeem (light realm)",
                         "galeem (final realm)", // Solo Galeem Final Boss
                         "dharkon (dark realm)",
                         "dharkon (final realm)", // Solo Dharkon Final Boss
                         "galeem & dharkon (phase 1)",
                         "galeem & dharkon (phase 2)",
                         "galeem & dharkon (phase 3)"
                     };

            isValid = false;
        }

        isValid &= defaultSettings.Validate();

        return isValid;
    }
}

/// <summary>
/// Save data for a playthrough of Smash Ultimate's Spirit Board or World of Light.
/// </summary>
[Serializable]
public class CalculatorSaveData : ICalculatorData
{
    public delegate void CalculatorSaveDataDelegate(CalculatorSaveData saveData);

    /// <summary>An event called when a new <see cref="BattleEntry"/> is added or removed.</summary>
    public event CalculatorSaveDataDelegate OnBattleEntryListUpdated;

    /// <summary>The save data version. Increment this if modifying this class in a way that cannot auto-resolve, such as a variable changing name or type.</summary>
    private const int CurrentSaveDataVersion = 1;

    /// <summary>This save data's file name, without the path.</summary>
    [JsonIgnore] public string FileName
    {
        get { return _fileName; } set { _fileName = string.IsNullOrWhiteSpace(_fileName) ? value : _fileName; }
    }

    /// <summary>The name of the last added battle.</summary>
    public string lastAddedBattle = string.Empty;

    /// <summary>The version of this save data. Used in promoting legacy data.</summary>
    [JsonProperty] private int _saveVersion;

    /// <summary>The general settings of this save.</summary>
    [JsonProperty] private SaveDataSettings _settings = new SaveDataSettings();

    /// <summary>Data on each battle, indexed by the sanitized name of the entity.</summary>
    [JsonProperty] private Dictionary<string, BattleEntry> _battleEntries = new Dictionary<string, BattleEntry>();

    /// <summary>See: <see cref="FileName"/></summary>
    [JsonIgnore] private string _fileName;

    public CalculatorSaveData() { }

    public CalculatorSaveData(string fileName)
    {
        FileName = fileName;
    }

    public bool Validate()
    {
        bool isValid = true;

        if (_battleEntries == null)
        {
            _battleEntries = new Dictionary<string, BattleEntry>();
            isValid = false;
        }

        Dictionary<string, BattleEntry> entryCopy = new Dictionary<string, BattleEntry>(_battleEntries);
        foreach (KeyValuePair<string, BattleEntry> entry in entryCopy)
        {
            bool isEntryValid = entry.Value.Validate();
            isValid &= isEntryValid;
            _battleEntries[entry.Key] = entry.Value;

            if (!isValid)
                LogManager.WriteLogLine(LogType.Warning, $"Battle Entry {entry.Key} failed validation!");
        }

        if (_saveVersion != CurrentSaveDataVersion)
        {
            _saveVersion = CurrentSaveDataVersion;
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Sets the player list directly.
    /// </summary>
    /// <param name="inPlayers">The players to add.</param>
    public void SetPlayers(List<string> inPlayers)
    {
        if (_settings == null || inPlayers == null)
            return;

        _settings.players.Clear();
        _settings.players.AddRange(inPlayers);
    }

    /// <summary>
    /// Gets if the given index is a valid player.
    /// </summary>
    /// <param name="inIndex">The index to check.</param>
    /// <returns>Returns if <see cref="inIndex"/> represents a valid player.</returns>
    public bool IsValidPlayer(int inIndex)
    {
        return inIndex >= 0 && inIndex < _settings.GetPlayerCount();
    }

    /// <summary>
    /// Gets the player count for this save data.
    /// </summary>
    /// <returns>Returns the data's player count.</returns>
    public int GetPlayerCount()
    {
        return _settings?.GetPlayerCount() ?? 0;
    }

    /// <summary>
    /// Gets the player's name for the given player index.
    /// </summary>
    /// <param name="index">The index of the player.</param>
    /// <returns>Returns the player's name. Returns null if the index is invalid.</returns>
    public string GetPlayerName(int index)
    {
        if (index >= 0 && index < _settings.players.Count)
            return _settings.players[index];

        return null;
    }

    /// <summary>
    /// Adds a <see cref="BattleEntry"/>, or updates an existing one if it didn't already exist.
    /// </summary>
    /// <param name="name">The name of the entry.</param>
    /// <param name="entry">The <see cref="BattleEntry"/> to add.</param>
    public void AddOrUpdateBattleEntry(string name, BattleEntry entry)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        string fixedName = CalculatorTools.SanitizeBattleName(name);
        bool added = _battleEntries.TryAdd(fixedName, new BattleEntry());
        _battleEntries[fixedName] = entry;

        LogManager.WriteLogLine(LogType.Log, $"Successfully added or updated Battle Entry [{fixedName}, {entry.type}] to save data [{FileName}]");

        if (!added)
            return;

        lastAddedBattle = fixedName;
        OnBattleEntryListUpdated?.Invoke(this);
    }

    /// <summary>
    /// Removes a <see cref="BattleEntry"/> from the save data.
    /// </summary>
    /// <param name="name">The name of the <see cref="BattleEntry"/> to remove.</param>
    /// <returns>Returns if the entry was successfully removed.</returns>
    public bool RemoveBattleEntry(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        string fixedName = CalculatorTools.SanitizeBattleName(name);
        if (!_battleEntries.Remove(fixedName))
        {
            LogManager.WriteLogLine(LogType.Warning, $"Failed to remove battle entry [{name}] from save data [{_fileName}].");
            return false;
        }

        LogManager.WriteLogLine(LogType.Log, $"Removed battle entry [{name}] from save data [{_fileName}].");
        OnBattleEntryListUpdated?.Invoke(this);
        return true;
    }

    /// <summary>
    /// Gets the <see cref="BattleEntry"/> for the given key.
    /// </summary>
    /// <param name="name">The battle's key name.</param>
    /// <param name="outEntry">The found entry.</param>
    /// <returns>Returns whether the entry was found.</returns>
    public bool GetBattleEntry(string name, out BattleEntry outEntry)
    {
        outEntry = new BattleEntry();

        if (string.IsNullOrWhiteSpace(name))
            return false;

        string fixedName = CalculatorTools.SanitizeBattleName(name);
        bool result = _battleEntries.TryGetValue(fixedName, out outEntry);

        // Make a copy, so that any references are not the same.
        outEntry = new BattleEntry(outEntry);
        return result;
    }

    /// <summary>
    /// Gets a list of all <see cref="_battleEntries"/> keys.
    /// </summary>
    /// <returns>Returns all keys in <see cref="_battleEntries"/>.</returns>
    public List<string> GetBattleEntryKeys()
    {
        List<string> output = new List<string>();
        output.AddRange(_battleEntries.Keys);
        return output;
    }

    /// <summary>
    /// Iterates through all <see cref="_battleEntries"/>.
    /// </summary>
    /// <param name="inAction">The action to call for each entry. Passes the battle's key name and data.</param>
    public void IterateBattleEntries(Action<string, BattleEntry> inAction)
    {
        if (inAction == null)
            return;

        foreach (KeyValuePair<string, BattleEntry> battle in _battleEntries)
        {
            inAction.Invoke(battle.Key, battle.Value);
        }
    }
}

/// <summary>
/// The manager of all save data and settings.
/// </summary>
public class SaveDataManager : MonoBehaviour
{
    public delegate void SaveDataManagerDelegate();

    /// <summary>An event called when the <see cref="CurrentSaveData"/> reloads.</summary>
    public static event SaveDataManagerDelegate OnCurrentSaveLoaded;

    /// <summary>The main directory path.</summary>
    public static string DataDirectory { get { return $@"{Directory.GetCurrentDirectory()}\_data\"; } }

    /// <summary>The save data directory path.</summary>
    public static string SaveDataDirectory { get { return $@"{DataDirectory}_saves\"; } }

    /// <summary>The current save data being edited.</summary>
    public static CalculatorSaveData CurrentSaveData { get { return Manager?._currentSaveData; } }

    /// <summary>Default settings to use when asking the user to create new save data.</summary>
    public static DefaultSaveCreationSettings CreationSettings { get { return Manager?._creationSettings; } }

    /// <summary>The spirit metadata directory path.</summary>
    private static string SpiritMetaDataDirectory { get { return $@"{DataDirectory}_spirit\"; } }

    /// <summary>The additional spirit metadata directory path.</summary>
    private static string SpiritAddendumMetaDataDirectory { get { return $@"{DataDirectory}_spiritadd\"; } }

    /// <summary>The <see cref="SaveDataManager"/> singleton object.</summary>
    public static SaveDataManager Manager { get; private set; }

    /// <summary>The header portion of all save data files.</summary>
    public static readonly string SaveDataFileHeader = "calc_data_stats_";

    /// <summary>The file extension portion of all data files.</summary>
    public static readonly string DataFileExtension = ".json";

    /// <summary>The file name for the <see cref="_creationSettings"/>.</summary>
    private readonly string _saveCreationSettingsFileName = "calc_data_settings.json";

    /// <summary>The file name for spirit metadata. Link: https://www.ssbwiki.com/List_of_spirits_(complete_list)</summary>
    private readonly string _spiritMetaDataInfoFileName = "calc_metadata_spirit_info.txt";

    /// <summary>The file name for spirit battle metadata. Link: https://www.ssbwiki.com/List_of_spirits_(disambiguation)</summary>
    private readonly string _spiritMetaDataBattleFileName = "calc_metadata_spirit_battle.txt";

    /// <summary>The file name for fighter battle metadata. Link: https://www.ssbwiki.com/Adventure_Mode:_World_of_Light#Fighters</summary>
    private readonly string _fighterMetaDataBattleFileName = "calc_metadata_fighter_battle.txt";

    /// <summary>The metadata for all battles in the game.</summary>
    private readonly SpiritMetaDataCollection _spiritMetaData = new SpiritMetaDataCollection();

    /// <summary>See: <see cref="CurrentSaveData"/></summary>
    private CalculatorSaveData _currentSaveData;

    /// <summary>The default settings to use when creating new <see cref="CalculatorSaveData"/>.</summary>
    private DefaultSaveCreationSettings _creationSettings;

    private void Awake()
    {
        if (Manager != null)
            return;
        Manager = this;

        // Verify the directory for save data is ready to go.
        Directory.CreateDirectory(DataDirectory);

        // Load setting files.
        TryReadOrCreateDataFile(CalculatorTools.CreateDataPath(_saveCreationSettingsFileName), out _creationSettings);

        // Compile spirit metadata.
        CompileSpiritMetaData();

        LegacyDataManager.UpdateLegacyData();
    }

    /// <summary>
    /// Writes the given <see cref="CalculatorSaveData"/> to its file.
    /// </summary>
    /// <param name="inData">The <see cref="CalculatorSaveData"/> to write.</param>
    /// <returns>Returns if the data was successfully written to a file.</returns>
    public static bool WriteSaveData(CalculatorSaveData inData)
    {
        if (!Manager)
            return false;

        if (inData == null || string.IsNullOrWhiteSpace(inData.FileName))
        {
            LogManager.WriteLogLine(LogType.Error, "Unable to write given save data. File is either null, or has null path.");
            return false;
        }

        inData.Validate();
        return Manager.TryWriteDataFile(CalculatorTools.CreateSaveDataPath(inData.FileName), inData);
    }

    /// <summary>
    /// Loads the <see cref="CalculatorSaveData"/> at the given file, and loads it into <see cref="CurrentSaveData"/>.
    /// </summary>
    /// <param name="inFileName">The name of the file, without a directory path.</param>
    /// <returns>Returns if the data was successfully loaded.</returns>
    public static bool LoadSaveData(string inFileName)
    {
        if (!Manager || string.IsNullOrWhiteSpace(inFileName))
            return false;

        Manager._currentSaveData = null;
        string filePath = CalculatorTools.CreateSaveDataPath(inFileName);

        if (!Manager.TryReadOrCreateDataFile(filePath, out Manager._currentSaveData))
            return false;

        Manager._currentSaveData.FileName = inFileName;

        OnCurrentSaveLoaded?.Invoke();
        return true;
    }

    /// <summary>
    /// Iterates all <see cref="SpiritData"/> stored with a given predicate.
    /// </summary>
    /// <param name="iterationAction">The action to perform on each <see cref="SpiritData"/>.</param>
    public static void IterateSpiritData(Action<SpiritData> iterationAction)
    {
        if (!Manager || Manager._spiritMetaData == null || iterationAction == null)
            return;

        Manager._spiritMetaData.IterateSpiritData(iterationAction);
    }

    /// <summary>
    /// Gets the <see cref="SpiritData"/> for the given key.
    /// </summary>
    /// <param name="spiritName">The spirit's key name.</param>
    /// <param name="outData">The found data.</param>
    /// <returns>Returns whether the data was found.</returns>
    public static bool GetSpiritData(string spiritName, out SpiritData outData)
    {
        outData = new SpiritData();

        if (!Manager || Manager._spiritMetaData == null)
            return false;

        return Manager._spiritMetaData.GetSpiritData(spiritName, out outData);
    }

    /// <summary>
    /// Gets an appropriate display name for the given battle, if there is one.
    /// </summary>
    /// <param name="battleName">The name of the battle.</param>
    /// <returns>If there is a display name, returns the display name. Otherwise, returns <see cref="battleName"/>.</returns>
    public static string GetBattleDisplayName(string battleName)
    {
        if (!Manager || Manager._spiritMetaData == null || string.IsNullOrWhiteSpace(battleName))
            return battleName;

        Manager._spiritMetaData.GetSpiritData(battleName, out SpiritData spiritData);
        return string.IsNullOrWhiteSpace(spiritData.spiritName) ? battleName : spiritData.spiritName;
    }

    /// <summary>
    /// Attempts to read a data JSON file. If the file does not exist, it is created with fresh data.
    /// </summary>
    /// <param name="filePath">The full file path.</param>
    /// <param name="outData">The serialized data.</param>
    /// <typeparam name="T">The serialized data type.</typeparam>
    /// <returns>Returns if the read was successful, or a new file was created.</returns>
    private bool TryReadOrCreateDataFile<T>(string filePath, out T outData) where T : ICalculatorData, new()
    {
        bool bIsValid;
        LogManager.WriteLogLine(LogType.Log, $"Attempting to read [{typeof(T)}] file at path [{filePath}].");

        if (File.Exists(filePath))
        {
            bIsValid = true;

            try
            {
                using StreamReader reader = new StreamReader(filePath);
                string data = reader.ReadToEnd();
                outData = JsonConvert.DeserializeObject<T>(data);
                reader.Close();

                LogManager.WriteLogLine(LogType.Log, $"Successfully read [{typeof(T)}] file at path [{filePath}].");
            }
            catch (Exception e)
            {
                LogManager.WriteLogLine(LogType.Error, $"Failed to read file at path [{filePath}]. Creating a default [{typeof(T)}] for this session. Exception: [{e}]");
                outData = new T();
                return false;
            }
        }
        else
        {
            LogManager.WriteLogLine(LogType.Warning, $"[{typeof(T)}] file at path [{filePath}] does not exist. Creating brand new file.");
            outData = new T();
            bIsValid = false;
        }

        bIsValid &= outData.Validate();
        if (bIsValid)
            return true;

        LogManager.WriteLogLine(LogType.Warning, $"[{typeof(T)}] file failed validation. Writing back to file.");
        return TryWriteDataFile(filePath, outData);
    }

    /// <summary>
    /// Writes a data file to JSON formatting.
    /// </summary>
    /// <param name="filePath">The full file path.</param>
    /// <param name="inData">The data to serialize.</param>
    /// <typeparam name="T">The serialized data type.</typeparam>
    /// <returns>Returns if the file write was successful.</returns>
    private bool TryWriteDataFile<T>(string filePath, T inData)
    {
        string data = JsonConvert.SerializeObject(inData, Formatting.Indented);

        try
        {
            using StreamWriter writer = new StreamWriter(filePath, false);
            writer.Write(data);
            writer.Close();

            LogManager.WriteLogLine(LogType.Log, $"Successfully wrote file [{filePath}].");
            return true;
        }
        catch (Exception e)
        {
            LogManager.WriteLogLine(LogType.Error, $"Failed to write file [{filePath}]. Exception: [{e}]");
            return false;
        }
    }

    /// <summary>
    /// Compiles all <see cref="SpiritData"/> for the <see cref="_spiritMetaData"/>.
    /// </summary>
    private void CompileSpiritMetaData()
    {
        LogManager.WriteLogLine(LogType.Log, "Begin compiling spirit metadata.");
        
        CompileInfoSpiritData();
        CompileBattleSpiritData();
        CompileBattleFighterData();
        CompileAppendedSpiritData();

        LogManager.WriteLogLine(LogType.Log, "Completed compiling spirit metadata.");
    }

    /// <summary>
    /// Compiles spirit metadata from the <see cref="_spiritMetaDataInfoFileName"/>. This data is compiled from SmashWiki,
    /// in order to make it easy to update via copy-paste.
    /// Data can be found here: https://www.ssbwiki.com/List_of_spirits_(complete_list)
    /// Edit the 'List of spirits' section, and copy from the first to last spirit line.
    /// </summary>
    private void CompileInfoSpiritData()
    {
        LogManager.WriteLogLine(LogType.Log, $"Parsing Spirit Info Metadata. File: [{_spiritMetaDataInfoFileName}]");
        if (_spiritMetaData == null)
            return;

        string path = Path.Combine(SpiritMetaDataDirectory, _spiritMetaDataInfoFileName);
        if (!LogManager.ValidateOrLog(File.Exists(path), LogType.Error, $"Unable to get Spirit Info Metadata. No file at path [{path}]"))
            return;

        try
        {
            int lineNumber = 0;
            using StreamReader reader = new StreamReader(path);
            while (reader.ReadLine() is { } currentLine)
            {
                ++lineNumber;

                // Validate that the line even has any data to parse.
                if (string.IsNullOrWhiteSpace(currentLine) || !currentLine.Contains("||"))
                    continue;

                bool success = GenerateSpiritDataFromInfoLine(currentLine, lineNumber, out SpiritData spiritData);
                if (!success)
                    continue;

                LogManager.WriteLogLine(LogType.Verbose, $"Successfully created spirit data for [{spiritData.spiritName}].");
                spiritData.Validate();
                _spiritMetaData.AddOrUpdateSpiritData(spiritData.spiritName, spiritData);
            }
        }
        catch (Exception e)
        {
            LogManager.WriteLogLine(LogType.Error, $"Could not parse metadata file [{_spiritMetaDataInfoFileName}]. Exception: [{e}]");
        }
    }

    /// <summary>
    /// Generates new <see cref="SpiritData"/> from a given line. Companion method to <see cref="CompileInfoSpiritData"/>.
    /// </summary>
    /// <param name="line">The line being parsed.</param>
    /// <param name="lineIndex">The index of the line. Used for logging.</param>
    /// <param name="outData">The finalized data.</param>
    /// <returns>Returns whether the data was finalized properly.</returns>
    private bool GenerateSpiritDataFromInfoLine(string line, int lineIndex, out SpiritData outData)
    {
        // All lines from SmashWiki follow similar formats, once they're split out into an array.
        // 0 - Collection Index
        // 1 - Spirit Name
        // 2 - Spirit Type
        // 3 - Series
        // 4 - Has Spirit Battle (Master Spirits Have No Valid Info Past This)
        // 5 - Class Rank
        // 6 - Affinity (Skipped. Battle Info contains more accurate data).
        // 7 - Slot Count
        // After this, the lines can differ slightly.
        // - If Primary:
        // -- 8 through 13 - Primary Spirit Stats. Can be skipped over.
        // -- 14 - Ability
        // -- 15 and Beyond - Obtain methods
        // - If Support
        // - 8 and 9 - These could possibly be null area used to black out primary spirit stats. If so, these can be skipped. Otherwise:
        // - 8 (10) - Ability
        // - 9 (11) And Beyond - Obtain methods

        outData = new SpiritData();
        string[] lineData = line.Split(new[] { "||", "{{", "}}" }, StringSplitOptions.RemoveEmptyEntries);
        int lineDataIndex = 0;

        if (line.Contains("Noah & Mio"))
            LogManager.WriteLogLine(LogType.Log, "");

        // Index 7 is the last shared data value.
        if (lineData.Length <= 7)
            return false;

        // Index 0 - Spirit Collection Index
        string spiritNumStr = Regex.Replace(lineData[lineDataIndex++], @"[^\d]", "");
        int.TryParse(spiritNumStr, out int spiritNum);
        outData.collectionIndex = spiritNum;

        // Index 1 - Spirit Name. If the spirit name contains a pipe character, we need what comes after it. The part before is an extra link or value sorter.
        outData.spiritName = lineData[lineDataIndex++];
        if (outData.spiritName.Contains('|'))
        {
            int pipeIndex = outData.spiritName.IndexOf('|');
            outData.spiritName = outData.spiritName.Remove(0, pipeIndex + 1);
        }

        // Index 2 - Spirit Type
        string spiritType = lineData[lineDataIndex++];
        if (spiritType == "Primary")
        {
            outData.Type = SpiritType.Primary;
        }
        else if (spiritType == "Support")
        {
            outData.Type = SpiritType.Support;
        }
        else if (spiritType == "Master")
        {
            outData.Type = SpiritType.Master;
        }
        else if (spiritType == "Fighter")
        {
            // Fighter spirits are not counted, as they have no data.
            return false;
        }
        else
        {
            LogManager.WriteLogLine(LogType.Warning, $"Cannot parse spirit info type. Line: [{lineIndex}], SpiritType: [{spiritType}]");
            return false;
        }

        // Index 3 - Series
        outData.series = lineData[lineDataIndex++];

        // WARNING: Three spirits (Noah & Mio, 13 Sentinel Pilots, Zagreus) do not have series listed in the game. As such, these have unique 'rollover' metadata that takes up 3 entries.
        if (outData.series.Contains("rollover"))
        {
            if (!LogManager.ValidateOrLog(lineData.Length > 9, LogType.Warning, $"Spirit [{outData.spiritName}], Line [{lineIndex}] cannot be parsed. Found rollover metadata, and not enough data afterwards."))
                return false;

            lineDataIndex += 2;
            outData.series = string.Empty;
        }

        // Index 4 - Has Battle. If the spirit has no battle, we do not want to add it. This can be easily proven by if this entry contains 'y|12' or 'n|12'
        if (!lineData[lineDataIndex++].Contains("y|12"))
            return false;

        // Master spirits have no further data.
        if (outData.Type == SpiritType.Master)
            return true;

        // Index 5 - Class Rank. This is based on the number of stars, so we just get the character count.
        outData.ClassRank = lineData[lineDataIndex++].Length;

        // Index 6 - Affinity. Skipped over. The Battle Data will contain accurate info, since Support spirits have separate Battle affinities.
        ++lineDataIndex;

        // Index 7 - Slots Count. Spirits with no slots just have '0' for this entry. Others have a hexagonal icon per slot.
        string slotStr = lineData[lineDataIndex++];
        outData.SlotCount = slotStr.Contains('0') ? 0 : slotStr.Length;

        // We still need to know where this spirit battle is located. Technically, however, the data is 'valid' at this stage.
        if (lineDataIndex >= lineData.Length)
        {
            LogManager.WriteLogLine(LogType.Warning, $"Spirit Data [{outData.spiritName}, Line {lineIndex}] cannot get an Ability or Battle Location.");
            return true;
        }

        // Depending on if the spirit is Primary or Support, the data gets listed out differently. We need to skip anything that's before the Ability.
        bool canGetRemainingData = false;
        while (lineDataIndex < lineData.Length)
        {
            // Skip anything that involves adding background space or involves primary stats (i.e. purely numbers)
            string currentLine = lineData[lineDataIndex];

            if (string.IsNullOrWhiteSpace(currentLine) || int.TryParse(currentLine, out int _) || currentLine.Contains("#DDD") || currentLine.Contains("colspan"))
            {
                ++lineDataIndex;
                continue;
            }

            canGetRemainingData = true;
            break;
        }

        if (!LogManager.ValidateOrLog(canGetRemainingData, LogType.Warning, $"Spirit Data [{outData.spiritName}, Line {lineIndex}] cannot get an Ability or Battle Location [After Entry Check]."))
            return true;

        // Extra Index A - Ability.
        outData.ability = lineData[lineDataIndex];
        if (!LogManager.ValidateOrLog(++lineDataIndex < lineData.Length, LogType.Warning, $"Spirit Data [{outData.spiritName}, Line {lineIndex}] cannot get a Battle Location."))
            return true;

        // Extra Index B and Beyond - Battle Location. This can come in several forms from Smash Wiki. Spirit Board is always determined first.

        // Obtain Methods vary in how they are displayed in the line.
        // If 'y|12' or '#var:y1' is first, the spirit is guaranteed to be a Spirit Board battle.
        // - If 'y|12' or '#var:yN' is after this, the spirit is a World of Light battle.
        // - If 'n|12' or '#var:nN' is after this, the spirit is not a Spirit Board battle.
        // If 'n|12' or '#var:nN' is first, the spirit is not a Spirit Board battle.
        // - If 'y|12' or '#var:yN' is after this, the spirit is a World of Light battle.
        // If '#var:y' is first, and the number after is 2 or more, the spirit is a World of Light battle.
        // If the spirit is a WOL battle, and the next data value contains 'chest', it is a reward spirit, rather than a battle.

        // First, figure out if the spirit is part of the Spirit Board.
        string locationStr = lineData[lineDataIndex];
        if (locationStr.Contains("y|12"))
        {
            outData.IsSpiritBoard = true;
        }
        else if (locationStr.Contains("#var:y"))
        {
            outData.IsSpiritBoard = true;
            CalculatorTools.ParseOutNumberFromString(locationStr, out int locationNum);
            outData.IsWOL = locationNum > 1; // If greater than 1, we know that this is a WOL spirit.
        }
        else if (locationStr.Contains("n|12"))
        {
            outData.IsSpiritBoard = false;
        }
        else if (locationStr.Contains("#var:n"))
        {
            outData.IsSpiritBoard = false;
            CalculatorTools.ParseOutNumberFromString(locationStr, out int locationNum);
            outData.IsWOL = locationNum > 1 ? false : null; // Don't set WOL if we don't need to. That way we go to the next line.
        }

        // If we haven't set the WOL value yet, move to the next line to get that data.
        if (outData.IsWOL == null)
        {
            if (!LogManager.ValidateOrLog(++lineDataIndex < lineData.Length, LogType.Warning, $"Spirit Data [{outData.spiritName}, Line {lineIndex}] cannot check WOL Battle Location."))
                return true;

            // We just need to see if 'y|12' or '#var:y' is contained.
            locationStr = lineData[lineDataIndex];
            outData.IsWOL = locationStr.Contains("y|12") || locationStr.Contains("#var:y");
        }

        // If this is a WOL spirit, and there's still more data, we need to check if it is a reward spirit. Otherwise, we are done.
        if (!outData.IsWOL.GetValueOrDefault(false) || ++lineDataIndex >= lineData.Length)
        {
            outData.IsWOLReward = false;
            return true;
        }

        string rewardStr = lineData[lineDataIndex];
        outData.IsWOLReward = rewardStr.Contains("chest");

        return true;
    }

    /// <summary>
    /// Compiles spirit metadata from the <see cref="_spiritMetaDataBattleFileName"/>. This data is compiled from SmashWiki,
    /// in order to make it easy to update via copy-paste.
    /// Data can be found here: https://www.ssbwiki.com/List_of_spirits_(disambiguation)
    /// Go to each 'List of spirits' page for each series. Edit the 'Spirit Battles' section, and copy from the first to last spirit battle line.
    /// </summary>
    private void CompileBattleSpiritData()
    {
        // The metadata from Smash Wiki is in paragraph form, with different wanted information on each line.
        // Number (Skipped)
        // Name (Parsed by finding 'SSBU|')
        // Spirit Battle Affinity (Parsed by finding 'SpiritType|')
        // Spirit Battle Power
        // Everything else is unnecessary. Skip ahead to next entry.

        LogManager.WriteLogLine(LogType.Log, $"Parsing Spirit Battle Metadata. File: [{_spiritMetaDataBattleFileName}]");
        if (_spiritMetaData == null)
            return;

        string path = Path.Combine(SpiritMetaDataDirectory, _spiritMetaDataBattleFileName);
        if (!LogManager.ValidateOrLog(File.Exists(path), LogType.Error, $"Unable to get Spirit Battle Metadata. No file at path [{path}]"))
            return;

        try
        {
            bool hasFoundEntry = false;
            SpiritData spiritData = new SpiritData();

            // These are delimiters used to find data necessary.
            string nameDelimiter = "SpiritTableName|";
            string affinityDelimiter = "SpiritType|";

            int lineNumber = 0;
            using StreamReader reader = new StreamReader(path);
            while (reader.ReadLine() is { } currentLine)
            {
                ++lineNumber;

                // Validate that the line even has any data to parse.
                if (string.IsNullOrWhiteSpace(currentLine))
                    continue;

                // If the line contains a rowspan format, it needs to be removed, or else we can't find proper information.
                if (currentLine.Contains("rowspan"))
                {
                    // Remove the very first character if it is a pipe symbol. Otherwise, we'll fail to parse out the actual divisor pipe.
                    if (currentLine.StartsWith('|'))
                        currentLine = currentLine.Remove(0, 1);

                    int pipeIndex = currentLine.IndexOf('|');
                    if (!LogManager.ValidateOrLog(pipeIndex > 0, LogType.Warning, $"Cannot parse spirit battle line. 'rowspan' formatter cannot be split out. Line: [{lineNumber}]"))
                        continue;

                    currentLine = currentLine.Remove(0, pipeIndex + 1);
                }

                // First, we need to find a name for the spirit, so we know which spirit is getting new information.
                if (!hasFoundEntry)
                {
                    if (!currentLine.Contains(nameDelimiter))
                        continue;

                    // Example line: |{{SpiritTableName|Super Star|link=y|size=64}}
                    // The spirit name is always the second item.
                    string[] lineData = currentLine.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    if (lineData.Length < 2)
                        continue;

                    string spiritName = lineData[1];
                    if (!_spiritMetaData.GetSpiritData(spiritName, out spiritData))
                    {
                        LogManager.WriteLogLine(LogType.Warning, $"Found spirit [{spiritName}] in battle data, but entry was not added while parsing info metadata! Line: [{lineNumber}]");
                        continue;
                    }

                    hasFoundEntry = true;
                    continue;
                }

                // Second, we need to get the battle affinity.
                if (spiritData.BattleAffinity == null)
                {
                    if (!currentLine.Contains(affinityDelimiter))
                        continue;

                    // Example line: |{{SpiritType|Shield}}
                    // The spirit type is always the second item.
                    string[] lineData = currentLine.Split(new[] { "|", "{{", "}}" }, StringSplitOptions.RemoveEmptyEntries);
                    if (!LogManager.ValidateOrLog(lineData.Length >= 2, LogType.Warning, $"Spirit [{spiritData.spiritName}] has malformed battle data. Unable to parse affinity! Line: [{lineNumber}]"))
                    {
                        hasFoundEntry = false;
                        spiritData = new SpiritData();
                        continue;
                    }

                    // Parse out the right affinity.
                    string affinity = lineData[1];
                    spiritData.BattleAffinity = affinity switch
                    {
                        "Neutral" => SpiritAffinity.Neutral,
                        "Attack" => SpiritAffinity.Attack,
                        "Shield" => SpiritAffinity.Shield,
                        "Grab" => SpiritAffinity.Grab,
                        _ => null
                    };

                    if (!LogManager.ValidateOrLog(spiritData.BattleAffinity != null, LogType.Warning, $"Spirit [{spiritData.spiritName}] has malformed battle data. Invalid affinity! Affinity: [{affinity}], Line: [{lineNumber}]"))
                    {
                        hasFoundEntry = false;
                        spiritData = new SpiritData();
                    }

                    continue;
                }

                // The battle power is always after finding spirit battle affinity. We can guarantee success or failure here.
                bool success = CalculatorTools.ParseOutNumberFromString(currentLine, out int foundPower);
                if (LogManager.ValidateOrLog(success, LogType.Warning, $"Spirit [{spiritData.spiritName}] has malformed battle data. Did not find battle power after spirit affinity! Line: [{lineNumber}]"))
                {
                    spiritData.BattlePower = foundPower;

                    LogManager.WriteLogLine(LogType.Verbose, $"Successfully found spirit battle data for [{spiritData.spiritName}].");
                    spiritData.Validate();
                    _spiritMetaData.AddOrUpdateSpiritData(spiritData.spiritName, spiritData);
                }

                hasFoundEntry = false;
                spiritData = new SpiritData();
            }
        }
        catch (Exception e)
        {
            LogManager.WriteLogLine(LogType.Error, $"Could not parse metadata file [{_spiritMetaDataInfoFileName}]. Exception: [{e}]");
        }
    }

    /// <summary>
    /// Compiles spirit metadata from the <see cref="_spiritMetaDataBattleFileName"/>. This data is compiled from SmashWiki,
    /// in order to make it easy to update via copy-paste.
    /// Data can be found here: https://www.ssbwiki.com/Adventure_Mode:_World_of_Light#Fighter_Battles
    /// Edit the 'Fighter Battles' grid section. After the header, copy from the first to last entry line.
    /// </summary>
    private void CompileBattleFighterData()
    {
        // The metadata from Smash Wiki is in paragraph form, with different wanted information on each line.
        // Number (Skipped)
        // Icon (Used as delimiter)
        // Name (Parsed by finding 'SSBU|')
        // Spirit Battle Affinity (Parsed by finding 'SpiritType|')
        // Spirit Battle Power
        // Everything else is unnecessary. Skip ahead to next entry.

        LogManager.WriteLogLine(LogType.Log, $"Parsing Fighter Battle Metadata. File: [{_fighterMetaDataBattleFileName}]");
        if (_spiritMetaData == null)
            return;

        string path = Path.Combine(SpiritMetaDataDirectory, _fighterMetaDataBattleFileName);
        if (!LogManager.ValidateOrLog(File.Exists(path), LogType.Error, $"Unable to get Fighter Battle Metadata. No file at path [{path}]"))
            return;

        try
        {
            bool hasFoundEntry = false;
            SpiritData spiritData = new SpiritData();

            // These are delimiters used to find data necessary.
            string iconDelimiter = "File:";
            string nameDelimiter = "SSBU|";
            string affinityDelimiter = "SpiritType|";

            int lineNumber = 0;
            using StreamReader reader = new StreamReader(path);
            while (reader.ReadLine() is { } currentLine)
            {
                ++lineNumber;

                // Validate that the line even has any data to parse.
                if (string.IsNullOrWhiteSpace(currentLine))
                    continue;

                // Entries start once we find a photo line.
                if (!hasFoundEntry)
                {
                    hasFoundEntry = currentLine.Contains(iconDelimiter);
                    continue;
                }

                // If the line contains a rowspan format, it needs to be removed, or else we can't find proper information.
                if (currentLine.Contains("rowspan"))
                {
                    // Remove the very first character if it is a pipe symbol. Otherwise, we'll fail to parse out the actual divisor pipe.
                    if (currentLine.StartsWith('|'))
                        currentLine = currentLine.Remove(0, 1);

                    int pipeIndex = currentLine.IndexOf('|');
                    if (!LogManager.ValidateOrLog(pipeIndex > 0, LogType.Warning, $"Cannot parse spirit battle line. 'rowspan' formatter cannot be split out. Line: [{lineNumber}]"))
                        continue;

                    currentLine = currentLine.Remove(0, pipeIndex + 1);
                }

                // First, we need to find a name for the spirit, so we know which spirit is getting new information.
                if (string.IsNullOrWhiteSpace(spiritData.spiritName))
                {
                    if (!currentLine.Contains(nameDelimiter))
                        continue;

                    // Example line: |{{SSBU|Mario}}
                    // The spirit name is always the second item.
                    string[] lineData = currentLine.Split(new[] { "|", "{{", "}}" }, StringSplitOptions.RemoveEmptyEntries);
                    if (lineData.Length < 2)
                        continue;

                    spiritData.spiritName = lineData[1];

                    hasFoundEntry = !string.IsNullOrWhiteSpace(spiritData.spiritName);
                    continue;
                }

                // Second, we need to get the battle affinity.
                if (spiritData.BattleAffinity == null)
                {
                    if (!currentLine.Contains(affinityDelimiter))
                        continue;

                    // Example line: |{{SpiritType|Neutral}} <center>{{color|#796581|Neutral}}</center>
                    // The spirit type is always the second item.
                    string[] lineData = currentLine.Split(new[] { "|", "{{", "}}" }, StringSplitOptions.RemoveEmptyEntries);
                    if (!LogManager.ValidateOrLog(lineData.Length >= 2, LogType.Warning, $"Fighter [{spiritData.spiritName}] has malformed battle data. Unable to parse affinity! Line: [{lineNumber}]"))
                    {
                        hasFoundEntry = false;
                        spiritData = new SpiritData();
                        continue;
                    }

                    // Parse out the right affinity.
                    string affinity = lineData[1];
                    spiritData.BattleAffinity = affinity switch
                    {
                        "Neutral" => SpiritAffinity.Neutral,
                        "Attack" => SpiritAffinity.Attack,
                        "Shield" => SpiritAffinity.Shield,
                        "Grab" => SpiritAffinity.Grab,
                        _ => null
                    };

                    if (!LogManager.ValidateOrLog(spiritData.BattleAffinity != null, LogType.Warning, $"Fighter [{spiritData.spiritName}] has malformed battle data. Invalid affinity! Affinity: [{affinity}], Line: [{lineNumber}]"))
                    {
                        hasFoundEntry = false;
                        spiritData = new SpiritData();
                    }

                    continue;
                }

                // The battle power is always after finding spirit battle affinity. We can guarantee success or failure here.
                bool success = CalculatorTools.ParseOutNumberFromString(currentLine, out int foundPower);
                if (LogManager.ValidateOrLog(success, LogType.Warning, $"Fighter [{spiritData.spiritName}] has malformed battle data. Did not find battle power after spirit affinity! Line: [{lineNumber}]"))
                {
                    spiritData.BattlePower = foundPower;

                    LogManager.WriteLogLine(LogType.Verbose, $"Successfully found fighter battle data for [{spiritData.spiritName}].");

                    // Fill in other known data.
                    spiritData.Type = SpiritType.Fighter;
                    spiritData.IsSpiritBoard = false;
                    spiritData.IsWOL = true;
                    spiritData.IsWOLReward = false;

                    spiritData.Validate();
                    _spiritMetaData.AddOrUpdateSpiritData(spiritData.spiritName, spiritData);
                }

                hasFoundEntry = false;
                spiritData = new SpiritData();
            }
        }
        catch (Exception e)
        {
            LogManager.WriteLogLine(LogType.Error, $"Could not parse metadata file [{_spiritMetaDataInfoFileName}]. Exception: [{e}]");
        }
    }

    /// <summary>
    /// Compiles addendum <see cref="SpiritData"/> to the <see cref="_spiritMetaData"/>. Addendum data is user-specified touch-ups
    /// to the spirit data. This can be useful for customized games or information that cannot be acquired from wiki files.
    /// </summary>
    private void CompileAppendedSpiritData()
    {
        LogManager.WriteLogLine(LogType.Log, "Compiling appended spirit data.");
        if (_spiritMetaData == null)
            return;

        // Verify the addendum directory exists.
        DirectoryInfo addDir;
        try
        {
            addDir = Directory.CreateDirectory(SpiritAddendumMetaDataDirectory);
        }
        catch (Exception e)
        {
            LogManager.WriteLogLine(LogType.Error, $"Failed to find or create spirit addendum directory at [{SpiritAddendumMetaDataDirectory}]. Exception: [{e}]");
            return;
        }

        // Go through each addendum file, and append its collection to the main metadata.
        bool appendedData = false;
        IEnumerable<FileInfo> addFiles = addDir.EnumerateFiles($"*{DataFileExtension}");
        foreach (FileInfo fileInfo in addFiles)
        {
            try
            {
                using StreamReader reader = new StreamReader(fileInfo.FullName);
                string data = reader.ReadToEnd();
                SpiritMetaDataCollection collection = JsonConvert.DeserializeObject<SpiritMetaDataCollection>(data);
                reader.Close();

                // Force throw an error to be caught.
                if (collection == null)
                    throw new NullReferenceException($"Parsed a collection file, but it did not create a valid metadata collection. File: [{fileInfo.Name}]");

                LogManager.WriteLogLine(LogType.Log, $"Successfully read addendum metadata collection at file [{fileInfo.Name}].");

                appendedData |= _spiritMetaData.AppendCollection(collection);
            }
            catch (Exception e)
            {
                LogManager.WriteLogLine(LogType.Error, $"Failed to read file at path [{fileInfo.FullName}]. Make sure only spirit metadata files are in this folder. Exception: [{e}]");
            }
        }

        if (appendedData)
            return;

        // If no data was appended, create an example file for the user.
        SpiritMetaDataCollection exampleCollection = new SpiritMetaDataCollection();
        SpiritData exampleData = new SpiritData();
        exampleData.spiritName = "Mario";
        exampleCollection.AddOrUpdateSpiritData(exampleData.spiritName, exampleData);
        TryWriteDataFile(Path.Combine(SpiritAddendumMetaDataDirectory, $"spirit_addendum_example{DataFileExtension}"), exampleCollection);
    }
}