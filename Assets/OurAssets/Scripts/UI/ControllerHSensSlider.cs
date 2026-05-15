using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class ControllerHSensSlider : MonoBehaviour
{
    [SerializeField]
    ControllerHSensInputField m_SensInput;

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
        m_SensInput.InputField.onSubmit.RemoveAllListeners();
        float sens = UserSettingsManager.Instance?.ControllerHorizontalSensitivityMultiplier ?? 1f;
        Slider.value = Mathf.RoundToInt((sens - UserSettingsManager.MIN_SENS_MULT_UI) / (UserSettingsManager.MAX_SENS_MULT_UI - UserSettingsManager.MIN_SENS_MULT_UI) * (Slider.maxValue - Slider.minValue));
        m_SensInput.InputField.text = sens.ToString("F1");
        Slider.onValueChanged.AddListener(ChangeSens);
        m_SensInput.InputField.onSubmit.AddListener(m_SensInput.ChangeSens);
    }

    public void ChangeSens(float value)
    {
        if (!UserSettingsManager.Instance) return;
        m_SensInput.InputField.onSubmit.RemoveAllListeners();
        float sens = UserSettingsManager.MIN_SENS_MULT_UI + value / (Slider.maxValue - Slider.minValue) * (UserSettingsManager.MAX_SENS_MULT_UI - UserSettingsManager.MIN_SENS_MULT_UI);
        m_SensInput.InputField.text = sens.ToString("F1");
        UserSettingsManager.Instance.ControllerHorizontalSensitivityMultiplier = sens;
        m_SensInput.InputField.onSubmit.AddListener(m_SensInput.ChangeSens);
    }
}
