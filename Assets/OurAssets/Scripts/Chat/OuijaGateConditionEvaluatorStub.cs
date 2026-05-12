using System;
using UnityEngine;

namespace OurAssets.Scripts.Chat
{
    /// <summary>
    /// Development helper: resolves every gate condition uniformly until a real gameplay evaluator exists.
    /// Optionally resolves <see cref="IOuijaGateResponseResolver"/> ids for playtesting gated response branches.
    /// </summary>
    [Serializable]
    public sealed class OuijaGateResponseOverride
    {
        public string responseId = string.Empty;
        [TextArea(1, 4)] public string responseText = string.Empty;
    }

    public sealed class OuijaGateConditionEvaluatorStub : MonoBehaviour, IOuijaGateConditionEvaluator, IOuijaGateResponseResolver
    {
        [SerializeField] private bool treatAllConditionsAsMet = true;
        [SerializeField] private OuijaGateResponseOverride[] gatedResponseOverrides = Array.Empty<OuijaGateResponseOverride>();

        public bool IsConditionMet(string conditionId)
        {
            if (string.IsNullOrWhiteSpace(conditionId))
            {
                return true;
            }

            return treatAllConditionsAsMet;
        }

        public string GetGatedResponseText(string responseId)
        {
            if (string.IsNullOrWhiteSpace(responseId) || gatedResponseOverrides == null || gatedResponseOverrides.Length == 0)
            {
                return string.Empty;
            }

            string key = responseId.Trim();
            foreach (OuijaGateResponseOverride row in gatedResponseOverrides)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.responseId))
                {
                    continue;
                }

                if (string.Equals(row.responseId.Trim(), key, StringComparison.OrdinalIgnoreCase))
                {
                    return row.responseText != null ? row.responseText.Trim() : string.Empty;
                }
            }

            return string.Empty;
        }
    }
}
