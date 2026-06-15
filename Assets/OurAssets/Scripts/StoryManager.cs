using UnityEngine;

public enum StoryQuestion
{
    WifeLeft,
    WifeSad,
    WifeDead,
    WhereWife
}

public enum ExtraQuestion
{
    WhereTasks,
    HowOpenDoor,
    SpiritName,
    FirstTask,
    SecondTask
}

public enum ExtraQuestionStatus
{
    DoesntKnow,
    ShouldAsk,
    Asked,
    DoesntNeedAsk
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
    ExtraQuestionStatus m_WhereTasksStatus;
    ExtraQuestionStatus m_HowOpenDoorStatus;
    ExtraQuestionStatus m_SpiritNameStatus;
    ExtraQuestionStatus m_FirstTaskStatus;
    ExtraQuestionStatus m_SecondTaskStatus;

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
        m_WhereTasksStatus = ExtraQuestionStatus.DoesntKnow;
        m_HowOpenDoorStatus = ExtraQuestionStatus.DoesntKnow;
        m_SpiritNameStatus = ExtraQuestionStatus.DoesntKnow;
        m_FirstTaskStatus = ExtraQuestionStatus.DoesntKnow;
        m_SecondTaskStatus = ExtraQuestionStatus.DoesntKnow;
        QuestionText.ExtraQuestionsToDisplay.Clear();
    }

    public void OnQuestionAnswered(StoryQuestion storyQuestion)
    {
        switch (storyQuestion)
        {
            case StoryQuestion.WifeLeft:
                m_bWifeLeft = true;
                break;
            case StoryQuestion.WifeSad:
                m_bWifeSad = true;
                break;
            case StoryQuestion.WifeDead:
                m_bWifeDead = true;
                break;
            case StoryQuestion.WhereWife:
                m_bWhereWife = true;
                break;
            default:
                break;
        }
    }

    public void SetExtraQuestionStatus(ExtraQuestion extraQuestion, ExtraQuestionStatus newStatus)
    {
        switch (extraQuestion)
        {
            case ExtraQuestion.WhereTasks:
                m_WhereTasksStatus = newStatus;
                break;
            case ExtraQuestion.HowOpenDoor:
                m_HowOpenDoorStatus = newStatus;
                break;
            case ExtraQuestion.SpiritName:
                m_SpiritNameStatus = newStatus;
                break;
            case ExtraQuestion.FirstTask:
                m_FirstTaskStatus = newStatus;
                break;
            case ExtraQuestion.SecondTask:
                m_SecondTaskStatus = newStatus;
                break;
            default:
                break;
        }
    }

    public bool IsQuestionAnswered(StoryQuestion question) => question switch
    {
        StoryQuestion.WifeLeft => m_bWifeLeft,
        StoryQuestion.WifeSad => m_bWifeSad,
        StoryQuestion.WifeDead => m_bWifeDead,
        StoryQuestion.WhereWife => m_bWhereWife,
        _ => false
    };

    public ExtraQuestionStatus GetExtraQuestionStatus(ExtraQuestion question) => question switch
    {
        ExtraQuestion.WhereTasks => m_WhereTasksStatus,
        ExtraQuestion.HowOpenDoor => m_HowOpenDoorStatus,
        ExtraQuestion.SpiritName => m_SpiritNameStatus,
        ExtraQuestion.FirstTask => m_FirstTaskStatus,
        ExtraQuestion.SecondTask => m_SecondTaskStatus,
        _ => ExtraQuestionStatus.DoesntKnow
    };
}
