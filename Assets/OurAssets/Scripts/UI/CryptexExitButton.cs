using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CryptexExitButton : MonoBehaviour
{
    [SerializeField]
    Player m_Player;
    [SerializeField]
    FirstPersonCharacter m_FirstPersonCharacter;
    [SerializeField]
    GameObject m_HUD;
    [SerializeField]
    GameObject m_CryptexUI;
    [SerializeField]
    CryptexRingButton[] m_CryptexRingButtons;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            m_HUD?.SetActive(true);
            m_CryptexUI?.SetActive(false);
            foreach (CryptexRingButton b in m_CryptexRingButtons) b.gameObject.SetActive(false);
            m_Player.ChangeCharacter(m_FirstPersonCharacter);
        });
    }
}
