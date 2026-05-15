using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class ResolutionDropdown : MonoBehaviour
{
    readonly string[] Resolutions = {
        "1280x720",
        "1920x1080",
        "2560x1440",
        "3840x2160"
    };

    readonly List<string> m_ResList = new List<string>();

    TMP_Dropdown m_Dropdown;

    void Awake()
    {
        m_Dropdown = GetComponent<TMP_Dropdown>();
        m_ResList.AddRange(Resolutions);
    }

    void OnEnable()
    {
        if (!UserSettingsManager.Instance) return;
        m_Dropdown.onValueChanged.RemoveAllListeners();
        m_Dropdown.ClearOptions();
        string resolution = $"{UserSettingsManager.Instance.Resolution.HorizontalResolution}x{UserSettingsManager.Instance.Resolution.VerticalResolution}";
        if (!m_ResList.Contains(resolution)) m_ResList.Insert(0, "CUSTOM");
        m_Dropdown.AddOptions(m_ResList);
        m_Dropdown.value = m_ResList[0] == "CUSTOM" ? 0 : m_ResList.IndexOf(resolution);
        m_Dropdown.onValueChanged.AddListener(ChangeResolution);
    }

    void ChangeResolution(int index)
    {
        if (!UserSettingsManager.Instance) return;
        string resolution = m_Dropdown.options[index].text;
        if (m_ResList.Contains("CUSTOM") && resolution != "CUSTOM")
        {
            m_Dropdown.onValueChanged.RemoveAllListeners();
            m_Dropdown.ClearOptions();
            m_ResList.RemoveAt(0);
            m_Dropdown.AddOptions(m_ResList);
            m_Dropdown.value = m_ResList.IndexOf(resolution);
            m_Dropdown.onValueChanged.AddListener(ChangeResolution);
        }
        UserSettingsManager.Instance.Resolution = resolution switch
        {
            "1280x720" => (1280, 720),
            "1920x1080" => (1920, 1080),
            "2560x1440" => (2560, 1440),
            "3840x2160" => (3840, 2160),
            _ => UserSettingsManager.Instance.Resolution
        };
    }
}
