using UnityEngine;

public class Candle : Interactable
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
        IsLit = false;
        CanBeRelit = true;
        Ignite();
        return true;
    }

    public void Extinguish()
    {
        if (!IsLit) return;
        IsLit = false;
        if (m_CandlePlaceholder) Destroy(m_CandlePlaceholder);
        m_CandlePlaceholder = Instantiate(m_UnlitCandle, m_CandleTransform ?? transform);
    }

    public void Ignite()
    {
        if (IsLit || !CanBeRelit) return;
        IsLit = true;
        if (m_CandlePlaceholder) Destroy(m_CandlePlaceholder);
        m_CandlePlaceholder = Instantiate(m_LitCandle, m_CandleTransform ?? transform);
        m_Owner.ReigniteCandle(this);
    }

    public override object[] Interact(params object[] args)
    {
        if (args != null && args.Length != 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Candle expects 0 args! Received {args.Length} args");
#endif
        }
        else Ignite();
        return null;
    }
}
