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
        [SerializeField] private TextAsset sessionLorePromptTemplate;

        [Header("Timing")]
        [SerializeField] private int keepAliveSeconds = 120;
        [SerializeField] private int warmRequestTimeoutSeconds = 20;
        [SerializeField] private int coldStartTimeoutSeconds = 90;

        [Header("Narrative sampling (session lore + story)")]
        [Tooltip("Higher values diversify model output; 0 is greedy / often identical rerolls for the same prompt.")]
        [SerializeField, Range(0.1f, 1.5f)] private float narrativeTemperature = 0.88f;
        [SerializeField, Range(0.5f, 1f)] private float narrativeTopP = 0.92f;
        [SerializeField, Range(1, 100)] private int narrativeTopK = 48;

        [Header("Debug")]
        [SerializeField] private bool enableRegularDebugLogs = true;

        private string _storyModelName;

        private const string StoryPromptResourcePath = "Prompts/StoryPrompt";
        private const string SessionLorePromptResourcePath = "Prompts/SessionLorePrompt";

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

        [ContextMenu("Generate Session Lore")]
        public async void GenerateSessionLoreFromInspector()
        {
            try
            {
                StorySessionLore lore = await GenerateSessionLoreAsync(CancellationToken.None);
                if (enableRegularDebugLogs)
                {
                    Debug.Log(
                        $"Session lore: player={lore.playerName}, wife={lore.wifeName}, " +
                        $"left={lore.wifeLeftReason}, sad={lore.wifeSadReason}");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Session lore generation failed: {exception.Message}");
            }
        }

        [ContextMenu("Generate Story Context")]
        public async void GenerateStoryContextFromInspector()
        {
            try
            {
                string storyPrompt = BuildStoryPromptWithSessionBindings();
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
		public async Task<string> GenerateStoryContextAsync()
        {
			string storyPrompt = BuildStoryPromptWithSessionBindings();
			string story = await GenerateStoryContextAsync(storyPrompt, CancellationToken.None);
			return story;
		}

        /// <summary>
        /// Runs before <see cref="GenerateStoryContextAsync"/> for a new game: fills session lore JSON
        /// (names + two answer strings) so story and gated Ouija lines stay aligned.
        /// </summary>
        public async Task<StorySessionLore> GenerateSessionLoreAsync(CancellationToken cancellationToken = default)
        {
            await Session.EnsureServerReadyAsync(cancellationToken);

            Dictionary<string, object> loreVars = new Dictionary<string, object>
            {
                ["run_variant_id"] = Guid.NewGuid().ToString("N"),
            };
            string lorePrompt = RenderPromptTemplate(sessionLorePromptTemplate, SessionLorePromptResourcePath, loreVars);
            if (string.IsNullOrWhiteSpace(lorePrompt))
            {
                throw new ArgumentException("Session lore prompt is empty.");
            }

            OllamaChatRequest request = new OllamaChatRequest
            {
                model = _storyModelName,
                keep_alive = ConvertKeepAliveSeconds(keepAliveSeconds),
                stream = false,
                options = BuildNarrativeInferenceOptions(),
                messages = new List<OllamaMessage>
                {
                    new OllamaMessage("user", lorePrompt),
                },
            };

            int timeout = Session.ResolveRequestTimeoutSeconds(
                _storyModelName,
                keepAliveSeconds,
                warmRequestTimeoutSeconds,
                coldStartTimeoutSeconds);
            OllamaChatResponse response = await Session.Client.SendChatAsync(request, timeout, cancellationToken);
            string generated = response?.message?.content?.Trim() ?? string.Empty;

            if (!StorySessionLoreParser.TryParseFromModelContent(generated, out StorySessionLore lore, out string parseDetail))
            {
                throw new InvalidOperationException($"Session lore model output was unusable ({parseDetail}). Raw length={generated.Length}.");
            }

            WriteSessionLoreToCache(lore);
            Session.MarkModelWarm(_storyModelName);
            return lore;
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
                options = BuildNarrativeInferenceOptions(),
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

        public void WriteSessionLoreToCache(StorySessionLore lore)
        {
            OuijaGameCachePaths.EnsureRootExists();
            string path = OuijaGameCachePaths.SessionLoreFilePath;
            StorySessionLore toWrite = lore ?? new StorySessionLore();
            toWrite.TrimInPlace();
            File.WriteAllText(path, JsonUtility.ToJson(toWrite, prettyPrint: true));
        }

        public static bool TryReadSessionLoreFromCache(out StorySessionLore lore)
        {
            lore = null;
            string path = OuijaGameCachePaths.SessionLoreFilePath;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path) ?? string.Empty;
                lore = JsonUtility.FromJson<StorySessionLore>(json);
                lore?.TrimInPlace();
                return lore != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to read session lore cache: {exception.Message}");
                return false;
            }
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

        private string BuildStoryPromptWithSessionBindings()
        {
            StorySessionLore lore = new StorySessionLore();
            if (TryReadSessionLoreFromCache(out StorySessionLore fromDisk) && fromDisk != null)
            {
                lore = fromDisk;
            }

            Dictionary<string, object> storyVars = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kv in lore.ToJinjaBindings())
            {
                storyVars[kv.Key] = kv.Value;
            }

            storyVars["run_variant_id"] = Guid.NewGuid().ToString("N");
            return RenderPromptTemplate(storyPromptTemplate, StoryPromptResourcePath, storyVars);
        }

        private static string RenderPromptTemplate(
            TextAsset serializedTemplate,
            string resourcePath,
            IDictionary<string, object> variables)
        {
            TextAsset templateAsset = serializedTemplate != null
                ? serializedTemplate
                : Resources.Load<TextAsset>(resourcePath);

            if (templateAsset == null || string.IsNullOrWhiteSpace(templateAsset.text))
            {
                return string.Empty;
            }

            Dictionary<string, object> dict = new Dictionary<string, object>();
            if (variables != null)
            {
                foreach (KeyValuePair<string, object> kv in variables)
                {
                    dict[kv.Key] = kv.Value;
                }
            }

            Template template = new Template(templateAsset.text);
            string rendered = template.Render(dict);
            return rendered ?? string.Empty;
        }

        private OllamaChatInferenceOptions BuildNarrativeInferenceOptions()
        {
            return new OllamaChatInferenceOptions
            {
                temperature = narrativeTemperature,
                top_p = narrativeTopP,
                top_k = Mathf.Max(1, narrativeTopK),
                seed = NextNarrativeSeed(),
            };
        }

        /// <summary>
        /// Ollama repeats identical completions for identical prompts when sampling is effectively greedy.
        /// A fresh seed each request keeps session lore and story diverging between runs.
        /// </summary>
        private static int NextNarrativeSeed()
        {
            unchecked
            {
                int h = Guid.NewGuid().GetHashCode();
                int t = global::System.Environment.TickCount;
                int x = (h * 397) ^ t;
                if (x == 0) x = 1;
                if (x == int.MinValue) x = 2;
                return x;
            }
        }

        private static string ConvertKeepAliveSeconds(int seconds)
        {
            return $"{Math.Max(1, seconds)}s";
        }
    }
}
