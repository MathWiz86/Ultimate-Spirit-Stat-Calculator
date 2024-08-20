// Copyright (c) Craig Williams, MathWiz86

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A menu for creating a new save data.
/// </summary>
public class MenuNewSave : MenuBase
{
    /// <summary>The <see cref="Button"/> used for creating the save file.</summary>
    [SerializeField] [Header("Menu")] private Button createButton;

    /// <summary>A <see cref="Button"/> for returning back to the previous menu.</summary>
    [SerializeField] private Button backButton;

    /// <summary>An indicator to show that the file name specified in the <see cref="fileNameInput"/> already exists.</summary>
    [SerializeField] private GameObject fileExistsWarning;

    /// <summary>The next menu to go to after creating a save file successfully.</summary>
    [SerializeField] private MenuMain mainMenu;

    /// <summary>The panel to place data type options into.</summary>
    [SerializeField] [Header("Data Type Options")] private Transform dataTypeOptionPanel;

    /// <summary>The data type option prefab to use.</summary>
    [SerializeField] private DataTypeOption dataTypeOptionPrefab;

    /// <summary>The <see cref="TMP_InputField"/> for the file's name.</summary>
    [SerializeField] [Header("Creation Options")] private TMP_InputField fileNameInput;

    /// <summary>The <see cref="TMP_InputField"/> for the names of all players. Must be filled out.</summary>
    [SerializeField] private TMP_InputField playerInput;

    /// <summary>The <see cref="TMP_InputField"/> for the bosses to generate entries for. Optional.</summary>
    [SerializeField] private TMP_InputField bossInput;

    /// <summary>The data type option to use.</summary>
    private DataTypeOption _selectedDataTypeOption;

    private void Awake()
    {
        createButton?.onClick.AddListener(CreateNewSaveDataFromOptions);
        backButton?.onClick.AddListener(() => MenuManager.PopMenu(this));

        fileNameInput?.onValueChanged.AddListener(_ => UpdateCreateButton());
        playerInput?.onValueChanged.AddListener(_ => UpdateCreateButton());

        CreateDataTypeOptions();
        UpdateCreateButton();
    }

    private void OnEnable()
    {
        if (fileNameInput)
            fileNameInput.text = string.Empty;

        DefaultSaveCreationSettings settings = SaveDataManager.CreationSettings;

        if (playerInput)
            playerInput.text = string.Join('\n', settings.defaultSettings.players);

        if (bossInput)
            bossInput.text = string.Join('\n', settings.bosses);

        _selectedDataTypeOption?.SetSelected(false);

        _selectedDataTypeOption = dataTypeOptionPanel.GetComponentInChildren<DataTypeOption>();
        _selectedDataTypeOption?.SetSelected(true);

        backButton?.gameObject.SetActive(true);
    }

    /// <summary>
    /// Sets whether the <see cref="backButton"/> can be used.
    /// </summary>
    /// <param name="isEnabled">If true, this menu can return to the previous.</param>
    public void SetBackButtonEnabled(bool isEnabled)
    {
        backButton?.gameObject.SetActive(isEnabled);
    }

    /// <summary>
    /// Updates whether the <see cref="createButton"/> can be used.
    /// </summary>
    private void UpdateCreateButton()
    {
        if (createButton)
            createButton.enabled = CanCreateSave();
    }

    /// <summary>
    /// Gets whether the save file can be created, based on several checks.
    /// </summary>
    /// <returns>Returns if a save file can be created.</returns>
    private bool CanCreateSave()
    {
        fileExistsWarning?.SetActive(false);

        if (fileNameInput)
        {
            if (string.IsNullOrWhiteSpace(fileNameInput.text))
                return false;

            if (File.Exists(CalculatorTools.CreateSaveDataPath(fileNameInput.text)))
            {
                fileExistsWarning?.SetActive(true);
                return false;
            }
        }

        if (playerInput && string.IsNullOrWhiteSpace(playerInput.text))
            return false;

        if (!_selectedDataTypeOption)
            return false;

        return true;
    }

    /// <summary>
    /// Creates all <see cref="DataTypeOption"/>s this menu can create presets with.
    /// </summary>
    private void CreateDataTypeOptions()
    {
        CalculatorTools.DestroyAllChildren(dataTypeOptionPanel);

        CreateDataTypeOption("Blank Preset", true, CreateBlankPreset);
        CreateDataTypeOption("World of Light Preset", false, CreateWOLPreset);
        CreateDataTypeOption("Spirit Board Preset", false, CreateSpiritBoardPreset);
    }

