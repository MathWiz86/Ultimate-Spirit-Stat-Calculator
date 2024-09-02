// Copyright (c) Craig Williams, MathWiz86

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The type of the stat. Determines what UI to create.
/// </summary>
public enum StatType
{
    Comparison,
    Single
}

/// <summary>
/// A struct representing a finalized player stat.
/// </summary>
public struct PlayerStatResult
{
    /// <summary>The numeric result value for the stat.</summary>
    public float ResultValue;

    /// <summary>A formatted result value to display.</summary>
    public string ResultText;
}

/// <summary>
/// Data on how a player stat's results are being tallied.
/// </summary>
/// <typeparam name="TExtraData">The type for any extra information the stat may need for tallying.</typeparam>
public class PlayerStatTally<TExtraData>
{
    /// <summary>The main result data.</summary>
    public PlayerStatResult ResultData;

    /// <summary>Any extra data needed for tallying.</summary>
    public TExtraData ExtraData;
}

/// <summary>
/// The base class for a player stat tally. This class should not be directly inherited. Inherit <see cref="PlayerStat{TExtraData}"/> instead.
/// </summary>
public abstract class PlayerStat
{
    /// <summary>The standard result format to use for formatting float values.</summary>
    protected static readonly string StandardResultFormat = "N2";

    /// <summary>The display type of the stat.</summary>
    public StatType PlayerStatType { get; protected set; } = StatType.Comparison;

    /// <summary>If true, the highest value is the best value, and should be highlighted. Otherwise, it is the lowest value.</summary>
    public bool IsHighestValueBest { get; protected set; } = true;

    /// <summary>The display title of the stat.</summary>
    public virtual string Title { get; protected set; }

    /// <summary>
    /// Returns if this stat is valid for the current save data.
    /// </summary>
    /// <returns>Returns if the stat is valid and can be displayed.</returns>
    public virtual bool IsValidStat()
    {
        return SaveDataManager.CurrentSaveData != null && SaveDataManager.CurrentSaveData.GetPlayerCount() > 0;
    }

    /// <summary>
    /// Tallies this stat's results based on the <see cref="SaveDataManager.CurrentSaveData"/>.
    /// </summary>
    public abstract void TallyStat();

    /// <summary>
    /// Gets the <see cref="PlayerStatResult"/> of the given player index.
    /// </summary>
    /// <param name="playerIndex">The player to check.</param>
    /// <returns>Returns the relevant <see cref="PlayerStatResult"/>.</returns>
    public abstract PlayerStatResult GetPlayerResult(int playerIndex);
}

/// <summary>
/// A base class for a player stat tally. Templated for extra data.
/// </summary>
/// <typeparam name="TExtraData">The type of the extra data in the <see cref="PlayerStatTally{TExtraData}"/>.</typeparam>
public abstract class PlayerStat<TExtraData> : PlayerStat
{
    /// <summary>The display title of the stat.</summary>
    public sealed override string Title { get { return _title + (_filter != null ? _filter.GetFilterTitle() : string.Empty); } protected set { _title = value; } }

    /// <summary>The result dictionary, mapping player index to their stat result.</summary>
    private readonly Dictionary<int, PlayerStatTally<TExtraData>> _results = new Dictionary<int, PlayerStatTally<TExtraData>>();

    /// <summary>A <see cref="PlayerStatFilter"/> to apply to battles before tallying them.</summary>
    private readonly PlayerStatFilter _filter;

    /// <summary>The inner title, without the <see cref="PlayerStatFilter"/> addendum.</summary>
    private string _title = string.Empty;

    protected PlayerStat(PlayerStatFilter inFilter)
    {
        _filter = inFilter;
    }

    public sealed override void TallyStat()
    {
        if (!IsValidStat())
            return;

        InitializeStat();
        OnTallyStat();

        int playerCount = SaveDataManager.CurrentSaveData.GetPlayerCount();
        for (int i = CalculatorTools.NoneIndex; i < playerCount; ++i)
        {
            PlayerStatTally<TExtraData> result = _results[i];
            FinalizeStatResult(i, result);
        }
    }

