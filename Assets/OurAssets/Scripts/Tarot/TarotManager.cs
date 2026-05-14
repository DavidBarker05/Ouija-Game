using System.Collections.Generic;
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
	TarotCard m_TarotCardPrefab;
	[SerializeField]
	Transform[] m_CardPositions;
	[field: SerializeField]
	public Material CardBack { get; private set; }
	[SerializeField]
	CardFront[] m_CardFrontMaterials;

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

	TarotCard flippedCardA = null;
	TarotCard flippedCardB = null;

	int m_Pairs = 0;

	float m_CurrentTime;

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
		m_CurrentTime -= Time.deltaTime;
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
	}

	public void RestartGame()
	{
		ClearValues();
		RecreateCards();
		ShuffleCards();
		CanFlipCard = true;
	}

	void ClearValues()
	{
		flippedCardA = null;
		flippedCardB = null;
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
		if (flippedCardA) flippedCardB = tarotCard;
		else flippedCardA = tarotCard;
		if (flippedCardA && flippedCardB) CheckIfMatch();
	}

	void CheckIfMatch()
	{
		if ((int)flippedCardA.Card + (int)flippedCardB.Card == PAIR_VALUE)
		{
			++m_Pairs;
			if (m_Pairs == MAX_PAIRS) DoWin();
		}
		else
		{
			flippedCardA.Unflip();
			flippedCardB.Unflip();
			flippedCardA = null;
			flippedCardB = null;
		}
	}

	void DoWin()
	{
		CanFlipCard = false;
		MinigameManager.Instance.OnMinigameBeaten(Minigames.Tarot);
	}

	void DoLose()
	{
		RestartGame();
	}
}
