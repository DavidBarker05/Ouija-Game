using System.Text;
using UnityEngine;

public class CryptexManager : MonoBehaviour
{
    public static CryptexManager Instance { get; private set; }

    [SerializeField]
    Player m_Player;
    [SerializeField]
    FirstPersonCharacter m_FirstPersonCharacter;
    [SerializeField]
    GameObject m_Door;
    [SerializeField]
    Vector3 m_DoorRotation = new Vector3(0f, -90f, 0f);
    [SerializeField]
    CryptexInteraction m_CryptexInteraction;

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
            m_Door.transform.Rotate(m_DoorRotation, Space.Self);
            m_CryptexInteraction.gameObject.SetActive(false);
            MinigameManager.Instance.OnMinigameBeaten(Minigames.Cryptex);
            m_Player.ChangeCharacter(m_FirstPersonCharacter);
        }
    }
}
