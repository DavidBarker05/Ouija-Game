using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OurAssets.Scripts.AI
{
    public sealed class OllamaProcessManager
    {
        private const string DefaultBaseUrl = "http://127.0.0.1:11434";
        private static readonly HttpClient HttpClient = new HttpClient();
        private Process _ownedServerProcess;

        public sealed class StartupResult
        {
            public bool IsAvailable;
            public bool DidStartProcess;
            public string ErrorMessage;
        }

        public async Task<StartupResult> EnsureServerRunningAsync(
            int startupTimeoutSeconds,
            float probeIntervalSeconds,
            CancellationToken cancellationToken = default)
        {
            if (await IsOllamaResponsiveAsync(cancellationToken))
            {
                return new StartupResult { IsAvailable = true, DidStartProcess = false };
            }

            bool started = TryStartOllamaServe(out string startError);
            if (!started)
            {
                return new StartupResult
                {
                    IsAvailable = false,
                    DidStartProcess = false,
                    ErrorMessage = startError
                };
            }

            DateTime deadline = DateTime.UtcNow.AddSeconds(Math.Max(1, startupTimeoutSeconds));
            while (DateTime.UtcNow < deadline)
            {
                if (await IsOllamaResponsiveAsync(cancellationToken))
                {
                    return new StartupResult { IsAvailable = true, DidStartProcess = true };
                }

                int delayMs = (int)(Math.Max(0.1f, probeIntervalSeconds) * 1000f);
                await Task.Delay(delayMs, cancellationToken);
            }

            return new StartupResult
            {
                IsAvailable = false,
                DidStartProcess = true,
                ErrorMessage = "Ollama was started but never became responsive before timeout."
            };
        }

        public async Task<bool> IsOllamaResponsiveAsync(CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{DefaultBaseUrl}/api/tags");

            try
            {
                using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public bool StopOwnedServer(out string errorMessage)
        {
            errorMessage = string.Empty;
            if (_ownedServerProcess == null)
            {
                return false;
            }

            try
            {
                if (_ownedServerProcess.HasExited)
                {
                    return false;
                }

                _ownedServerProcess.Kill();
                _ownedServerProcess.WaitForExit(2000);
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = $"Failed to stop owned ollama serve process: {exception.Message}";
                return false;
            }
        }

        private bool TryStartOllamaServe(out string errorMessage)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "ollama",
                    Arguments = "serve",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                _ownedServerProcess = Process.Start(startInfo);
                if (_ownedServerProcess == null)
                {
                    errorMessage = "Process.Start returned null for ollama serve.";
                    return false;
                }
                errorMessage = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = $"Failed to start ollama serve: {exception.Message}";
                return false;
            }
        }
    }
}
