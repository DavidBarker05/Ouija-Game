using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jinja2.NET;
using OurAssets.Scripts.AI;
using UnityEngine;

namespace OurAssets.Scripts.Chat
{
    public class OuijaAiOrchestrator : MonoBehaviour
    {
        [Header("Ollama Connection")]
        [SerializeField] private string ollamaBaseUrl = "http://127.0.0.1:11434";
        [SerializeField] private int ollamaStartupTimeoutSeconds = 20;
        [SerializeField] private float ollamaProbeIntervalSeconds = 0.5f;

        [Header("Models")]
		[SerializeField] private string fallbackStoryModelName = "llama3.2";
		[SerializeField] private string fallbackOuijaModelName = "llama3.2";

		[Header("Prompts")]
        [SerializeField] private TextAsset ouijaSystemPromptTemplate;
        [SerializeField] private TextAsset storyPromptTemplate;

        [Header("Timing")]
        [SerializeField] private int keepAliveSeconds = 120;
        [SerializeField] private int warmRequestTimeoutSeconds = 20;
        [SerializeField] private int coldStartTimeoutSeconds = 90;
        [SerializeField] private int unloadRequestTimeoutSeconds = 10;

        [Header("Debug")]
        [SerializeField] private bool enableRegularDebugLogs = true;

        [Header("Gated scripted questions")]
        [Tooltip("If true, specific player lines can be routed to scripted answers before the conversational model.")]
        [SerializeField] private bool enableQuestionGate = true;
        [SerializeField] private OuijaGatedQuestionEntry[] gatedQuestions = Array.Empty<OuijaGatedQuestionEntry>();
        [Tooltip("Optional: MonoBehaviour implementing IOuijaGateConditionEvaluator (minigame flags, progress, inventory).")]
        [SerializeField] private Component gateConditionEvaluator;
        [Tooltip("Prepended classifier rules; fuzzy matching still runs before this call.")]
        [SerializeField] private TextAsset gateClassifierInstructions;
        [Tooltip("Blank uses the configured Ouija chat model.")]
        [SerializeField] private string gateClassifierModelOverride = string.Empty;
        [SerializeField, Range(0f, 1f)] private float gatedFuzzyStrongThreshold = 0.72f;
        [SerializeField, Range(0f, 1f)] private float gatedFuzzyMinAiCandidateScore = 0.18f;
        [SerializeField, Range(1, 20)] private int gatedMaxClassifierCandidates = 5;
        [SerializeField, Range(0f, 1f)] private float gatedClassifierMinConfidence = 0.62f;
        [SerializeField, Range(5, 300)] private int gateClassifierTimeoutSeconds = 25;
        [SerializeField] private bool enableGateDebugLogs = true;

        private readonly Dictionary<string, DateTime> _lastModelResponseUtc = new Dictionary<string, DateTime>();

		private string storyModelName;
		private string ouijaModelName;

		private OllamaClient _ollamaClient;
        private OuijaConversationState _conversationState;
        private string _latestStoryContext;
        private bool _isUnloadingModels;
        private bool _isQuitting;
        private IOuijaGateConditionEvaluator _gateConditionEvaluator;
        private OuijaQuestionGateResolver _questionGateResolver;

		string ApplicationPath => Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')); // David - The path of the application, this is where the executable is for the build or in the project is for the editor

		public OuijaConversationState ConversationState => _conversationState;

        private const string OuijaSystemPromptResourcePath = "Prompts/OuijaSystemPrompt";
        private const string StoryPromptResourcePath = "Prompts/StoryPrompt";

        private void Awake()
        {
            _ollamaClient = new OllamaClient(ollamaBaseUrl);
            _conversationState = new OuijaConversationState();
            GetAIModels();
            CacheGateEvaluator();
            _questionGateResolver = new OuijaQuestionGateResolver(
                gatedFuzzyStrongThreshold,
                gatedFuzzyMinAiCandidateScore,
                gatedMaxClassifierCandidates,
                gatedClassifierMinConfidence,
                gateClassifierTimeoutSeconds,
                ResolveTimeoutSeconds,
                ConvertKeepAliveSeconds(keepAliveSeconds),
                enableGateDebugLogs && enableRegularDebugLogs);
            RebuildConstants();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (_isQuitting)
            {
                return;
            }

            if (pauseStatus)
            {
                TriggerModelUnload("application paused");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_isQuitting)
            {
                return;
            }

            if (!hasFocus)
            {
                TriggerModelUnload("application minimized/unfocused");
            }
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
            bool stoppedOwnedServer = ShutdownOwnedOllamaServer();
            if (!stoppedOwnedServer)
            {
                TriggerModelUnload("application quit");
            }
            else
            {
                _lastModelResponseUtc.Clear();
            }
        }

