using System;
using System.Threading.Tasks;
using OurAssets.Scripts.Chat;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    static GameManager s_Instance;
    public static GameManager Instance
    {
        get
        {
            if (!s_Instance)
            {
                GameObject go = new GameObject(nameof(GameManager));
                s_Instance = go.AddComponent<GameManager>();
                DontDestroyOnLoad(go);
            }
            return s_Instance;
        }
    }

    public bool CanMoveToFinalLevel => MinigameManager.Instance.AreAllMinigamesBeaten;

	void Awake()
	{
        if (s_Instance && s_Instance != this) Destroy(gameObject);
        else
        {
            s_Instance = this;
            DontDestroyOnLoad(gameObject);
        }
	}

    public async Task<string> StartNewGame(IProgress<string> progress)
    {
        progress.Report("Clearing Any Previous Game Data");
        TempCacheManager.Instance.ClearTempCache();
		progress.Report("Cleared Previous Game Data");
		progress.Report("Choosing a Random Spirit Name");
        SpiritNameManager.Instance.StartNewGame();
        progress.Report("Chose a Random Spirit Name");
        progress.Report("Randomly Assigning Challenges");
        MinigameManager.Instance.StartNewGame();
        progress.Report("Assigned All Challenges");
        progress.Report("Generating a Story");
        string story = await StoryAiService.Instance.GenerateStoryContextAsync();
        progress.Report("Finished Generating Story");
        return story;
    }
}
