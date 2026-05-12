using UnityEngine;

public class EndSurvivalMinigame : MonoBehaviour
{
	public static EndSurvivalMinigame Instance { get; private set; } // Ensure only one exists in a scene

	[SerializeField, Range(60f, 600f)]
	float m_TimeToSurvive = 300f; // Maybe 180s
	[SerializeField]
	BigPentagram m_Pentagram;

	float m_CurrentTime;

	void Awake()
	{
		if (Instance && Instance != this) Destroy(gameObject);
		else Instance = this;
	}

	void Start()
	{
		if(m_Pentagram?.Init() ?? false) m_CurrentTime = m_TimeToSurvive;
	}

	void Update()
	{
		m_CurrentTime -= Time.deltaTime;
		if (m_CurrentTime <= 0f) DoWin();
		// TODO: Blow out candles, also how many candles should I even blow out?
	}

	void DoWin()
	{
		// TODO: Win
	}

	void DoLose()
	{
		// TODO: Lose
	}
}