    /// <summary>
    /// Gets the <see cref="PlayerStatResult"/> of the given player index.
    /// </summary>
    /// <param name="playerIndex">The player to check.</param>
    /// <returns>Returns the relevant <see cref="PlayerStatResult"/>.</returns>
    public sealed override PlayerStatResult GetPlayerResult(int playerIndex)
    {
        _results.TryGetValue(playerIndex, out PlayerStatTally<TExtraData> result);
        return result?.ResultData ?? new PlayerStatResult();
    }

    /// <summary>
    /// Tallies a single battle, if applicable to this stat.
    /// </summary>
    /// <param name="battleName">The key name of the <see cref="battleEntry"/>.</param>
    /// <param name="battleEntry">The <see cref="BattleEntry"/> to check.</param>
    protected void TallyBattle(string battleName, BattleEntry battleEntry)
    {
        // Spirit data can be null, from custom spirits or bosses.
        SaveDataManager.GetSpiritData(battleName, out SpiritData spiritData);

        // If the battle isn't applicable, don't use it.
        if (!IsBattleApplicable(battleEntry, spiritData))
            return;

        if (!string.IsNullOrWhiteSpace(spiritData.spiritName))
            battleName = spiritData.spiritName;

        // Iterate through all players, and tally their data.
        int playerCount = PlayerStatType == StatType.Comparison ? SaveDataManager.CurrentSaveData.GetPlayerCount() : 0;
        for (int i = PlayerStatType == StatType.Comparison ? 0 : CalculatorTools.NoneIndex; i < playerCount; ++i)
        {
            PlayerStatTally<TExtraData> tallyData = _results[i];
            UpdateStat(i, battleName, spiritData, battleEntry, tallyData);
            _results[i] = tallyData;
        }
    }

    /// <summary>
    /// Initializes the stat anew.
    /// </summary>
    private void InitializeStat()
    {
        if (SaveDataManager.CurrentSaveData == null)
            return;

        // Also create an index for the 'none' player for shared stats.
        _results.Clear();
        int playerCount = SaveDataManager.CurrentSaveData.GetPlayerCount();
        for (int i = CalculatorTools.NoneIndex; i < playerCount; ++i)
        {
            _results.Add(i, new PlayerStatTally<TExtraData>());
        }

        OnInitializeStat();
    }

    /// <summary>
    /// An event called when the stat is being reinitialized.
    /// </summary>
    protected virtual void OnInitializeStat() { }

    /// <summary>
    /// Checks if the current battle entry is applicable.
    /// </summary>
    /// <param name="battleEntry">The current <see cref="BattleEntry"/>.</param>
    /// <param name="spiritData">The relevant <see cref="SpiritData"/>, if applicable.</param>
    /// <returns>Returns if the given <see cref="battleEntry"/> can be tallied.</returns>
    protected virtual bool IsBattleApplicable(BattleEntry battleEntry, SpiritData spiritData)
    {
        return _filter == null || _filter.IsBattleValid(battleEntry, spiritData);
    }

    /// <summary>
    /// An event called to start tallying the stat.
    /// </summary>
    protected virtual void OnTallyStat()
    {
        if (SaveDataManager.CurrentSaveData == null)
            return;

        SaveDataManager.CurrentSaveData.IterateBattleEntries(TallyBattle);
    }

    /// <summary>
    /// Finalizes the text of the stat result.
    /// </summary>
    /// <param name="playerIndex">The index of the player.</param>
    /// <param name="tallyData">The <see cref="PlayerStatTally{TExtraData}"/> to finalize.</param>
    protected virtual void FinalizeStatResult(int playerIndex, PlayerStatTally<TExtraData> tallyData)
    {
        if (tallyData == null)
            return;

        bool isIntegral = CalculatorTools.IsNearlyEqual(0.0f, tallyData.ResultData.ResultValue - (float)Math.Truncate(tallyData.ResultData.ResultValue));
        tallyData.ResultData.ResultText = isIntegral ? ((int)tallyData.ResultData.ResultValue).ToString("N0") : tallyData.ResultData.ResultValue.ToString(StandardResultFormat);
    }

