using UnityEngine;

namespace OurAssets.Scripts.Chat
{
    public class OuijaGateResponseResolver : MonoBehaviour, IOuijaGateResponseResolver
    {
        public string GetGatedResponseText(string responseId) => responseId switch
        {
            "spirit_name" => SpiritName,
            "player_name" => LoreField(l => l.playerName),
            "wife_name" => LoreField(l => l.wifeName),
            "wife_left_reason" => WifeLeft,
            "wife_sad_reason" => WifeSad,
            "tasks_where" => TasksWhere,
            "open_door" => OpenDoor,
            "first_task" => FirstTask,
            "second_task" => SecondTask,
            "wife_where_blocked" => WifeWhereBlocked,
            _ => string.Empty
        };

        static string LoreField(System.Func<StorySessionLore, string> pick)
        {
            if (!StoryAiService.TryReadSessionLoreFromCache(out StorySessionLore lore) || lore == null)
            {
                return string.Empty;
            }

            string v = pick(lore);
            return string.IsNullOrWhiteSpace(v) ? string.Empty : v.Trim();
        }

        string SpiritName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SpiritNameManager.Instance.SpiritName)) SpiritNameManager.Instance.StartNewGame();
                if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.SpiritName) == ExtraQuestionStatus.DoesntKnow)
                    StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.SpiritName, ExtraQuestionStatus.DoesntNeedAsk);
                else StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.SpiritName, ExtraQuestionStatus.Asked);
                return SpiritNameManager.Instance.SpiritName;
            }
        }

        string WifeLeft
        {
            get
            {
                StoryManager.Instance.OnQuestionAnswered(StoryQuestion.WifeLeft);
                return LoreField(l => l.wifeLeftReason);
            }
        }

        string WifeSad
        {
            get
            {
                StoryManager.Instance.OnQuestionAnswered(StoryQuestion.WifeSad);
                return LoreField(l => l.wifeSadReason);
            }
        }

        string TasksWhere
        {
            get
            {
                if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.WhereTasks) != ExtraQuestionStatus.DoesntNeedAsk)
                    StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.WhereTasks, ExtraQuestionStatus.Asked);
                return "PAST THE DOOR";
            }
        }

        string OpenDoor
        {
            get
            {
                if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.HowOpenDoor) != ExtraQuestionStatus.DoesntNeedAsk)
                {
                    StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.HowOpenDoor, ExtraQuestionStatus.Asked);
                    if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.SpiritName) != ExtraQuestionStatus.Asked)
                        StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.SpiritName, ExtraQuestionStatus.ShouldAsk);
                }
                return "MY NAME";
            }
        }

        // m_MinigameOrder: [0]=Cryptex, [1]=first shuffled ritual, [2]=second shuffled ritual (see MinigameManager.StartNewGame).
        string FirstTask
        {
            get
            {
                try
                {
                    if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.FirstTask) != ExtraQuestionStatus.DoesntNeedAsk)
                        StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.FirstTask, ExtraQuestionStatus.Asked);
                    return MinigameManager.MinigameToString(MinigameManager.Instance.WhichMinigame(1));
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[OuijaGateResponseResolver] first_task: invalid minigame order index 1 ({ex.Message}).");
                    return string.Empty;
                }
            }
        }

        string SecondTask
        {
            get
            {
                try
                {
                    if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.SecondTask) != ExtraQuestionStatus.DoesntNeedAsk)
                        StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.SecondTask, ExtraQuestionStatus.Asked);
                    return MinigameManager.MinigameToString(MinigameManager.Instance.WhichMinigame(2));
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[OuijaGateResponseResolver] second_task: invalid minigame order index 2 ({ex.Message}).");
                    return string.Empty;
                }
            }
        }

        string WifeWhereBlocked
        {
            get
            {
                if (MinigameManager.Instance.NumMinigamesBeaten < 3)
                {
                    if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.WhereTasks) != ExtraQuestionStatus.Asked)
                        StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.WhereTasks, ExtraQuestionStatus.ShouldAsk);
                    if (StoryManager.Instance.GetExtraQuestionStatus(ExtraQuestion.FirstTask) != ExtraQuestionStatus.Asked)
                        StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.FirstTask, ExtraQuestionStatus.ShouldAsk);
                    StoryManager.Instance.SetExtraQuestionStatus(ExtraQuestion.SecondTask, ExtraQuestionStatus.ShouldAsk);
                    return "DO MY SECOND TASK";
                }
                return "ASK WHAT HAPPENED FIRST";
            }
        }
    }
}
