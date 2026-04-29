using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace OurAssets.Scripts.AI
{
    public sealed class OllamaClient
    {
        [Serializable]
        private sealed class OllamaUnloadRequest
        {
            public string model;
            public string prompt = string.Empty;
            public bool stream = false;
            public string keep_alive = "0s";
        }

        private readonly string _baseUrl;

        public OllamaClient(string baseUrl = "http://127.0.0.1:11434")
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public async Task<OllamaChatResponse> SendChatAsync(
            OllamaChatRequest requestPayload,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            string url = $"{_baseUrl}/api/chat";
            string json = JsonUtility.ToJson(requestPayload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(bodyRaw),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Math.Max(1, timeoutSeconds)
            };

            request.SetRequestHeader("Content-Type", "application/json");

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException(
                    $"Ollama request failed ({request.responseCode}): {request.error}. Body: {request.downloadHandler.text}");
            }

            string responseText = request.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new InvalidOperationException("Ollama returned an empty response body.");
            }

            OllamaChatResponse parsed = JsonUtility.FromJson<OllamaChatResponse>(responseText);
            if (parsed == null)
            {
                throw new InvalidOperationException("Failed to parse Ollama response JSON.");
            }

            return parsed;
        }

        public async Task UnloadModelAsync(string modelName, int timeoutSeconds, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                return;
            }

            string url = $"{_baseUrl}/api/generate";
            OllamaUnloadRequest unloadPayload = new OllamaUnloadRequest
            {
                model = modelName.Trim()
            };

            string json = JsonUtility.ToJson(unloadPayload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(bodyRaw),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Math.Max(1, timeoutSeconds)
            };

            request.SetRequestHeader("Content-Type", "application/json");

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException(
                    $"Ollama unload failed for {modelName} ({request.responseCode}): {request.error}. Body: {request.downloadHandler.text}");
            }
        }
    }
}
