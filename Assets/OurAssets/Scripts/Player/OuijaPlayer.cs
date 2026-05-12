using UnityEngine;

public class OuijaCharacterInitData : IPlayerCharacterInitData { }

public class OuijaCharacterUpdateData : IPlayerCharacterUpdateData
{
	public float DeltaTime { get; set; }
	public Quaternion CameraRotation { get; set; }
	public MouseInfo MouseInfo { get; set; }
}

public class OuijaCharacter : PlayerCharacter
{
	public override bool HasBeenInitialised { get; protected set; }

	public override string ActionMap => string.Empty;
	public override bool MouseVisible => true;
	public override bool DoCameraRotation => false;
	public override bool UseMouseScreenPosition => false;

	public override void Init(IPlayerCharacterInitData playerCharacterInitData)
	{
		if (playerCharacterInitData is not OuijaCharacterInitData)
		{
			Debug.LogError($"playerCharacterInitData needs to be type OuijaCharacterInitData! Received {playerCharacterInitData.GetType()}");
			return;
		}
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
		if (playerCharacterUpdateData is not OuijaCharacterUpdateData updateData)
		{
			Debug.LogError($"playerCharacterUpdateData needs to be type OuijaCharacterUpdateData! Received {playerCharacterUpdateData.GetType()}");
			return;
		}
	}
}
