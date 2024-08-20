// Copyright (c) Craig Williams, MathWiz86

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The main menu, displayed once a file is selected.
/// </summary>
public class MenuMain : MenuBase
{
    /// <summary>The title bar of the menu. Used to show the name of the current save data being edited.</summary>
    [SerializeField] private TMP_Text menuTitle;

    /// <summary>The <see cref="Button"/> to open up the <see cref="battleMenu"/>.</summary>
    [SerializeField] private Button battleMenuButton;

    /// <summary>The battle entry menu.</summary>
    [SerializeField] private MenuSearch battleMenu;

    /// <summary>The <see cref="Button"/> to open up the <see cref="playerStatMenu"/>.</summary>
    [SerializeField] private Button playerStatButton;

    /// <summary>The stat menu.</summary>
    [SerializeField] private MenuPlayerStats playerStatMenu;

    /// <summary>The <see cref="Button"/> to open up the <see cref="fileSelectMenu"/>.</summary>
    [SerializeField] private Button backButton;

    /// <summary>The file open menu.</summary>
    [SerializeField] private MenuFileSelect fileSelectMenu;

    private void Awake()
    {
        battleMenuButton?.onClick.AddListener(OnBattleMenuButtonClicked);
        playerStatButton?.onClick.AddListener(OnPlayerStatMenuButtonClicked);
        backButton?.onClick.AddListener(OnBackButtonClicked);

        SaveDataManager.OnCurrentSaveLoaded += UpdateMenuTitle;
        UpdateMenuTitle();
    }

    /// <summary>
    /// Updates the menu's title to display the save data being edited.
    /// </summary>
    private void UpdateMenuTitle()
    {
        if (!menuTitle)
            return;

        if (SaveDataManager.CurrentSaveData == null)
        {
            menuTitle.text = "NO SAVE DATA TO EDIT";
            return;
        }

        menuTitle.text = $"EDITING: \"{CalculatorTools.GetSaveDataDisplayName(SaveDataManager.CurrentSaveData.FileName)}\"";
    }

    /// <summary>
    /// An event called when the <see cref="battleMenuButton"/> is clicked.
    /// </summary>
    private void OnBattleMenuButtonClicked()
    {
        MenuManager.OpenMenu(battleMenu);
    }

    /// <summary>
    /// An event called when the <see cref="playerStatButton"/> is clicked.
    /// </summary>
    private void OnPlayerStatMenuButtonClicked()
    {
        MenuManager.OpenMenu(playerStatMenu);
    }

    /// <summary>
    /// An event called when the <see cref="backButton"/> is clicked.
    /// </summary>
    private void OnBackButtonClicked()
    {
        MenuManager.PopMenu(this);
    }
}