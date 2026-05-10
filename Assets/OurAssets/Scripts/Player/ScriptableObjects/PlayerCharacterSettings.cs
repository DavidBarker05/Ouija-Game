using UnityEngine;

[CreateAssetMenu(fileName = "PlayerCharacterSettings", menuName = "Player/Character Settings")]
public class PlayerCharacterSettings : ScriptableObject
{
	[field: SerializeField, Min(1f)]
	public float MovementSpeed { get; private set; } = 4.5f;
}
