using System.Collections.Generic;
using UnityEngine;

public class SmallPentagram : MonoBehaviour
{
	[field: SerializeField]
	public Candle Candle0 { get; private set; }
	[field: SerializeField]
	public Candle Candle1 { get; private set; }
	[field: SerializeField]
	public Candle Candle2 { get; private set; }
	[field: SerializeField]
	public Candle Candle3 { get; private set; }
	[field: SerializeField]
	public Candle Candle4 { get; private set; }

	bool m_bInitted;
	HashSet<Candle> m_LitCandles = new HashSet<Candle>();

	public bool Init()
	{
		if (m_bInitted) return false;
		m_bInitted = true;
		m_bInitted |= InitCandle(Candle0);
		m_bInitted |= InitCandle(Candle1);
		m_bInitted |= InitCandle(Candle2);
		m_bInitted |= InitCandle(Candle3);
		m_bInitted |= InitCandle(Candle4);
		return true;
	}

	bool InitCandle(Candle candle)
	{
		bool bInitted = candle?.Init(this) ?? false;
		if (bInitted) m_LitCandles.Add(candle);
		return bInitted;
	}

	public bool ExtinguishRandomCandle(int numToExtinguish, out int leftoverFromNumToExtinguish)
	{
		int count = Mathf.Min(m_LitCandles.Count, numToExtinguish);
		List<Candle> _litCandles = new List<Candle>(m_LitCandles);
		for (int i = 0; i < count; ++i)
		{
			int index = Random.Range(0, _litCandles.Count);
			Candle _candle = _litCandles[index];
			_candle.Extinguish();
			_litCandles.RemoveAt(index);
			m_LitCandles.Remove(_candle);
		}
		leftoverFromNumToExtinguish = numToExtinguish - count;
		bool bOutOfCandles = m_LitCandles.Count == 0;
		if (bOutOfCandles)
		{
			if (Candle0) Candle0.CanBeRelit = false;
			if (Candle1) Candle1.CanBeRelit = false;
			if (Candle2) Candle2.CanBeRelit = false;
			if (Candle3) Candle3.CanBeRelit = false;
			if (Candle4) Candle4.CanBeRelit = false;
		}
		return bOutOfCandles;
	}

	public void ReigniteCandle(Candle candle) => m_LitCandles.Add(candle);
}
