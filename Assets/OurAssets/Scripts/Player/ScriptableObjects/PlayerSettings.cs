using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Player/Settings")]
public class PlayerSettings : ScriptableObject
{
	[field: SerializeField]
	public PlayerCharacterSettings CharacterSettings { get; private set; }
	[field: SerializeField]
	public PlayerCameraSettings CameraSettings { get; private set; }
	[field: SerializeField]
	public PlayerInteractSettings InteractSettings { get; private set; }
}
