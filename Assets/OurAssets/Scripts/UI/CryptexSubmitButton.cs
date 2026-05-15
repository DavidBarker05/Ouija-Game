using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CryptexSubmitButton : MonoBehaviour
{
    [SerializeField]
    Cryptex m_Cryptex;

    void Awake() => GetComponent<Button>().onClick.AddListener(() => m_Cryptex.TestName());
}
