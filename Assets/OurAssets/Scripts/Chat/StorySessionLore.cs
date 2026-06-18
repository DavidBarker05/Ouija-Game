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
        private static readonly string[] JsonKeys =
        {
            "playerName",
            "wifeName",
            "wifeLeftReason",
            "wifeSadReason",
            "player_name",
            "wife_name",
            "wife_left_reason",
            "wife_sad_reason",
        };

        internal static bool TryParseFromModelContent(string raw, out StorySessionLore lore, out string failureDetail)
        {
            lore = new StorySessionLore();
            failureDetail = string.Empty;

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

            if (!TryBind(jsonBlob, stripped, out lore))
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

        private static bool TryBind(string jsonBlob, string fullText, out StorySessionLore lore)
        {
            lore = new StorySessionLore();
            string sanitized = SanitizeJsonForUnity(jsonBlob);

            try
            {
                StorySessionLore camel = JsonUtility.FromJson<StorySessionLore>(sanitized);
                if (camel != null && AnyFieldSet(camel))
                {
                    lore = camel;
                    return true;
                }
            }
            catch
            {
                // JsonUtility rejects common model quirks; fall through to snake_case / regex.
            }

            try
            {
                StorySessionLoreSnake snake = JsonUtility.FromJson<StorySessionLoreSnake>(sanitized);
                if (snake != null && AnyFieldSet(snake))
                {
                    lore = new StorySessionLore
                    {
                        playerName = snake.player_name,
                        wifeName = snake.wife_name,
                        wifeLeftReason = snake.wife_left_reason,
                        wifeSadReason = snake.wife_sad_reason,
                    };
                    return true;
                }
            }
            catch
            {
                // ignored
            }

            if (TryExtractFieldsWithRegex(fullText, out StorySessionLore fromRegex) && AnyFieldSet(fromRegex))
            {
                lore = fromRegex;
                return true;
            }

            return false;
        }

        private static bool AnyFieldSet(StorySessionLore l)
        {
            return !string.IsNullOrWhiteSpace(l.playerName)
                || !string.IsNullOrWhiteSpace(l.wifeName)
                || !string.IsNullOrWhiteSpace(l.wifeLeftReason)
                || !string.IsNullOrWhiteSpace(l.wifeSadReason);
        }

        private static bool AnyFieldSet(StorySessionLoreSnake l)
        {
            return !string.IsNullOrWhiteSpace(l.player_name)
                || !string.IsNullOrWhiteSpace(l.wife_name)
                || !string.IsNullOrWhiteSpace(l.wife_left_reason)
                || !string.IsNullOrWhiteSpace(l.wife_sad_reason);
        }

        private static string SanitizeJsonForUnity(string json)
        {
            string s = json
                .Replace('\u201c', '"').Replace('\u201d', '"')
                .Replace('\u2018', '\'').Replace('\u2019', '\'');

            s = Regex.Replace(s, @":\s*null\b", @": """"", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @",\s*}", "}");
            s = Regex.Replace(s, @",\s*]", "]");

            foreach (string key in JsonKeys)
            {
                string pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*([^\"\\s,}][^,}]*?)\\s*(?=[,}])";
                s = Regex.Replace(
                    s,
                    pattern,
                    match => $"\"{key}\": \"{match.Groups[1].Value.Trim()}\"");
            }

            return s;
        }

        private static bool TryExtractFieldsWithRegex(string source, out StorySessionLore lore)
        {
            lore = new StorySessionLore();
            bool any = false;
            any |= TryExtractStringField(source, "playerName", "player_name", out lore.playerName);
            any |= TryExtractStringField(source, "wifeName", "wife_name", out lore.wifeName);
            any |= TryExtractStringField(source, "wifeLeftReason", "wife_left_reason", out lore.wifeLeftReason);
            any |= TryExtractStringField(source, "wifeSadReason", "wife_sad_reason", out lore.wifeSadReason);
            return any;
        }

        private static bool TryExtractStringField(
            string source,
            string camelKey,
            string snakeKey,
            out string value)
        {
            value = string.Empty;
            string[] patterns =
            {
                $@"""{camelKey}""\s*:\s*""((?:\\.|[^""\\])*)""",
                $@"""{snakeKey}""\s*:\s*""((?:\\.|[^""\\])*)""",
                $@"""{camelKey}""\s*:\s*'([^']*)'",
                $@"""{snakeKey}""\s*:\s*'([^']*)'",
                $@"""{camelKey}""\s*:\s*([^,}}\s][^,}}]*)",
                $@"""{snakeKey}""\s*:\s*([^,}}\s][^,}}]*)",
            };

            foreach (string pattern in patterns)
            {
                Match match = Regex.Match(source, pattern, RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    continue;
                }

                value = match.Groups[1].Value.Trim();
                return !string.IsNullOrWhiteSpace(value);
            }

            return false;
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
