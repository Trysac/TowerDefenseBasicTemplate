using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class EndScreenUI : MonoBehaviour
{
    #region // Private Variables

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI headerText;
    [SerializeField] TextMeshProUGUI bodyText;

    #endregion

    // ------------------------------------------------

    #region // Public Methods

    public void SetEndScreen(bool didWin, int roundsSurvived)
    {
        HeaderText.text = didWin ? "You Win!" : "Game Over!";
        HeaderText.color = didWin ? Color.green : Color.red;
        BodyText.text = $"You survived {roundsSurvived} rounds.";
    }

    public void OnPlayAgainButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }

    #endregion

    // ------------------------------------------------

    #region // Variables Properties

    public TextMeshProUGUI HeaderText { get => headerText; set => headerText = value; }
    public TextMeshProUGUI BodyText { get => bodyText; set => bodyText = value; }

    #endregion

}