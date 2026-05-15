using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class ControllerVSensInputField : MonoBehaviour
{
    [SerializeField]
    ControllerVSensSlider m_SensSlider;

    TMP_InputField m_InputField;
    public TMP_InputField InputField
    {
        get
        {
            m_InputField ??= GetComponent<TMP_InputField>();
            return m_InputField;
        }
    }

    public void ChangeSens(string input)
    {
        if (!UserSettingsManager.Instance) return;
        InputField.onSubmit.RemoveAllListeners();
        m_SensSlider.Slider.onValueChanged.RemoveAllListeners();
        float sens = Mathf.Round(Mathf.Clamp(float.Parse(input), UserSettingsManager.MIN_SENS_MULT_UI, UserSettingsManager.MAX_SENS_MULT_UI) * 10f) / 10f;
        InputField.text = sens.ToString("F1");
        m_SensSlider.Slider.value = Mathf.RoundToInt((sens - UserSettingsManager.MIN_SENS_MULT_UI) / (UserSettingsManager.MAX_SENS_MULT_UI - UserSettingsManager.MIN_SENS_MULT_UI) * (m_SensSlider.Slider.maxValue - m_SensSlider.Slider.minValue));
        UserSettingsManager.Instance.ControllerVerticalSensitivityMultiplier = sens;
        InputField.onSubmit.AddListener(ChangeSens);
        m_SensSlider.Slider.onValueChanged.AddListener(m_SensSlider.ChangeSens);
    }
}
