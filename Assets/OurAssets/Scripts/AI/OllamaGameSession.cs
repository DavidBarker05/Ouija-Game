using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace OurAssets.Scripts.AI
{
    /// <summary>
    /// Single place for the Ollama <see cref="OllamaClient"/>, per-model warm/cold timing, and
    /// unload / server shutdown on app pause or quit. Story and Ouija stacks share this so model
    /// names and lifetimes stay consistent when scenes unload.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class OllamaGameSession : MonoBehaviour
    {
        private static OllamaGameSession _instance;

        public static OllamaGameSession Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                OllamaGameSession found = FindAnyObjectByType<OllamaGameSession>();
                if (found != null)
                {
                    _instance = found;
                    return _instance;
                }

                GameObject go = new GameObject(nameof(OllamaGameSession));
                DontDestroyOnLoad(go);
				_instance = go.AddComponent<OllamaGameSession>();
                return _instance;
            }
        }

        [Header("Ollama connection")]
        [SerializeField] private string ollamaBaseUrl = "http://127.0.0.1:11434";
        [SerializeField] private int ollamaStartupTimeoutSeconds = 20;
        [SerializeField] private float ollamaProbeIntervalSeconds = 0.5f;
        [SerializeField] private int unloadRequestTimeoutSeconds = 10;

        [Header("Debug")]
        [SerializeField] private bool enableRegularDebugLogs = true;

        private OllamaClient _client;

        private readonly Dictionary<string, DateTime> _lastModelResponseUtc = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        private bool _isUnloadingModels;
        private bool _isQuitting;

        public OllamaClient Client => _client;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            UnityMainThread.RegisterMainThreadContext(SynchronizationContext.Current);
            _client = new OllamaClient(ollamaBaseUrl);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (_isQuitting)
            {
                return;
            }

            if (pauseStatus)
            {
                TriggerUnloadAllModels("application paused");
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
                TriggerUnloadAllModels("application minimized/unfocused");
            }
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
            bool stoppedOwnedServer = ShutdownOwnedOllamaServer();
            if (!stoppedOwnedServer)
            {
                TriggerUnloadAllModels("application quit");
            }
            else
            {
                _lastModelResponseUtc.Clear();
            }
        }

        public void MarkModelWarm(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                return;
            }

            _lastModelResponseUtc[modelName.Trim()] = DateTime.UtcNow;
        }

        public bool IsModelCold(string modelName, int keepAliveSeconds)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                return true;
            }

            if (!_lastModelResponseUtc.TryGetValue(modelName.Trim(), out DateTime lastResponseUtc))
            {
                return true;
            }

            double elapsedSeconds = (DateTime.UtcNow - lastResponseUtc).TotalSeconds;
            return elapsedSeconds > Math.Max(1, keepAliveSeconds);
        }

        public int ResolveRequestTimeoutSeconds(
            string modelName,
            int keepAliveSecondsForHeuristic,
            int warmRequestTimeoutSeconds,
            int coldStartTimeoutSeconds)
        {
            return IsModelCold(modelName, keepAliveSecondsForHeuristic)
                ? Math.Max(1, coldStartTimeoutSeconds)
                : Math.Max(1, warmRequestTimeoutSeconds);
        }

        public async Task EnsureServerReadyAsync(CancellationToken cancellationToken = default)
        {
            OllamaProcessManager.StartupResult result = await Client.EnsureServerReadyAsync(
                ollamaStartupTimeoutSeconds,
                ollamaProbeIntervalSeconds,
                cancellationToken);

            if (!result.IsAvailable)
            {
                throw new InvalidOperationException($"Ollama is unavailable. {result.ErrorMessage}");
            }

            if (result.DidStartProcess && enableRegularDebugLogs)
            {
                Debug.Log("Ollama server was not running and has been started.");
            }
        }

        private void TriggerUnloadAllModels(string reason)
        {
            if (_isUnloadingModels)
            {
                return;
            }

            _ = UnloadAllTouchedModelsAsync(reason);
        }

        private async Task UnloadAllTouchedModelsAsync(string reason)
        {
            _isUnloadingModels = true;
            HashSet<string> distinctModels = new HashSet<string>(StringComparer.Ordinal);
            foreach (string key in _lastModelResponseUtc.Keys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    distinctModels.Add(key.Trim());
                }
            }

            _lastModelResponseUtc.Clear();

            if (enableRegularDebugLogs)
            {
                Debug.Log($"[OllamaGameSession] Unloading {distinctModels.Count} touched model(s) — {reason}.");
            }

            try
            {
                await EnsureServerReadyAsync(CancellationToken.None);
                foreach (string model in distinctModels)
                {
                    await Client.UnloadModelAsync(model, unloadRequestTimeoutSeconds, CancellationToken.None);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[OllamaGameSession] Model unload failed: {exception.Message}");
            }
            finally
            {
                _isUnloadingModels = false;
            }
        }

        private bool ShutdownOwnedOllamaServer()
        {
            if (_client == null)
            {
                return false;
            }

            bool stopped = _client.ShutdownOwnedServer(out string errorMessage);
            if (stopped)
            {
                if (enableRegularDebugLogs)
                {
                    Debug.Log("Stopped Ollama server because this game session started it.");
                }

                return true;
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                Debug.LogWarning(errorMessage);
            }

            return false;
        }
    }
}
