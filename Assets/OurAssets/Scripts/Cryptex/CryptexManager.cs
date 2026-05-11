using System.Text;
using UnityEngine;

public class CryptexManager : MonoBehaviour
{
    public static CryptexManager Instance { get; private set; }

    [SerializeField]
    Player m_Player;
    [SerializeField]
    FirstPersonCharacter m_FirstPersonCharacter;

	void Awake()
	{
        if (Instance && Instance != this) Destroy(gameObject);
        else Instance = this;
	}

	public void CheckNameMatches(CrypexRing[] cryptexRings)
    {
        if (cryptexRings == null || string.IsNullOrWhiteSpace(SpiritNameManager.Instance.SpiritName)) return;
        if (cryptexRings.Length != SpiritNameManager.NAME_LENGTH)
        {
            Debug.LogError($"Number of cryptex rings ({cryptexRings.Length}) doesn't match name length ({SpiritNameManager.NAME_LENGTH})");
            return;
        }
        StringBuilder name = new StringBuilder();
        for (int i = 0; i < SpiritNameManager.NAME_LENGTH; ++i) name.Append(char.ToUpper(cryptexRings[i].Letter));
        if (name.ToString().Equals(SpiritNameManager.Instance.SpiritName, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("You Win");
            MinigameManager.Instance.OnMinigameBeaten(Minigames.Cryptex);
            m_Player.ChangeCharacter(m_FirstPersonCharacter);
        }
    }
}
