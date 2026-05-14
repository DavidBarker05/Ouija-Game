using UnityEngine;

public class MenuCharacterInitData : IPlayerCharacterInitData
{
    public Player Player { get; set; }
}

public class MenuCharacterUpdateData : IPlayerCharacterUpdateData
{
    public float DeltaTime { get; set; }
    public Quaternion CameraRotation { get; set; }
    public MouseInfo MouseInfo { get; set; }
}

public class MenuCharacter : PlayerCharacter
{
    public override bool HasBeenInitialised { get; protected set; }

    public override string ActionMap => string.Empty;
    public override bool MouseVisible => true;
    public override bool DoCameraRotation => false;
    public override bool UseMouseScreenPosition => false;

    Player m_Player;
    PlayerCharacter m_LastCharacter;
    GameObject m_LastUI;
    GameObject m_CurrentUI;

    public override void Init(IPlayerCharacterInitData playerCharacterInitData)
    {
        if (playerCharacterInitData is not MenuCharacterInitData initData)
        {
            Debug.LogError($"playerCharacterInitData needs to be type MenuCharacterInitData! Received {playerCharacterInitData.GetType()}");
            return;
        }
        m_Player = initData.Player;
        HasBeenInitialised = true;
    }

    public override void LoadSceneData(PlayerSceneData playerSceneData)
    {
        if (!HasBeenInitialised)
        {
            Debug.LogError("MenuCharacter hasn't been initialised!");
            return;
        }
    }

    public override void UpdateCharacter(ref IPlayerCharacterUpdateData playerCharacterUpdateData)
    {
        if (!HasBeenInitialised)
        {
            Debug.LogError("MenuCharacter hasn't been initialised!");
            return;
        }
        if (playerCharacterUpdateData is not MenuCharacterUpdateData)
        {
            Debug.LogError($"playerCharacterUpdateData needs to be type MenuCharacterUpdateData! Received {playerCharacterUpdateData.GetType()}");
            return;
        }
    }

    public override void OnPausePressed()
    {
        if (!HasBeenInitialised)
        {
            Debug.LogError("MenuCharacter hasn't been initialised!");
            return;
        }
    }

    public void OnMenuOpen(PlayerCharacter currentCharacter, GameObject currentUI, GameObject newUI)
    {
        if (m_LastCharacter || !currentCharacter || !newUI) return;
        CameraTarget = currentCharacter.CameraTarget;
        m_LastCharacter = currentCharacter;
        m_Player.ChangeCharacter(this);
        m_LastUI = currentUI;
        m_CurrentUI = newUI;
        m_CurrentUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnMenuExit()
    {
        if (!m_LastCharacter) return;
        Time.timeScale = 1f;
        m_Player.ChangeCharacter(m_LastCharacter);
        m_LastUI?.SetActive(true);
        m_CurrentUI.SetActive(false);
        CameraTarget = null;
        m_LastCharacter = null;
        m_LastUI = null;
        m_CurrentUI = null;
    }
}
