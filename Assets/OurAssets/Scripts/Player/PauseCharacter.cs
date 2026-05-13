using UnityEngine;

public class PauseCharacterInitData : IPlayerCharacterInitData
{
    public Player Player { get; set; }
}

public class PauseCharacterUpdateData : IPlayerCharacterUpdateData
{
    public float DeltaTime { get; set; }
    public Quaternion CameraRotation { get; set; }
    public MouseInfo MouseInfo { get; set; }
}

public class PauseCharacter : PlayerCharacter
{
    [SerializeField]
	GameObject m_PauseScreen;

    public override bool HasBeenInitialised { get; protected set; }

    public override string ActionMap => "PausePlayer";
    public override bool MouseVisible => true;
    public override bool DoCameraRotation => false;
    public override bool UseMouseScreenPosition => false;

    public bool Paused { get; private set; } = false;

    Player m_Player;
    PlayerCharacter m_LastCharacter = null;

    public override void Init(IPlayerCharacterInitData playerCharacterInitData)
    {
        if (playerCharacterInitData is not PauseCharacterInitData initData)
        {
            Debug.LogError($"playerCharacterInitData needs to be type PauseCharacterInitData! Received {playerCharacterInitData.GetType()}");
            return;
        }
        m_Player = initData.Player;
        HasBeenInitialised = true;
    }

    public override void LoadSceneData(PlayerSceneData playerSceneData)
    {
        if (!HasBeenInitialised)
		{
			Debug.LogError("PauseCharacter hasn't been initialised!");
			return;
		}
    }

    public override void UpdateCharacter(ref IPlayerCharacterUpdateData playerCharacterUpdateData)
    {
        if (!HasBeenInitialised)
		{
			Debug.LogError("PauseCharacter hasn't been initialised!");
			return;
		}
		if (playerCharacterUpdateData is not PauseCharacterUpdateData)
		{
			Debug.LogError($"playerCharacterUpdateData needs to be type PauseCharacterUpdateData! Received {playerCharacterUpdateData.GetType()}");
			return;
		}
    }

    public override void OnPausePressed() => ResumeGame();
    public void PauseGame(PlayerCharacter characterToSwitchBackTo)
    {
        if (!HasBeenInitialised)
		{
			Debug.LogError("PauseCharacter hasn't been initialised!");
			return;
		}
        if (!m_PauseScreen)
		{
			Debug.LogWarning("Pause screen is missing");
			return;
		}
        if (Paused || Time.timeScale == 0f || characterToSwitchBackTo == null) return;
        Paused = true;
        m_LastCharacter = characterToSwitchBackTo;
        CameraTarget = m_LastCharacter.CameraTarget;
        m_Player.ChangeCharacter(this);
		m_PauseScreen.SetActive(true);
        Time.timeScale = 0f;
    }

    void ResumeGame()
    {
        if (!HasBeenInitialised)
		{
			Debug.LogError("PauseCharacter hasn't been initialised!");
			return;
		}
        if (!m_PauseScreen)
		{
			Debug.LogWarning("Pause screen is missing");
			return;
		}
        if (!Paused || Time.timeScale == 1f || m_LastCharacter == null) return;
        Paused = false;
        m_Player.ChangeCharacter(m_LastCharacter);
        m_LastCharacter = null;
        CameraTarget = null;
		m_PauseScreen.SetActive(false);
        Time.timeScale = 1f;
    }
}