        void GetAIModels() // David - Get the AI model names from their folders
        {
            string path = ApplicationPath;
            if (!path.EndsWith('/')) path += '/';
            string storyFile = path + "StoryModel.txt";
            if (System.IO.File.Exists(storyFile))
            {
                string storyContents = System.IO.File.ReadAllText(storyFile);
				if (string.IsNullOrWhiteSpace(storyContents))
                {
					System.IO.File.WriteAllText(storyFile, fallbackStoryModelName);
					storyModelName = fallbackStoryModelName;
				}
                else storyModelName = storyContents.Split(' ')[0];
            }
            else
            {
				System.IO.File.WriteAllText(storyFile, fallbackStoryModelName);
                storyModelName = fallbackStoryModelName;
			}
            string ouijaFile = path + "OuijaModel.txt";
			if (System.IO.File.Exists(ouijaFile))
			{
				string ouijaContents = System.IO.File.ReadAllText(ouijaFile);
				if (string.IsNullOrWhiteSpace(ouijaContents))
				{
					System.IO.File.WriteAllText(ouijaFile, fallbackOuijaModelName);
					ouijaModelName = fallbackStoryModelName;
				}
				else ouijaModelName = ouijaContents.Split(' ')[0];
			}
			else
			{
				System.IO.File.WriteAllText(ouijaFile, fallbackOuijaModelName);
				ouijaModelName = fallbackOuijaModelName;
			}
		}

		[ContextMenu("Generate Story Context")]
        public async void GenerateStoryContextFromInspector()
        {
            try
            {
                string storyPrompt = RenderPromptTemplate(storyPromptTemplate, StoryPromptResourcePath);
                string story = await GenerateStoryContextAsync(storyPrompt, CancellationToken.None);
                if (enableRegularDebugLogs) Debug.Log(story); // David - Added for debugging
                if (enableRegularDebugLogs) Debug.Log($"Story context updated. Characters: {story.Length}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Story context generation failed: {exception.Message}");
            }
        }

        public async Task<string> GenerateStoryContextAsync(string prompt, CancellationToken cancellationToken)
        {
            await EnsureOllamaReadyAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Story prompt is empty.");
            }

            OllamaChatRequest request = new OllamaChatRequest
            {
                model = storyModelName,
                keep_alive = ConvertKeepAliveSeconds(keepAliveSeconds),
                stream = false,
                messages = new List<OllamaMessage>
                {
                    new OllamaMessage("user", prompt)
                }
            };

            int timeout = ResolveTimeoutSeconds(storyModelName);
            OllamaChatResponse response = await _ollamaClient.SendChatAsync(request, timeout, cancellationToken);
            string generated = response?.message?.content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(generated))
            {
                throw new InvalidOperationException("Story model returned empty content.");
            }

