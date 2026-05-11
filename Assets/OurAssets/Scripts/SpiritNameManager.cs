using UnityEngine;

public class SpiritNameManager : MonoBehaviour
{
	public const int NAME_LENGTH = 6;

	static SpiritNameManager s_Instance;
	public static SpiritNameManager Instance
	{
		get
		{
			if (!s_Instance)
			{
				GameObject go = new GameObject(nameof(SpiritNameManager));
				s_Instance = go.AddComponent<SpiritNameManager>();
				DontDestroyOnLoad(go);
			}
			return s_Instance;
		}
	}

	static readonly string[] SpiritNames = new string[10]
	{
		"MURWEN",
		"OSSIAN",
		"VETHIS",
		"NUMARA",
		"TAEVOX",
		"QUILEM",
		"AZMOTH",
		"SORVEL",
		"PELLUN",
		"IXAVEL"
	};

	public string SpiritName { get; private set; }

	void Awake()
	{
		if (s_Instance && s_Instance != this) Destroy(gameObject);
		else
		{
			s_Instance = this;
			DontDestroyOnLoad(gameObject);
		}
	}

	public void StartNewGame() => SpiritName = SpiritNames[Random.Range(0, SpiritNames.Length)];
}
