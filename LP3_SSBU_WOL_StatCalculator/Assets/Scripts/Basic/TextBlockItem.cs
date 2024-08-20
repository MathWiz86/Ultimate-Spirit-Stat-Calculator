// Copyright (c) Craig Williams, MathWiz86

using TMPro;
using UnityEngine;

/// <summary>
/// A simple designed text block.
/// </summary>
public class TextBlockItem : MonoBehaviour
{
    /// <summary>The <see cref="TMP_Text"/> to place text into.</summary>
    [SerializeField] private TMP_Text textBlock;
    
    /// <summary>The displayed text.</summary>
    public string Text { get { return textBlock?.text; } set { textBlock?.SetText(value);} }
    
    /// <summary>The color of the text.</summary>
    public Color TextColor { get { return textBlock ? textBlock.color : Color.white; } set { if (textBlock) textBlock.color = value; } }
}