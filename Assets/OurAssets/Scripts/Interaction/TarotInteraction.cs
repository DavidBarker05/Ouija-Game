using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TarotInteraction : Interactable
{
    [SerializeField]
    FirstPersonCharacter m_FirstPersonCharacter;
    [SerializeField]
    LoadingScreen m_LoadingScreen;
    [SerializeField, Min(0)]
    int m_TarotSceneIndex = 3;

    void Awake()
    {
        if (MinigameManager.Instance.IsMinigameBeaten(Minigame.Tarot)) CanInteractWith = false;
        else StartCoroutine(ChechCanInteract());
    }

    public override object[] Interact(params object[] args)
    {
        if (args != null && args.Length != 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"TarotInteraction expects 0 args. Received {args.Length} args");
#endif
        }
        else if (MinigameManager.Instance.CanPlayMinigame(Minigame.Tarot))
        {
            PlayerSceneDataManager.Instance?.SaveSceneData(m_FirstPersonCharacter);
            m_LoadingScreen.SceneIndexToLoad = m_TarotSceneIndex;
            m_LoadingScreen.gameObject.SetActive(true);
        }
        return null;
    }

    IEnumerator ChechCanInteract()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.2f);
            CanInteractWith = MinigameManager.Instance.CanPlayMinigame(Minigame.Tarot);
            if (CanInteractWith) break;
        }
    }
}
