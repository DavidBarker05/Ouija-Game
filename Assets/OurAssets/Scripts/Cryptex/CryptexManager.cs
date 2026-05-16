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
    [SerializeField]
    GameObject m_HUD;
    [SerializeField]
    GameObject m_CryptexUI;
    [SerializeField]
    Cryptex m_Cryptex;

    Quaternion m_DoorInitialLocalRotation;
    bool m_HasCachedDoorInitialRotation;

    void Awake()
    {
        if (Instance && Instance != this) Destroy(gameObject);
        else Instance = this;

        CacheDoorInitialLocalRotationIfNeeded();
    }

    void Start()
    {
        // Cursor - House scene reload resets transforms; reopen door / hide puzzle if Cryptex was already beaten this session (MinigameManager survives via DontDestroyOnLoad).
        RestoreCryptexBeatWorldStateIfNeeded();
    }

    void CacheDoorInitialLocalRotationIfNeeded()
    {
        if (m_Door == null || m_HasCachedDoorInitialRotation) return;
        m_DoorInitialLocalRotation = m_Door.transform.localRotation;
        m_HasCachedDoorInitialRotation = true;
    }

    // Cursor - Matches Transform.Rotate(..., Space.Self): one local-axis open pose from the scene closed rotation (no double-rotate on reload).
    void ApplyCryptexDoorOpenRotation()
    {
        if (m_Door == null) return;
        CacheDoorInitialLocalRotationIfNeeded();
        if (!m_HasCachedDoorInitialRotation) return;
        m_Door.transform.localRotation = m_DoorInitialLocalRotation * Quaternion.Euler(m_DoorRotation);
    }

    // Cursor - Same visuals as solving Cryptex without calling OnMinigameBeaten again (used after reloading the house scene).
    void RestoreCryptexBeatWorldStateIfNeeded()
    {
        if (MinigameManager.Instance == null || !MinigameManager.Instance.IsMinigameBeaten(Minigames.Cryptex)) return;

        ApplyCryptexDoorOpenRotation();

        if (m_CryptexInteraction != null)
            m_CryptexInteraction.gameObject.SetActive(false);

        if (m_Cryptex != null)
            Destroy(m_Cryptex.gameObject);
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
            ApplyCryptexDoorOpenRotation();
            m_CryptexInteraction.gameObject.SetActive(false);
            MinigameManager.Instance.OnMinigameBeaten(Minigames.Cryptex);
            m_Player.ChangeCharacter(m_FirstPersonCharacter);
            m_CryptexUI.SetActive(false);
            m_HUD.SetActive(true);
            Destroy(m_Cryptex.gameObject);
        }
    }
}
