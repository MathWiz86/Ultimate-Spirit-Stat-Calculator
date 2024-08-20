// Copyright (c) Craig Williams, MathWiz86

using UnityEngine;

/// <summary>
/// A <see cref="PlayerStatItem"/> for <see cref="StatType.Single"/> <see cref="PlayerStat"/>s.
/// </summary>
public class SingleStatItem : PlayerStatItem
{
    /// <summary>The <see cref="TextBlockItem"/> to place the result in.</summary>
    [SerializeField] private TextBlockItem resultItem;

    protected override void OnStatTallied(PlayerStat stat)
    {
        base.OnStatTallied(stat);

        if (!resultItem)
            return;

        resultItem.Text = Stat.GetPlayerResult(CalculatorTools.NoneIndex).ResultText;
    }

    protected override string GetResultMessage()
    {
        if (SaveDataManager.CurrentSaveData == null || Stat == null)
            return base.GetResultMessage();

        return Stat.GetPlayerResult(CalculatorTools.NoneIndex).ResultText;
    }
}