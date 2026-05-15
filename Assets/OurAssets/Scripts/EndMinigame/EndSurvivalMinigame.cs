using TMPro;
using UnityEngine;

[System.Serializable]
public struct CandleBlowOutTimeFrame
{
	[field: SerializeField, Min(0.001f), Tooltip("The time in seconds between each blow out of candles")]
	public float BlowOutTime { get; private set; }
	[field: SerializeField, Min(0)]
	public int MinCandles { get; private set; }
	[field: SerializeField, Min(0)]
	public int MaxCandles { get; private set; }
}

[System.Serializable]
public struct CandleBlowOutSettings
{
	[field: SerializeField]
	public CandleBlowOutTimeFrame FirstEighth { get; private set; }
	[field: SerializeField]
	public CandleBlowOutTimeFrame FirstQuarter { get; private set; }
	[field: SerializeField]
	public CandleBlowOutTimeFrame FirstHalf { get; private set; }
	[field: SerializeField]
	public CandleBlowOutTimeFrame LastHalf { get; private set; }
	[field: SerializeField]
	public CandleBlowOutTimeFrame LastQuarter { get; private set; }
	[field: SerializeField]
	public CandleBlowOutTimeFrame LastEighth { get; private set; }
}

public class EndSurvivalMinigame : MonoBehaviour
{
	public static EndSurvivalMinigame Instance { get; private set; } // Ensure only one exists in a scene

	[SerializeField, Range(60f, 600f)]
	float m_TimeToSurvive = 300f; // Maybe 180s
	[SerializeField]
	TMP_Text m_Timer;
	[SerializeField]
	CandleBlowOutSettings m_CandleBlowOutSettings;
	[SerializeField]
	BigPentagram m_Pentagram;
	[SerializeField]
	GameObject m_WinScreen;
	[SerializeField]
	GameObject m_LoseScreen;
	[SerializeField]
	GameObject m_HUD;
	[SerializeField]
	MenuCharacter m_MenuCharacter;
	[SerializeField]
	FirstPersonCharacter m_FirstPersonCharacter;

	float m_CurrentTime;
	float m_CandleBlowTimer;

	void Awake()
	{
		if (Instance && Instance != this) Destroy(gameObject);
		else Instance = this;
	}

	void Start()
	{
		if (m_Pentagram?.Init() ?? false) m_CurrentTime = m_TimeToSurvive;
		m_CandleBlowTimer = 0f;
	}

	void Update()
	{
		m_CurrentTime -= Time.deltaTime;
		m_Timer.text = $"Time Remaining: {Mathf.Max(Mathf.CeilToInt(m_CurrentTime), 0)}s";
		if (m_CurrentTime <= 0f) DoWin();
		m_CandleBlowTimer += Time.deltaTime;
		CandleBlowOutTimeFrame currentBlowOutFrame = CurrentCandleBlowOutFrame;
		if (m_CandleBlowTimer < currentBlowOutFrame.BlowOutTime) return;
		m_CandleBlowTimer = 0f;
		int numCandlesToExtinguish = Random.Range(currentBlowOutFrame.MinCandles, currentBlowOutFrame.MaxCandles + 1);
		if (m_Pentagram.ExtinguishCandles(numCandlesToExtinguish)) DoLose();
	}

	CandleBlowOutTimeFrame CurrentCandleBlowOutFrame
	{
		get
		{
			if (m_CurrentTime >= m_TimeToSurvive - m_TimeToSurvive / 8f) return m_CandleBlowOutSettings.FirstEighth;
			if (m_CurrentTime >= m_TimeToSurvive - m_TimeToSurvive / 4f) return m_CandleBlowOutSettings.FirstQuarter;
			if (m_CurrentTime >= m_TimeToSurvive - m_TimeToSurvive / 2f) return m_CandleBlowOutSettings.FirstHalf;
			if (m_CurrentTime >= m_TimeToSurvive - m_TimeToSurvive / 2f - m_TimeToSurvive / 8f) return m_CandleBlowOutSettings.LastHalf;
			if (m_CurrentTime >= m_TimeToSurvive - m_TimeToSurvive / 2f - m_TimeToSurvive / 4f) return m_CandleBlowOutSettings.LastQuarter;
			return m_CandleBlowOutSettings.LastEighth;
		}
	}

	void DoWin() => m_MenuCharacter.OnMenuOpen(m_FirstPersonCharacter, m_HUD, m_WinScreen);

	void DoLose() => m_MenuCharacter.OnMenuOpen(m_FirstPersonCharacter, m_HUD, m_LoseScreen);
}
