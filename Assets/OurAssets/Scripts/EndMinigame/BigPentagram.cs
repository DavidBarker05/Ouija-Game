using System.Collections.Generic;
using UnityEngine;

public class BigPentagram : MonoBehaviour
{
	[field: SerializeField]
	public SmallPentagram SmallPentagram0 { get; private set; }
	[field: SerializeField]
	public SmallPentagram SmallPentagram1 { get; private set; }
	[field: SerializeField]
	public SmallPentagram SmallPentagram2 { get; private set; }
	[field: SerializeField]
	public SmallPentagram SmallPentagram3 { get; private set; }
	[field: SerializeField]
	public SmallPentagram SmallPentagram4 { get; private set; }

	bool m_bInitted;
	HashSet<SmallPentagram> m_LitPentagrams = new HashSet<SmallPentagram>();

	public bool Init()
	{
		if (m_bInitted) return false;
		m_bInitted = true;
		bool bInitted = true;
		bInitted |= InitSmallPentagram(SmallPentagram0);
		bInitted |= InitSmallPentagram(SmallPentagram1);
		bInitted |= InitSmallPentagram(SmallPentagram2);
		bInitted |= InitSmallPentagram(SmallPentagram3);
		bInitted |= InitSmallPentagram(SmallPentagram4);
		return bInitted;
	}

	bool InitSmallPentagram(SmallPentagram smallPentagram)
	{
		bool bInitted = smallPentagram?.Init() ?? false;
		if (bInitted) m_LitPentagrams.Add(smallPentagram);
		return bInitted;
	}

	public bool ExtinguishCandles(int numCandlesToExtinguish)
	{
		List<SmallPentagram> _litPentagrams = new List<SmallPentagram>(m_LitPentagrams);
		while (numCandlesToExtinguish > 0 && m_LitPentagrams.Count > 0)
		{
			int index = Random.Range(0, _litPentagrams.Count);
			SmallPentagram _smallPentagram = _litPentagrams[index];
			int _randomNumToExtinguish = Mathf.Min(Random.Range(1, numCandlesToExtinguish + 1), 3); // Usually never blow out more than 3 candles per small pentagram unless it gets selected again
			numCandlesToExtinguish -= _randomNumToExtinguish;
			if (_smallPentagram.ExtinguishRandomCandle(_randomNumToExtinguish, out int leftover))
			{
				_litPentagrams.RemoveAt(index);
				m_LitPentagrams.Remove(_smallPentagram);
			}
			numCandlesToExtinguish += leftover;
		}
		return m_LitPentagrams.Count == 0;
	}
}
