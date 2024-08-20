// Copyright (c) Craig Williams, MathWiz86

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A menu for showing <see cref="PlayerStat"/>s.
/// </summary>
public class MenuPlayerStats : MenuBase
{
    /// <summary>The panel to place the names of each stat.</summary>
    [SerializeField] private Transform statTitlePanel;

    /// <summary>The panel to place each <see cref="PlayerStatItem"/>.</summary>
    [SerializeField] private Transform statItemPanel;

    /// <summary>The <see cref="PlayerStatItem"/>s to use for each stat. Each one should have a different <see cref="StatType"/>.</summary>
    [SerializeField] private List<PlayerStatItem> statItemPrefabs;

    /// <summary>The <see cref="TextBlockItem"/> to create for each stat title.</summary>
    [SerializeField] private TextBlockItem statTitlePrefab;

    /// <summary>A list of <see cref="ScrollRect"/>s to reset the position of when opening the menu or changing pages.</summary>
    [SerializeField] private List<ScrollRect> menuScrolls;

    /// <summary>A <see cref="Button"/> to go to the previous page of stats.</summary>
    [SerializeField] private Button prevPageButton;

    /// <summary>A <see cref="Button"/> to go to the next page of stats.</summary>
    [SerializeField] private Button nextPageButton;

    /// <summary>The <see cref="Button"/> to return to the previous menu.</summary>
    [SerializeField] private Button backButton;

    /// <summary>The maximum number of <see cref="PlayerStat"/>s to display on each page.</summary>
    [SerializeField] private int maxStatsPerPage = 50;

    /// <summary>A lookup map of each <see cref="StatType"/>'s <see cref="PlayerStatItem"/>.</summary>
    private readonly Dictionary<StatType, PlayerStatItem> _itemPrefabMap = new Dictionary<StatType, PlayerStatItem>();

    /// <summary>The list of <see cref="PlayerStat"/>s to display.</summary>
    private CalculatorDisplayedStats _statsToDisplay;

    /// <summary>The index of the current page.</summary>
    private int _currentPage = -1;

    private void Awake()
    {
        backButton?.onClick.AddListener(OnBackButtonClicked);
        prevPageButton?.onClick.AddListener(OnPreviousPageButtonClicked);
        nextPageButton?.onClick.AddListener(OnNextPageButtonClicked);

        foreach (PlayerStatItem item in statItemPrefabs)
        {
            if (item != null)
                _itemPrefabMap.Add(item.GetStatType(), item);
        }

        _statsToDisplay = new CalculatorDisplayedStats();
    }

    private void OnEnable()
    {
        foreach (ScrollRect scrollRect in menuScrolls)
        {
            if (!scrollRect)
                continue;

            scrollRect.horizontalNormalizedPosition = 0;
            scrollRect.verticalNormalizedPosition = 1;
        }
        
        _currentPage = 0;
        CreateStatItems();
    }

    /// <summary>
    /// Creates <see cref="PlayerStatItem"/>s for the <see cref="_currentPage"/>.
    /// </summary>
    private void CreateStatItems()
    {
        CalculatorTools.DestroyAllChildren(statTitlePanel);
        CalculatorTools.DestroyAllChildren(statItemPanel);

        if (_statsToDisplay == null || statItemPanel == null || statTitlePanel == null || !statTitlePrefab)
            return;

        // Iterate through all stats possible to show on the current page.
        _statsToDisplay.IterateDisplayedStats(_currentPage, maxStatsPerPage, (_, tally) =>
                                                                             {
                                                                                 if (!_itemPrefabMap.ContainsKey(tally.StatType))
                                                                                     return;

                                                                                 TextBlockItem statTitle = Instantiate(statTitlePrefab, statTitlePanel);
                                                                                 statTitle.Text = tally.Title;

                                                                                 PlayerStatItem statItem = Instantiate(_itemPrefabMap[tally.StatType], statItemPanel);
                                                                                 if (statItem == null)
                                                                                     return;

                                                                                 statItem.SetDisplayedStat(tally, true);
                                                                             });

        // Update the page buttons.
        if (prevPageButton)
            prevPageButton.enabled = _currentPage > 0;

        if (nextPageButton)
            nextPageButton.enabled = _currentPage * maxStatsPerPage + maxStatsPerPage < _statsToDisplay.GetStatCount();

        // Reset any scrolls to the base positions.
        foreach (ScrollRect scrollRect in menuScrolls)
        {
            if (!scrollRect)
                continue;

            // Don't reset horizontal, so that comparisons can still be easy to make while scrolling
            scrollRect.verticalNormalizedPosition = 1;
        }
    }

    /// <summary>
    /// An event called when the <see cref="prevPageButton"/> is clicked.
    /// </summary>
    private void OnPreviousPageButtonClicked()
    {
        int nextPage = Math.Max(_currentPage - 1, 0);
        if (_currentPage == nextPage)
            return;

        _currentPage = nextPage;
        CreateStatItems();
    }

    /// <summary>
    /// An event called when the <see cref="nextPageButton"/> is clicked.
    /// </summary>
    private void OnNextPageButtonClicked()
    {
        int maxPages = _statsToDisplay.GetStatCount() / maxStatsPerPage + (_statsToDisplay.GetStatCount() % maxStatsPerPage > 0 ? 1 : 0);
        int nextPage = Math.Min(_currentPage + 1, Math.Max(0, maxPages - 1));
        if (_currentPage == nextPage)
            return;

        _currentPage = nextPage;
        CreateStatItems();
    }

    /// <summary>
    /// An event called when the <see cref="backButton"/> is clicked.
    /// </summary>
    private void OnBackButtonClicked()
    {
        MenuManager.PopMenu(this);
    }
}