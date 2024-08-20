// Copyright (c) Craig Williams, MathWiz86

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A menu for editing a <see cref="BattleEntry"/>
/// </summary>
public class MenuBattleEdit : MenuBase
{
    /// <summary>The title bar to show what <see cref="BattleEntry"/> is being edited.</summary>
    [SerializeField] private TMP_Text battleTitle;

    /// <summary>The <see cref="Button"/> to save the entry.</summary>
    [SerializeField] [Header("Options")] private Button saveButton;

    /// <summary>A UI element to show that there are unsaved changes.</summary>
    [SerializeField] private GameObject saveWarning;

    /// <summary>The <see cref="Button"/> to delete the entry.</summary>
    [SerializeField] private Button deleteButton;

    /// <summary>The <see cref="MenuDialogData"/> to display when the player tries to delete the entry.</summary>
    [SerializeField] private MenuDialogData deleteEntryDialog;

    /// <summary>The <see cref="Button"/> to exit the menu.</summary>
    [SerializeField] private Button backButton;

    /// <summary>The dropdown to show the <see cref="BattleType"/> options.</summary>
    [SerializeField] [Header("Entry Data")] private TMP_Dropdown battleTypeDropdown;

    /// <summary>The dropdown to show the player options for the winner.</summary>
    [SerializeField] private TMP_Dropdown winnerDropdown;

    /// <summary>A <see cref="Toggle"/> for setting the entry as a shared battle.</summary>
    [SerializeField] private Toggle sharedBattleToggle;

    /// <summary>The panel to place <see cref="PlayerBattleDataObject"/>s into for each player.</summary>
    [SerializeField] private Transform playerDataPanel;

    /// <summary>The <see cref="PlayerBattleDataObject"/> to used for shared battle data.</summary>
    [SerializeField] private PlayerBattleDataObject sharedDataObject;

    /// <summary>The prefab for the <see cref="PlayerBattleDataObject"/> to create for each player.</summary>
    [SerializeField] private PlayerBattleDataObject playerDataPrefab;

    /// <summary>The created player <see cref="PlayerBattleDataObject"/>s.</summary>
    private readonly List<PlayerBattleDataObject> _playerDataObjects = new List<PlayerBattleDataObject>();

    /// <summary>The name of the edited <see cref="BattleEntry"/>.</summary>
    private string _battleName;

    /// <summary>The original <see cref="BattleEntry"/>.</summary>
    private BattleEntry _originalEntry;

    /// <summary>A copy of the <see cref="_originalEntry"/> for editing without saving.</summary>
    private BattleEntry _editingEntry;

    private void Awake()
    {
        CalculatorTools.DestroyAllChildren(playerDataPanel);
        SaveDataManager.OnCurrentSaveLoaded += UpdateEditingInterface;
        UpdateEditingInterface();

        saveButton?.onClick.AddListener(OnSaveButtonClicked);
        backButton?.onClick.AddListener(OnBackButtonClicked);
        deleteButton?.onClick.AddListener(OnDeleteButtonClicked);

        sharedBattleToggle?.onValueChanged.AddListener(OnSharedBattleChanged);
        winnerDropdown?.onValueChanged.AddListener(OnWinnerChanged);

        if (sharedDataObject)
        {
            sharedDataObject.SetPlayerIndex(CalculatorTools.NoneIndex);
            sharedDataObject.OnBattleObjectUpdated += OnBattleObjectUpdated;
        }

        // Create the dropdown options for the battle type.
        if (battleTypeDropdown)
        {
            List<TMP_Dropdown.OptionData> typeOptions = new List<TMP_Dropdown.OptionData>();
            foreach (string s in Enum.GetNames(typeof(BattleType)))
            {
                TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(s);
                typeOptions.Add(option);
            }

            battleTypeDropdown.options = typeOptions;
            battleTypeDropdown.onValueChanged.AddListener(OnBattleTypeChanged);
        }
    }

    private void Update()
    {
        // Check if there are unsaved changes.
        if (saveWarning)
            saveWarning.SetActive(_originalEntry != _editingEntry);
    }

    /// <summary>
    /// Sets the battle that's being edited.
    /// </summary>
    /// <param name="battleName">The name of the <see cref="BattleEntry"/> to edit.</param>
    public void SetEditingBattle(string battleName)
    {
        _battleName = CalculatorTools.SanitizeBattleName(battleName);

        // If the entry doesn't exist, we should close this menu.
        if (!SaveDataManager.CurrentSaveData.GetBattleEntry(battleName, out _originalEntry))
        {
            LogManager.WriteLogLine(LogType.Error, $"Unable to find battle entry [{battleName}]. Closing editing menu.");
            MenuManager.PopMenu(this);
            return;
        }

        // Create a copy of the entry for actual editing.
        _editingEntry = new BattleEntry(_originalEntry);

        // If there's data on this battle, use its display name in the title.
        SaveDataManager.GetSpiritData(battleName, out SpiritData spiritData);
        battleTitle?.SetText($"Editing \"{(!string.IsNullOrWhiteSpace(spiritData.spiritName) ? spiritData.spiritName : _battleName)}\"");

        battleTypeDropdown?.SetValueWithoutNotify((int)_editingEntry.type);
        winnerDropdown?.SetValueWithoutNotify(_editingEntry.winningPlayer + 1); // Adjust for 'None'
        sharedBattleToggle?.SetIsOnWithoutNotify(_editingEntry.isBattleShared);

        // Initialize all data objects.
        sharedDataObject?.InitializeFromBattle(_editingEntry);
        foreach (PlayerBattleDataObject dataObj in _playerDataObjects)
        {
            dataObj.InitializeFromBattle(_editingEntry);
        }

        OnSharedBattleChanged(_editingEntry.isBattleShared);
    }

