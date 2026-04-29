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

    [Serializable]
    public class OllamaChatRequest
    {
        public string model;
        public List<OllamaMessage> messages = new List<OllamaMessage>();
        public bool stream = false;
        public string keep_alive;
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
