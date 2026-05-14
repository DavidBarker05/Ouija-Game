using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [field: SerializeField, Min(0)]
    public int SceneIndexToLoad { get; set; } = 0;
    [SerializeField]
    Slider m_LoadingBar;

    void OnEnable() => StartCoroutine(LoadAsync());

    IEnumerator LoadAsync()
    {
        AsyncOperation loadScene = SceneManager.LoadSceneAsync(SceneIndexToLoad);
        while (!loadScene.isDone)
        {
            float loadProgress = Mathf.Clamp01(loadScene.progress);
            m_LoadingBar.value = loadProgress;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
