// Copyright (c) Craig Williams, MathWiz86

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A bar at the top of the <see cref="MenuPlayerStats"/> that shows the names of all players.
/// </summary>
public class StatBoardNameBar : MonoBehaviour
{
    /// <summary>The primary scroll view.</summary>
    [SerializeField] private ScrollRect titleScroll;

    /// <summary>The layout group that player name items are placed into.</summary>
    [SerializeField] private HorizontalLayoutGroup playerNameGroup;

    /// <summary>The <see cref="ScrollRect"/> that the <see cref="titleScroll"/> gets its position from. The two should line up in position and size.</summary>
    [SerializeField] private ScrollRect controllingScroll;

    /// <summary>The <see cref="TextBlockItem"/> to use for each player name.</summary>
    [SerializeField] private TextBlockItem nameFieldPrefab;

    private void Awake()
    {
        SaveDataManager.OnCurrentSaveLoaded += UpdatePlayerNames;
        UpdatePlayerNames();
    }

    private void Update()
    {
        if (titleScroll && controllingScroll)
            titleScroll.horizontalNormalizedPosition = controllingScroll.horizontalNormalizedPosition;
    }

    private void OnDestroy()
    {
        SaveDataManager.OnCurrentSaveLoaded -= UpdatePlayerNames;
    }

    /// <summary>
    /// An event called to update the player names displayed. Called whenever save data is updated.
    /// </summary>
    private void UpdatePlayerNames()
    {
        CalculatorTools.DestroyAllChildren(playerNameGroup?.transform);

        if (!playerNameGroup || !nameFieldPrefab)
            return;

        if (SaveDataManager.CurrentSaveData == null)
            return;

        // Create a text block for each player name, and set the appropriate title.
        int playerCount = SaveDataManager.CurrentSaveData.GetPlayerCount();
        for (int i = 0; i < playerCount; ++i)
        {
            TextBlockItem textBlock = Instantiate(nameFieldPrefab, playerNameGroup.transform);
            textBlock.Text = SaveDataManager.CurrentSaveData.GetPlayerName(i);
        }
    }
}