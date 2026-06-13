using System.Collections.Generic;
using UnityEngine;

public enum Minigame
{
    Cryptex, // Keep 0 always, don't mess with this
    Tarot,
    Rune
}

public class MinigameManager : MonoBehaviour
{
    static MinigameManager s_Instance;
    public static MinigameManager Instance
    {
        get
        {
            if (!s_Instance)
            {
                GameObject go = new GameObject(nameof(MinigameManager));
                s_Instance = go.AddComponent<MinigameManager>();
                DontDestroyOnLoad(go);
            }
            return s_Instance;
        }
    }

    HashSet<Minigame> m_MinigamesBeaten = new HashSet<Minigame>();
    List<Minigame> m_MinigameOrder = new List<Minigame>();

    public bool AreAllMinigamesBeaten => m_MinigamesBeaten.Count > 0 && m_MinigamesBeaten.Count == m_MinigameOrder.Count;
    public int NumMinigamesBeaten => m_CurrentMinigameIndexToBeat;

    int m_CurrentMinigameIndexToBeat = 0;
    public Minigame CurrentMinigameToBeat => m_MinigameOrder.Count > 0 ? m_MinigameOrder[Mathf.Clamp(m_CurrentMinigameIndexToBeat, 0, m_MinigameOrder.Count - 1)] : Minigame.Cryptex;

    public Minigame WhichMinigame(int index) => m_MinigameOrder[index];

    void Awake()
    {
        if (s_Instance && s_Instance != this) Destroy(gameObject);
        else
        {
            s_Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void StartNewGame()
    {
        m_MinigamesBeaten.Clear();
        m_MinigameOrder.Clear();
        m_MinigameOrder.Add(Minigame.Cryptex);
        RandomiseMinigames();
        m_CurrentMinigameIndexToBeat = 0;
    }

    void RandomiseMinigames()
    {
        Minigame[] _minigames = (Minigame[])System.Enum.GetValues(typeof(Minigame));
        Minigame[] minigames = new Minigame[_minigames.Length - 1];
        System.Array.Copy(_minigames, 1, minigames, 0, minigames.Length); // Skip Minigames.Cryptex this is why it should be 0
        System.Random rng = new System.Random();
        for (int i = minigames.Length - 1; i > 0; --i) // Shuffle order
        {
            int j = rng.Next(i + 1);
            Minigame _temp = minigames[i];
            minigames[i] = minigames[j];
            minigames[j] = _temp;
        }
        // Append every non-Cryptex ritual (Tarot + Rune). Cryptex is already at index 0; gated replies use WhichMinigame(1) / (2) for first/second optional rituals.
        // BUGFIX: Previously loop started at i=1, which skipped minigames[0] and left only one ritual in the queue — SecondTask / CanPlayMinigame for the other ritual broke.
        for (int i = 0; i < minigames.Length; ++i)
        {
            m_MinigameOrder.Add(minigames[i]);
        }
    }

    public bool IsMinigameBeaten(Minigame minigame) => m_MinigamesBeaten.Contains(minigame);

    public bool CanPlayMinigame(Minigame minigame) => !IsMinigameBeaten(minigame) && CurrentMinigameToBeat == minigame;

    public void OnMinigameBeaten(Minigame minigame)
    {
        m_MinigamesBeaten.Add(minigame);
        if (m_CurrentMinigameIndexToBeat == 1 && StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.FirstTask) == ExtraQuestionStatus.DoesntKnow)
            StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.FirstTask, ExtraQuestionStatus.DoesntNeedAsk);
        else if (m_CurrentMinigameIndexToBeat == 2 && StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.SecondTask) == ExtraQuestionStatus.DoesntKnow)
        {
            StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.SecondTask, ExtraQuestionStatus.DoesntNeedAsk);
            if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.WhereTasks) != ExtraQuestionStatus.Asked)
                StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.WhereTasks, ExtraQuestionStatus.DoesntNeedAsk);
        }
        ++m_CurrentMinigameIndexToBeat;
    }

    public static string MinigameToString(Minigame minigame) => minigame switch
    {
        Minigame.Cryptex => "CRYPTEX",
        Minigame.Tarot => "TAROT CARDS",
        Minigame.Rune => "BLOOD RUNES",
        _ => throw new System.NotImplementedException($"{minigame} hasn't been implemented")
    };
}
