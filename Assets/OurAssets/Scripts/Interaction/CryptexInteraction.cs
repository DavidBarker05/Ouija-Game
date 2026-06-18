using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CryptexInteraction : Interactable
{
    [SerializeField]
    Player m_Player;
    [SerializeField]
    CryptexCharacter m_CryptexCharacter;
    [SerializeField]
    GameObject m_HUD;
    [SerializeField]
    GameObject m_CryptexUI;
    [SerializeField]
    GameObject[] m_CryptexPhysicalUI;

    void Awake() => CanInteractWith = true;

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
            foreach (GameObject go in m_CryptexPhysicalUI) go.SetActive(true);
            m_Player.ChangeCharacter(m_CryptexCharacter);
            if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.HowOpenDoor) != ExtraQuestionStatus.Asked)
                StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.HowOpenDoor, ExtraQuestionStatus.ShouldAsk);
        }
        return null;
    }
}
