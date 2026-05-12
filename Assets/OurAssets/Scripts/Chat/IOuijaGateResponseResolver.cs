namespace OurAssets.Scripts.Chat
{
    /// <summary>
    /// Turns optional <see cref="OuijaGatedQuestionEntry"/> response ids into the line spoken for that gate branch.
    /// Use for story-dependent or runtime-built text; return null or whitespace to use the inspector fallback strings instead.
    /// </summary>
    public interface IOuijaGateResponseResolver
    {
        /// <summary>
        /// Lookup for <paramref name="responseId"/> (trimmed by the gate resolver before call). Whitespace ids are never sent.
        /// </summary>
        /// <returns>Resolved line, or null/whitespace to fall back to responseWhenBlocked / responseWhenEligible in the inspector.</returns>
        string GetGatedResponseText(string responseId);
    }
}
