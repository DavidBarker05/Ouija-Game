using System.Collections.Generic;
using UnityEngine;

public enum Minigames
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

    HashSet<Minigames> m_MinigamesBeaten = new HashSet<Minigames>();
    List<Minigames> m_MinigameOrder = new List<Minigames>();

    public bool AreAllMinigamesBeaten => m_MinigamesBeaten.Count > 0 && m_MinigamesBeaten.Count == m_MinigameOrder.Count;
    public int NumMinigamesBeaten => m_CurrentMinigameIndexToBeat;

    int m_CurrentMinigameIndexToBeat = 0;
    public Minigames CurrentMinigameToBeat => m_MinigameOrder.Count > 0 ? m_MinigameOrder[Mathf.Clamp(m_CurrentMinigameIndexToBeat, 0, m_MinigameOrder.Count - 1)] : Minigames.Cryptex;

    public Minigames WhichMinigame(int index) => m_MinigameOrder[index];

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
        m_MinigameOrder.Add(Minigames.Cryptex);
        RandomiseMinigames();
        m_CurrentMinigameIndexToBeat = 0;
    }

    void RandomiseMinigames()
    {
        Minigames[] _minigames = (Minigames[])System.Enum.GetValues(typeof(Minigames));
        Minigames[] minigames = new Minigames[_minigames.Length - 1];
        System.Array.Copy(_minigames, 1, minigames, 0, minigames.Length); // Skip Minigames.Cryptex this is why it should be 0
        System.Random rng = new System.Random();
        for (int i = minigames.Length - 1; i > 0; --i) // Shuffle order
        {
            int j = rng.Next(i + 1);
            Minigames _temp = minigames[i];
            minigames[i] = minigames[j];
            minigames[j] = _temp;
        }
        for (int i = 1; i < minigames.Length; ++i) // Start at 1 so skip 
        {
            m_MinigameOrder.Add(minigames[i]);
        }
    }

    public bool IsMinigameBeaten(Minigames minigame) => m_MinigamesBeaten.Contains(minigame);

    public bool CanPlayMinigame(Minigames minigame) => !IsMinigameBeaten(minigame) && CurrentMinigameToBeat == minigame;

    public void OnMinigameBeaten(Minigames minigame)
    {
        m_MinigamesBeaten.Add(minigame);
        ++m_CurrentMinigameIndexToBeat;
    }

    public static string MinigameToString(Minigames minigame) => minigame switch
    {
        Minigames.Cryptex => "CRYPTEX",
        Minigames.Tarot => "TAROT CARDS",
        Minigames.Rune => "BLOOD RUNES",
        _ => throw new System.NotImplementedException()
    };
}
