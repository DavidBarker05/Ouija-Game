using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace OurAssets.Scripts.AI
{
    /// <summary>
    /// Ensures UnityEngine networking objects are created on the main thread after awaits that may resume on the thread pool
    /// (e.g. HttpClient / Ollama readiness probes).
    /// </summary>
    public static class UnityMainThread
    {
        private static SynchronizationContext _capturedContext;

        /// <summary>Prefer capturing from a MonoBehaviour Awake on the main thread; falls back to first OllamaClient construction.</summary>
        public static void RegisterMainThreadContext(SynchronizationContext context)
        {
            if (context != null)
            {
                _capturedContext = context;
            }
        }

        internal static void TryRegisterFromCurrentSynchronizationContext()
        {
            if (_capturedContext != null)
            {
                return;
            }

            _capturedContext = SynchronizationContext.Current;
        }

        public static Task SwitchToMainThreadAsync()
        {
            TryRegisterFromCurrentSynchronizationContext();

            if (_capturedContext == null)
            {
                Debug.LogWarning(
                    "UnityMainThread: no main-thread SynchronizationContext registered; if this runs after an await, UnityWebRequest may fail.");
                return Task.CompletedTask;
            }

            if (SynchronizationContext.Current == _capturedContext)
            {
                return Task.CompletedTask;
            }

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            _capturedContext.Post(
                _ => tcs.TrySetResult(true),
                null);
            return tcs.Task;
        }
    }
}
