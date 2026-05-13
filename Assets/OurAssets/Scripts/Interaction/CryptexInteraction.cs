using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CryptexInteraction : Interactable
{
    [SerializeField]
    Player m_Player;
    [SerializeField]
    OuijaCharacter m_OuijaCharacter;
    [SerializeField]
    GameObject m_HUD;
    [SerializeField]
    GameObject m_CryptexUI;
    [SerializeField]
    CryptexRingButton[] m_CryptexRingButtons;

    public override object[] Interact(params object[] args)
    {
        if (args != null && args.Length != 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"CryptexInteraction expects 0 args! Received {args.Length} args");
#endif
        }
        else
        {
            m_HUD?.SetActive(false);
            m_CryptexUI?.SetActive(true);
            foreach (CryptexRingButton b in m_CryptexRingButtons) b.gameObject.SetActive(true);
            m_Player.ChangeCharacter(m_OuijaCharacter);
        }
        return null;
    }
}
