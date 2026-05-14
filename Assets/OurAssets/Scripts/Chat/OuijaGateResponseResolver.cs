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
            "wife_left_reason" => LoreField(l => l.wifeLeftReason),
            "wife_sad_reason" => LoreField(l => l.wifeSadReason),
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
				return SpiritNameManager.Instance.SpiritName;
			}
        }
    }
}
