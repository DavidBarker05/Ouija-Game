using System.IO;
using UnityEngine;

namespace OurAssets.Scripts.Chat
{
    /// <summary>
    /// Session-scratch files under <see cref="Application.temporaryCachePath"/> so the Ouija scene
    /// can read story output and conversation history without sharing a loaded scene with the story pipeline.
    /// </summary>
    public static class OuijaGameCachePaths
    {
        private const string SubFolderName = "OuijaGame";

        public static string RootDirectory => Path.Combine(Application.temporaryCachePath, SubFolderName);

        public static string StoryContextFilePath => Path.Combine(RootDirectory, "story_context.txt");

        /// <summary>JSON written at the start of each story session (player/wife names and canon reasons).</summary>
        public static string SessionLoreFilePath => Path.Combine(RootDirectory, "session_lore.json");

        public static string OuijaConversationFilePath => Path.Combine(RootDirectory, "ouija_conversation.json");

        public static void EnsureRootExists()
        {
            if (!Directory.Exists(RootDirectory))
            {
                Directory.CreateDirectory(RootDirectory);
            }
        }
    }
}
