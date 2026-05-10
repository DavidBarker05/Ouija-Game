using UnityEngine;

[CreateAssetMenu(fileName = "PlayerCameraSettings", menuName = "Player/Camera Settings")]
public class PlayerCameraSettings : ScriptableObject
{
	[field: SerializeField, Min(0f)]
	public float MouseHorizontalSensitivity { get; private set; } = 0.2f;
	[field: SerializeField, Min(0f)]
	public float MouseVerticalSensitivity { get; private set; } = 0.225f;
	[field: SerializeField, Min(0f)]
	public float ControllerHorizontalSensitivity { get; private set; } = 180f;
	[field: SerializeField, Min(0f)]
	public float ControllerVerticalSensitivity { get; private set; } = 202.5f;
	[field: SerializeField, Range(-90f, 0f)]
	public float MinVerticalAngle { get; private set; } = -80f;
	[field: SerializeField, Range(0f, 90f)]
	public float MaxVerticalAngle { get; private set; } = 80f;
}
