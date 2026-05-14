using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SceneChangeButton : MonoBehaviour
{
    [SerializeField]
    LoadingScreen m_LoadingScreen;
    [SerializeField, Min(0)]
    int m_SceneIndexToLoad = 0;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            m_LoadingScreen.SceneIndexToLoad = m_SceneIndexToLoad;
            m_LoadingScreen.gameObject.SetActive(true);
        });
    }
}
