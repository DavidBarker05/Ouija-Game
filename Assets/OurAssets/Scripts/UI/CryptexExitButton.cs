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
    GameObject[] m_CryptexPhysicalUI;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            m_CryptexUI?.SetActive(false);
            m_HUD?.SetActive(true);
            foreach (GameObject go in m_CryptexPhysicalUI) go.SetActive(false);
            m_Player.ChangeCharacter(m_FirstPersonCharacter);
        });
    }
}
