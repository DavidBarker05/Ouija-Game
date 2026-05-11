using System;
using System.IO;
using UnityEngine;

namespace OurAssets.Scripts.Chat
{
    /// <summary>
    /// Persists Ouija player/assistant turns (not system constants) as JSON for cross-scene reload.
    /// </summary>
    public static class OuijaConversationHistoryStore
    {
        [Serializable]
        private sealed class ConversationDto
        {
            public string[] playerMessages = Array.Empty<string>();
            public string[] aiMessages = Array.Empty<string>();
        }

        public static bool TryLoad(out string[] playerMessages, out string[] aiMessages)
        {
            playerMessages = Array.Empty<string>();
            aiMessages = Array.Empty<string>();
            string path = OuijaGameCachePaths.OuijaConversationFilePath;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                ConversationDto dto = JsonUtility.FromJson<ConversationDto>(json);
                if (dto == null)
                {
                    return false;
                }

                playerMessages = dto.playerMessages ?? Array.Empty<string>();
                aiMessages = dto.aiMessages ?? Array.Empty<string>();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to read Ouija conversation cache: {exception.Message}");
                return false;
            }
        }

        public static void Save(OuijaConversationState state)
        {
            if (state == null)
            {
                return;
            }

            OuijaGameCachePaths.EnsureRootExists();
            ConversationDto dto = new ConversationDto
            {
                playerMessages = ToArray(state.PlayerMessages),
                aiMessages = ToArray(state.AiMessages),
            };

            string json = JsonUtility.ToJson(dto, prettyPrint: false);
            string path = OuijaGameCachePaths.OuijaConversationFilePath;
            File.WriteAllText(path, json);
        }

        public static void Delete()
        {
            string path = OuijaGameCachePaths.OuijaConversationFilePath;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string[] ToArray(System.Collections.Generic.IReadOnlyList<string> list)
        {
            if (list == null || list.Count == 0)
            {
                return Array.Empty<string>();
            }

            string[] copy = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                copy[i] = list[i] ?? string.Empty;
            }

            return copy;
        }
    }
}
