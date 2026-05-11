using UnityEngine;

public class TarotCharacterInitData : IPlayerCharacterInitData { }

public class TarotCharacterUpdateData : IPlayerCharacterUpdateData
{
	public float DeltaTime { get; set; }
	public Quaternion CameraRotation { get; set; }
	public MouseInfo MouseInfo { get; set; }

	public bool LeftClickedThisFrame { get; set; }
}

public class TarotCharacter : PlayerCharacter
{
	public override bool HasBeenInitialised { get; protected set; }

	public override bool MouseVisible => true;
	public override bool DoCameraRotation => false;
	public override bool UseMouseScreenPosition => true;

	public override void Init(IPlayerCharacterInitData playerCharacterInitData)
	{
		if (playerCharacterInitData is not TarotCharacterInitData)
		{
			Debug.LogError($"playerCharacterInitData needs to be type TarotCharacterInitData! Received {playerCharacterInitData.GetType()}");
			return;
		}
		HasBeenInitialised = true;
	}

	public override void LoadSceneData(PlayerSceneData playerSceneData)
	{
		if (!HasBeenInitialised)
		{
			Debug.LogError("TarotCharacter hasn't been initialised!");
			return;
		}
		if (playerSceneData == null) return;
	}

	public override void UpdateCharacter(ref IPlayerCharacterUpdateData playerCharacterUpdateData)
	{
		if (!HasBeenInitialised)
		{
			Debug.LogError("TarotCharacter hasn't been initialised!");
			return;
		}
		if (playerCharacterUpdateData is not TarotCharacterUpdateData updateData)
		{
			Debug.LogError($"playerCharacterUpdateData needs to be type TarotCharacterUpdateData! Received {playerCharacterUpdateData.GetType()}");
			return;
		}
		if (updateData.MouseInfo.IsOverUI || !updateData.MouseInfo.DidHitObject)
		{
			updateData.LeftClickedThisFrame = false;
			return;
		}
	}
}
