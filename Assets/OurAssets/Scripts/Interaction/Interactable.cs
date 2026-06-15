using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [field: SerializeField]
    public Transform InteractPromptTransform { get; private set; }

    public bool CanInteractWith { get; protected set; }

    public abstract object[] Interact(params object[] args);
}
