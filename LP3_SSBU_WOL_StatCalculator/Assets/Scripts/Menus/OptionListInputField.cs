// Copyright (c) Craig Williams, MathWiz86

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// An input field for a large list of options, which trim values based on the current entry.
/// </summary>
public class OptionListInputField : MonoBehaviour
{
    public delegate void OptionListSearchValidDelegate(bool hasValidEntry);

    /// <summary>An event called when the search entry is or is no longer valid.</summary>
    public event OptionListSearchValidDelegate OnHasValidEntryChanged;

    /// <summary>The <see cref="TMP_InputField"/> for searching for an option.</summary>
    [SerializeField] private TMP_InputField searchField;

    /// <summary>A <see cref="TMP_Dropdown"/> list of possible options.</summary>
    [SerializeField] private TMP_Dropdown optionDropdown;

    /// <summary>The list of dropdown options for the input field.</summary>
    private List<TMP_Dropdown.OptionData> _options;

    /// <summary>If true, the <see cref="searchField"/> has a valid entry.</summary>
    private bool _hasValidEntry;

    private void Awake()    
    {
        _options = new List<TMP_Dropdown.OptionData>();

        if (searchField)
            searchField.onValueChanged.AddListener(OnSearchInputChanged);
    }

    /// <summary>
    /// Sets the available options for the search.
    /// </summary>
    /// <param name="options">The list of option names.</param>
    public void SetOptions(List<string> options)
    {
        _options.Clear();

        foreach (string s in options)
        {
            _options.Add(new TMP_Dropdown.OptionData(s));
        }

        UpdateDropdownFilter();
    }

    /// <summary>
    /// Gets the search entry text.
    /// </summary>
    /// <returns>Returns the current search text.</returns>
    public string GetSearchText()
    {
        return searchField.text;
    }

    /// <summary>
    /// Gets the name for the battle to edit.
    /// </summary>
    /// <returns>Returns the name of the battle to edit.</returns>
    public string GetEditingBattleName()
    {
        return optionDropdown.options[optionDropdown.value].text;
    }

    /// <summary>
    /// Updates the option dropdown filter.
    /// </summary>
    private void UpdateDropdownFilter()
    {
        if (!searchField || !optionDropdown)
            return;

        string field = searchField.text;
        optionDropdown.options =  _options.FindAll(option => option.text.IndexOf(field, StringComparison.OrdinalIgnoreCase) >= 0);
        optionDropdown.RefreshShownValue();
        optionDropdown.value = Math.Clamp(optionDropdown.value, -1, optionDropdown.options.Count - 1);
    }

    /// <summary>
    /// An event called when the <see cref="searchField"/> updates its value.
    /// </summary>
    /// <param name="text">The current <see cref="searchField"/> value.</param>
    private void OnSearchInputChanged(string text)
    {
        UpdateDropdownFilter();

        bool isValid = !string.IsNullOrWhiteSpace(text);
        if (_hasValidEntry == isValid)
            return;

        _hasValidEntry = isValid;
        OnHasValidEntryChanged?.Invoke(_hasValidEntry);
    }
}