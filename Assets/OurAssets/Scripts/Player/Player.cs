using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class Player : MonoBehaviour
{
    [SerializeField]
    PlayerSettings m_PlayerSettings;
    [SerializeField]
    FirstPersonCharacter m_FirstPersonCharacter;
    [SerializeField]
    PlayerCamera m_PlayerCamera;
	[SerializeField]
	Camera m_Camera;

	PlayerInput m_PlayerInput;

	PlayerCharacter m_CurrentPlayerCharacter;
	IPlayerCharacterUpdateData m_CurrentPlayerCharacterUpdateData;
	CameraInput m_CameraInput;
	MouseInfo m_MouseInfo;

	bool m_bCursorHidden;

	void Awake()
	{
		m_PlayerInput = GetComponent<PlayerInput>();
		ChangeCharacter(m_PlayerInput.currentActionMap.name);
		m_PlayerCamera.Init(m_PlayerSettings.CameraSettings, m_CurrentPlayerCharacter.CameraTarget);
		m_CameraInput = new CameraInput();
		m_MouseInfo = new MouseInfo();
	}

	void Update()
	{
		if (!m_CurrentPlayerCharacter || !m_PlayerCamera) return;
		SetCursorVisibility(m_CurrentPlayerCharacter.MouseVisible);
		m_CurrentPlayerCharacterUpdateData.DeltaTime = Time.deltaTime;
		if (m_CurrentPlayerCharacter.DoCameraRotation)
		{
			m_PlayerCamera.UpdateRotation(ref m_CameraInput, Time.deltaTime);
			m_CurrentPlayerCharacterUpdateData.CameraRotation = m_PlayerCamera.transform.rotation;
		}
		if (m_CurrentPlayerCharacter.UseMouseScreenPosition)
		{
			m_MouseInfo.MouseScreenPosition = GetMousePositionOnScreen();
			GetMouseInfo(ref m_MouseInfo, m_CurrentPlayerCharacter.MouseHitLayer, m_CurrentPlayerCharacter.MouseHitDistance);
			m_CurrentPlayerCharacterUpdateData.MouseInfo = m_MouseInfo;
		}
		m_CurrentPlayerCharacter.UpdateCharacter(ref m_CurrentPlayerCharacterUpdateData);
	}

	void LateUpdate() => m_PlayerCamera.UpdatePosition(m_CurrentPlayerCharacter.CameraTarget);

	#region Change Action Map
	public void ChangeActionMap(string actionMap)
	{
		if (m_PlayerInput.currentActionMap.name == actionMap) return;
		m_PlayerInput.SwitchCurrentActionMap(actionMap);
		ChangeCharacter(actionMap);
	}

	#region Change Character
	PlayerCharacter ChangePlayerCharacter(string actionMap) => actionMap switch
	{
		"Player" => m_FirstPersonCharacter,
		_ => null
	};

	IPlayerCharacterInitData CurrentPlayerCharacterInitData => m_CurrentPlayerCharacter switch
	{
		FirstPersonCharacter => new FirstPersonCharacterInitData()
		{
			CharacterSettings = m_PlayerSettings.CharacterSettings,
			InteractSettings = m_PlayerSettings.InteractSettings
		},
		_ => null
	};

	IPlayerCharacterUpdateData CurrentPlayerCharacterUpdateData => m_CurrentPlayerCharacter switch
	{
		FirstPersonCharacter => new FirstPersonCharacterUpdateData(),
		_ => null
	};

	void ChangeCharacter(string actionMap)
	{
		m_CurrentPlayerCharacter = ChangePlayerCharacter(actionMap);
		if (!m_CurrentPlayerCharacter.HasBeenInitialised) m_CurrentPlayerCharacter.Init(CurrentPlayerCharacterInitData);
		m_CurrentPlayerCharacterUpdateData = CurrentPlayerCharacterUpdateData;
		m_PlayerCamera.ChangeCameraTarget(m_CurrentPlayerCharacter.CameraTarget);
		SetCursorVisibility(m_CurrentPlayerCharacter.MouseVisible);
	}
	#endregion Change Character
	#endregion Change Action Map

	#region Cursor Toggles
	public void ShowCursor()
	{
		m_bCursorHidden = false;
		Cursor.lockState = CursorLockMode.Confined;
		Cursor.visible = true;
	}

	public void HideCursor()
	{
		m_bCursorHidden = true;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	public void SetCursorVisibility(bool bVisible)
	{
		if (bVisible && m_bCursorHidden) ShowCursor();
		else if (!bVisible && !m_bCursorHidden) HideCursor();
	}
	#endregion Cursor Toggles

	#region Mouse Info
	public Vector3 GetMousePositionOnScreen()
	{
		Vector3 pos = Mouse.current.position.value;
		pos.z = m_Camera.nearClipPlane;
		return pos;
	}

	public void GetMouseInfo(ref MouseInfo mouseInfo, LayerMask layerToHit, float maxDistance = 100f)
	{
		Ray ray = m_Camera.ScreenPointToRay(mouseInfo.MouseScreenPosition);
		mouseInfo.DidHitObject = Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerToHit);
		if (mouseInfo.DidHitObject) mouseInfo.HitInfo = hit;
	}
	#endregion Mouse Info

	#region Handle PlayerInput Events

	delegate void SetDataValueFunc<T>(T _) where T : class, IPlayerCharacterUpdateData;

	void SetDataValue<T>(SetDataValueFunc<T> dataChangeFunction) where T : class, IPlayerCharacterUpdateData
	{
		if (m_CurrentPlayerCharacterUpdateData is T t) dataChangeFunction(t);
	}

	public void HandleMoveInput(InputAction.CallbackContext ctx)
	{
		SetDataValue<FirstPersonCharacterUpdateData>(updateData => updateData.MovementInput = ctx.ReadValue<Vector2>());
	}

	public void HandleLookInput(InputAction.CallbackContext ctx)
	{
		if (!m_CurrentPlayerCharacter.DoCameraRotation) return;
		m_CameraInput.LookInput = ctx.ReadValue<Vector2>();
		m_CameraInput.LookDevice = ctx.control.device;
	}

	public void HandleInteractInput(InputAction.CallbackContext ctx)
	{
		SetDataValue<FirstPersonCharacterUpdateData>(updateData => updateData.PressedInteract |= ctx.started);
	}

	//public void HandleLeftClickInput(InputAction.CallbackContext ctx)
	//{
	//	//SetDataValue<PipePlayerCharacterUpdateData>(input => input.ClickedThisFrame |= ctx.started);
	//	//SetDataValue<WirePlayerCharacterUpdateData>(input => input.ClickedThisFrame = ctx.action.WasPressedThisFrame());
	//	//SetDataValue<WallKnockPlayerCharacterUpdateData>(input => input.LeftClickedThisFrame |= ctx.started);
	//}
	//
	//public void HandleRightClickInput(InputAction.CallbackContext ctx)
	//{
	//	//SetDataValue<WallKnockPlayerCharacterUpdateData>(input => input.RightClickedThisFrame |= ctx.started);
	//}

	#region Control Scheme Change
	public InputDevice CurrentDevice { get; private set; }

	public void HandleControlsChange(PlayerInput input) => CurrentDevice = input.devices.Count > 0 ? input.devices[0] : null;
	#endregion Control Scheme Change
	#endregion Handle PlayerInput Events
}