    /// <summary>
    /// Checks if the current battle entry is applicable.
    /// </summary>
    /// <param name="playerIndex">The index of the current player.</param>
    /// <param name="battleName">The name of the battle. Uses <see cref="SpiritData.spiritName"/> if applicable.</param>
    /// <param name="spiritData">The relevant <see cref="SpiritData"/>, if applicable.</param>
    /// <param name="battleEntry">The current <see cref="BattleEntry"/>.</param>
    /// <param name="tallyData">The current <see cref="PlayerStatTally{TExtraData}"/>.</param>
    /// <returns>Returns if the given <see cref="battleEntry"/> can be tallied.</returns>
    protected abstract void UpdateStat(int playerIndex, string battleName, SpiritData spiritData, BattleEntry battleEntry, PlayerStatTally<TExtraData> tallyData);
}

/// <summary>
/// A type of <see cref="PlayerStat"/> that finds a ranked commonality between battle entries.
/// </summary>
/// <typeparam name="TKey">The type of the commonality key.</typeparam>
/// <typeparam name="TExtraData">The type of the extra data in the <see cref="PlayerStatTally{TExtraData}"/>.</typeparam>
public abstract class PlayerStatCommonality<TKey, TExtraData> : PlayerStat<ValueTuple<Dictionary<TKey, int>, TExtraData>>
{
    /// <summary>The max values to list in a single result.</summary>
    protected const int MaxValuesListed = 15;

    /// <summary>If true, the most common result is displayed. Otherwise, it is the least common result.</summary>
    private readonly bool _showMostCommon;

    /// <summary>The minimum commonality value required for an entry to be considered.</summary>
    private readonly int _minCount;

    /// <summary>The rank of the result to return.</summary>
    private readonly int _rank;

    protected PlayerStatCommonality(PlayerStatFilter inFilter, bool showMostCommon, int minCount, int rank) : base(inFilter)
    {
        _showMostCommon = showMostCommon;
        _minCount = Math.Max(minCount, 1);
        _rank = Math.Max(rank, 1);
    }

    protected PlayerStatCommonality(bool showMostCommon, int minCount, int rank, PlayerStatCommonalityFilter<TKey> inFilter) : this(inFilter, showMostCommon, minCount, rank)
    {
        inFilter?.SetKeyFunc(GetCommonalityFilterKey);
    }

    protected override void UpdateStat(int playerIndex, string battleName, SpiritData spiritData, BattleEntry battleEntry, PlayerStatTally<ValueTuple<Dictionary<TKey, int>, TExtraData>> tallyData)
    {
        tallyData.ExtraData.Item1 ??= new Dictionary<TKey, int>();

        if (!GetCommonalityValues(playerIndex, battleName, battleEntry, spiritData, out TKey key, out int addition))
            return;

        tallyData.ExtraData.Item1.TryAdd(key, 0);
        tallyData.ExtraData.Item1[key] += addition;
    }

