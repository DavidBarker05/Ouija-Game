using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace OurAssets.Scripts.Chat
{
    /// <summary>
    /// Per-story-session facts generated once before narrative context; used in prompts and gated Ouija replies.
    /// </summary>
    [Serializable]
    public sealed class StorySessionLore
    {
        public string playerName;
        public string wifeName;
        public string wifeLeftReason;
        public string wifeSadReason;

        public void TrimInPlace()
        {
            playerName = playerName?.Trim() ?? string.Empty;
            wifeName = wifeName?.Trim() ?? string.Empty;
            wifeLeftReason = wifeLeftReason?.Trim() ?? string.Empty;
            wifeSadReason = wifeSadReason?.Trim() ?? string.Empty;
        }

        public bool IsComplete()
        {
            return !string.IsNullOrWhiteSpace(playerName)
                && !string.IsNullOrWhiteSpace(wifeName)
                && !string.IsNullOrWhiteSpace(wifeLeftReason)
                && !string.IsNullOrWhiteSpace(wifeSadReason);
        }

        public Dictionary<string, object> ToJinjaBindings()
        {
            return new Dictionary<string, object>
            {
                ["player_name"] = playerName ?? string.Empty,
                ["wife_name"] = wifeName ?? string.Empty,
                ["wife_left_reason"] = wifeLeftReason ?? string.Empty,
                ["wife_sad_reason"] = wifeSadReason ?? string.Empty,
            };
        }
    }

    [Serializable]
    internal sealed class StorySessionLoreSnake
    {
        public string player_name;
        public string wife_name;
        public string wife_left_reason;
        public string wife_sad_reason;
    }

    internal static class StorySessionLoreParser
    {
        internal static bool TryParseFromModelContent(string raw, out StorySessionLore lore, out string failureDetail)
        {
            lore = new StorySessionLore();
            failureDetail = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    failureDetail = "empty model content";
                    return false;
                }

                string stripped = StripCodeFence(raw);
                Match m = Regex.Match(stripped, @"\{[\s\S]*\}");
                if (!m.Success)
                {
                    failureDetail = "no JSON object found in model output";
                    return false;
                }

                string jsonBlob = m.Value;

                if (!TryBind(jsonBlob, out lore))
                {
                    failureDetail = "could not parse session lore JSON (expected camelCase or snake_case keys)";
                    return false;
                }

                lore.TrimInPlace();
                if (lore.IsComplete())
                {
                    return true;
                }

                failureDetail = "parsed JSON but one or more required fields were empty";
                return false;
            }
            catch (Exception e)
            {
                failureDetail = e.Message;
                return false;
            }
        }

        private static bool TryBind(string jsonBlob, out StorySessionLore lore)
        {
            lore = JsonUtility.FromJson<StorySessionLore>(jsonBlob);
            if (lore != null && AnyFieldSet(lore))
            {
                return true;
            }

            StorySessionLoreSnake snake = JsonUtility.FromJson<StorySessionLoreSnake>(jsonBlob);
            if (snake == null)
            {
                lore = new StorySessionLore();
                return false;
            }

            lore = new StorySessionLore
            {
                playerName = snake.player_name,
                wifeName = snake.wife_name,
                wifeLeftReason = snake.wife_left_reason,
                wifeSadReason = snake.wife_sad_reason,
            };
            return AnyFieldSet(lore);
        }

        private static bool AnyFieldSet(StorySessionLore l)
        {
            return !string.IsNullOrWhiteSpace(l.playerName)
                || !string.IsNullOrWhiteSpace(l.wifeName)
                || !string.IsNullOrWhiteSpace(l.wifeLeftReason)
                || !string.IsNullOrWhiteSpace(l.wifeSadReason);
        }

        private static string StripCodeFence(string raw)
        {
            string t = raw.Trim();
            if (t.StartsWith("```", StringComparison.Ordinal))
            {
                int nl = t.IndexOf('\n');
                if (nl >= 0)
                {
                    t = t.Substring(nl + 1);
                }

                int endFence = t.LastIndexOf("```", StringComparison.Ordinal);
                if (endFence >= 0)
                {
                    t = t.Substring(0, endFence);
                }

                return t.Trim();
            }

            return t;
        }
    }
}
