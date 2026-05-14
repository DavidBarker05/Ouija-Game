using UnityEngine;

public class OuijaCharacterInitData : IPlayerCharacterInitData
{
	public PauseCharacter PauseCharacter { get; set; }
}

public class OuijaCharacterUpdateData : IPlayerCharacterUpdateData
{
	public float DeltaTime { get; set; }
	public Quaternion CameraRotation { get; set; }
	public MouseInfo MouseInfo { get; set; }
}

public class OuijaCharacter : PlayerCharacter
{
	public override bool HasBeenInitialised { get; protected set; }

	public override string ActionMap => "OuijaPlayer";
	public override bool MouseVisible => true;
	public override bool DoCameraRotation => false;
	public override bool UseMouseScreenPosition => false;

	PauseCharacter m_PauseCharacter;

	public override void Init(IPlayerCharacterInitData playerCharacterInitData)
	{
		if (playerCharacterInitData is not OuijaCharacterInitData initData)
		{
			Debug.LogError($"playerCharacterInitData needs to be type OuijaCharacterInitData! Received {playerCharacterInitData.GetType()}");
			return;
		}
		m_PauseCharacter = initData.PauseCharacter;
		HasBeenInitialised = true;
	}

	public override void LoadSceneData(PlayerSceneData playerSceneData)
	{
		if (!HasBeenInitialised)
		{
			Debug.LogError("OuijaCharacter hasn't been initialised!");
			return;
		}
	}

	public override void UpdateCharacter(ref IPlayerCharacterUpdateData playerCharacterUpdateData)
	{
		if (!HasBeenInitialised)
		{
			Debug.LogError("OuijaCharacter hasn't been initialised!");
			return;
		}
		if (playerCharacterUpdateData is not OuijaCharacterUpdateData)
		{
			Debug.LogError($"playerCharacterUpdateData needs to be type OuijaCharacterUpdateData! Received {playerCharacterUpdateData.GetType()}");
			return;
		}
	}

	public override void OnPausePressed()
	{
		if (!HasBeenInitialised)
		{
			Debug.LogError("OuijaCharacter hasn't been initialised!");
			return;
		}
		m_PauseCharacter.PauseGame(this);
	}
}
