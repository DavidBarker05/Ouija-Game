using UnityEngine;

public enum StoryQuestions
{
    WifeLeft,
    WifeSad,
    WifeDead,
    WhereWife
}

public class StoryManager : MonoBehaviour
{
    static StoryManager s_Instance;
    public static StoryManager Instance
    {
        get
        {
            if (!s_Instance)
            {
                GameObject go = new GameObject(nameof(StoryManager));
                s_Instance = go.AddComponent<StoryManager>();
                DontDestroyOnLoad(go);
            }
            return s_Instance;
        }
    }

    bool m_bWifeLeft;
    bool m_bWifeSad;
    bool m_bWifeDead;
    bool m_bWhereWife;

    public bool KnowsAllAnswers => m_bWifeLeft && m_bWifeSad && m_bWifeDead && m_bWhereWife;

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
        m_bWifeDead = false;
        m_bWifeSad = false;
        m_bWifeDead = false;
        m_bWhereWife = false;
    }

    public void OnQuestionAnswered(StoryQuestions storyQuestion)
    {
        switch (storyQuestion)
        {
            case StoryQuestions.WifeLeft:
                m_bWifeLeft = true;
                break;
            case StoryQuestions.WifeSad:
                m_bWifeSad = true;
                break;
            case StoryQuestions.WifeDead:
                m_bWifeDead = true;
                break;
            case StoryQuestions.WhereWife:
                m_bWhereWife = true;
                break;
            default:
                break;
        }
    }

    public bool IsQuestionAnswered(StoryQuestions question) => question switch
    {
        StoryQuestions.WifeLeft => m_bWifeLeft,
        StoryQuestions.WifeSad => m_bWifeSad,
        StoryQuestions.WifeDead => m_bWifeDead,
        StoryQuestions.WhereWife => m_bWhereWife,
        _ => false
    };
}
