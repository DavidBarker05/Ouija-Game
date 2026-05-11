using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jinja2.NET;
using OurAssets.Scripts.AI;
using UnityEngine;

namespace OurAssets.Scripts.Chat
{
    /// <summary>
    /// Generates narrative/story context via Ollama and writes it to the temp cache so any scene
    /// (including the dedicated Ouija scene) can consume it without an inspector reference.
    /// </summary>
    public sealed class StoryAiService : MonoBehaviour
    {
        private static StoryAiService _instance;

        /// <summary>Lazily creates a DontDestroyOnLoad host if none exists in any loaded scene.</summary>
        public static StoryAiService Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                StoryAiService found = FindAnyObjectByType<StoryAiService>();
                if (found != null)
                {
                    _instance = found;
                    return _instance;
                }

                GameObject go = new GameObject(nameof(StoryAiService));
                DontDestroyOnLoad(go);
				_instance = go.AddComponent<StoryAiService>();
                return _instance;
            }
        }

        [Header("Models")]
        [SerializeField] private string fallbackStoryModelName = "llama3.2";

        [Header("Prompts")]
        [SerializeField] private TextAsset storyPromptTemplate;

        [Header("Timing")]
        [SerializeField] private int keepAliveSeconds = 120;
        [SerializeField] private int warmRequestTimeoutSeconds = 20;
        [SerializeField] private int coldStartTimeoutSeconds = 90;

        [Header("Debug")]
        [SerializeField] private bool enableRegularDebugLogs = true;

        private string _storyModelName;

        private const string StoryPromptResourcePath = "Prompts/StoryPrompt";

        private string ApplicationPath =>
            Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));

        private OllamaGameSession Session => OllamaGameSession.Instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _ = Session;
            LoadStoryModelName();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void LoadStoryModelName()
        {
            string path = ApplicationPath;
            if (!path.EndsWith("/"))
            {
                path += '/';
            }

            string storyFile = path + "StoryModel.txt";
            if (File.Exists(storyFile))
            {
                string storyContents = File.ReadAllText(storyFile);
                if (string.IsNullOrWhiteSpace(storyContents))
                {
                    File.WriteAllText(storyFile, fallbackStoryModelName);
                    _storyModelName = fallbackStoryModelName;
                }
                else
                {
                    _storyModelName = storyContents.Split(' ')[0];
                }
            }
            else
            {
                File.WriteAllText(storyFile, fallbackStoryModelName);
                _storyModelName = fallbackStoryModelName;
            }
        }

        [ContextMenu("Generate Story Context")]
        public async void GenerateStoryContextFromInspector()
        {
            try
            {
                string storyPrompt = RenderPromptTemplate(storyPromptTemplate, StoryPromptResourcePath);
                string story = await GenerateStoryContextAsync(storyPrompt, CancellationToken.None);
                if (enableRegularDebugLogs)
                {
                    Debug.Log(story);
                    Debug.Log($"Story context updated. Characters: {story.Length}");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Story context generation failed: {exception.Message}");
            }
        }

        /// <summary>
        /// Runs the story model, updates the temp-cache file used by the Ouija scene, and returns the text.
        /// </summary>
        public async Task<string> GenerateStoryContextAsync(string prompt, CancellationToken cancellationToken)
        {
            await Session.EnsureServerReadyAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Story prompt is empty.");
            }

            OllamaChatRequest request = new OllamaChatRequest
            {
                model = _storyModelName,
                keep_alive = ConvertKeepAliveSeconds(keepAliveSeconds),
                stream = false,
                messages = new List<OllamaMessage>
                {
                    new OllamaMessage("user", prompt),
                },
            };

            int timeout = Session.ResolveRequestTimeoutSeconds(
                _storyModelName,
                keepAliveSeconds,
                warmRequestTimeoutSeconds,
                coldStartTimeoutSeconds);
            OllamaChatResponse response = await Session.Client.SendChatAsync(request, timeout, cancellationToken);
            string generated = response?.message?.content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(generated))
            {
                throw new InvalidOperationException("Story model returned empty content.");
            }

            WriteStoryContextToCache(generated);
            Session.MarkModelWarm(_storyModelName);
            return generated;
        }

        /// <summary>
        /// Writes the current story context to <see cref="OuijaGameCachePaths.StoryContextFilePath"/> (e.g. menu-driven seed text).
        /// </summary>
        public void WriteStoryContextToCache(string storyContext)
        {
            OuijaGameCachePaths.EnsureRootExists();
            string path = OuijaGameCachePaths.StoryContextFilePath;
            File.WriteAllText(path, storyContext ?? string.Empty);
        }

        public static bool TryReadStoryContextFromCache(out string storyContext)
        {
            storyContext = string.Empty;
            string path = OuijaGameCachePaths.StoryContextFilePath;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                storyContext = File.ReadAllText(path) ?? string.Empty;
                return !string.IsNullOrWhiteSpace(storyContext);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to read story context cache: {exception.Message}");
                return false;
            }
        }

        private static string RenderPromptTemplate(TextAsset serializedTemplate, string resourcePath)
        {
            TextAsset templateAsset = serializedTemplate != null
                ? serializedTemplate
                : Resources.Load<TextAsset>(resourcePath);

            if (templateAsset == null || string.IsNullOrWhiteSpace(templateAsset.text))
            {
                return string.Empty;
            }

            Template template = new Template(templateAsset.text);
            string rendered = template.Render(new Dictionary<string, object>());
            return rendered ?? string.Empty;
        }

        private static string ConvertKeepAliveSeconds(int seconds)
        {
            return $"{Math.Max(1, seconds)}s";
        }
    }
}