    protected override void FinalizeStatResult(int playerIndex, PlayerStatTally<ValueTuple<Dictionary<TKey, int>, TExtraData>> tallyData)
    {
        tallyData.ResultData.ResultValue = 0;
        tallyData.ResultData.ResultText = "None";

        Dictionary<TKey, int> counter = tallyData.ExtraData.Item1;
        if (counter == null || counter.Count <= 0)
            return;

        // Sort the counter based on commonality.
        List<KeyValuePair<TKey, int>> sortedCounter = counter.ToList();
        sortedCounter.Sort((pair1, pair2) => { return pair1.Value.CompareTo(pair2.Value); });

        int currentRank = 0;
        List<TKey> validKeys = new List<TKey>();
        int bestValue = _showMostCommon ? int.MaxValue : int.MinValue;

        // The keys are sorted least to greatest. If we're showing the most common, we can start from the bottom of the list.
        int startValue = _showMostCommon ? sortedCounter.Count - 1 : 0;
        int targetValue = _showMostCommon ? -1 : sortedCounter.Count;
        int direction = _showMostCommon ? -1 : 1;
        for (int i = startValue; i != targetValue; i += direction)
        {
            KeyValuePair<TKey, int> value = sortedCounter[i];

            // Don't count values below the minimum count.
            if (value.Value < _minCount)
            {
                // If we're showing the most common, and can't go any lower, then stop here.
                if (_showMostCommon)
                    break;

                continue;
            }

            if (currentRank == _rank)
            {
                // If we're at the right rank, cache off values that match our best value. Otherwise, we can finish.
                if (value.Value == bestValue)
                    validKeys.Add(value.Key);
                else
                    break;
            }
            else if ((_showMostCommon && value.Value < bestValue) || (!_showMostCommon && value.Value > bestValue))
            {
                // Remember, we're technically traversing the list in the reverse order to what we're looking for.
                bestValue = value.Value;

                // Once we reach our appropriate rank
                if (++currentRank == _rank)
                    validKeys.Add(value.Key);
            }
        }

        if (validKeys.Count <= 0)
            return;

        // Form a result string with all valid keys.
        tallyData.ResultData.ResultValue = bestValue;
        tallyData.ResultData.ResultText = $"({bestValue}) ";

        int maxCount = Math.Min(MaxValuesListed, validKeys.Count);
        for (int i = 0; i < maxCount; ++i)
        {
            tallyData.ResultData.ResultText += $"{validKeys[i].ToString()}{(i < validKeys.Count - 1 ? ", " : "")}";
        }

        // Stop idiots from showing every single battle in one text block.
        if (validKeys.Count > MaxValuesListed)
            tallyData.ResultData.ResultText += "...";
    }

    /// <summary>
    /// Formats the title string based on how the commonality is set.
    /// </summary>
    /// <param name="baseTitle">The initial title.</param>
    /// <returns>Returns the formatted title.</returns>
    protected string FormatCommonalityTitle(string baseTitle)
    {
        return $"{(_showMostCommon ? "Most" : "Least")} {baseTitle} [Rank {_rank}, >= {_minCount}]";
    }

    /// <summary>
    /// Gets the key to use for the <see cref="PlayerStatCommonalityFilter{T}"/>.
    /// </summary>
    /// <param name="battleEntry">The current <see cref="BattleEntry"/>.</param>
    /// <param name="spiritData">The relevant <see cref="SpiritData"/>, if applicable.</param>
    /// <returns>Returns the found key.</returns>
    protected abstract TKey GetCommonalityFilterKey(BattleEntry battleEntry, SpiritData spiritData);

    /// <summary>
    /// Gets the key to use for tracking some common point.
    /// </summary>
    /// <param name="playerIndex">The index of the current player.</param>
    /// <param name="battleName">The name of the battle. Uses <see cref="SpiritData.spiritName"/> if applicable.</param>
    /// <param name="battleEntry">The current <see cref="BattleEntry"/>.</param>
    /// <param name="spiritData">The relevant <see cref="SpiritData"/>, if applicable.</param>
    /// <param name="key">The commonality key that is tracked.</param>
    /// <param name="addition">How many points to give the <see cref="key"/>.</param>
    /// <returns></returns>
    protected abstract bool GetCommonalityValues(int playerIndex, string battleName, BattleEntry battleEntry, SpiritData spiritData, out TKey key, out int addition);
}

/// <summary>
/// A decorator <see cref="PlayerStat"/> to visually divide subsections.
/// </summary>
public class PlayerStatDivider : PlayerStat<int>
{
    public PlayerStatDivider() : base(null)
    {
        Title = string.Empty;
        PlayerStatType = StatType.Single;
    }

    public PlayerStatDivider(string title) : this()
    {
        Title = title;
    }

