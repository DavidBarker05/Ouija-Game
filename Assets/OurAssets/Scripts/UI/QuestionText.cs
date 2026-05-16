using System.Text;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class QuestionText : MonoBehaviour
{
    TMP_Text m_Text;

    void Awake() => m_Text = GetComponent<TMP_Text>();

    void Update()
    {
        if (StoryManager.Instance.KnowsAllAnswers)
        {
            m_Text.text = "<size=32>I need to leave and find HER</size>";
            return;
        }
        StringBuilder sb = new StringBuilder("<size=32>I need to know:</size>");
        if (!StoryManager.Instance.IsQuestionAnswered(StoryQuestions.WifeLeft)) sb.Append("- Why did she leave?");
        if (!StoryManager.Instance.IsQuestionAnswered(StoryQuestions.WifeSad)) sb.Append("- Why was she so sad?");
        if (!StoryManager.Instance.IsQuestionAnswered(StoryQuestions.WifeDead)) sb.Append("- What happened to her?");
        if (!StoryManager.Instance.IsQuestionAnswered(StoryQuestions.WhereWife)) sb.Append("- Where is she now?");
        m_Text.text = sb.ToString();
    }
}
