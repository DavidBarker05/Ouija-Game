using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TarotRestartButton : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            TarotManager.Instance.RestartGame();
        });
    }
}
