using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SettingsSaveButton : MonoBehaviour
{
    void Awake() => GetComponent<Button>().onClick.AddListener(() => UserSettingsManager.Instance.SaveSettings());
}
