// Copyright (c) Craig Williams, MathWiz86

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A simple menu stack manager.
/// </summary>
public class MenuManager : MonoBehaviour
{
    /// <summary>The manager singleton.</summary>
    private static MenuManager _manager;

    /// <summary>The first menu to open up.</summary>
    [SerializeField] private MenuBase firstMenu;

    /// <summary>The <see cref="MenuDialog"/> to use for pop-ups.</summary>
    [SerializeField] private MenuDialog dialogMenu;

    /// <summary>The current menu stack, in order of how they've been opened.</summary>
    private readonly List<MenuBase> _menuStack = new List<MenuBase>();

    private void Awake()
    {
        if (_manager != null)
            return;

        _manager = this;
        OpenMenu(firstMenu);
    }

    /// <summary>
    /// Opens up the given menu object and removes it from the stack.
    /// </summary>
    /// <param name="inMenu">The menu object.</param>
    public static void OpenMenu(MenuBase inMenu)
    {
        if (!LogManager.ValidateOrLog(_manager, LogType.Error, $"Menu manager not initialized. Cannot open menu [{inMenu}]."))
            return;

        if (!LogManager.ValidateOrLog(inMenu, LogType.Error, "Cannot open null menu."))
            return;

        if (_manager._menuStack.Count > 0)
        {
            MenuBase lastMenu = _manager._menuStack[^1];

            if (inMenu.CollapseLowerMenus)
                lastMenu?.gameObject.SetActive(false);
        }

        _manager._menuStack.Add(inMenu);
        inMenu.gameObject.SetActive(true);
        LogManager.WriteLogLine(LogType.Verbose, $"Added menu [{inMenu}] to the menu stack.");
    }

    /// <summary>
    /// Pops the top menu off the stack and shows the bottommost menu.
    /// </summary>
    public static void PopTopMenu()
    {
        if (!_manager || _manager._menuStack.Count <= 0)
            return;

        MenuBase lastMenu = _manager._menuStack[^1];
        lastMenu?.gameObject.SetActive(false);
        _manager._menuStack.RemoveAt(_manager._menuStack.Count - 1);
        LogManager.WriteLogLine(LogType.Verbose, $"Popped menu [{lastMenu}]");

        // Find the next valid menu, and display it.
        while (_manager._menuStack.Count > 0)
        {
            MenuBase topMenu = _manager._menuStack[^1];

            if (topMenu)
            {
                topMenu.gameObject.SetActive(true);
                break;
            }

            _manager._menuStack.RemoveAt(_manager._menuStack.Count - 1);
        }
    }

    /// <summary>
    /// Removes the given menu from the menu stack.
    /// </summary>
    /// <param name="inMenu">The menu to remove.</param>
    public static void PopMenu(MenuBase inMenu)
    {
        if (!_manager || _manager._menuStack.Count <= 0)
            return;

        if (!LogManager.ValidateOrLog(inMenu, LogType.Error, "Cannot pop null menu."))
            return;

        MenuBase lastMenu = _manager._menuStack[^1];
        if (lastMenu == inMenu)
        {
            PopTopMenu();
            return;
        }

        int index = _manager._menuStack.IndexOf(inMenu);
        if (!LogManager.ValidateOrLog(index >= 0, LogType.Warning, $"Unable to pop menu [{inMenu}]. It is not part of the menu stack."))
            return;

        inMenu.gameObject.SetActive(false);
        _manager._menuStack.RemoveAt(index);
    }

    /// <summary>
    /// Displays the given <see cref="MenuDialogData"/>, using the <see cref="dialogMenu"/>.
    /// </summary>
    /// <param name="inDialog">The dialog to display.</param>
    /// <param name="yesAction">The <see cref="Action"/> to invoke when the <see cref="MenuDialog.dialogYesButton"/> is clicked.</param>
    /// <param name="noAction">The <see cref="Action"/> to invoke when the <see cref="MenuDialog.dialogNoButton"/> is clicked.</param>
    public static void DisplayDialog(MenuDialogData inDialog, Action yesAction = null, Action noAction = null)
    {
        if (!LogManager.ValidateOrLog(_manager, LogType.Error, "Cannot display dialog. Menu manager is not initialized."))
            return;

        if (!LogManager.ValidateOrLog(_manager.dialogMenu, LogType.Error, "Cannot display dialog. Dialog menu not initialized."))
            return;

        if (_manager.dialogMenu.SetDialogData(inDialog, yesAction, noAction))
            OpenMenu(_manager.dialogMenu);
    }
}