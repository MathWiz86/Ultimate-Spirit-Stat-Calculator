// Copyright (c) Craig Williams, MathWiz86

using UnityEngine;

/// <summary>
/// A base class for all menus.
/// </summary>
public class MenuBase : MonoBehaviour
{
    /// <summary>If true, this menu should collapse any menu below it.</summary>
    public bool CollapseLowerMenus { get; protected set; } = true;
}