using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RuneMatchManager : MonoBehaviour
{
    [SerializeField]
    Sprite[] m_Runes;
    [SerializeField]
    Button[] m_RuneButtons;
    [SerializeField, Min(1)]
    int m_StartingRunes = 3;
    [SerializeField, Min(3)]
    int m_Rounds = 5;
    [SerializeField]
    Image m_RuneDisplayer;
    [SerializeField]
    float m_TimeBeforeRoundStart;
    [SerializeField]
    float m_RuneDisplayDuration;
    [SerializeField]
    GameObject m_WinScreen;
    [SerializeField]
    GameObject m_LoseScreen;
    [SerializeField]
    GameObject m_HUD;
    [SerializeField]
    MenuCharacter m_MenuCharacter;
    [SerializeField]
    RuneCharacter m_RuneCharacter;

    int[] m_CurrentRunesIndices;
    int m_CurrentRound;
    Coroutine m_RuneMatchRound;
    int m_CurrentRuneIndex;

    void Awake()
    {
        if (m_Runes == null || m_Runes.Length == 0)
        {
            Debug.LogError("No runes");
            return;
        }
        if (m_RuneButtons == null || m_RuneButtons.Length == 0)
        {
            Debug.LogError("No buttons");
            return;
        }
        if (m_Runes.Length != m_RuneButtons.Length)
        {
            Debug.LogError("Mismatch num rune sprites and buttons");
            return;
        }
        SetButtonImages();
    }

    void Start() => StartGame();

    void SetButtonImages()
    {
        for (int i = 0; i < m_RuneButtons.Length; ++i)
        {
            m_RuneButtons[i].image.sprite = m_Runes[i];
        }
    }

    void StartGame()
    {
        if (m_RuneMatchRound != null) StopCoroutine(m_RuneMatchRound);
        m_CurrentRunesIndices = new int[m_StartingRunes + m_Rounds - 1];
        RandomiseRunes();
        m_CurrentRound = 0;
        m_RuneMatchRound = StartCoroutine(StartRound(m_CurrentRound));
    }

    public void RestartGame()
    {
        m_MenuCharacter.OnMenuExit();
        StartGame();
    }

    void RandomiseRunes()
    {
        for (int i = 0; i < m_CurrentRunesIndices.Length; ++i)
        {
            m_CurrentRunesIndices[i] = Random.Range(0, m_Runes.Length);
        }
    }

    IEnumerator StartRound(int roundNumber)
    {
        foreach (Button b in m_RuneButtons) b.interactable = false; // Stop all button clicking while displaying
        yield return new WaitForSeconds(m_TimeBeforeRoundStart);
        for (int rune = 0; rune < m_StartingRunes + roundNumber; ++rune)
        {
            m_RuneDisplayer.sprite = m_Runes[m_CurrentRunesIndices[rune]];
            if (!m_RuneDisplayer.gameObject.activeSelf) m_RuneDisplayer.gameObject.SetActive(true);
            yield return new WaitForSeconds(m_RuneDisplayDuration);
        }
        m_RuneDisplayer.gameObject.SetActive(false); // Stop displaying the final rune
        m_CurrentRuneIndex = 0;
        foreach (Button b in m_RuneButtons) b.interactable = true; // Re-enable all button clicking after displaying
    }

    public void PressRune(int index)
    {
        if (index != m_CurrentRunesIndices[m_CurrentRuneIndex]) DoLose(); // Doesn't match so lose
        else if (++m_CurrentRuneIndex == m_CurrentRunesIndices.Length - 1) // They match so increase index for next rune press, but also check if that was last rune index
        {
            // Last rune index
            if (++m_CurrentRound == m_Rounds - 1) DoWin(); // Increase to the next round, but also check if that was the last round and if it is then win
            else m_RuneMatchRound = StartCoroutine(StartRound(m_CurrentRound)); // Not the last round so start the next round
        }
    }

    void DoWin()
    {
        MinigameManager.Instance.OnMinigameBeaten(Minigames.Rune);
        m_MenuCharacter.OnMenuOpen(m_RuneCharacter, m_HUD, m_WinScreen);
    }

    void DoLose() => m_MenuCharacter.OnMenuOpen(m_RuneCharacter, m_HUD, m_LoseScreen);
}
