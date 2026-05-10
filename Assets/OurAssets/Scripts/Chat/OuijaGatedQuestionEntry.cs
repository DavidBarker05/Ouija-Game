using System;
using UnityEngine;

namespace OurAssets.Scripts.Chat
{
    /// <summary>
    /// Describes one special-case question routed to scripted answers instead of the main conversation model.
    /// Player messages that match never touch <see cref="OuijaConversationState"/>.
    /// </summary>
    [Serializable]
    public sealed class OuijaGatedQuestionEntry
    {
        [Tooltip("Stable id referenced by AI classification and logs. Lowercase alphanumeric + underscores recommended.")]
        [SerializeField] private string questionId;

        [Tooltip("If false, entry is skipped entirely.")]
        [SerializeField] private bool enabled = true;

        [Tooltip("Phrases canonical questions similar to — used for fuzzy matching and shown to classifier.")]
        [SerializeField] private string[] matchPhrases = Array.Empty<string>();

        [Tooltip("All listed condition ids must be met (AND) for the eligible response. Leave empty — always eligible branch.")]
        [SerializeField] private string[] conditionIdsRequired = Array.Empty<string>();

        [SerializeField] private string responseWhenBlocked = string.Empty;
        [SerializeField] private string responseWhenEligible = string.Empty;

        public string QuestionId => questionId;
        public bool Enabled => enabled;
        public string[] MatchPhrases => matchPhrases;
        public string[] ConditionIdsRequired => conditionIdsRequired;
        public string ResponseWhenBlocked => responseWhenBlocked;
        public string ResponseWhenEligible => responseWhenEligible;
    }
}
