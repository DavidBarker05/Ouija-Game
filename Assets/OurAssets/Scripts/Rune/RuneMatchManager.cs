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

    Sprite[] m_CurrentRunes;
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
        m_CurrentRunes = new Sprite[m_StartingRunes + m_Rounds - 1];
        RandomiseRunes();
        m_CurrentRound = 0;
		m_RuneMatchRound = StartCoroutine(StartRound(m_CurrentRound));
    }

    void RandomiseRunes()
    {
        for (int i = 0; i < m_CurrentRunes.Length; ++i)
        {
            int index = Random.Range(0, m_Runes.Length);
            m_CurrentRunes[i] = m_Runes[index];
        }
    }

    IEnumerator StartRound(int roundNumber)
    {
        foreach (Button b in m_RuneButtons) b.interactable = false; // Stop all button clicking while displaying
        yield return new WaitForSeconds(m_TimeBeforeRoundStart);
		for (int rune = 0; rune < m_StartingRunes + roundNumber; ++rune)
		{
			m_RuneDisplayer.sprite = m_Runes[rune];
            if (!m_RuneDisplayer.gameObject.activeSelf) m_RuneDisplayer.gameObject.SetActive(true);
			yield return new WaitForSeconds(m_RuneDisplayDuration);
		}
        m_RuneDisplayer.gameObject.SetActive(false); // Stop displaying the final rune
        m_CurrentRuneIndex = 0;
		foreach (Button b in m_RuneButtons) b.interactable = true; // Re-enable all button clicking after displaying
	}

    public void PressRune(int index)
    {
        Sprite rune = m_Runes[index];
        Sprite currentRune = m_CurrentRunes[m_CurrentRuneIndex];
        if (rune != currentRune) DoLose(); // Doesn't match so lose
        else if (++m_CurrentRuneIndex == m_CurrentRunes.Length) // They match so increase index for next rune press, but also check if that was last rune index
        {
            // Last rune index
            if (++m_CurrentRound == m_Rounds) DoWin(); // Increase to the next round, but also check if that was the last round and if it is then win
            else m_RuneMatchRound = StartCoroutine(StartRound(m_CurrentRound)); // Not the last round so start the next round
        }
    }

	void DoWin()
	{
        MinigameManager.Instance.OnMinigameBeaten(Minigames.Rune);
	}

	void DoLose()
	{
        StartGame();
	}
}
