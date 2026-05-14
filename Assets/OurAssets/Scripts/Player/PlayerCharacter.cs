using UnityEngine;

public struct MouseInfo
{
	public Vector3 MouseScreenPosition { get; set; }
	public bool IsOverUI { get; set; }
	public bool DidHitObject { get; set; }
	public RaycastHit HitInfo { get; set; }
}

public interface IPlayerCharacterInitData { }

public interface IPlayerCharacterUpdateData
{
	public float DeltaTime { get; set; }
	public Quaternion CameraRotation { get; set; }
	public MouseInfo MouseInfo { get; set; }
}

public abstract class PlayerCharacter : MonoBehaviour
{
	[field: SerializeField]
	public Transform CameraTarget { get; protected set; }
	[field: SerializeField]
	public LayerMask MouseHitLayer { get; protected set; }
	[field: SerializeField]
	public float MouseHitDistance { get; protected set; } = 100f;

	public abstract bool HasBeenInitialised { get; protected set; }

	public abstract string ActionMap { get; }
	public abstract bool MouseVisible { get; }
	public abstract bool DoCameraRotation { get; }
	public abstract bool UseMouseScreenPosition { get; }

	public abstract void Init(IPlayerCharacterInitData playerCharacterInitData);
	public abstract void LoadSceneData(PlayerSceneData playerSceneData);
	public abstract void UpdateCharacter(ref IPlayerCharacterUpdateData playerCharacterUpdateData);

	public abstract void OnPausePressed();
}
