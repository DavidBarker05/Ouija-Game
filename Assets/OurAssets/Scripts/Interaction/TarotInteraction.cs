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

    public override object[] Interact(params object[] args)
    {
        if (args != null && args.Length != 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"TarotInteraction expects 0 args. Received {args.Length} args");
#endif
        }
        else if (MinigameManager.Instance.CanPlayMinigame(Minigames.Tarot))
        {
            PlayerSceneDataManager.Instance?.SaveSceneData(m_FirstPersonCharacter);
            m_LoadingScreen.SceneIndexToLoad = m_TarotSceneIndex;
            m_LoadingScreen.gameObject.SetActive(true);
        }
        return null;
    }
}
