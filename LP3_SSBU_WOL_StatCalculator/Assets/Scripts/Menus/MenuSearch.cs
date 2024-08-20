// Copyright (c) Craig Williams, MathWiz86

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A menu for searching for a <see cref="BattleEntry"/> to edit.
/// </summary>
public class MenuSearch : MenuBase
{
    /// <summary>A <see cref="OptionListInputField"/> for finding the right <see cref="BattleEntry"/>.</summary>
    [SerializeField] private OptionListInputField inputField;

    /// <summary>The <see cref="Button"/> used to add a new <see cref="BattleEntry"/>.</summary>
    [SerializeField] private Button addButton;

    /// <summary>The <see cref="Button"/> used to edit a <see cref="BattleEntry"/>.</summary>
    [SerializeField] private Button editButton;

    /// <summary>A <see cref="Button"/> for returning to the previous menu.</summary>
    [SerializeField] private Button backButton;

    /// <summary>A text block for the <see cref="CalculatorSaveData.lastAddedBattle"/>.</summary>
    [SerializeField] private TMP_Text lastAddedText;

    /// <summary>The menu for editing a <see cref="BattleEntry"/>.</summary>
    [SerializeField] private MenuBattleEdit editMenu;

    private void Awake()
    {
        addButton?.onClick.AddListener(OnAddButtonClicked);
        editButton?.onClick.AddListener(OnEditButtonClicked);
        backButton?.onClick.AddListener(OnBackButtonClicked);

        if (inputField != null)
            inputField.OnHasValidEntryChanged += OnInputFieldValidChanged;

        SaveDataManager.OnCurrentSaveLoaded += OnCurrentSaveLoaded;
        OnCurrentSaveLoaded();
    }

    private void OnEnable()
    {
        UpdateInputs();
    }

    /// <summary>
    /// An event called when a new save data is loaded.
    /// </summary>
    private void OnCurrentSaveLoaded()
    {
        if (SaveDataManager.CurrentSaveData == null)
            return;

        SaveDataManager.CurrentSaveData.OnBattleEntryListUpdated += _ => UpdateInputs();
        UpdateInputs();
    }

    /// <summary>
    /// Updates the <see cref="inputField"/>'s entries.
    /// </summary>
    private void UpdateInputs()
    {
        if (SaveDataManager.CurrentSaveData == null)
            return;

        if (inputField)
            inputField.SetOptions(SaveDataManager.CurrentSaveData.GetBattleEntryKeys());

        if (lastAddedText)
            lastAddedText.text = $"Last Added: {SaveDataManager.GetBattleDisplayName(SaveDataManager.CurrentSaveData.lastAddedBattle)}";
    }

    /// <summary>
    /// An event called when the <see cref="addButton"/> is clicked.
    /// </summary>
    private void OnAddButtonClicked()
    {
        if (!inputField || SaveDataManager.CurrentSaveData == null)
            return;

        string newName = inputField.GetSearchText();

        // Don't add copies, or attempt to.
        if (SaveDataManager.CurrentSaveData.GetBattleEntry(newName, out BattleEntry _))
            return;

        SaveDataManager.CurrentSaveData.AddOrUpdateBattleEntry(newName, new BattleEntry(BattleType.Spirit));
        SaveDataManager.WriteSaveData(SaveDataManager.CurrentSaveData);
    }

    /// <summary>
    /// An event called when the <see cref="editButton"/> is clicked.
    /// </summary>
    private void OnEditButtonClicked()
    {
        if (!editMenu)
            return;

        MenuManager.OpenMenu(editMenu);
        editMenu.SetEditingBattle(inputField.GetEditingBattleName());
    }

    /// <summary>
    /// An event called when the <see cref="backButton"/> is clicked.
    /// </summary>
    private void OnBackButtonClicked()
    {
        MenuManager.PopMenu(this);
    }

    /// <summary>
    /// An event called when the <see cref="inputField"/> has or no longer has a valid entry to check.
    /// </summary>
    private void OnInputFieldValidChanged(bool isValid)
    {
        if (addButton)
            addButton.enabled = isValid;
    }
}