using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class QuestionText : MonoBehaviour
{
    public static readonly HashSet<ExtraQuestion> ExtraQuestionsToDisplay = new HashSet<ExtraQuestion>(5);

    TMP_Text m_Text;

    void Awake() => m_Text = GetComponent<TMP_Text>();

    void Update()
    {
        if (StoryManager.Instance.KnowsAllAnswers)
        {
            m_Text.text = "<size=32>I need to leave and find HER</size>";
            return;
        }
        if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.WhereTasks) != ExtraQuestionStatus.DoesntKnow)
            ExtraQuestionsToDisplay.Add(ExtraQuestion.WhereTasks);
        if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.HowOpenDoor) != ExtraQuestionStatus.DoesntKnow)
            ExtraQuestionsToDisplay.Add(ExtraQuestion.HowOpenDoor);
        if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.SpiritName) != ExtraQuestionStatus.DoesntKnow)
            ExtraQuestionsToDisplay.Add(ExtraQuestion.SpiritName);
        if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.HowOpenDoor) != ExtraQuestionStatus.DoesntKnow)
            ExtraQuestionsToDisplay.Add(ExtraQuestion.FirstTask);
        if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.SpiritName) != ExtraQuestionStatus.DoesntKnow)
            ExtraQuestionsToDisplay.Add(ExtraQuestion.SecondTask);
        StringBuilder sb = new StringBuilder("<size=32>I need to know:</size>");
        if (!StoryManager.Instance.IsQuestionAnswered(StoryQuestion.WifeLeft)) sb.Append("\n- Why did she leave?");
        else sb.Append("\n- <s>Why did she leave?</s>");
        if (!StoryManager.Instance.IsQuestionAnswered(StoryQuestion.WifeSad)) sb.Append("\n- Why was she so sad?");
        else sb.Append("\n- <s>Why was she so sad?</s>");
        if (!StoryManager.Instance.IsQuestionAnswered(StoryQuestion.WifeDead)) sb.Append("\n- What happened to her?");
        else sb.Append("\n- <s>What happened to her?</s>");
        if (!StoryManager.Instance.IsQuestionAnswered(StoryQuestion.WhereWife)) sb.Append("\n- Where is she now?");
        else sb.Append("\n- <s>Where is she now?</s>");
        foreach (ExtraQuestion question in ExtraQuestionsToDisplay)
        {
            ExtraQuestionStatus status = StoryManager.Instance.GetExtraQuestionStatus(question);
            if (status == ExtraQuestionStatus.ShouldAsk) sb.Append($"\n- {ExtraQuestionString(question)}");
            else if (status == ExtraQuestionStatus.Asked) sb.Append($"\n- <s>{ExtraQuestionString(question)}</s>");
        }
        m_Text.text = sb.ToString();
    }

    string ExtraQuestionString(ExtraQuestion question) => question switch
    {
        ExtraQuestion.WhereTasks => "Where are the tasks?",
        ExtraQuestion.HowOpenDoor => "How do I open the door?",
        ExtraQuestion.SpiritName => "The spirit's name.",
        ExtraQuestion.FirstTask => "What is my first task?",
        ExtraQuestion.SecondTask => "What is my second task?",
        _ => throw new System.NotImplementedException($"{question} hasn't been implemented")
    };
}
