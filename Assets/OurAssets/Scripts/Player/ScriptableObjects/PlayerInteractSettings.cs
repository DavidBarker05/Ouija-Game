using UnityEngine;

[CreateAssetMenu(fileName = "PlayerInteractSettings", menuName = "Player/Interact Settings")]
public class PlayerInteractSettings : ScriptableObject
{
	[field: SerializeField]
	public float InteractionDistance { get; private set; } = 2f;
	[field: SerializeField]
	public LayerMask InteractableLayer { get; private set; }
}
