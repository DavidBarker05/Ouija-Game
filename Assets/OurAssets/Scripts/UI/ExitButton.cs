using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ExitButton : MonoBehaviour
{
    void Awake() => GetComponent<Button>().onClick.AddListener(() => OwnUtils.Exit());
}
