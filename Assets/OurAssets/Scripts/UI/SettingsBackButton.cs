using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SettingsBackButton : MonoBehaviour
{
    void Awake() => GetComponent<Button>().onClick.AddListener(() => UserSettingsManager.Instance.ClearTempSettings());
}
