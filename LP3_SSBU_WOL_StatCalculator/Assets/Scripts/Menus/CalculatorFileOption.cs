// Copyright (c) Craig Williams, MathWiz86

using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// An element showing a file option to use for this calculator session.
/// </summary>
public class CalculatorFileOption : MonoBehaviour
{
    public delegate void OnFileOptionSelectedDelegate(CalculatorFileOption inOption);

    /// <summary>An event called when this file option is selected for editing.</summary>
    public event OnFileOptionSelectedDelegate OnFileOptionSelected;

    /// <summary>The whole name of the file, minus directories.</summary>
    public string FileName { get; private set; }

    /// <summary>The main button for the option.</summary>
    [SerializeField] private Button selectButton;

    /// <summary>The text block for the file's name.</summary>
    [SerializeField] private TMP_Text fileNameBlock;

    /// <summary>If true, the full file name is shown. Otherwise, just the pretty name is used.</summary>
    [SerializeField] private bool showFullFileName;

    private void Awake()
    {
        if (selectButton)
            selectButton.onClick.AddListener(() => OnFileOptionSelected?.Invoke(this));
    }

    /// <summary>
    /// Sets the file name of the option.
    /// </summary>
    /// <param name="inFile">The given file.</param>
    public void SetFile(FileInfo inFile)
    {
        if (inFile == null)
            return;

        FileName = inFile.Name;

        if (!fileNameBlock)
            return;

        if (showFullFileName)
        {
            fileNameBlock.text = FileName;
            return;
        }

        // Clean out the file name to the actual display name.
        fileNameBlock.text = CalculatorTools.GetSaveDataDisplayName(FileName);
    }
}