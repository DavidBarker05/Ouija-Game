using UnityEngine;

public class RuneCharacterInitData : IPlayerCharacterInitData
{
	public PauseCharacter PauseCharacter { get; set; }
}

public class RuneCharacterUpdateData : IPlayerCharacterUpdateData
{
	public float DeltaTime { get; set; }
	public Quaternion CameraRotation { get; set; }
	public MouseInfo MouseInfo { get; set; }
}

public class RuneCharacter : PlayerCharacter
{
	public override bool HasBeenInitialised { get; protected set; }

	public override string ActionMap => "RunePlayer";
	public override bool MouseVisible => true;
	public override bool DoCameraRotation => false;
	public override bool UseMouseScreenPosition => false;

	PauseCharacter m_PauseCharacter;

	public override void Init(IPlayerCharacterInitData playerCharacterInitData)
	{
		if (playerCharacterInitData is not RuneCharacterInitData initData)
		{
			Debug.LogError($"playerCharacterInitData needs to be type RuneCharacterInitData! Received {playerCharacterInitData.GetType()}");
			return;
		}
		m_PauseCharacter = initData.PauseCharacter;
		HasBeenInitialised = true;
	}

	public override void LoadSceneData(PlayerSceneData playerSceneData)
	{
		if (!HasBeenInitialised)
		{
			Debug.LogError("RuneCharacter hasn't been initialised!");
			return;
		}
	}

	public override void UpdateCharacter(ref IPlayerCharacterUpdateData playerCharacterUpdateData)
	{
		if (playerCharacterUpdateData is not RuneCharacterUpdateData updateData)
		{
			Debug.LogError($"playerCharacterUpdateData needs to be type RuneCharacterUpdateData! Received {playerCharacterUpdateData.GetType()}");
			return;
		}
		if (!HasBeenInitialised)
		{
			Debug.LogError("RuneCharacter hasn't been initialised!");
			return;
		}
	}

	public override void OnPausePressed()
	{
		if (!HasBeenInitialised)
		{
			Debug.LogError("RuneCharacter hasn't been initialised!");
			return;
		}
		m_PauseCharacter.PauseGame(this);
	}
}
