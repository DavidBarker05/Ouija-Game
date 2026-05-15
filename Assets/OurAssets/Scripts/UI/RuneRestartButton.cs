using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class RuneRestartButton : MonoBehaviour
{
    [SerializeField]
    RuneMatchManager m_RuneMatchManager;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            m_RuneMatchManager.RestartGame();
        });
    }
}
