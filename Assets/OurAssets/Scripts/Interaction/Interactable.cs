using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public abstract object[] Interact(params object[] args);
}