    /// <summary>
    /// Creates a single <see cref="DataTypeOption"/> preset.
    /// </summary>
    /// <param name="title">The name of the preset.</param>
    /// <param name="isSelected">Whether to set the option as selected immediately.</param>
    /// <param name="creationAction">The action to call when creating the option.</param>
    private void CreateDataTypeOption(string title, bool isSelected, Func<CalculatorSaveData> creationAction)
    {
        if (!dataTypeOptionPanel || !dataTypeOptionPrefab || creationAction == null)
            return;

        DataTypeOption option = Instantiate(dataTypeOptionPrefab, dataTypeOptionPanel);
        option.OnDataTypeSelected += OnDataTypeSelected;
        option.SetTitle(title);
        option.SetSelected(isSelected);
        option.DataCreationAction = creationAction;
    }

    /// <summary>
    /// An event called when a <see cref="DataTypeOption"/> is selected.
    /// </summary>
    /// <param name="inOption">The selected option.</param>
    private void OnDataTypeSelected(DataTypeOption inOption)
    {
        if (!inOption)
            return;

        _selectedDataTypeOption?.SetSelected(false);
        _selectedDataTypeOption = inOption;
        _selectedDataTypeOption?.SetSelected(true);

        UpdateCreateButton();
    }

    /// <summary>
    /// Creates a new save data, based on the <see cref="_selectedDataTypeOption"/>.
    /// </summary>
    private void CreateNewSaveDataFromOptions()
    {
        if (!CanCreateSave())
            return;

        CalculatorSaveData saveData = _selectedDataTypeOption.DataCreationAction.Invoke();
        if (saveData == null)
        {
            LogManager.WriteLogLine(LogType.Error, "Failed to create new save data!");
            return;
        }

        if (!SaveDataManager.WriteSaveData(saveData))
        {
            LogManager.WriteLogLine(LogType.Error, $"Failed to write new save file [{saveData.FileName}]!");
            return;
        }

        if (!SaveDataManager.LoadSaveData(saveData.FileName))
            return;

        // Ensure this menu is no longer part of the stack before adding the main menu.
        MenuManager.PopMenu(this);
        MenuManager.OpenMenu(mainMenu);
    }

    /// <summary>
    /// A basis method for creating fresh save data. All preset methods should call this.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns>Returns the created save data.</returns>
    private CalculatorSaveData CreateFreshSaveData(string fileName)
    {
        CalculatorSaveData saveData = new CalculatorSaveData(fileName);
        saveData.SetPlayers(playerInput.text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList());

        return saveData;
    }

    /// <summary>
    /// Creates a blank <see cref="CalculatorSaveData"/>.
    /// </summary>
    /// <returns>Returns the created save data.</returns>
    private CalculatorSaveData CreateBlankPreset()
    {
        CalculatorSaveData saveData = CreateFreshSaveData(fileNameInput.text);

        AddExtraBattleEntries(saveData, bossInput.text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(), BattleType.Boss);

        return saveData;
    }

    /// <summary>
    /// Creates a <see cref="CalculatorSaveData"/> built for a game of World of Light.
    /// </summary>
    /// <returns>Returns the created save data.</returns>
    private CalculatorSaveData CreateWOLPreset()
    {
        CalculatorSaveData saveData = CreateFreshSaveData(fileNameInput.text);

        AddExtraBattleEntries(saveData, bossInput.text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(), BattleType.Boss);

        SaveDataManager.IterateSpiritData(data =>
                                          {
                                              bool isCharacter = data.Type == SpiritType.Fighter;
                                              if (isCharacter || data.HasWorldOfLightBattle())
                                                  saveData.AddOrUpdateBattleEntry(data.spiritName, new BattleEntry(isCharacter ? BattleType.Fighter : BattleType.Spirit));
                                          });

        return saveData;
    }

    /// <summary>
    /// Creates a <see cref="CalculatorSaveData"/> built for all spirit battles, but no fighters or bosses.
    /// </summary>
    /// <returns>Returns the created save data.</returns>
    private CalculatorSaveData CreateSpiritBoardPreset()
    {
        CalculatorSaveData saveData = CreateFreshSaveData(fileNameInput.text);

        SaveDataManager.IterateSpiritData(data =>
                                          {
                                              if (data.IsSpiritBoard.GetValueOrDefault(false))
                                                  saveData.AddOrUpdateBattleEntry(data.spiritName, new BattleEntry(BattleType.Spirit));
                                          });

        return saveData;
    }

    /// <summary>
    /// Adds the given set of battle names as entries in the given save data.
    /// </summary>
    /// <param name="inData">The <see cref="CalculatorSaveData"/> to add to.</param>
    /// <param name="inBattles">The battle names to add.</param>
    /// <param name="battleType">The <see cref="BattleType"/> of the battles.</param>
    private void AddExtraBattleEntries(CalculatorSaveData inData, List<string> inBattles, BattleType battleType)
    {
        if (inData == null || inBattles == null)
            return;

        foreach (string battleName in inBattles)
        {
            string fixedName = CalculatorTools.SanitizeBattleName(battleName);
            BattleEntry entry = new BattleEntry(battleType);
            entry.type = battleType;

            inData.AddOrUpdateBattleEntry(fixedName, entry);
        }
    }
}