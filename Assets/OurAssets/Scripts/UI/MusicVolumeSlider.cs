using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class MusicVolumeSlider : MonoBehaviour
{
    [SerializeField]
    MusicVolumeInputField m_VolumeInput;

    Slider m_Slider;
    public Slider Slider
    {
        get
        {
            m_Slider ??= GetComponent<Slider>();
            return m_Slider;
        }
    }

    void OnEnable()
    {
        Slider.onValueChanged.RemoveAllListeners();
        m_VolumeInput.InputField.onSubmit.RemoveAllListeners();
        float volume = UserSettingsManager.Instance?.MusicAmbientVolume ?? 1f;
        int volume0100 = Mathf.RoundToInt(volume * 100f);
        Slider.value = Mathf.Clamp(volume0100, Slider.minValue, Slider.maxValue);
        m_VolumeInput.InputField.text = $"{volume0100}";
        Slider.onValueChanged.AddListener(ChangeVolume);
        m_VolumeInput.InputField.onSubmit.AddListener(m_VolumeInput.ChangeVolume);
    }

    public void ChangeVolume(float value)
    {
        if (!UserSettingsManager.Instance) return;
        m_VolumeInput.InputField.onSubmit.RemoveAllListeners();
        float volume01 = value / 100f;
        UserSettingsManager.Instance.MusicAmbientVolume = volume01;
        int volume0100 = Mathf.RoundToInt(value);
        m_VolumeInput.InputField.text = $"{volume0100}";
        m_VolumeInput.InputField.onSubmit.AddListener(m_VolumeInput.ChangeVolume);
    }
}
