// Copyright (c) Craig Williams, MathWiz86

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Data to display a modal dialog.
/// </summary>
[Serializable]
public struct MenuDialogData
{
    /// <summary>The header text.</summary>
    public string header;

    /// <summary>The message text.</summary>
    public string message;

    /// <summary>The text the <see cref="MenuDialog.dialogYesButton"/> will display.</summary>
    public string yesButtonText;

    /// <summary>The text the <see cref="MenuDialog.dialogNoButton"/> will display.</summary>
    public string noButtonText;

    public MenuDialogData(string header, string message, string yesText = "Yes", string noText = "No")
    {
        this.header = header;
        this.message = message;
        yesButtonText = yesText;
        noButtonText = noText;
    }
}

/// <summary>
/// A dialog menu to show a pop-up to the user.
/// </summary>
public class MenuDialog : MenuBase
{
    /// <summary>The text block for the header.</summary>
    [SerializeField] private TMP_Text dialogHeader;

    /// <summary>A scroll for the <see cref="dialogMessage"/>.</summary>
    [SerializeField] private ScrollRect dialogScroll;

    /// <summary>The text block for the message.</summary>
    [SerializeField] private TMP_Text dialogMessage;

    /// <summary>The <see cref="Button"/> for the <see cref="_yesAction"/>.</summary>
    [SerializeField] private Button dialogYesButton;

    /// <summary>The text block for the yes text.</summary>
    [SerializeField] private TMP_Text dialogYesButtonText;

    /// <summary>The <see cref="Button"/> for the <see cref="_noAction"/>.</summary>
    [SerializeField] private Button dialogNoButton;

    /// <summary>The text block for the no text.</summary>
    [SerializeField] private TMP_Text dialogNoButtonText;

    /// <summary>The <see cref="MenuDialogData"/> being displayed.</summary>
    private MenuDialogData _currentDialog;

    /// <summary>The <see cref="Action"/> to invoke when the <see cref="dialogYesButton"/> is clicked.</summary>
    private Action _yesAction;

    /// <summary>The <see cref="Action"/> to invoke when the <see cref="dialogNoButton"/> is clicked.</summary>
    private Action _noAction;

    public MenuDialog()
    {
        CollapseLowerMenus = false;
    }

    private void Awake()
    {
        dialogYesButton?.onClick.AddListener(OnYesButtonClicked);
        dialogNoButton?.onClick.AddListener(OnNoButtonClicked);
    }

    /// <summary>
    /// Displays the given <see cref="MenuDialogData"/>.
    /// </summary>
    /// <param name="inDialog">The dialog to display.</param>
    /// <param name="yesAction">The <see cref="Action"/> to invoke when the <see cref="dialogYesButton"/> is clicked.</param>
    /// <param name="noAction">The <see cref="Action"/> to invoke when the <see cref="dialogNoButton"/> is clicked.</param>
    /// <returns>Returns if the dialog was set successfully.</returns>
    public bool SetDialogData(MenuDialogData inDialog, Action yesAction = null, Action noAction = null)
    {
        if (!LogManager.ValidateOrLog(yesAction != null || noAction != null, LogType.Error, $"Cannot display dialog without any actions. Dialog message: [{inDialog.message}]"))
            return false;

        if (!LogManager.ValidateOrLog(dialogYesButton && dialogNoButton, LogType.Error, "Cannot show dialog. Buttons are null!"))
            return false;

        _currentDialog = inDialog;
        _yesAction = yesAction;
        _noAction = noAction;

        dialogHeader?.SetText(_currentDialog.header);
        dialogMessage?.SetText(_currentDialog.message);
        dialogYesButtonText?.SetText(_currentDialog.yesButtonText);
        dialogNoButtonText?.SetText(_currentDialog.noButtonText);

        dialogYesButton?.gameObject.SetActive(_yesAction != null);
        dialogNoButton?.gameObject.SetActive(_noAction != null);

        if (dialogScroll)
            dialogScroll.verticalNormalizedPosition = 1.0f;

        return true;
    }

    /// <summary>
    /// An event called when the <see cref="dialogYesButton"/> is clicked.
    /// </summary>
    private void OnYesButtonClicked()
    {
        MenuManager.PopMenu(this);
        _yesAction?.Invoke();
        CleanDialog();
    }

    /// <summary>
    /// An event called when the <see cref="dialogNoButton"/> is clicked.
    /// </summary>
    private void OnNoButtonClicked()
    {
        MenuManager.PopMenu(this);
        _noAction?.Invoke();
        CleanDialog();
    }

    /// <summary>
    /// Resets the dialog UI.
    /// </summary>
    private void CleanDialog()
    {
        _currentDialog = new MenuDialogData();
        _yesAction = null;
        _noAction = null;
    }
}