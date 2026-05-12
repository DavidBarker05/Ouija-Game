using UnityEngine;

public class Candle : MonoBehaviour
{
    [SerializeField]
    Transform m_CandleTransform;
    [SerializeField]
    GameObject m_LitCandle;
    [SerializeField]
    GameObject m_UnlitCandle;
    
    public bool IsLit { get; private set; }
    public bool CanBeRelit { get; set; }

	bool m_bInitted;
	SmallPentagram m_Owner;
	GameObject m_CandlePlaceholder = null;

    public bool Init(SmallPentagram owner)
    {
        if (m_bInitted) return false;
        m_Owner = owner;
        Ignite();
        return true;
    }

	public void Extinguish()
    {
        if (!IsLit) return;
        if (m_CandlePlaceholder) Destroy(m_CandlePlaceholder);
        m_CandlePlaceholder = Instantiate(m_UnlitCandle, m_CandleTransform ?? transform);
    }

    public void Ignite()
    {
        if (IsLit || !CanBeRelit) return;
		if (m_CandlePlaceholder) Destroy(m_CandlePlaceholder);
		m_CandlePlaceholder = Instantiate(m_UnlitCandle, m_CandleTransform ?? transform);
	}
}
