using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;

public class InteractPrompt : MonoBehaviour
{
    [SerializeField]
    Camera m_PlayerCamera;
    [SerializeField]
    Player m_Player;
    [SerializeField]
    TMP_Text m_DisplayText;
    [SerializeField, TextArea]
    string m_FormatText;
    [SerializeField]
    string m_KeyboardSprite;
    [SerializeField]
    string m_XboxSprite;
    [SerializeField]
    string m_PlayStation4Sprite;
    [SerializeField]
    string m_PlayStation5Sprite;
    [SerializeField]
    string m_SwitchSprite;
    [SerializeField]
    string m_GenericControllerSprite;

    void Update()
    {
        transform.LookAt(2 * transform.position - m_PlayerCamera.transform.position);
        m_DisplayText.text = string.Format(m_FormatText, ButtonSprite);
    }

    string ButtonSprite => m_Player.CurrentDevice switch
    {
        Keyboard or Mouse => m_KeyboardSprite,
        XInputController => m_XboxSprite,
        DualShock4GamepadHID => m_PlayStation4Sprite,
        DualSenseGamepadHID => m_PlayStation5Sprite,
        SwitchProControllerHID => m_SwitchSprite,
        _ => m_GenericControllerSprite
    };
}
