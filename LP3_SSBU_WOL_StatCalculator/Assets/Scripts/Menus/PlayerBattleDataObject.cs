// Copyright (c) Craig Williams, MathWiz86

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A display of a single player's <see cref="BattleEntry"/> data.
/// </summary>
public class PlayerBattleDataObject : MonoBehaviour
{
    public delegate void OnBattleObjectUpdatedDelegate(PlayerBattleDataObject inBattleObj);

    /// <summary>An event called when this <see cref="PlayerBattleDataObject"/> is updated.</summary>
    public event OnBattleObjectUpdatedDelegate OnBattleObjectUpdated;

    /// <summary>The index of the represented player.</summary>
    public int PlayerIndex { get; private set; }

    /// <summary>The number of losses the represented player has.</summary>
    public int LossCount { get; private set; }

    /// <summary>The text for the player name.</summary>
    [SerializeField] private TMP_Text playerTitle;

    /// <summary>The field for the user to input the <see cref="LossCount"/> directly.</summary>
    [SerializeField] private TMP_InputField lossInput;

    /// <summary>A <see cref="Button"/> for incrementing the <see cref="LossCount"/> by 1.</summary>
    [SerializeField] private Button lossIncreaseButton;

    /// <summary>A <see cref="Button"/> for decrementing the <see cref="LossCount"/> by 1.</summary>
    [SerializeField] private Button lossDecreaseButton;

    private void Awake()
    {
        lossInput?.onValueChanged.AddListener(OnLossInputChanged);
        lossIncreaseButton?.onClick.AddListener(OnLossIncreaseClicked);
        lossDecreaseButton?.onClick.AddListener(OnLossDecreaseClicked);
    }

    /// <summary>
    /// Sets the player index, initializing the object.
    /// </summary>
    /// <param name="playerIndex">The represented player.</param>
    public void SetPlayerIndex(int playerIndex)
    {
        PlayerIndex = playerIndex;

        string playerName = string.Empty;
        if (playerIndex == CalculatorTools.NoneIndex)
            playerName = "Shared Data";
        else if (SaveDataManager.CurrentSaveData != null && SaveDataManager.CurrentSaveData.IsValidPlayer(playerIndex))
            playerName = SaveDataManager.CurrentSaveData.GetPlayerName(playerIndex);

        playerTitle?.SetText(playerName);
    }

    /// <summary>
    /// Initializes the battle data from the given <see cref="BattleEntry"/>. Call after <see cref="SetPlayerIndex"/>.
    /// </summary>
    /// <param name="inEntry">The <see cref="BattleEntry"/> to gather data from.</param>
    public void InitializeFromBattle(BattleEntry inEntry)
    {
        LossCount = inEntry.GetPlayerLosses(PlayerIndex, false);
        lossInput?.SetTextWithoutNotify(LossCount.ToString());
    }

    /// <summary>
    /// An event called when the <see cref="lossInput"/> updates its value.
    /// </summary>
    /// <param name="text">The loss text to be parsed.</param>
    private void OnLossInputChanged(string text)
    {
        int.TryParse(text, out int lossCount);
        LossCount = lossCount;

        OnBattleObjectUpdated?.Invoke(this);
    }

    /// <summary>
    /// An event called when the <see cref="lossIncreaseButton"/> is clicked.
    /// </summary>
    private void OnLossIncreaseClicked()
    {
        ++LossCount;

        if (lossInput)
            lossInput.text = LossCount.ToString();
    }

    /// <summary>
    /// An event called when the <see cref="lossDecreaseButton"/> is clicked.
    /// </summary>
    private void OnLossDecreaseClicked()
    {
        LossCount = Math.Max(0, LossCount - 1);

        if (lossInput)
            lossInput.text = LossCount.ToString();
    }
}