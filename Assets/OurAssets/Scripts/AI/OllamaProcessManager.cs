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

        private static bool TryStartOllamaServe(out string errorMessage)
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

                Process.Start(startInfo);
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