    // No battle can be tallied.
    protected override bool IsBattleApplicable(BattleEntry battleEntry, SpiritData spiritData)
    {
        return false;
    }

    // No need to update a stat that won't exist.
    protected override void UpdateStat(int playerIndex, string battleName, SpiritData spiritData, BattleEntry battleEntry, PlayerStatTally<int> tallyData) { }

    protected override void FinalizeStatResult(int playerIndex, PlayerStatTally<int> tallyData)
    {
        tallyData.ResultData.ResultText = string.Empty;
    }
}

/// <summary>
/// A <see cref="PlayerStat"/> to total the number of battles.
/// </summary>
public class PlayerStatBattlesTotal : PlayerStat<int>
{
    public PlayerStatBattlesTotal(PlayerStatFilter inFilter) : base(inFilter)
    {
        Title = "Total Battles";
    }

    protected override void UpdateStat(int playerIndex, string battleName, SpiritData spiritData, BattleEntry battleEntry, PlayerStatTally<int> tallyData)
    {
        if (battleEntry.winningPlayer == playerIndex || battleEntry.isBattleShared)
            tallyData.ResultData.ResultValue += 1.0f;

        tallyData.ResultData.ResultValue += battleEntry.GetPlayerLosses(playerIndex);
    }
}

/// <summary>
/// A <see cref="PlayerStat"/> to total the number of unique battles.
/// </summary>
public class PlayerStatBattlesUnique : PlayerStat<int>
{
    public PlayerStatBattlesUnique(PlayerStatFilter inFilter) : base(inFilter)
    {
        Title = "Unique Battles";
    }

    protected override void UpdateStat(int playerIndex, string battleName, SpiritData spiritData, BattleEntry battleEntry, PlayerStatTally<int> tallyData)
    {
        bool bAddPoint = battleEntry.winningPlayer == playerIndex || battleEntry.isBattleShared;
        bAddPoint |= battleEntry.GetPlayerLosses(playerIndex) > 0;

        if (bAddPoint)
            tallyData.ResultData.ResultValue += 1.0f;
    }
}

/// <summary>
/// A <see cref="PlayerStat"/> to total the number of battles won.
/// </summary>
public class PlayerStatBattleWins : PlayerStat<int>
{
    /// <summary>If true, the winning player also cannot have lost at all.</summary>
    private readonly bool _noWinnerLosses;

    public PlayerStatBattleWins(bool noWinnerLosses, PlayerStatFilter inFilter) : base(inFilter)
    {
        _noWinnerLosses = noWinnerLosses;
        Title = "Battles Won" + $"{(_noWinnerLosses ? " First Try" : "")}";
    }

    protected override void UpdateStat(int playerIndex, string battleName, SpiritData spiritData, BattleEntry battleEntry, PlayerStatTally<int> tallyData)
    {
        if (battleEntry.winningPlayer != playerIndex)
            return;

        if (_noWinnerLosses && battleEntry.GetPlayerLosses(playerIndex) > 0)
            return;

        tallyData.ResultData.ResultValue += 1.0f;
    }
}

/// <summary>
/// A <see cref="PlayerStat"/> to total the number of battles won without any others trying.
/// </summary>
public class PlayerStatBattleWinsSolo : PlayerStat<int>
{
    /// <summary>If true, the winning player also cannot have lost at all.</summary>
    private readonly bool _noWinnerLosses;

    public PlayerStatBattleWinsSolo(bool noWinnerLosses, PlayerStatFilter inFilter) : base(inFilter)
    {
        _noWinnerLosses = noWinnerLosses;
        Title = "Battles Won Solo" + $"{(_noWinnerLosses ? " First Try" : "")}";
    }

    protected override bool IsBattleApplicable(BattleEntry battleEntry, SpiritData spiritData)
    {
        return base.IsBattleApplicable(battleEntry, spiritData) && !battleEntry.isBattleShared;
    }

