using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public struct CardFront
{
	public TarotCards Card;
	public Material Material;
}

public class TarotManager : MonoBehaviour
{
	public static TarotManager Instance { get; private set; }
	const int NUM_CARDS = 22;
	const int PAIR_VALUE = 21;
	const int MAX_PAIRS = 11;

	[SerializeField, Min(30f)]
	float m_TimeLimit = 180f;
	[SerializeField]
	TMP_Text m_Timer;
	[SerializeField, Min(0.1f)]
	float m_RemainFlippedTime = 0.5f;
	[SerializeField]
	TarotCard m_TarotCardPrefab;
	[SerializeField]
	Transform[] m_CardPositions;
	[field: SerializeField]
	public Material CardBack { get; private set; }
	[SerializeField]
	CardFront[] m_CardFrontMaterials;
	[SerializeField]
	GameObject m_WinScreen;
	[SerializeField]
	GameObject m_LoseScreen;
	[SerializeField]
	GameObject m_HUD;
	[SerializeField]
	MenuCharacter m_MenuCharacter;
	[SerializeField]
	TarotCharacter m_TarotCharacter;

	public bool CanFlipCard { get; private set; }

	Dictionary<TarotCards, Material> m_CardFronts = new Dictionary<TarotCards, Material>()
	{
		{ TarotCards.Fool, null  },
		{ TarotCards.Magician, null },
		{ TarotCards.HighPriestess, null },
		{ TarotCards.Empress, null },
		{ TarotCards.Emperor, null },
		{ TarotCards.Hierophant, null },
		{ TarotCards.Lovers, null },
		{ TarotCards.Chariot, null },
		{ TarotCards.Strength, null },
		{ TarotCards.Hermit, null },
		{ TarotCards.WheelOfFortune, null },
		{ TarotCards.Justice, null },
		{ TarotCards.HangedMan, null },
		{ TarotCards.Death, null },
		{ TarotCards.Temperance, null },
		{ TarotCards.Devil, null },
		{ TarotCards.Tower, null },
		{ TarotCards.Star, null },
		{ TarotCards.Moon, null },
		{ TarotCards.Sun, null },
		{ TarotCards.Judgement, null },
		{ TarotCards.World, null }
	};

	TarotCard[] m_Cards;

	TarotCard m_FlippedCardA = null;
	TarotCard m_FlippedCardB = null;

	int m_Pairs = 0;
	bool m_bDontUpdate = true;

	float m_CurrentTime;
	float m_CurrentFlipTime;

	void Awake()
	{
		if (Instance && Instance != this) Destroy(gameObject);
		else Instance = this;
	}

	void Start()
	{
		if (m_CardPositions == null || m_CardPositions.Length != NUM_CARDS)
		{
			Debug.LogError($"Invalid number of card positions need {NUM_CARDS}");
			return;
		}
		if (m_CardFrontMaterials == null || m_CardFrontMaterials.Length != NUM_CARDS)
		{
			Debug.LogError($"Invalid number of card front materials need {NUM_CARDS}");
			return;
		}
		StartGame();
	}

	void Update()
	{
		if (m_bDontUpdate) return;
		if (!CanFlipCard)
		{
			m_CurrentFlipTime += Time.deltaTime;
			if (m_CurrentFlipTime < m_RemainFlippedTime) return;
			m_CurrentFlipTime = 0f;
			m_FlippedCardA.Unflip();
			m_FlippedCardB.Unflip();
			m_FlippedCardA = null;
			m_FlippedCardB = null;
			CanFlipCard = true;
			return;
		}
		m_CurrentTime -= Time.deltaTime;
		m_Timer.text = $"Time Remaining: {Mathf.Max(Mathf.CeilToInt(m_CurrentTime), 0)}s";
		if (m_CurrentTime <= 0) DoLose();
	}

	void StartGame()
	{
		ClearValues();
		PopulateDictionary();
		m_Cards = new TarotCard[NUM_CARDS];
		CreateCards();
		ShuffleCards();
		CanFlipCard = true;
		m_bDontUpdate = false;
	}

	public void RestartGame()
	{
		m_MenuCharacter.OnMenuExit();
		ClearValues();
		RecreateCards();
		ShuffleCards();
		CanFlipCard = true;
		m_bDontUpdate = false;
	}

	void ClearValues()
	{
		m_FlippedCardA = null;
		m_FlippedCardB = null;
		m_Pairs = 0;
		m_CurrentTime = m_TimeLimit;
	}

	void PopulateDictionary()
	{
		foreach (CardFront front in m_CardFrontMaterials)
		{
			if (!front.Material || m_CardFronts[front.Card]) continue;
			m_CardFronts[front.Card] = front.Material;
		}
	}

	void CreateCards()
	{
		for (int i = 0; i < NUM_CARDS; ++i)
		{
			m_Cards[i] = Instantiate(m_TarotCardPrefab.gameObject, m_CardPositions[i]).GetComponent<TarotCard>();
			m_Cards[i].gameObject.name = ((TarotCards)i).ToString();
			m_Cards[i].SetCard((TarotCards)i);
		}
	}

	void RecreateCards()
	{
		for (int i = 0; i < NUM_CARDS; ++i)
		{
			m_Cards[i].gameObject.name = ((TarotCards)i).ToString();
			m_Cards[i].SetCard((TarotCards)i);
		}
	}

	void ShuffleCards()
	{
		System.Random rng = new System.Random();
		for (int i = NUM_CARDS - 1; i > 0; --i)
		{
			int j = rng.Next(i + 1);
			Vector3 _temp = m_Cards[i].transform.position;
			m_Cards[i].transform.position = m_Cards[j].transform.position;
			m_Cards[j].transform.position = _temp;
		}
	}

	public Material CardFront(TarotCards card) => m_CardFronts[card];

	public void FlipCard(TarotCard tarotCard)
	{
		if (m_FlippedCardA) m_FlippedCardB = tarotCard;
		else m_FlippedCardA = tarotCard;
		if (m_FlippedCardA && m_FlippedCardB) CheckIfMatch();
	}

	void CheckIfMatch()
	{
		if ((int)m_FlippedCardA.Card + (int)m_FlippedCardB.Card == PAIR_VALUE)
		{
			m_FlippedCardA = null;
			m_FlippedCardB = null;
			++m_Pairs;
			if (m_Pairs == MAX_PAIRS) DoWin();
		}
		else CanFlipCard = false;
	}

	void DoWin()
	{
		CanFlipCard = false;
		m_bDontUpdate = true;
		MinigameManager.Instance.OnMinigameBeaten(Minigames.Tarot);
		m_MenuCharacter.OnMenuOpen(m_TarotCharacter, m_HUD, m_WinScreen);
	}

	void DoLose()
	{
		CanFlipCard = false;
		m_bDontUpdate = true;
		m_MenuCharacter.OnMenuOpen(m_TarotCharacter, m_HUD, m_LoseScreen);
	}
}
