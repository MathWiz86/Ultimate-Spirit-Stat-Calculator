// Copyright (c) Craig Williams, MathWiz86

using System;
using System.Collections.Generic;

/// <summary>
/// A filter used to determine if a <see cref="BattleEntry"/> can be tallied. Use this as a base for your own custom filters.
/// </summary>
public abstract class PlayerStatFilter
{
    /// <summary>
    /// Gets the title of the filter, which may be based on provided data.
    /// </summary>
    /// <returns>Returnst he final title.</returns>
    public virtual string GetFilterTitle()
    {
        return string.Empty;
    }

    /// <summary>
    /// Checks if the given <see cref="BattleEntry"/> passes this filter, and can be counted.
    /// </summary>
    /// <param name="battleEntry">The <see cref="BattleEntry"/> being checked.</param>
    /// <param name="spiritData">The relevant <see cref="SpiritData"/>.</param>
    /// <returns></returns>
    public abstract bool IsBattleValid(BattleEntry battleEntry, SpiritData spiritData);
}

/// <summary>
/// A common <see cref="PlayerStatFilter"/> to take into account <see cref="BattleType"/> and <see cref="SpiritAffinity"/>
/// </summary>
public class PlayerStatCommonFilter : PlayerStatFilter
{
    /// <summary>The <see cref="BattleType"/>s to require.</summary>
    protected readonly BattleType? BattleTypeFilter;

    /// <summary>The battle <see cref="SpiritAffinity"/>s to require.</summary>
    protected readonly SpiritAffinity? BattleAffinityFilter;

    /// <summary>The use <see cref="SpiritAffinity"/>s to require.</summary>
    protected readonly SpiritType? UseAffinityFilter;

    public PlayerStatCommonFilter(BattleType? battleTypeFilter = null, SpiritAffinity? battleAffinityFilter = null, SpiritType? useAffinityFilter = null)
    {
        BattleTypeFilter = battleTypeFilter;
        BattleAffinityFilter = battleAffinityFilter;
        UseAffinityFilter = useAffinityFilter;
    }

    public override bool IsBattleValid(BattleEntry battleEntry, SpiritData spiritData)
    {
        if (BattleTypeFilter != null && BattleTypeFilter != battleEntry.type)
            return false;

        if (BattleAffinityFilter != null && BattleAffinityFilter != spiritData.BattleAffinity)
            return false;

        if (UseAffinityFilter != null && UseAffinityFilter != spiritData.Type)
            return false;

        return true;
    }

    public override string GetFilterTitle()
    {
        string title = string.Empty;

        if (BattleAffinityFilter != null)
            title += $" ({BattleAffinityFilter.ToString()})";

        if (BattleTypeFilter != null)
            title += $" ({BattleTypeFilter.ToString()})";

        if (UseAffinityFilter != null)
            title += $" ({UseAffinityFilter.ToString()})";

        return title;
    }
}

/// <summary>
/// A filter for <see cref="PlayerStatCommonality{TKey,TExtraData}"/>s.
/// </summary>
/// <typeparam name="T">The type of the parent <see cref="PlayerStatCommonality{TKey,TExtraData}"/>.</typeparam>
public class PlayerStatCommonalityFilter<T> : PlayerStatCommonFilter
{
    /// <summary>The key values to not allow.</summary>
    private readonly List<T> _keyFilter;

    /// <summary>A helper <see cref="Func{T1,T2,TResult}"/> to get the value key.</summary>
    private Func<BattleEntry, SpiritData, T> _keyFunc;

    public PlayerStatCommonalityFilter(List<T> keyFilter, BattleType? battleTypeFilter = null, SpiritAffinity? battleAffinityFilter = null, SpiritType? useAffinityFilter = null)
        : base(battleTypeFilter, battleAffinityFilter, useAffinityFilter)
    {
        _keyFilter = keyFilter;
    }

    public override string GetFilterTitle()
    {
        string title = base.GetFilterTitle();
        return title + $"{(_keyFilter != null && _keyFilter.Count > 0 ? " (Filtered)" : "")}";
    }

    public override bool IsBattleValid(BattleEntry battleEntry, SpiritData spiritData)
    {
        if (!base.IsBattleValid(battleEntry, spiritData))
            return false;

        return _keyFilter == null || _keyFunc == null || !_keyFilter.Contains(_keyFunc.Invoke(battleEntry, spiritData));
    }

    /// <summary>
    /// Sets the <see cref="_keyFunc"/> to use.
    /// </summary>
    /// <param name="inFunc">The <see cref="Func{T1, T2, TResult}"/> to use.</param>
    public void SetKeyFunc(Func<BattleEntry, SpiritData, T> inFunc)
    {
        _keyFunc = inFunc;
    }
}