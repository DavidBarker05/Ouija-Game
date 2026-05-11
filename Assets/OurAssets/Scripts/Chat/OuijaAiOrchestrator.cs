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
        [Header("Models")]
		[SerializeField] private string fallbackOuijaModelName = "llama3.2";

		[Header("Prompts")]
        [SerializeField] private TextAsset ouijaSystemPromptTemplate;

        [Header("Timing")]
        [SerializeField] private int keepAliveSeconds = 120;
        [SerializeField] private int warmRequestTimeoutSeconds = 20;
        [SerializeField] private int coldStartTimeoutSeconds = 90;

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
        [Tooltip(
            "If the best fuzzy score is at or above this value, a gate may match without calling the semantic classifier. " +
            "Lowering this makes fuzzy act alone more often (classifier skipped). Raising it (e.g. 0.85) keeps borderline lines for the classifier.")]
        [SerializeField, Range(0f, 1f)] private float gatedFuzzyStrongThreshold = 0.72f;
        [Tooltip(
            "When enabled, fuzzy similarity never locks a gate by itself: the semantic classifier still runs whenever the candidate pool is non-empty.")]
        [SerializeField] private bool gatedAlwaysRunSemanticClassifier = false;
        [SerializeField, Range(0f, 1f)] private float gatedFuzzyMinAiCandidateScore = 0.18f;
        [SerializeField, Range(1, 20)] private int gatedMaxClassifierCandidates = 5;
        [Tooltip(
            "Minimum model-reported confidence to accept a gate (0–1). Lower accepts more paraphrases; higher is stricter. " +
            "Fuzzy matching still limits which gates are candidates.")]
        [SerializeField, Range(0f, 1f)] private float gatedClassifierMinConfidence = 0.52f;
        [SerializeField, Range(5, 300)] private int gateClassifierTimeoutSeconds = 25;
        [Tooltip("Logs fuzzy scores per gated question/phrase and classifier candidate pool.")]
        [SerializeField] private bool enableGateDebugLogs = true;
        [Tooltip(
            "If the LLM returns empty or unusable JSON, optionally accept a gate when the player line equals a match phrase after normalization. " +
            "Off by default — the classifier is for semantic intent, not string equality.")]
        [SerializeField] private bool gateClassifierLexicalExactFallback = false;
        [Tooltip(
            "If the classifier picks a different gate than the highest fuzzy score in the candidate pool, reject when the fuzzy gap exceeds this value. " +
            "Stops location questions being mis-filed as name gates while keeping paraphrases when scores are close. 0 = off.")]
        [SerializeField, Range(0f, 0.45f)] private float gateClassifierMaxFuzzyLeaderGap = 0.14f;
        [Tooltip("Only the gate semantic classifier uses these; main Ouija/story chat is unchanged. Moderate values favor paraphrase and intent.")]
        [SerializeField, Range(0f, 2f)] private float gateClassifierTemperature = 0.25f;
        [SerializeField, Range(0f, 1f)] private float gateClassifierTopP = 0.9f;
        [Tooltip("Higher (e.g. 40) helps the model allow alternate wordings for the same intent. Very low acts like greedy decoding.")]
        [SerializeField, Range(1, 100)] private int gateClassifierTopK = 40;
        [Tooltip("Same prompt + fixed seed improves repeatability with supported backends.")]
        [SerializeField] private int gateClassifierSeed = 42;

		private string ouijaModelName;

        private OllamaGameSession _session;
        private OuijaConversationState _conversationState;
        private IOuijaGateConditionEvaluator _gateConditionEvaluator;
        private OuijaQuestionGateResolver _questionGateResolver;

		string ApplicationPath => Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')); // David - The path of the application, this is where the executable is for the build or in the project is for the editor

		public OuijaConversationState ConversationState => _conversationState;

        private const string OuijaSystemPromptResourcePath = "Prompts/OuijaSystemPrompt";

        private void Awake()
        {
            _session = OllamaGameSession.Instance;
            _conversationState = new OuijaConversationState();
            TryLoadConversationFromCache();
            GetOuijaModelName();
            CacheGateEvaluator();
            _questionGateResolver = new OuijaQuestionGateResolver(
                gatedFuzzyStrongThreshold,
                gatedFuzzyMinAiCandidateScore,
                gatedMaxClassifierCandidates,
                gatedClassifierMinConfidence,
                gateClassifierTimeoutSeconds,
                ResolveClassifierTimeoutSeconds,
                ConvertKeepAliveSeconds(keepAliveSeconds),
                enableGateDebugLogs,
                gatedAlwaysRunSemanticClassifier,
                gateClassifierLexicalExactFallback,
                gateClassifierMaxFuzzyLeaderGap,
                BuildGateClassifierInferenceOptions());
            RebuildConstants();
        }

        private int ResolveClassifierTimeoutSeconds(string modelName)
        {
            return _session.ResolveRequestTimeoutSeconds(
                modelName,
                keepAliveSeconds,
                warmRequestTimeoutSeconds,
                coldStartTimeoutSeconds);
        }

        void GetOuijaModelName() // David - Get the Ouija model name from its folder
        {
            string path = ApplicationPath;
            if (!path.EndsWith('/')) path += '/';
            string ouijaFile = path + "OuijaModel.txt";
			if (System.IO.File.Exists(ouijaFile))
			{
				string ouijaContents = System.IO.File.ReadAllText(ouijaFile);
				if (string.IsNullOrWhiteSpace(ouijaContents))
				{
					System.IO.File.WriteAllText(ouijaFile, fallbackOuijaModelName);
					ouijaModelName = fallbackOuijaModelName;
				}
				else ouijaModelName = ouijaContents.Split(' ')[0];
			}
			else
			{
				System.IO.File.WriteAllText(ouijaFile, fallbackOuijaModelName);
                ouijaModelName = fallbackOuijaModelName;
			}
		}

        private void TryLoadConversationFromCache()
        {
            if (OuijaConversationHistoryStore.TryLoad(out string[] players, out string[] ai))
            {
                _conversationState.ReplaceTranscript(players, ai);
            }

            RebuildConstants();
        }

        private void PersistConversationToCache()
        {
            OuijaConversationHistoryStore.Save(_conversationState);
        }

        public async Task<string> SendPlayerMessageToOuijaAsync(string playerMessage, CancellationToken cancellationToken = default)
        {
            await EnsureOllamaReadyAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(playerMessage))
            {
                throw new ArgumentException("Player message is empty.");
            }

            RebuildConstants();

            if (enableQuestionGate && _questionGateResolver != null)
            {
                List<OuijaQuestionGateResolver.GatedQuestionEntrySnap> gateSnapshots = CollectGateSnapshots();
                if (gateSnapshots.Count > 0)
                {
                    string classifierModel = string.IsNullOrWhiteSpace(gateClassifierModelOverride)
                        ? ouijaModelName
                        : gateClassifierModelOverride.Trim();

                    // Default ConfigureAwait:true — UnityWebRequest in OllamaClient must run on the main thread.
                    OuijaQuestionGateResolver.ResolveResult gateOutcome =
                        await _questionGateResolver.TryResolveAsync(
                            playerMessage,
                            gateSnapshots,
                            _gateConditionEvaluator,
                            _session.Client,
                            classifierModel,
                            gateClassifierInstructions != null ? gateClassifierInstructions.text : null,
                            cancellationToken);

                    if (gateOutcome.InvokedClassifier)
                    {
                        _session.MarkModelWarm(classifierModel);
                    }

                    if (gateOutcome.MatchedGate && !string.IsNullOrWhiteSpace(gateOutcome.Reply))
                    {
                        return gateOutcome.Reply.Trim();
                    }
                }
            }

            _conversationState.AddPlayerMessage(playerMessage);
            PersistConversationToCache();

            OllamaChatRequest request = new OllamaChatRequest
            {
                model = ouijaModelName,
                keep_alive = ConvertKeepAliveSeconds(keepAliveSeconds),
                stream = false,
                messages = _conversationState.ComposeForOuijaRequest()
            };

            int timeout = _session.ResolveRequestTimeoutSeconds(
                ouijaModelName,
                keepAliveSeconds,
                warmRequestTimeoutSeconds,
                coldStartTimeoutSeconds);
            if (enableRegularDebugLogs) Debug.Log($"Ouija request timeout selected: {timeout}s");

            OllamaChatResponse response = await _session.Client.SendChatAsync(request, timeout, cancellationToken);
            string aiText = response?.message?.content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(aiText))
            {
                throw new InvalidOperationException("Ouija model returned empty content.");
            }

            _conversationState.AddAiMessage(aiText);
            PersistConversationToCache();
            _session.MarkModelWarm(ouijaModelName);
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

        private Task EnsureOllamaReadyAsync(CancellationToken cancellationToken)
        {
            return _session.EnsureServerReadyAsync(cancellationToken);
        }

        private void RebuildConstants()
        {
            List<string> constants = new List<string>();
            string systemPrompt = RenderPromptTemplate(ouijaSystemPromptTemplate, OuijaSystemPromptResourcePath);
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                constants.Add(systemPrompt.Trim());
            }

            if (StoryAiService.TryReadStoryContextFromCache(out string storyFromDisk) &&
                !string.IsNullOrWhiteSpace(storyFromDisk))
            {
                constants.Add(storyFromDisk.Trim());
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

        private OllamaChatInferenceOptions BuildGateClassifierInferenceOptions()
        {
            return new OllamaChatInferenceOptions
            {
                temperature = gateClassifierTemperature,
                top_p = gateClassifierTopP,
                top_k = Mathf.Max(1, gateClassifierTopK),
                seed = gateClassifierSeed,
            };
        }

        private static string ConvertKeepAliveSeconds(int seconds)
        {
            return $"{Math.Max(1, seconds)}s";
        }
    }
}
