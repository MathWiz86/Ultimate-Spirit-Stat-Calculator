// Copyright (c) Craig Williams, MathWiz86

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

// Some things weren't meant to be hashed.
#pragma warning disable CS0660, CS0661

/// <summary>
/// The type of the spirit battle.
/// </summary>
[Serializable]
public enum BattleType
{
    Spirit,
    Fighter,
    Boss
}

/// <summary>
/// The affinity of the spirit in their battle.
/// </summary>
[Serializable]
public enum SpiritAffinity
{
    None,
    Neutral,
    Attack,
    Shield,
    Grab
}

/// <summary>
/// The type of the spirit. This is their usage outside of battle.
/// </summary>
[Serializable]
public enum SpiritType
{
    None,
    Fighter,
    Primary,
    Support,
    Master
}

/// <summary>
/// Metadata on a spirit. Note: 'Spirit' in this context can mean an actual spirit, fighter, or boss.
/// </summary>
[Serializable]
public struct SpiritData : ICalculatorData
{
    /// <summary>The many names that having no ability can be listed as on the Smash Wiki.</summary>
    private static readonly List<string> NoneAbilities = new List<string> { "None", "[None]", "N/A", "No Effect" };

    /// <summary>The display name of the spirit.</summary>
    public string spiritName;

    /// <summary>The index of the spirit in the collection.</summary>
    [JsonIgnore] public int collectionIndex;

    /// <summary>The series that the spirit is from.</summary>
    public string series;

    /// <summary>The ability of the spirit.</summary>
    public string ability;

    /// <summary>The affinity of the spirit in battle.</summary>
    public SpiritAffinity? BattleAffinity;

    /// <summary>The usage type of the spirit.</summary>
    public SpiritType? Type;

    /// <summary>The star rank of the spirit.</summary>
    public int? ClassRank;

    /// <summary>The number of slots. For Primary spirits, this is the slots they have. For Support, this is the slots taken.</summary>
    public int? SlotCount;

    /// <summary>The power of the spirit in its battle.</summary>
    public int? BattlePower;

    /// <summary>If true, this spirit has a Spirit Board battle.</summary>
    public bool? IsSpiritBoard;

    /// <summary>If true, this spirit can be found in World of Light.</summary>
    public bool? IsWOL;

    /// <summary>If true, this spirit is found in World of Light as a reward, rather than a fight.</summary>
    public bool? IsWOLReward;

    public bool Validate()
    {
        bool isValid = true;

        if (ability != null)
        {
            // 'None' abilities are under several names under the Smash Wiki. Convert them to one standard.
            if (NoneAbilities.Contains(ability))
            {
                ability = "None";
                isValid = false;
            }

            // Enhanceable spirits can have their ability truncated.
            if (ability.Contains("Can Be Enhanced"))
            {
                ability = "Enhanced";
                isValid = false;
            }
        }

        return isValid;
    }

    /// <summary>
    /// Checks if the represented battle is a World of Light battle.
    /// </summary>
    /// <returns>Returns if this is a World of Light battle.</returns>
    public bool HasWorldOfLightBattle()
    {
        return IsWOL.GetValueOrDefault(false) && !IsWOLReward.GetValueOrDefault(false);
    }

    /// <summary>
    /// Appends data from another <see cref="SpiritData"/>. Only non-null data is appended.
    /// </summary>
    /// <param name="otherData">The other <see cref="SpiritData"/> to append from.</param>
    public void AppendData(SpiritData otherData)
    {
        if (!string.IsNullOrWhiteSpace(otherData.spiritName))
            spiritName = otherData.spiritName;

        if (!string.IsNullOrWhiteSpace(otherData.series))
            series = otherData.series;

        if (!string.IsNullOrWhiteSpace(otherData.ability))
            ability = otherData.ability;

        if (otherData.BattleAffinity != null)
            BattleAffinity = otherData.BattleAffinity;

        if (otherData.Type != null)
            Type = otherData.Type;

        if (otherData.ClassRank != null)
            ClassRank = otherData.ClassRank;

        if (otherData.BattlePower != null)
            BattlePower = otherData.BattlePower;

        if (otherData.IsSpiritBoard != null)
            IsSpiritBoard = otherData.IsSpiritBoard;

        if (otherData.IsWOL != null)
            IsWOL = otherData.IsWOL;

        if (otherData.IsWOLReward != null)
            IsWOLReward = otherData.IsWOLReward;
    }
}

[Serializable]
public struct BattlePlayerData : ICalculatorData
{
    /// <summary>The number of losses this player has had in this battle.</summary>
    public int losses;

    public bool Validate()
    {
        bool isValid = true;

        if (losses < 0)
        {
            losses = 0;
            isValid = false;
        }

        return isValid;
    }

    public static bool operator ==(BattlePlayerData a, BattlePlayerData b)
    {
        return a.losses == b.losses;
    }

