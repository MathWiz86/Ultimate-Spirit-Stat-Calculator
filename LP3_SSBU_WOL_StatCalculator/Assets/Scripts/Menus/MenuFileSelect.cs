// Copyright (c) Craig Williams, MathWiz86

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A menu to select a save data file.
/// </summary>
public class MenuFileSelect : MenuBase
{
    /// <summary>The parent to place <see cref="CalculatorFileOption"/>s into.</summary>
    [SerializeField] private Transform fileOptionPanel;
    
    /// <summary>A <see cref="Button"/> for opening the <see cref="newSaveMenu"/>.</summary>
    [SerializeField] private Button newSaveButton;

    /// <summary>The prefab for the file options.</summary>
    [SerializeField] private CalculatorFileOption fileOptionPrefab;

    /// <summary>The main menu to open, after selecting and loading a save file.</summary>
    [SerializeField] private MenuMain mainMenu;

    /// <summary>The menu to open if no save files are found, or the user requests to make a new file.</summary>
    [SerializeField] private MenuNewSave newSaveMenu;

    private void Awake()
    {
        newSaveButton?.onClick.AddListener(() => MenuManager.OpenMenu(newSaveMenu));
    }

    private void OnEnable()
    {
        if (CreateSaveFileOptions())
            return;

        if (!newSaveMenu)
            return;
        
        // If no save file options were created, go straight to creating a new save.
        MenuManager.OpenMenu(newSaveMenu);
        newSaveMenu.SetBackButtonEnabled(false);
    }

    /// <summary>
    /// Creates all save file options, based on files in the directory.
    /// </summary>
    /// <returns>Returns if any save file options were created.</returns>
    private bool CreateSaveFileOptions()
    {
        if (!fileOptionPanel || !fileOptionPrefab)
        {
            LogManager.WriteLogLine(LogType.Fatal, "No root parent for file options, or no option prefab!");
            return false;
        }

        CalculatorTools.DestroyAllChildren(fileOptionPanel);

        DirectoryInfo saveDirectory;
        try
        {
            saveDirectory = Directory.CreateDirectory(SaveDataManager.SaveDataDirectory);
        }
        catch (Exception e)
        {
            LogManager.WriteLogLine(LogType.Fatal, $"No save data directory set, or was unable to create directory! Exception: [{e}]");
            return false;
        }

        FileInfo[] files = saveDirectory.GetFiles($"{SaveDataManager.SaveDataFileHeader}*{SaveDataManager.DataFileExtension}");
        LogManager.WriteLogLine(LogType.Log, $"Found {files.Length} save data files");

        if (files.Length <= 0)
        {
            LogManager.WriteLogLine(LogType.Warning, "No save data files found. Prompting user to create a new save file.");
            return false;
        }

        // Go through all files, and create an option for them.
        foreach (FileInfo file in files)
        {
            CalculatorFileOption option = Instantiate(fileOptionPrefab, fileOptionPanel);
            option.SetFile(file);
            option.OnFileOptionSelected += OnFileOptionSelected;
        }

        return true;
    }

    /// <summary>
    /// An event called when a save file option is selected to be opened.
    /// </summary>
    /// <param name="inOption">The selected option.</param>
    private void OnFileOptionSelected(CalculatorFileOption inOption)
    {
        if (!inOption)
            return;

        LogManager.WriteLogLine(LogType.Log, $"Selected File Option [{inOption.FileName}]");
        
        CalculatorTools.DestroyAllChildren(fileOptionPanel);
        if (!SaveDataManager.LoadSaveData(inOption.FileName))
            return;

        MenuManager.OpenMenu(mainMenu);
    }
}