using System;
using System.Collections.Generic;

namespace OurAssets.Scripts.AI
{
    [Serializable]
    public class OllamaMessage
    {
        public string role;
        public string content;

        public OllamaMessage()
        {
        }

        public OllamaMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    /// <summary>
    /// Ollama <c>/api/chat</c> optional sampling parameters. Omit from JSON when null so normal chat keeps server defaults.
    /// </summary>
    [Serializable]
    public class OllamaChatInferenceOptions
    {
        /// <summary>0 = greedy / most deterministic for classification.</summary>
        public float temperature;
        public float top_p;
        /// <summary>1 = always take the top token (very stable with temperature 0).</summary>
        public int top_k;
        /// <summary>Fixed seed improves repeatability across calls for the same prompt.</summary>
        public int seed;
    }

    [Serializable]
    public class OllamaChatRequest
    {
        public string model;
        public List<OllamaMessage> messages = new List<OllamaMessage>();
        public bool stream = false;
        public string keep_alive;
        /// <summary>When set, sent as Ollama <c>options</c> (sampling). Used for gate classifier for stable routing.</summary>
        public OllamaChatInferenceOptions options;
    }

    [Serializable]
    public class OllamaChatResponse
    {
        public string model;
        public string created_at;
        public OllamaMessage message;
        public bool done;
        public string done_reason;
        public long total_duration;
        public long load_duration;
        public int prompt_eval_count;
        public long prompt_eval_duration;
        public int eval_count;
        public long eval_duration;
    }
}
