using UnityEngine;

public class FirstPersonCharacterInitData : IPlayerCharacterInitData
{
	public PlayerCharacterSettings CharacterSettings { get; set; }
	public PlayerInteractSettings InteractSettings { get; set; }
}

public class FirstPersonCharacterUpdateData : IPlayerCharacterUpdateData
{
	public float DeltaTime { get; set; }
	public Quaternion CameraRotation { get; set; }
	public MouseInfo MouseInfo { get; set; }

	public Vector2 MovementInput { get; set; }
	public bool PressedInteract { get; set; }
}

[RequireComponent(typeof(CharacterController))]
public class FirstPersonCharacter : PlayerCharacter
{
	public override bool HasBeenInitialised { get; protected set; }

	public override bool MouseVisible => false;
	public override bool DoCameraRotation => true;
	public override bool UseMouseScreenPosition => false;

	static readonly float s_Epsilon = 0.05f;
	static readonly float s_SqrEpsilon = s_Epsilon * s_Epsilon;

	PlayerCharacterSettings m_CharacterSettings;
	PlayerInteractSettings m_InteractSettings;
	CharacterController m_CharacterController;

	Vector3 m_Velocity;

	public override void Init(IPlayerCharacterInitData playerCharacterInitData)
	{
		if (playerCharacterInitData is not FirstPersonCharacterInitData initData)
		{
			Debug.LogError($"playerCharacterInitData needs to be type FirstPersonCharacterInitData! Received {playerCharacterInitData.GetType()}");
			return;
		}
		m_CharacterSettings = initData.CharacterSettings;
		m_InteractSettings = initData.InteractSettings;
		m_CharacterController = GetComponent<CharacterController>();
		m_Velocity = Vector3.zero;
		HasBeenInitialised = true;
	}

	public override void LoadSceneData(PlayerSceneData playerSceneData)
	{
		if (!HasBeenInitialised)
		{
			Debug.LogError("FirstPersonCharacter hasn't been initialised!");
			return;
		}
		if (playerSceneData == null) return;
		transform.position = playerSceneData.Position;
		transform.eulerAngles = playerSceneData.EulerAngles;
	}

	public override void UpdateCharacter(ref IPlayerCharacterUpdateData playerCharacterUpdateData)
	{
		if (!HasBeenInitialised)
		{
			Debug.LogError("FirstPersonCharacter hasn't been initialised!");
			return;
		}
		if (playerCharacterUpdateData is not FirstPersonCharacterUpdateData updateData)
		{
			Debug.LogError($"playerCharacterUpdateData needs to be type FirstPersonCharacterUpdateData! Received {playerCharacterUpdateData.GetType()}");
			return;
		}
		HandleMovement(ref updateData);
		HandleInteraction(ref updateData);
	}

	#region Movement
	void HandleMovement(ref FirstPersonCharacterUpdateData updateData)
	{
		UpdateRotation(updateData.CameraRotation);
		UpdateHorizontalVelocity(updateData.MovementInput);
		m_Velocity.y = -1f;
		m_CharacterController.Move(m_Velocity * updateData.DeltaTime);
	}

	void UpdateRotation(Quaternion rotation)
	{
		Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up).normalized;
		transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
	}

	void UpdateHorizontalVelocity(Vector2 movementInput)
	{
		float xIn = movementInput.x;
		float zIn = movementInput.y;
		Vector3 hIn = Vector3.ClampMagnitude(xIn * transform.right + zIn * transform.forward, 1f);
		hIn = hIn.sqrMagnitude > s_SqrEpsilon ? hIn : Vector3.zero;
		m_Velocity.x = hIn.x * m_CharacterSettings.MovementSpeed;
		m_Velocity.z = hIn.z * m_CharacterSettings.MovementSpeed;
	}
	#endregion Movement

	#region Interaction
	void HandleInteraction(ref FirstPersonCharacterUpdateData updateData)
	{
		if (updateData.PressedInteract)
		{
			Vector3 direction = updateData.CameraRotation * Vector3.forward; // Rotate forward vector by camera rotation to get camera's forward vector
			DoInteraction(direction);
		}
		updateData.PressedInteract = false;
	}

	void DoInteraction(Vector3 direction)
	{
		if (Physics.Raycast(
			origin: CameraTarget.position,
			direction: direction,
			hitInfo: out RaycastHit hitInfo,
			maxDistance: m_InteractSettings.InteractionDistance,
			layerMask: m_InteractSettings.InteractableLayer,
			queryTriggerInteraction: QueryTriggerInteraction.Collide))
		{
			Interactable interactable = hitInfo.collider.gameObject.GetComponent<Interactable>();
			if (interactable == null) return;
			interactable.Interact();
		}
	}
	#endregion Interaction
}