    protected override void UpdateStat(int playerIndex, string battleName, SpiritData spiritData, BattleEntry battleEntry, PlayerStatTally<int> tallyData)
    {
        if (battleEntry.winningPlayer != playerIndex)
            return;

        int playerCount = SaveDataManager.CurrentSaveData.GetPlayerCount();
        for (int i = 0; i < playerCount; ++i)
        {
            int losses = battleEntry.GetPlayerLosses(i);
            if (losses <= 0)
                continue;

            if (i != playerIndex || _noWinnerLosses)
                return;
        }

        tallyData.ResultData.ResultValue += 1.0f;
    }
}

/// <summary>
/// A <see cref="PlayerStat"/> to total the number of battles lost.
/// </summary>
public class PlayerStatBattleLosses : PlayerStat<int>
{
    public PlayerStatBattleLosses(PlayerStatFilter inFilter) : base(inFilter)
    {
        IsHighestValueBest = false;
        Title = "Total Battles Lost";
    }

    protected override void UpdateStat(int playerIndex, string battleName, SpiritData spiritData, BattleEntry battleEntry, PlayerStatTally<int> tallyData)
    {
        tallyData.ResultData.ResultValue += battleEntry.GetPlayerLosses(playerIndex);
    }
}

/// <summary>
/// A <see cref="PlayerStat"/> to total the number of unique battles lost.
/// </summary>
public class PlayerStatBattleLossesUnique : PlayerStat<int>
{
    public PlayerStatBattleLossesUnique(PlayerStatFilter inFilter) : base(inFilter)
    {
        IsHighestValueBest = false;
        Title = "Unique Battles Lost";
    }

    protected override void UpdateStat(int playerIndex, string battleName, SpiritData spiritData, BattleEntry battleEntry, PlayerStatTally<int> tallyData)
    {
        tallyData.ResultData.ResultValue += battleEntry.GetPlayerLosses(playerIndex) > 0 ? 1.0f : 0;
    }
}

/// <summary>
/// A <see cref="PlayerStat"/> to total the number of battles a player one, where all other players lost at least once.
/// </summary>
public class PlayerStatSaviorWins : PlayerStat<int>
{
    /// <summary>The minimum number of losses all players must have to count the battle.</summary>
    private readonly int _lossesRequired;

    /// <summary>If true, the winning player also cannot have lost at all.</summary>
    private readonly bool _noWinnerLosses;

    public PlayerStatSaviorWins(int lossesRequired, bool noWinnerLosses, PlayerStatFilter inFilter) : base(inFilter)
    {
        _lossesRequired = Math.Max(lossesRequired, 1);
        _noWinnerLosses = noWinnerLosses;
        Title = $"Savior Wins ({_lossesRequired} {(lossesRequired > 1 ? "Losses" : "Loss")}){(_noWinnerLosses ? " First Try" : "")}";
    }

    protected override bool IsBattleApplicable(BattleEntry battleEntry, SpiritData spiritData)
    {
        return base.IsBattleApplicable(battleEntry, spiritData) && !battleEntry.isBattleShared;
    }

    protected override void UpdateStat(int playerIndex, string battleName, SpiritData spiritData, BattleEntry battleEntry, PlayerStatTally<int> tallyData)
    {
        if (battleEntry.winningPlayer != playerIndex)
            return;

        int playerCount = SaveDataManager.CurrentSaveData.GetPlayerCount();
        for (int i = 0; i < playerCount; ++i)
        {
            int losses = battleEntry.GetPlayerLosses(i);

            // If the player lost at all, and we aren't allowing that, return immediately.
            if (i == playerIndex && losses > 0 && _noWinnerLosses)
                return;

            if (losses >= _lossesRequired)
                continue;

            if (!(i == playerIndex && _noWinnerLosses && losses <= 0))
                return;
        }

        tallyData.ResultData.ResultValue += 1.0f;
    }
}

/// <summary>
/// A <see cref="PlayerStat"/> for the battle power of won battles.
/// </summary>
public class PlayerStatSpiritPower : PlayerStat<float>
{
    /// <summary>If true, the average spirit power is used as the result.</summary>
    private readonly bool _showAverage;

