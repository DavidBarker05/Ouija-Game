namespace OurAssets.Scripts.Chat
{
    /// <summary>
    /// Evaluates opaque condition ids referenced by gated Ouija questions (minigames, flags, inventory, etc.).
    /// Attach an implementation alongside <see cref="OuijaAiOrchestrator"/> and assign it in the inspector.
    /// </summary>
    public interface IOuijaGateConditionEvaluator
    {
        /// <returns>True if <paramref name="conditionId"/> is satisfied for the current session.</returns>
        bool IsConditionMet(string conditionId);
    }
}
