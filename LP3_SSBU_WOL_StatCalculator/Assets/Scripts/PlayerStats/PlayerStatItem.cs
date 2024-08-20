// Copyright (c) Craig Williams, MathWiz86

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// An element for displaying the results of a <see cref="PlayerStat"/>.
/// </summary>
public class PlayerStatItem : MonoBehaviour
{
    /// <summary>A <see cref="Button"/> for opening up the stat's details.</summary>
    [SerializeField] private Button statButton;

    /// <summary>The type of stat this item can be used for.</summary>
    [SerializeField] private StatType statType;

    /// <summary>The <see cref="PlayerStat"/> displayed.</summary>
    protected PlayerStat Stat;

    private void Start()
    {
        statButton?.onClick.AddListener(OnStatButtonClicked);
    }

    /// <summary>
    /// Gets the <see cref="statType"/>.
    /// </summary>
    /// <returns>Returns the <see cref="statType"/>.</returns>
    public StatType GetStatType()
    {
        return statType;
    }

    /// <summary>
    /// Sets the displayed <see cref="PlayerStat"/>.
    /// </summary>
    /// <param name="stat">The <see cref="PlayerStat"/> to represent.</param>
    /// <param name="autoTally">If true, the stat tallies instantly.</param>
    public void SetDisplayedStat(PlayerStat stat, bool autoTally = false)
    {
        Stat = stat;
        if (Stat == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (autoTally)
            TallyStat();
    }

    /// <summary>
    /// Tallies the stat's total value and stores the results.
    /// </summary>
    public void TallyStat()
    {
        if (Stat == null || !Stat.IsValidStat())
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        Stat.TallyStat();
        OnStatTallied(Stat);
    }

    /// <summary>
    /// An event called when the <see cref="statButton"/> is clicked, opening up a dialog to show the results closer.
    /// </summary>
    private void OnStatButtonClicked()
    {
        if (Stat == null)
            return;

        string message = GetResultMessage();
        if (string.IsNullOrWhiteSpace(message))
            return;

        MenuDialogData dialogData = new MenuDialogData();
        dialogData.header = Stat.Title;
        dialogData.message = message;
        dialogData.noButtonText = "Back";

        MenuManager.DisplayDialog(dialogData, null, () => { });
    }

    /// <summary>
    /// An event called once the <see cref="Stat"/> is tallied.
    /// </summary>
    /// <param name="stat">See: <see cref="Stat"/></param>
    protected virtual void OnStatTallied(PlayerStat stat) { }

    /// <summary>
    /// Gets the text to use for the result pop-up message.
    /// </summary>
    /// <returns>Returns the total message.</returns>
    protected virtual string GetResultMessage()
    {
        return string.Empty;
    }
}