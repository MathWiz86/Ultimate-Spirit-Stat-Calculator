// Copyright (c) Craig Williams, MathWiz86

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A <see cref="PlayerStatItem"/> used for comparing each player's result.
/// </summary>
public class ComparisonStatItem : PlayerStatItem
{
    /// <summary>The layout group to place the <see cref="_resultItems"/>.</summary>
    [SerializeField] private HorizontalLayoutGroup resultLayout;

    /// <summary>The <see cref="TextBlockItem"/> to use for each result.</summary>
    [SerializeField] private TextBlockItem resultItemPrefab;

    /// <summary>The color to use on a highlighted value.</summary>
    [SerializeField] private Color highlightColor = Color.yellow;

    /// <summary>The created <see cref="TextBlockItem"/> to show the results.</summary>
    private readonly List<TextBlockItem> _resultItems = new List<TextBlockItem>();

    private void Awake()
    {
        if (SaveDataManager.CurrentSaveData == null || resultLayout == null || resultItemPrefab == null)
        {
            gameObject.SetActive(false);
            return;
        }

        SaveDataManager.OnCurrentSaveLoaded += UpdateResultItems;
        UpdateResultItems();
    }

    protected override void OnStatTallied(PlayerStat stat)
    {
        if (SaveDataManager.CurrentSaveData == null || stat == null)
            return;

        // For keeping track of the best values.
        List<int> bestValueIndices = new List<int>();
        float bestValue = stat.IsHighestValueBest ? float.MinValue : float.MaxValue;

        int playerCount = SaveDataManager.CurrentSaveData.GetPlayerCount();
        for (int i = 0; i < playerCount; ++i)
        {
            if (_resultItems.Count <= i)
                break;

            TextBlockItem item = _resultItems[i];
            if (item == null)
                continue;

            PlayerStatResult result = Stat.GetPlayerResult(i);

            item.Text = result.ResultText;

            // Determine if this is one of the best values.
            if (CalculatorTools.IsNearlyEqual(bestValue, result.ResultValue))
            {
                bestValueIndices.Add(i);
            }
            else if ((stat.IsHighestValueBest && bestValue < result.ResultValue) || (!stat.IsHighestValueBest && bestValue > result.ResultValue))
            {
                bestValueIndices.Clear();
                bestValueIndices.Add(i);
                bestValue = result.ResultValue;
            }
        }

        // Highlight the best values. If everyone had the best value, highlight no one.
        bool shouldHighlight = bestValueIndices.Count < playerCount;
        for (int i = 0; i < playerCount; ++i)
        {
            TextBlockItem item = _resultItems[i];
            if (item == null)
                continue;

            item.TextColor = shouldHighlight && bestValueIndices.Contains(i) ? highlightColor : Color.white;
        }
    }

    protected override string GetResultMessage()
    {
        if (SaveDataManager.CurrentSaveData == null || Stat == null)
            return base.GetResultMessage();

        string message = string.Empty;

        int playerCount = SaveDataManager.CurrentSaveData.GetPlayerCount();
        for (int i = 0; i < playerCount; ++i)
        {
            string playerName = SaveDataManager.CurrentSaveData.GetPlayerName(i);
            PlayerStatResult result = Stat.GetPlayerResult(i);

            message += $"[{playerName}]: {result.ResultText}";

            if (i < playerCount - 1)
                message += "\n\n";
        }

        return message;
    }

    /// <summary>
    /// Updates the displayed number of result items.
    /// </summary>
    private void UpdateResultItems()
    {
        if (SaveDataManager.CurrentSaveData == null || resultLayout == null || resultItemPrefab == null)
            return;

        // Display the appropriate number of result blocks.
        int playerCount = SaveDataManager.CurrentSaveData.GetPlayerCount();

        for (int i = 0; i < _resultItems.Count; ++i)
        {
            TextBlockItem resultItem = _resultItems[i];
            if (!resultItem)
            {
                _resultItems.RemoveAt(i--);
                continue;
            }

            resultItem.gameObject.SetActive(i < playerCount);
        }

        for (int i = _resultItems.Count; i < playerCount; ++i)
        {
            TextBlockItem resultItem = Instantiate(resultItemPrefab, resultLayout.transform);
            resultItem.Text = string.Empty;
            _resultItems.Add(resultItem);
        }
    }
}