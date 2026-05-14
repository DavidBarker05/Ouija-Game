using UnityEngine;

public class CryptexCharacterInitData : IPlayerCharacterInitData
{
    public PauseCharacter PauseCharacter { get; set; }
}

public class CryptexCharacterUpdateData : IPlayerCharacterUpdateData
{
    public float DeltaTime { get; set; }
    public Quaternion CameraRotation { get; set; }
    public MouseInfo MouseInfo { get; set; }

    public bool LeftClickedThisFrame { get; set; }
}

public class CryptexCharacter : PlayerCharacter
{
    public override bool HasBeenInitialised { get; protected set; }

    public override string ActionMap => "CryptexPlayer";
    public override bool MouseVisible => true;
    public override bool DoCameraRotation => false;
    public override bool UseMouseScreenPosition => true;

    PauseCharacter m_PauseCharacter;

    public override void Init(IPlayerCharacterInitData playerCharacterInitData)
    {
        if (playerCharacterInitData is not CryptexCharacterInitData initData)
        {
            Debug.LogError($"playerCharacterInitData needs to be type CryptexCharacterInitData! Received {playerCharacterInitData.GetType()}");
            return;
        }
        m_PauseCharacter = initData.PauseCharacter;
        HasBeenInitialised = true;
    }

    public override void LoadSceneData(PlayerSceneData playerSceneData)
    {
        if (!HasBeenInitialised)
        {
            Debug.LogError("CryptexCharacter hasn't been initialised!");
            return;
        }
    }

    public override void UpdateCharacter(ref IPlayerCharacterUpdateData playerCharacterUpdateData)
    {
        if (playerCharacterUpdateData is not CryptexCharacterUpdateData updateData)
        {
            Debug.LogError($"playerCharacterUpdateData needs to be type CryptexCharacterUpdateData! Received {playerCharacterUpdateData.GetType()}");
            return;
        }
        if (!HasBeenInitialised)
        {
            Debug.LogError("CryptexCharacter hasn't been initialised!");
            return;
        }
        if (m_PauseCharacter.Paused || Time.timeScale == 0f)
        {
            updateData.LeftClickedThisFrame = false;
            return;
        }
        if (updateData.LeftClickedThisFrame && !updateData.MouseInfo.IsOverUI && updateData.MouseInfo.DidHitObject)
            playerCharacterUpdateData.MouseInfo.HitInfo.collider.gameObject.GetComponent<CryptexRingButton>()?.RotateCryptex();
        updateData.LeftClickedThisFrame = false;
    }

    public override void OnPausePressed()
    {
        if (!HasBeenInitialised)
        {
            Debug.LogError("CryptexCharacter hasn't been initialised!");
            return;
        }
        m_PauseCharacter.PauseGame(this);
    }
}