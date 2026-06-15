using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EndInteraction : Interactable
{
    [SerializeField]
    LoadingScreen m_LoadingScreen;
    [SerializeField, Min(0)]
    int m_EndSceneIndex = 5;

    void Awake() => StartCoroutine(ChechCanInteract());

    public override object[] Interact(params object[] args)
    {
        if (args != null && args.Length != 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"EndInteraction expects 0 args. Received {args.Length} args");
#endif
        }
        else if (GameManager.Instance.CanMoveToFinalLevel)
        {
            m_LoadingScreen.SceneIndexToLoad = m_EndSceneIndex;
            m_LoadingScreen.gameObject.SetActive(true);
        }
        return null;
    }

    IEnumerator ChechCanInteract()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.2f);
            CanInteractWith = GameManager.Instance.CanMoveToFinalLevel;
            if (CanInteractWith) break;
        }
    }
}
