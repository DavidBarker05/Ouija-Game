using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class VSyncDropdown : MonoBehaviour
{
    TMP_Dropdown m_Dropdown;

    void Awake() => m_Dropdown = GetComponent<TMP_Dropdown>();

    void OnEnable()
    {
        m_Dropdown.onValueChanged.RemoveAllListeners();
        m_Dropdown.value = UserSettingsManager.Instance?.VSyncCount ?? QualitySettings.vSyncCount;
        m_Dropdown.onValueChanged.AddListener(ChangeVSync);
    }

    void ChangeVSync(int index)
    {
        if (UserSettingsManager.Instance) UserSettingsManager.Instance.VSyncCount = index;
    }
}
