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
                    if (MinigameManager.Instance.NumMinigamesBeaten < 2) return false;
                    StoryManager.Instance.OnQuestionAnswered(StoryQuestions.WifeDead);
                    return true;
                case "wife_where_condition":
                    if (MinigameManager.Instance.NumMinigamesBeaten < 3 || !StoryManager.Instance.IsQuestionAnswered(StoryQuestions.WifeDead)) return false;
                    StoryManager.Instance.OnQuestionAnswered(StoryQuestions.WhereWife);
                    return true;
                default:
                    return true;
            }
        }
    }
}
