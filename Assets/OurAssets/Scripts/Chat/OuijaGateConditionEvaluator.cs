using UnityEngine;

namespace OurAssets.Scripts.Chat
{
    public class OuijaGateConditionEvaluator : MonoBehaviour, IOuijaGateConditionEvaluator
    {
        public bool IsConditionMet(string conditionId)
        {
            switch (conditionId)
            {
                case "wife_what_condition":
                    if (MinigameManager.Instance.NumMinigamesBeaten < 2)
                    {
                        if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.WhereTasks) != ExtraQuestionStatus.Asked)
                            StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.WhereTasks, ExtraQuestionStatus.ShouldAsk);
                        if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.FirstTask) == ExtraQuestionStatus.DoesntKnow)
                            StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.FirstTask, ExtraQuestionStatus.ShouldAsk);
                        return false;
                    }
                    StoryManager.Instance.OnQuestionAnswered(StoryQuestion.WifeDead);
                    return true;
                case "wife_where_condition":
                    if (MinigameManager.Instance.NumMinigamesBeaten < 3 || !StoryManager.Instance.IsQuestionAnswered(StoryQuestion.WifeDead)) return false;
                    StoryManager.Instance.OnQuestionAnswered(StoryQuestion.WhereWife);
                    return true;
                default:
                    return true;
            }
        }
    }
}
