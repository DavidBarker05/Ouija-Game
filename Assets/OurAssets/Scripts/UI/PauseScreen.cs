using UnityEngine;

public class PauseScreen : MonoBehaviour
{
    [SerializeField]
    GameObject m_PausePanel;
    [SerializeField]
    GameObject m_ControlsPanel;
    [SerializeField]
    GameObject m_SettingsPanel;

    public void Open() => gameObject.SetActive(true);

    public void Close()
    {
        m_PausePanel.gameObject.SetActive(true);
        m_ControlsPanel.gameObject.SetActive(false);
        m_SettingsPanel.gameObject.SetActive(false);
        UserSettingsManager.Instance.ClearTempSettings();
        gameObject.SetActive(false);
    }
}
