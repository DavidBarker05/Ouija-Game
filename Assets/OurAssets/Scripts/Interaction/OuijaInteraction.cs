using OurAssets.Scripts.Chat;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class OuijaInteraction : Interactable
{
    [SerializeField]
    OuijaPlayerInputController m_OuijaPlayerInputController;
    [SerializeField]
    Player m_Player;
    [SerializeField]
    OuijaCharacter m_OuijaCharacter;
    [SerializeField]
    GameObject m_Reticle;

    void OnValidate() => GetComponent<BoxCollider>().isTrigger = true; // Make sure player can't just walk into the collider

    void OnEnable() => GetComponent<BoxCollider>().isTrigger = true; // Make sure player can't just walk into the collider

    public override object[] Interact(params object[] args)
    {
        if (args != null && args.Length != 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"OuijaInteraction expects 0 args, received {args.Length}");
#endif
        }
        else
        {
            m_Reticle.SetActive(false);
            m_Player.ChangeCharacter(m_OuijaCharacter);
            m_OuijaPlayerInputController.gameObject.SetActive(true);
        }
        return null;
    }
}
