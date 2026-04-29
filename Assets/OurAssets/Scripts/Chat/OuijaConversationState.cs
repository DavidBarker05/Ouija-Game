using System.Collections.Generic;
using OurAssets.Scripts.AI;

namespace OurAssets.Scripts.Chat
{
    public sealed class OuijaConversationState
    {
        private readonly List<string> _constantMessages = new List<string>();
        private readonly List<string> _playerMessages = new List<string>();
        private readonly List<string> _aiMessages = new List<string>();

        public IReadOnlyList<string> ConstantMessages => _constantMessages;
        public IReadOnlyList<string> PlayerMessages => _playerMessages;
        public IReadOnlyList<string> AiMessages => _aiMessages;

        public void Clear()
        {
            _constantMessages.Clear();
            _playerMessages.Clear();
            _aiMessages.Clear();
        }

        public void SetConstants(IEnumerable<string> constants)
        {
            _constantMessages.Clear();
            if (constants == null)
            {
                return;
            }

            foreach (string message in constants)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    _constantMessages.Add(message);
                }
            }
        }

        public void AddConstant(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _constantMessages.Add(message);
            }
        }

        public void AddPlayerMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _playerMessages.Add(message);
            }
        }

        public void AddAiMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _aiMessages.Add(message);
            }
        }

        public List<OllamaMessage> ComposeForOuijaRequest()
        {
            List<OllamaMessage> composed = new List<OllamaMessage>();

            for (int i = 0; i < _constantMessages.Count; i++)
            {
                composed.Add(new OllamaMessage("system", _constantMessages[i]));
            }

            int playerCount = _playerMessages.Count;
            int aiCount = _aiMessages.Count;
            int maxTurns = playerCount > aiCount ? playerCount : aiCount;

            for (int i = 0; i < maxTurns; i++)
            {
                if (i < playerCount)
                {
                    composed.Add(new OllamaMessage("user", _playerMessages[i]));
                }

                if (i < aiCount)
                {
                    composed.Add(new OllamaMessage("assistant", _aiMessages[i]));
                }
            }

            return composed;
        }
    }
}
