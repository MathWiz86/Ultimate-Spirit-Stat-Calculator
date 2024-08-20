// Copyright (c) Craig Williams, MathWiz86

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// An option button for the type of default save data to create.
/// </summary>
public class DataTypeOption : MonoBehaviour
{
    public delegate void DataTypeSelectedDelegate(DataTypeOption option);

    /// <summary>An event called when the data type is selected.</summary>
    public event DataTypeSelectedDelegate OnDataTypeSelected;

    /// <summary>The main <see cref="Button"/> to click.</summary>
    [SerializeField] private Button mainButton;

    /// <summary>A simple <see cref="GameObject"/> to display when this option is toggled selected.</summary>
    [SerializeField] private GameObject selectedIndicator;

    /// <summary>The text block for the data type's name.</summary>
    [SerializeField] private TMP_Text dataTypeTitle;

    /// <summary>A <see cref="Func{TResult}"/> that creates save data, based on what this option represents.</summary>
    public Func<CalculatorSaveData> DataCreationAction;

    /// <summary>If true, this option is selected.</summary>
    private bool _isSelected;

    private void Awake()
    {
        mainButton?.onClick.AddListener(() => OnDataTypeSelected?.Invoke(this));
        SetSelected(_isSelected);
    }

    /// <summary>
    /// Sets the option's name.
    /// </summary>
    /// <param name="inTitle">The name of the option.</param>
    public void SetTitle(string inTitle)
    {
        if (dataTypeTitle)
            dataTypeTitle.text = inTitle;
    }

    /// <summary>
    /// Sets if this option should show as selected.
    /// </summary>
    /// <param name="inSelected">If true, this option is selected.</param>
    public void SetSelected(bool inSelected)
    {
        _isSelected = inSelected;
        selectedIndicator?.SetActive(_isSelected);
    }
}