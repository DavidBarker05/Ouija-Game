using UnityEngine;

public enum TarotCards
{
	Fool,
	Magician,
	HighPriestess,
	Empress,
	Emporor,
	Hierophant,
	Lovers,
	Chariot,
	Strength,
	Hermit,
	WheelOfFortune,
	Justice,
	HangedMan,
	Death,
	Temperance,
	Devil,
	Tower,
	Star,
	Moon,
	Sun,
	Judgement,
	World
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(Collider))]
public class TarotCard : MonoBehaviour
{
	public TarotCards Card { get; private set; }

	MeshRenderer m_Renderer;
	bool m_bFlipped;

	void Awake() => m_Renderer = GetComponent<MeshRenderer>();

	public void SetCard(TarotCards card)
	{
		Card = card;
		m_Renderer.material = TarotManager.Instance.CardBack;
		m_bFlipped = false;
	}

	public void Flip()
	{
		if (m_bFlipped || !TarotManager.Instance.CanFlipCard) return;
		m_bFlipped = true;
		m_Renderer.material = TarotManager.Instance.CardFront(Card);
		TarotManager.Instance.FlipCard(this);
	}

	public void Unflip()
	{
		m_bFlipped = false;
		m_Renderer.material = TarotManager.Instance.CardBack;
	}
}
