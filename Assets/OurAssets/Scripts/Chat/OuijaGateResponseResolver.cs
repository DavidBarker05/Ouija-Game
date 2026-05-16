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

        string WifeLeft
        {
            get
            {
                StoryManager.Instance.OnQuestionAnswered(StoryQuestions.WifeLeft);
                return LoreField(l => l.wifeLeftReason);
            }
        }

        string WifeSad
        {
            get
            {
                StoryManager.Instance.OnQuestionAnswered(StoryQuestions.WifeSad);
                return LoreField(l => l.wifeSadReason);
            }
        }

        string SpiritName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SpiritNameManager.Instance.SpiritName)) SpiritNameManager.Instance.StartNewGame();
                return SpiritNameManager.Instance.SpiritName;
            }
        }

        // m_MinigameOrder: [0]=Cryptex, [1]=first shuffled ritual, [2]=second shuffled ritual (see MinigameManager.StartNewGame).
        string FirstTask
        {
            get
            {
                try
                {
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
                if (MinigameManager.Instance.NumMinigamesBeaten < 3) return "DO MY SECOND TASK";
                return "ASK WHAT HAPPENED FIRST";
            }
        }
    }
}