    public static bool operator !=(BattlePlayerData a, BattlePlayerData b)
    {
        return !(a == b);
    }
}

/// <summary>
/// A single battle entry for the user's current run.
/// </summary>
[Serializable]
public struct BattleEntry : ICalculatorData
{
    /// <summary>The type of the battle.</summary>
    public BattleType type;

    /// <summary>The index of the winning player. This can get reset to invalid.</summary>
    public int winningPlayer;

    /// <summary>If true, losses are equal amongst all players, and everyone is considered participated, even if not marked as the winner.</summary>
    public bool isBattleShared;

    /// <summary>Data for how each player did in this battle.</summary>
    [JsonProperty] private Dictionary<int, BattlePlayerData> _playerData;

    /// <summary>Battle data used if <see cref="isBattleShared"/> is true.</summary>
    [JsonProperty] private BattlePlayerData _sharedPlayerData;

    public BattleEntry(BattleType inType)
    {
        type = BattleType.Spirit;
        winningPlayer = CalculatorTools.NoneIndex;
        isBattleShared = false;

        _playerData = new Dictionary<int, BattlePlayerData>();
        _sharedPlayerData = new BattlePlayerData();

        Validate();
    }

    public BattleEntry(BattleEntry original)
    {
        type = original.type;
        winningPlayer = original.winningPlayer;
        isBattleShared = original.isBattleShared;

        _playerData = original._playerData != null ? new Dictionary<int, BattlePlayerData>(original._playerData) : new Dictionary<int, BattlePlayerData>();
        _sharedPlayerData = original._sharedPlayerData;

        Validate();
    }

    /// <summary>
    /// Validates that the data in the <see cref="BattleEntry"/> is ready for use.
    /// </summary>
    /// <returns>Returns if the <see cref="BattleEntry"/> is valid.</returns>
    public bool Validate()
    {
        bool isValid = true;

        if (_playerData == null)
        {
            _playerData = new Dictionary<int, BattlePlayerData>();
            isValid = false;
        }
        else
        {
            Dictionary<int, BattlePlayerData> playerDataCopy = new Dictionary<int, BattlePlayerData>(_playerData);
            foreach (KeyValuePair<int, BattlePlayerData> data in playerDataCopy)
            {
                isValid &= data.Value.Validate();
                _playerData[data.Key] = data.Value;
            }
        }

        if (SaveDataManager.CurrentSaveData != null)
        {
            for (int i = 0; i < SaveDataManager.CurrentSaveData.GetPlayerCount(); ++i)
            {
                isValid &= !_playerData.TryAdd(i, new BattlePlayerData());
            }
        }

        isValid &= _sharedPlayerData.Validate();

        return isValid;
    }

    /// <summary>
    /// Gets the number of losses for a player.
    /// </summary>
    /// <param name="playerIndex">The index of the player. If equal to <see cref="CalculatorTools.NoneIndex"/>,
    /// the method gets <see cref="_sharedPlayerData"/>'s losses.</param>
    /// <param name="deferToShared">If true, and this is a shared battle, <see cref="_sharedPlayerData"/>'s losses are returned no matter what.</param>
    /// <returns>Returns the given player's number of losses.</returns>
    public int GetPlayerLosses(int playerIndex, bool deferToShared = true)
    {
        if ((deferToShared && isBattleShared) || playerIndex == CalculatorTools.NoneIndex)
            return _sharedPlayerData.losses;

        return _playerData.TryGetValue(playerIndex, out BattlePlayerData value) ? value.losses : 0;
    }

    /// <summary>
    /// Updates the number of losses on a player by 1 or -1.
    /// </summary>
    /// <param name="playerIndex">The index of the player. If equal to <see cref="CalculatorTools.NoneIndex"/>,
    /// the method updates <see cref="_sharedPlayerData"/>.</param>
    /// <param name="increment">If true, increments losses by 1. Otherwise, decrements by 1.</param>
    public void UpdateLoss(int playerIndex, bool increment)
    {
        if (playerIndex == CalculatorTools.NoneIndex)
        {
            _sharedPlayerData.losses = Math.Max(_sharedPlayerData.losses + (increment ? 1 : -1), 0);
            return;
        }

        if (!_playerData.ContainsKey(playerIndex))
            _playerData.Add(playerIndex, new BattlePlayerData());

        BattlePlayerData data = _playerData[playerIndex];
        data.losses = Math.Max(data.losses + (increment ? 1 : -1), 0);
        _playerData[playerIndex] = data;
    }

    /// <summary>
    /// Updates the number of losses on a player to the given value.
    /// </summary>
    /// <param name="playerIndex">The index of the player. If equal to <see cref="CalculatorTools.NoneIndex"/>,
    /// the method updates <see cref="_sharedPlayerData"/>'s losses.</param>
    /// <param name="newValue">The new number of losses.</param>
    public void UpdateLoss(int playerIndex, int newValue)
    {
        newValue = Math.Max(newValue, 0);

        if (playerIndex == CalculatorTools.NoneIndex)
        {
            _sharedPlayerData.losses = newValue;
            return;
        }

        if (!_playerData.ContainsKey(playerIndex))
            _playerData.Add(playerIndex, new BattlePlayerData());

        BattlePlayerData data = _playerData[playerIndex];
        data.losses = Math.Max(newValue, 0);
        _playerData[playerIndex] = data;
    }

