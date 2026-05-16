using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HTPButton : MonoBehaviour
{
    [SerializeField]
    MenuCharacter m_MenuCharacter;
    [SerializeField]
    PlayerCharacter m_CurrentCharacter;
    [SerializeField]
    GameObject m_CurrentUI;
    [SerializeField]
    GameObject m_HTPPanel;

    void Awake() => GetComponent<Button>().onClick.AddListener(() => m_MenuCharacter.OnMenuOpen(m_CurrentCharacter, m_CurrentUI, m_HTPPanel));
}
