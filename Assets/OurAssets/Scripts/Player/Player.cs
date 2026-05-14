using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class Player : MonoBehaviour
{
	[SerializeField]
	PlayerSettings m_PlayerSettings;
	[SerializeField]
	PlayerCharacter m_StartingPlayerCharacter;
	[SerializeField]
	PauseCharacter m_PauseCharacter;
	[SerializeField]
	PlayerCamera m_PlayerCamera;
	[SerializeField]
	Camera m_Camera;

	PlayerInput m_PlayerInput;

	PlayerCharacter m_PlayerCharacter;
	IPlayerCharacterUpdateData m_PlayerCharacterUpdateData;
	CameraInput m_CameraInput;
	MouseInfo m_MouseInfo;

	bool m_bCursorHidden;

	void Awake()
	{
		m_PlayerInput = GetComponent<PlayerInput>();
		m_PauseCharacter.Init(new PauseCharacterInitData());
		ChangeCharacter(m_StartingPlayerCharacter);
		m_PlayerCamera.Init(m_PlayerSettings.CameraSettings, m_PlayerCharacter.CameraTarget);
		m_CameraInput = new CameraInput();
		m_MouseInfo = new MouseInfo();
	}

	void Update()
	{
		if (!m_PlayerCharacter || !m_PlayerCamera) return;
		SetCursorVisibility(m_PlayerCharacter.MouseVisible);
		m_PlayerCharacterUpdateData.DeltaTime = Time.deltaTime;
		if (m_PlayerCharacter.DoCameraRotation)
		{
			m_PlayerCamera.UpdateRotation(ref m_CameraInput, Time.deltaTime);
			m_PlayerCharacterUpdateData.CameraRotation = m_PlayerCamera.transform.rotation;
		}
		if (m_PlayerCharacter.UseMouseScreenPosition)
		{
			m_MouseInfo.MouseScreenPosition = GetMousePositionOnScreen();
			GetMouseInfo(ref m_MouseInfo, m_PlayerCharacter.MouseHitLayer, m_PlayerCharacter.MouseHitDistance);
			m_PlayerCharacterUpdateData.MouseInfo = m_MouseInfo;
		}
		m_PlayerCharacter.UpdateCharacter(ref m_PlayerCharacterUpdateData);
	}

	void LateUpdate() => m_PlayerCamera.UpdatePosition(m_PlayerCharacter.CameraTarget);

	public void ChangeCharacter(PlayerCharacter character)
	{
		m_PlayerCharacter = character;
		if (!m_PlayerCharacter.HasBeenInitialised) m_PlayerCharacter.Init(PlayerCharacterInitData);
		if (!string.IsNullOrWhiteSpace(m_PlayerCharacter.ActionMap)) m_PlayerInput.SwitchCurrentActionMap(m_PlayerCharacter.ActionMap);
		m_PlayerCharacterUpdateData = PlayerCharacterUpdateData;
		m_PlayerCamera.ChangeCameraTarget(m_PlayerCharacter.CameraTarget);
	}

	IPlayerCharacterInitData PlayerCharacterInitData => m_PlayerCharacter switch
	{
		FirstPersonCharacter => new FirstPersonCharacterInitData()
		{
			CharacterSettings = m_PlayerSettings.CharacterSettings,
			InteractSettings = m_PlayerSettings.InteractSettings,
			PauseCharacter = m_PauseCharacter
		},
		TarotCharacter => new TarotCharacterInitData() { PauseCharacter = m_PauseCharacter },
		RuneCharacter => new RuneCharacterInitData() { PauseCharacter = m_PauseCharacter },
		OuijaCharacter => new OuijaCharacterInitData() { PauseCharacter = m_PauseCharacter },
		PauseCharacter => new PauseCharacterInitData(),
		_ => null
	};

	IPlayerCharacterUpdateData PlayerCharacterUpdateData => m_PlayerCharacter switch
	{
		FirstPersonCharacter => new FirstPersonCharacterUpdateData(),
		TarotCharacter => new TarotCharacterUpdateData(),
		OuijaCharacter => new OuijaCharacterUpdateData(),
		PauseCharacter => new PauseCharacterUpdateData(),
		_ => null
	};

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
		mouseInfo.IsOverUI = EventSystem.current?.IsPointerOverGameObject() ?? false;
		Ray ray = m_Camera.ScreenPointToRay(mouseInfo.MouseScreenPosition);
		mouseInfo.DidHitObject = Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerToHit);
		if (mouseInfo.DidHitObject) mouseInfo.HitInfo = hit;
	}
	#endregion Mouse Info

	#region Handle PlayerInput Events

	delegate void SetDataValueFunc<T>(T _) where T : class, IPlayerCharacterUpdateData;

	void SetDataValue<T>(SetDataValueFunc<T> dataChangeFunction) where T : class, IPlayerCharacterUpdateData
	{
		if (m_PlayerCharacterUpdateData is T t) dataChangeFunction(t);
	}

	public void HandleMoveInput(InputAction.CallbackContext ctx)
	{
		SetDataValue<FirstPersonCharacterUpdateData>(updateData => updateData.MovementInput = ctx.ReadValue<Vector2>());
	}

	public void HandleLookInput(InputAction.CallbackContext ctx)
	{
		if (!m_StartingPlayerCharacter.DoCameraRotation) return;
		m_CameraInput.LookInput = ctx.ReadValue<Vector2>();
		m_CameraInput.LookDevice = ctx.control.device;
	}

	public void HandleInteractInput(InputAction.CallbackContext ctx)
	{
		SetDataValue<FirstPersonCharacterUpdateData>(updateData => updateData.PressedInteract |= ctx.started);
	}

	public void HandleLeftClickInput(InputAction.CallbackContext ctx)
	{
		SetDataValue<TarotCharacterUpdateData>(updateData => updateData.LeftClickedThisFrame |= ctx.started);
	}

	public void HandlePauseToggleInput(InputAction.CallbackContext ctx)
	{
		if (ctx.started) m_PlayerCharacter.OnPausePressed();
	}

	#region Control Scheme Change
	public InputDevice CurrentDevice { get; private set; }

	public void HandleControlsChange(PlayerInput input) => CurrentDevice = input.devices.Count > 0 ? input.devices[0] : null;
	#endregion Control Scheme Change
	#endregion Handle PlayerInput Events
}
