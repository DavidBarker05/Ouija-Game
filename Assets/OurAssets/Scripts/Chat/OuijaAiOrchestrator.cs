using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        [SerializeField] private string storyModelName = "llama3.2";
        [SerializeField] private string ouijaModelName = "llama3.2";

        [Header("Prompts")]
        [TextArea(2, 6)]
        [SerializeField] private string ouijaSystemPrompt;
        [TextArea(2, 6)]
        [SerializeField] private string storyPromptInput;

        [Header("Timing")]
        [SerializeField] private int keepAliveSeconds = 120;
        [SerializeField] private int warmRequestTimeoutSeconds = 20;
        [SerializeField] private int coldStartTimeoutSeconds = 90;
        [SerializeField] private int unloadRequestTimeoutSeconds = 10;

        private readonly Dictionary<string, DateTime> _lastModelResponseUtc = new Dictionary<string, DateTime>();
        private readonly SemaphoreSlim _startupLock = new SemaphoreSlim(1, 1);

        private OllamaClient _ollamaClient;
        private OllamaProcessManager _processManager;
        private OuijaConversationState _conversationState;
        private string _latestStoryContext;
        private bool _startupValidated;
        private bool _isUnloadingModels;

        public OuijaConversationState ConversationState => _conversationState;

        private void Awake()
        {
            _processManager = new OllamaProcessManager();
            _ollamaClient = new OllamaClient(ollamaBaseUrl);
            _conversationState = new OuijaConversationState();
            RebuildConstants();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                TriggerModelUnload("application paused");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                TriggerModelUnload("application minimized/unfocused");
            }
        }

        private void OnApplicationQuit()
        {
            TriggerModelUnload("application quit");
        }

        [ContextMenu("Generate Story Context")]
        public async void GenerateStoryContextFromInspector()
        {
            try
            {
                string story = await GenerateStoryContextAsync(storyPromptInput, CancellationToken.None);
                Debug.Log(story); // David - Added for debugging
                Debug.Log($"Story context updated. Characters: {story.Length}");
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

            _conversationState.AddPlayerMessage(playerMessage);

            OllamaChatRequest request = new OllamaChatRequest
            {
                model = ouijaModelName,
                keep_alive = ConvertKeepAliveSeconds(keepAliveSeconds),
                stream = false,
                messages = _conversationState.ComposeForOuijaRequest()
            };

            int timeout = ResolveTimeoutSeconds(ouijaModelName);
            Debug.Log($"Ouija request timeout selected: {timeout}s");

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

        private async Task EnsureOllamaReadyAsync(CancellationToken cancellationToken)
        {
            if (_startupValidated)
            {
                return;
            }

            await _startupLock.WaitAsync(cancellationToken);
            try
            {
                if (_startupValidated)
                {
                    return;
                }

                OllamaProcessManager.StartupResult result = await _processManager.EnsureServerRunningAsync(
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
                    Debug.Log("Ollama server was not running and has been started.");
                }
                else
                {
                    Debug.Log("Ollama server is already running.");
                }

                _startupValidated = true;
            }
            finally
            {
                _startupLock.Release();
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
            Debug.Log($"Forcing models cold due to {reason}.");

            try
            {
                await EnsureOllamaReadyAsync(CancellationToken.None);

                await _ollamaClient.UnloadModelAsync(storyModelName, unloadRequestTimeoutSeconds, CancellationToken.None);
                await _ollamaClient.UnloadModelAsync(ouijaModelName, unloadRequestTimeoutSeconds, CancellationToken.None);
                Debug.Log("Requested Ollama to unload story and ouija models from memory.");
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

        private void RebuildConstants()
        {
            List<string> constants = new List<string>();
            if (!string.IsNullOrWhiteSpace(ouijaSystemPrompt))
            {
                constants.Add(ouijaSystemPrompt.Trim());
            }

            if (!string.IsNullOrWhiteSpace(_latestStoryContext))
            {
                constants.Add(_latestStoryContext.Trim());
            }

            _conversationState.SetConstants(constants);
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