    public PlayerStatSpiritPower(bool showAverage, PlayerStatFilter inFilter) : base(inFilter)
    {
        _showAverage = showAverage;
        Title = $"{(showAverage ? "Average" : "Total")} Won Battle Power";
    }

    protected override bool IsBattleApplicable(BattleEntry battleEntry, SpiritData spiritData)
    {
        return base.IsBattleApplicable(battleEntry, spiritData) && spiritData.BattlePower > 0;
    }

    protected override void UpdateStat(int playerIndex, string battleName, SpiritData spiritData, BattleEntry battleEntry, PlayerStatTally<float> tallyData)
    {
        if (battleEntry.winningPlayer != playerIndex)
            return;

        tallyData.ResultData.ResultValue += spiritData.BattlePower.GetValueOrDefault(0);
        tallyData.ExtraData += 1;
    }

    protected override void FinalizeStatResult(int playerIndex, PlayerStatTally<float> tallyData)
    {
        if (_showAverage)
            tallyData.ResultData.ResultValue = tallyData.ExtraData > 0 ? tallyData.ResultData.ResultValue / tallyData.ExtraData : 0;

        base.FinalizeStatResult(playerIndex, tallyData);
    }
}

/// <summary>
/// A <see cref="PlayerStat"/> for the class of won battles.
/// </summary>
public class PlayerStatSpiritClass : PlayerStat<float>
{
    /// <summary>If true, the average spirit power is used as the result.</summary>
    private readonly bool _showAverage;

    public PlayerStatSpiritClass(bool showAverage, PlayerStatFilter inFilter) : base(inFilter)
    {
        _showAverage = showAverage;
        Title = $"{(showAverage ? "Average" : "Total")} Won Class Rank";
    }

    protected override bool IsBattleApplicable(BattleEntry battleEntry, SpiritData spiritData)
    {
        return base.IsBattleApplicable(battleEntry, spiritData) && spiritData.ClassRank > 0;
    }

    protected override void UpdateStat(int playerIndex, string battleName, SpiritData spiritData, BattleEntry battleEntry, PlayerStatTally<float> tallyData)
    {
        if (battleEntry.winningPlayer != playerIndex)
            return;

        tallyData.ResultData.ResultValue += spiritData.ClassRank.GetValueOrDefault(0);
        tallyData.ExtraData += 1;
    }

    protected override void FinalizeStatResult(int playerIndex, PlayerStatTally<float> tallyData)
    {
        if (_showAverage)
            tallyData.ResultData.ResultValue = tallyData.ExtraData > 0 ? tallyData.ResultData.ResultValue / tallyData.ExtraData : 0;

        base.FinalizeStatResult(playerIndex, tallyData);
    }
}

/// <summary>
/// A player stat for determining a player's common series acquired.
/// </summary>
public class PlayerStatCommonalitySeries : PlayerStatCommonality<string, int>
{
    public PlayerStatCommonalitySeries(PlayerStatFilter inFilter, bool showMostCommon, int minCount, int rank) : base(inFilter, showMostCommon, minCount, rank) { }

    public PlayerStatCommonalitySeries(bool showMostCommon, int minCount, int rank, PlayerStatCommonalityFilter<string> inFilter) : base(showMostCommon, minCount, rank, inFilter)
    {
        Title = FormatCommonalityTitle("Common Series Acquired");
    }

    protected override bool IsBattleApplicable(BattleEntry battleEntry, SpiritData spiritData)
    {
        return base.IsBattleApplicable(battleEntry, spiritData) && !string.IsNullOrWhiteSpace(spiritData.series);
    }

    protected override string GetCommonalityFilterKey(BattleEntry battleEntry, SpiritData spiritData)
    {
        return spiritData.series;
    }

    protected override bool GetCommonalityValues(int playerIndex, string battleName, BattleEntry battleEntry, SpiritData spiritData, out string key, out int addition)
    {
        key = spiritData.series;
        addition = 1;

        return battleEntry.winningPlayer == playerIndex;
    }

