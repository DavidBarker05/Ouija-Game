using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class MasterVolumeInputField : MonoBehaviour
{
    [SerializeField]
    MasterVolumeSlider m_VolumeSlider;

    TMP_InputField m_InputField;
    public TMP_InputField InputField
    {
        get
        {
            m_InputField ??= GetComponent<TMP_InputField>();
            return m_InputField;
        }
    }

    public void ChangeVolume(string input)
    {
        if (!UserSettingsManager.Instance) return;
        InputField.onSubmit.RemoveAllListeners();
        m_VolumeSlider.Slider.onValueChanged.RemoveAllListeners();
        int volume = Mathf.Clamp(int.Parse(input), 0, 100);
        InputField.text = $"{volume}";
        m_VolumeSlider.Slider.value = Mathf.Clamp(volume, m_VolumeSlider.Slider.minValue, m_VolumeSlider.Slider.maxValue);
        float volume01 = Mathf.Clamp01(volume / 100f);
        UserSettingsManager.Instance.MasterVolume = volume01;
        InputField.onSubmit.AddListener(ChangeVolume);
        m_VolumeSlider.Slider.onValueChanged.AddListener(m_VolumeSlider.ChangeVolume);
    }
}
