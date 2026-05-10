using UnityEngine;

namespace OurAssets.Scripts.Chat
{
    /// <summary>
    /// Development helper: resolves every gate condition uniformly until a real gameplay evaluator exists.
    /// </summary>
    public sealed class OuijaGateConditionEvaluatorStub : MonoBehaviour, IOuijaGateConditionEvaluator
    {
        [SerializeField] private bool treatAllConditionsAsMet = true;

        public bool IsConditionMet(string conditionId)
        {
            if (string.IsNullOrWhiteSpace(conditionId))
            {
                return true;
            }

            return treatAllConditionsAsMet;
        }
    }
}