    protected override void FinalizeStatResult(int playerIndex, PlayerStatTally<ValueTuple<Dictionary<string, int>, int>> tallyData)
    {
        base.FinalizeStatResult(playerIndex, tallyData);

        // No comparison.
        tallyData.ResultData.ResultValue = 0;
    }
}

/// <summary>
/// A player stat for determining a player's common ability acquired.
/// </summary>
public class PlayerStatCommonalityAbility : PlayerStatCommonality<string, int>
{
    public PlayerStatCommonalityAbility(PlayerStatFilter inFilter, bool showMostCommon, int minCount, int rank) : base(inFilter, showMostCommon, minCount, rank)
    {
        Title = FormatCommonalityTitle("Common Ability Acquired");
    }

    public PlayerStatCommonalityAbility(bool showMostCommon, int minCount, int rank, PlayerStatCommonalityFilter<string> inFilter) : base(showMostCommon, minCount, rank, inFilter)
    {
        Title = FormatCommonalityTitle("Common Ability Acquired");
    }

    protected override bool IsBattleApplicable(BattleEntry battleEntry, SpiritData spiritData)
    {
        return base.IsBattleApplicable(battleEntry, spiritData) && !string.IsNullOrWhiteSpace(spiritData.ability);
    }

    protected override string GetCommonalityFilterKey(BattleEntry battleEntry, SpiritData spiritData)
    {
        return spiritData.ability;
    }

    protected override bool GetCommonalityValues(int playerIndex, string battleName, BattleEntry battleEntry, SpiritData spiritData, out string key, out int addition)
    {
        key = spiritData.ability;
        addition = 1;

        return battleEntry.winningPlayer == playerIndex;
    }

    protected override void FinalizeStatResult(int playerIndex, PlayerStatTally<ValueTuple<Dictionary<string, int>, int>> tallyData)
    {
        base.FinalizeStatResult(playerIndex, tallyData);

        // No comparison.
        tallyData.ResultData.ResultValue = 0;
    }
}

/// <summary>
/// A player stat for determining a player's toughest battles.
/// </summary>
public class PlayerStatToughBattle : PlayerStatCommonality<string, int>
{
    public PlayerStatToughBattle(StatType playerStatType, int rank, PlayerStatFilter inFilter) : base(inFilter, true, 1, rank)
    {
        PlayerStatType = playerStatType;
        Title = $"Toughest Battle [Rank {rank}{(playerStatType == StatType.Single ? ", Shared" : "")}]";
    }

    protected override string GetCommonalityFilterKey(BattleEntry battleEntry, SpiritData spiritData)
    {
        return null; // Unused.
    }

    protected override bool GetCommonalityValues(int playerIndex, string battleName, BattleEntry battleEntry, SpiritData spiritData, out string key, out int addition)
    {
        key = battleName;

        if (PlayerStatType == StatType.Comparison)
        {
            addition = battleEntry.GetPlayerLosses(playerIndex) + (battleEntry.isBattleShared || battleEntry.winningPlayer == playerIndex ? 1 : 0);
        }
        else
        {
            // Add a point for the winning battle.
            addition = battleEntry.winningPlayer != CalculatorTools.NoneIndex ? 1 : 0;

            // Add points for the losses.
            if (battleEntry.isBattleShared)
            {
                addition += battleEntry.GetPlayerLosses(CalculatorTools.NoneIndex);
            }
            else
            {
                int playerCount = SaveDataManager.CurrentSaveData.GetPlayerCount();
                for (int i = 0; i < playerCount; ++i)
                {
                    addition += battleEntry.GetPlayerLosses(i);
                }
            }
        }

        return true;
    }

    protected override void FinalizeStatResult(int playerIndex, PlayerStatTally<ValueTuple<Dictionary<string, int>, int>> tallyData)
    {
        base.FinalizeStatResult(playerIndex, tallyData);

        // No comparison.
        tallyData.ResultData.ResultValue = 0;
    }
}