            _latestStoryContext = generated;
            RebuildConstants();
            MarkModelWarm(storyModelName);
            return generated;
        }

        public async Task<string> SendPlayerMessageToOuijaAsync(string playerMessage, CancellationToken cancellationToken = default)
        {
            await EnsureOllamaReadyAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(playerMessage))
            {
                throw new ArgumentException("Player message is empty.");
            }

            if (enableQuestionGate && _questionGateResolver != null)
            {
                List<OuijaQuestionGateResolver.GatedQuestionEntrySnap> gateSnapshots = CollectGateSnapshots();
                if (gateSnapshots.Count > 0)
                {
                    string classifierModel = string.IsNullOrWhiteSpace(gateClassifierModelOverride)
                        ? ouijaModelName
                        : gateClassifierModelOverride.Trim();

                    OuijaQuestionGateResolver.ResolveResult gateOutcome =
                        await _questionGateResolver.TryResolveAsync(
                            playerMessage,
                            gateSnapshots,
                            _gateConditionEvaluator,
                            _ollamaClient,
                            classifierModel,
                            gateClassifierInstructions != null ? gateClassifierInstructions.text : null,
                            cancellationToken).ConfigureAwait(false);

                    if (gateOutcome.InvokedClassifier)
                    {
                        MarkModelWarm(classifierModel);
                    }

                    if (gateOutcome.MatchedGate && !string.IsNullOrWhiteSpace(gateOutcome.Reply))
                    {
                        return gateOutcome.Reply.Trim();
                    }
                }
            }

            _conversationState.AddPlayerMessage(playerMessage);

            OllamaChatRequest request = new OllamaChatRequest
            {
                model = ouijaModelName,
                keep_alive = ConvertKeepAliveSeconds(keepAliveSeconds),
                stream = false,
                messages = _conversationState.ComposeForOuijaRequest()
            };

            int timeout = ResolveTimeoutSeconds(ouijaModelName);
            if (enableRegularDebugLogs) Debug.Log($"Ouija request timeout selected: {timeout}s");

            OllamaChatResponse response = await _ollamaClient.SendChatAsync(request, timeout, cancellationToken);
            string aiText = response?.message?.content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(aiText))
            {
                throw new InvalidOperationException("Ouija model returned empty content.");
            }

            _conversationState.AddAiMessage(aiText);
            MarkModelWarm(ouijaModelName);
            return aiText;
        }

        /// <summary>Lazy refresh if someone wires the evaluator component mid-session.</summary>
        private void CacheGateEvaluator()
        {
            IOuijaGateConditionEvaluator evaluator = gateConditionEvaluator as IOuijaGateConditionEvaluator;
            if (gateConditionEvaluator != null && evaluator == null)
            {
                Debug.LogWarning($"{nameof(OuijaAiOrchestrator)}: {gateConditionEvaluator.name} lacks {nameof(IOuijaGateConditionEvaluator)}.", gateConditionEvaluator);
            }

            _gateConditionEvaluator = evaluator;
        }

        private List<OuijaQuestionGateResolver.GatedQuestionEntrySnap> CollectGateSnapshots()
        {
            if (gatedQuestions == null || gatedQuestions.Length == 0)
            {
                return new List<OuijaQuestionGateResolver.GatedQuestionEntrySnap>();
            }

            List<OuijaQuestionGateResolver.GatedQuestionEntrySnap> list = new List<OuijaQuestionGateResolver.GatedQuestionEntrySnap>();
            foreach (OuijaGatedQuestionEntry entry in gatedQuestions)
            {
                OuijaQuestionGateResolver.GatedQuestionEntrySnap snap =
                    OuijaQuestionGateResolver.GatedQuestionEntrySnap.From(entry);
                if (snap != null && snap.Enabled)
                {
                    list.Add(snap);
                }
            }

            return list;
        }

        private async Task EnsureOllamaReadyAsync(CancellationToken cancellationToken)
        {
            OllamaProcessManager.StartupResult result = await _ollamaClient.EnsureServerReadyAsync(
                ollamaStartupTimeoutSeconds,
                ollamaProbeIntervalSeconds,
                cancellationToken);

            if (!result.IsAvailable)
            {
                throw new InvalidOperationException(
                    $"Ollama is unavailable. {result.ErrorMessage}");
            }

            if (result.DidStartProcess)
            {
                if (enableRegularDebugLogs) Debug.Log("Ollama server was not running and has been started.");
            }
        }

        private void TriggerModelUnload(string reason)
        {
            if (_isUnloadingModels)
            {
                return;
            }

            _ = UnloadModelsAndForceColdAsync(reason);
        }

        private async Task UnloadModelsAndForceColdAsync(string reason)
        {
            _isUnloadingModels = true;
            _lastModelResponseUtc.Clear();
            if (enableRegularDebugLogs) Debug.Log($"Forcing models cold due to {reason}.");

            try
            {
                await EnsureOllamaReadyAsync(CancellationToken.None);

                await _ollamaClient.UnloadModelAsync(storyModelName, unloadRequestTimeoutSeconds, CancellationToken.None);
                await _ollamaClient.UnloadModelAsync(ouijaModelName, unloadRequestTimeoutSeconds, CancellationToken.None);
                if (enableRegularDebugLogs) Debug.Log("Requested Ollama to unload story and ouija models from memory.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Model unload request failed: {exception.Message}");
            }
            finally
            {
                _isUnloadingModels = false;
            }
        }

        private bool ShutdownOwnedOllamaServer()
        {
            if (_ollamaClient == null)
            {
                return false;
            }

            bool stopped = _ollamaClient.ShutdownOwnedServer(out string errorMessage);
            if (stopped)
            {
                if (enableRegularDebugLogs) Debug.Log("Stopped Ollama server because this game session started it.");
                return true;
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                Debug.LogWarning(errorMessage);
            }

            return false;
        }

        private void RebuildConstants()
        {
            List<string> constants = new List<string>();
            string systemPrompt = RenderPromptTemplate(ouijaSystemPromptTemplate, OuijaSystemPromptResourcePath);
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                constants.Add(systemPrompt.Trim());
            }

            if (!string.IsNullOrWhiteSpace(_latestStoryContext))
            {
                constants.Add(_latestStoryContext.Trim());
            }

            _conversationState.SetConstants(constants);
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

        private int ResolveTimeoutSeconds(string modelName)
        {
            if (IsModelCold(modelName))
            {
                return Math.Max(1, coldStartTimeoutSeconds);
            }

            return Math.Max(1, warmRequestTimeoutSeconds);
        }

        private bool IsModelCold(string modelName)
        {
            if (!_lastModelResponseUtc.TryGetValue(modelName, out DateTime lastResponseUtc))
            {
                return true;
            }

            double elapsedSeconds = (DateTime.UtcNow - lastResponseUtc).TotalSeconds;
            return elapsedSeconds > Math.Max(1, keepAliveSeconds);
        }

        private void MarkModelWarm(string modelName)
        {
            _lastModelResponseUtc[modelName] = DateTime.UtcNow;
        }

        private static string ConvertKeepAliveSeconds(int seconds)
        {
            return $"{Math.Max(1, seconds)}s";
        }
    }
}