    /// <summary>
    /// Updates the displayed <see cref="PlayerBattleDataObject"/>s.
    /// </summary>
    private void UpdateEditingInterface()
    {
        if (!LogManager.ValidateOrLog(playerDataPanel && playerDataPrefab, LogType.Error, "Unable to create PlayerBattleDataObjects. No prefab, or no panel!"))
            return;

        if (SaveDataManager.CurrentSaveData == null)
            return;

        int playerCount = SaveDataManager.CurrentSaveData.GetPlayerCount();

        // Go through currently existing data objects, and update their player index and if they should be visible based on player count.
        for (int i = 0; i < _playerDataObjects.Count; ++i)
        {
            PlayerBattleDataObject dataObj = _playerDataObjects[i];
            if (!dataObj)
            {
                _playerDataObjects.RemoveAt(i--);
                continue;
            }

            dataObj.gameObject.SetActive(i < playerCount);
            dataObj.SetPlayerIndex(i);
        }

        // For any remaining items needed, create them.
        for (int i = _playerDataObjects.Count; i < playerCount; ++i)
        {
            PlayerBattleDataObject dataObj = Instantiate(playerDataPrefab, playerDataPanel);
            dataObj.SetPlayerIndex(i);
            dataObj.OnBattleObjectUpdated += OnBattleObjectUpdated;
            _playerDataObjects.Add(dataObj);
        }

        // Update the winner options.
        if (winnerDropdown)
        {
            List<TMP_Dropdown.OptionData> winnerOptions = new List<TMP_Dropdown.OptionData>();
            winnerOptions.Add(new TMP_Dropdown.OptionData("None"));

            for (int i = 0; i < playerCount; ++i)
            {
                TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(SaveDataManager.CurrentSaveData.GetPlayerName(i));
                winnerOptions.Add(option);
            }

            winnerDropdown.options = winnerOptions;
        }
    }

    /// <summary>
    /// An event called when the <see cref="saveButton"/> is clicked.
    /// </summary>
    private void OnSaveButtonClicked()
    {
        if (SaveDataManager.CurrentSaveData == null)
            return;

        SaveDataManager.CurrentSaveData.AddOrUpdateBattleEntry(_battleName, _editingEntry);
        SaveDataManager.WriteSaveData(SaveDataManager.CurrentSaveData);

        // Update the original entry copy again.
        _originalEntry = new BattleEntry(_editingEntry);
    }

    /// <summary>
    /// An event called when the <see cref="deleteButton"/> is clicked.
    /// </summary>
    private void OnDeleteButtonClicked()
    {
        MenuManager.DisplayDialog(deleteEntryDialog, OnDeleteConfirmed, () => { });
    }

    /// <summary>
    /// An event called when the player confirms to delete the current battle entry.
    /// </summary>
    private void OnDeleteConfirmed()
    {
        if (!SaveDataManager.CurrentSaveData.RemoveBattleEntry(_battleName))
            return;

        SaveDataManager.WriteSaveData(SaveDataManager.CurrentSaveData);
        MenuManager.PopMenu(this);
    }

    /// <summary>
    /// An event called when the <see cref="backButton"/> is clicked.
    /// </summary>
    private void OnBackButtonClicked()
    {
        MenuManager.PopMenu(this);
    }

    /// <summary>
    /// An event called when the <see cref="battleTypeDropdown"/> updates its value.
    /// </summary>
    /// <param name="value">The new <see cref="BattleType"/> value.</param>
    private void OnBattleTypeChanged(int value)
    {
        _editingEntry.type = (BattleType)value;
    }

    /// <summary>
    /// An event called when the <see cref="winnerDropdown"/> updates its value.
    /// </summary>
    /// <param name="value">The new winning player index.</param>
    private void OnWinnerChanged(int value)
    {
        // Adjust for 'None'
        _editingEntry.winningPlayer = value - 1;
    }

    /// <summary>
    /// An event called when the <see cref="sharedBattleToggle"/> updates its value.
    /// </summary>
    /// <param name="bValue">Whether the battle is now shared.</param>
    private void OnSharedBattleChanged(bool bValue)
    {
        _editingEntry.isBattleShared = bValue;

        sharedDataObject?.gameObject.SetActive(bValue);
        playerDataPanel?.gameObject.SetActive(!bValue);
    }

    /// <summary>
    /// An event called when one of the <see cref="PlayerBattleDataObject"/>s updates its values.
    /// </summary>
    /// <param name="inBattleObj">The updating <see cref="PlayerBattleDataObject"/>.</param>
    private void OnBattleObjectUpdated(PlayerBattleDataObject inBattleObj)
    {
        if (!inBattleObj)
            return;

        _editingEntry.UpdateLoss(inBattleObj.PlayerIndex, inBattleObj.LossCount);
    }
}