    public static bool operator ==(BattleEntry a, BattleEntry b)
    {
        if (a.type != b.type ||
            a.winningPlayer != b.winningPlayer ||
            a.isBattleShared != b.isBattleShared ||
            a._sharedPlayerData != b._sharedPlayerData)
        {
            return false;
        }

        if (a._playerData == null)
            return b._playerData == null;

        return a._playerData.Count == b._playerData.Count &&
               a._playerData.Keys.All(k => b._playerData.ContainsKey(k) && a._playerData[k] == b._playerData[k]);
    }

    public static bool operator !=(BattleEntry a, BattleEntry b)
    {
        return !(a == b);
    }
}

/// <summary>
/// A basic interface for all calculator data.
/// </summary>
public interface ICalculatorData
{
    /// <summary>
    /// Validates the data.
    /// </summary>
    /// <returns>If false is returned, the data failed validation, and needs to be re-saved.</returns>
    public bool Validate();
}

/// <summary>
/// A collection of <see cref="SpiritData"/>.
/// </summary>
[Serializable]
public class SpiritMetaDataCollection : ICalculatorData
{
    /// <summary>The spirit metadata map.</summary>
    [JsonProperty] private Dictionary<string, SpiritData> _metadata = new Dictionary<string, SpiritData>();

    public bool Validate()
    {
        bool isValid = true;

        if (_metadata == null)
        {
            _metadata = new Dictionary<string, SpiritData>();
            isValid = false;
        }

        foreach (KeyValuePair<string, SpiritData> spiritData in _metadata)
        {
            isValid &= spiritData.Value.Validate();
        }

        return isValid;
    }

    /// <summary>
    /// Adds the given <see cref="SpiritData"/> to the <see cref="_metadata"/>.
    /// </summary>
    /// <param name="spiritName">The key name of the spirit.</param>
    /// <param name="spiritData">The data to emplace.</param>
    public void AddOrUpdateSpiritData(string spiritName, SpiritData spiritData)
    {
        if (_metadata == null)
            return;

        string fixedName = CalculatorTools.SanitizeBattleName(spiritName);
        _metadata.TryAdd(fixedName, new SpiritData());
        _metadata[fixedName] = spiritData;
    }

    /// <summary>
    /// Gets the <see cref="SpiritData"/> for the given key.
    /// </summary>
    /// <param name="spiritName">The spirit's key name.</param>
    /// <param name="outData">The found data.</param>
    /// <returns>Returns whether the data was found.</returns>
    public bool GetSpiritData(string spiritName, out SpiritData outData)
    {
        outData = new SpiritData();

        if (_metadata == null)
            return false;

        string fixedName = CalculatorTools.SanitizeBattleName(spiritName);
        bool success = _metadata.TryGetValue(fixedName, out outData);
        return success;
    }

    /// <summary>
    /// Appends the data in the given <see cref="SpiritMetaDataCollection"/> to this one.
    /// </summary>
    /// <param name="other">The other data to append.</param>
    /// <returns>Returns if any data was appended.</returns>
    public bool AppendCollection(SpiritMetaDataCollection other)
    {
        if (other?._metadata == null || other._metadata.Count <= 0)
        {
            LogManager.WriteLogLine(LogType.Error, $"Cannot append spirit metadata to [{ToString()}]. Given collection is null");
            return false;
        }

        bool appendedData = false;
        foreach (KeyValuePair<string, SpiritData> otherData in other._metadata)
        {
            if (string.IsNullOrWhiteSpace(otherData.Key))
                continue;

            otherData.Value.Validate();

            // If the entry does not exist in this collection, add it and move on.
            if (_metadata.TryAdd(otherData.Key, otherData.Value))
                continue;

            // If the entry does already exist, append ONLY information that is not null.
            SpiritData data = _metadata[otherData.Key];
            data.AppendData(otherData.Value);
            _metadata[otherData.Key] = data;

            appendedData = true;
            LogManager.WriteLogLine(LogType.Log, $"Appended new spirit data to [{otherData.Key}].");
        }

        return appendedData;
    }

    /// <summary>
    /// Iterates all <see cref="SpiritData"/> stored with a given predicate.
    /// </summary>
    /// <param name="iterationAction">The action to perform on each <see cref="SpiritData"/>.</param>
    public void IterateSpiritData(Action<SpiritData> iterationAction)
    {
        if (iterationAction == null)
            return;

        foreach (KeyValuePair<string, SpiritData> data in _metadata)
        {
            iterationAction.Invoke(data.Value);
        }
    }
}