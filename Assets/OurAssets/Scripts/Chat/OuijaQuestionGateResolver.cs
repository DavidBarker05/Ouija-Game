using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OurAssets.Scripts.AI;
using UnityEngine;

namespace OurAssets.Scripts.Chat
{
    /// <summary>
    /// Two-stage gated question resolver: lexical similarity first, tiny single-purpose LLM classify among top candidates second.
    /// </summary>
    public sealed class OuijaQuestionGateResolver
    {
        [Serializable]
        private sealed class ClassifierEnvelope
        {
            public string matched_id;
            public float confidence;
        }

        public struct ResolveResult
        {
            public bool MatchedGate;
            public string Reply;
            public bool InvokedClassifier;
        }

        private const string DefaultClassifierPreamble =
            "You classify whether the player's line is asking essentially the SAME intent as ONE of the listed gate questions.\n" +
            "Rules:\n" +
            "- Use only semantic match; wording may differ wildly.\n" +
            "- If none fit, matched_id MUST be \"\" (empty).\n" +
            "- Confidence is 0-1 meaning how certain you are that the player asked that gate question.\n" +
            "- Output VALID JSON ONLY, one object, exactly these keys:\n" +
            "  {\"matched_id\":\"gate_id_here_or_empty\",\"confidence\":0.0}\n";

        private readonly float _fuzzyStrongThreshold;
        private readonly float _fuzzyMinAiCandidateScore;
        private readonly int _maxAiCandidates;
        private readonly float _classifierMinConfidence;
        private readonly int _classifierTimeoutSeconds;
        private readonly Func<string, int> _resolveClassifierTimeoutSeconds;
        private readonly string _keepAliveString;
        private readonly bool _debugLogs;

        public OuijaQuestionGateResolver(
            float fuzzyStrongThreshold,
            float fuzzyMinAiCandidateScore,
            int maxAiCandidates,
            float classifierMinConfidence,
            int classifierTimeoutSeconds,
            Func<string, int> resolveClassifierTimeoutSeconds,
            string keepAliveString,
            bool debugLogs)
        {
            _fuzzyStrongThreshold = Mathf.Clamp01(fuzzyStrongThreshold);
            _fuzzyMinAiCandidateScore = Mathf.Clamp01(fuzzyMinAiCandidateScore);
            _maxAiCandidates = Mathf.Max(1, maxAiCandidates);
            _classifierMinConfidence = Mathf.Clamp01(classifierMinConfidence);
            _classifierTimeoutSeconds = Mathf.Max(5, classifierTimeoutSeconds);
            _resolveClassifierTimeoutSeconds = resolveClassifierTimeoutSeconds ?? (_ => classifierTimeoutSeconds);
            _keepAliveString = keepAliveString ?? "120s";
            _debugLogs = debugLogs;
        }

        /// <returns>Matched gate preset answer or not matched.</returns>
        public async Task<ResolveResult> TryResolveAsync(
            string playerMessage,
            IReadOnlyList<GatedQuestionEntrySnap> gates,
            IOuijaGateConditionEvaluator evaluator,
            OllamaClient ollama,
            string classifierModelName,
            string optionalClassifierInstructions,
            CancellationToken cancellationToken)
        {
            string normalizedPlayer = NormalizeForMatch(playerMessage);
            if (string.IsNullOrEmpty(normalizedPlayer) || gates == null || gates.Count == 0)
            {
                return new ResolveResult { MatchedGate = false, Reply = string.Empty, InvokedClassifier = false };
            }

            List<ScoredGate> rankings = RankGates(normalizedPlayer, gates);
            if (rankings.Count == 0)
            {
                return new ResolveResult { MatchedGate = false, Reply = string.Empty, InvokedClassifier = false };
            }

            ScoredGate best = rankings[0];
            if (best.Score >= _fuzzyStrongThreshold)
            {
                if (_debugLogs)
                {
                    Debug.Log($"Gate auto-resolve '{best.Entry.QuestionId}' score={best.Score:F2}");
                }

                string replyLocal = ComposeReply(best.Entry, evaluator);
                return new ResolveResult { MatchedGate = true, Reply = replyLocal, InvokedClassifier = false };
            }

            List<ScoredGate> aiPool = rankings
                .Where(r => r.Score >= _fuzzyMinAiCandidateScore)
                .Take(_maxAiCandidates)
                .ToList();

            if (aiPool.Count == 0)
            {
                return new ResolveResult { MatchedGate = false, Reply = string.Empty, InvokedClassifier = false };
            }

            string classifiedId = await ClassifyAmongCandidatesAsync(
                    playerMessage.Trim(),
                    aiPool.Select(p => p.Entry).ToList(),
                    ollama,
                    classifierModelName,
                    string.IsNullOrWhiteSpace(optionalClassifierInstructions)
                        ? DefaultClassifierPreamble
                        : optionalClassifierInstructions.Trim() + "\n\n" + DefaultClassifierPreamble,
                    cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(classifiedId))
            {
                return new ResolveResult { MatchedGate = false, Reply = string.Empty, InvokedClassifier = true };
            }

            GatedQuestionEntrySnap chosen = gates.FirstOrDefault(g =>
                string.Equals(g.QuestionId, classifiedId, StringComparison.Ordinal));

            if (chosen == null)
            {
                return new ResolveResult { MatchedGate = false, Reply = string.Empty, InvokedClassifier = true };
            }

            if (_debugLogs)
            {
                Debug.Log($"Gate classifier picked '{chosen.QuestionId}'");
            }

            return new ResolveResult { MatchedGate = true, Reply = ComposeReply(chosen, evaluator), InvokedClassifier = true };
        }

        private sealed class ScoredGate
        {
            public GatedQuestionEntrySnap Entry;
            public float Score;
        }

        private static List<ScoredGate> RankGates(string normalizedPlayer, IReadOnlyList<GatedQuestionEntrySnap> gates)
        {
            List<ScoredGate> list = new List<ScoredGate>();
            foreach (GatedQuestionEntrySnap gate in gates)
            {
                if (!gate.Enabled || string.IsNullOrWhiteSpace(gate.QuestionId) || gate.MatchPhrases.Length == 0)
                {
                    continue;
                }

                float maxForGate = 0f;
                foreach (string phrase in gate.MatchPhrases)
                {
                    if (string.IsNullOrWhiteSpace(phrase))
                    {
                        continue;
                    }

                    string np = NormalizeForMatch(phrase);
                    if (string.IsNullOrEmpty(np))
                    {
                        continue;
                    }

                    float s = SimilarityCombined(normalizedPlayer, np);
                    maxForGate = Mathf.Max(maxForGate, s);
                }

                if (maxForGate > 0f)
                {
                    list.Add(new ScoredGate { Entry = gate, Score = maxForGate });
                }
            }

            list.Sort((a, b) => b.Score.CompareTo(a.Score));
            return list;
        }

        private static float SimilarityCombined(string normalizedA, string normalizedB)
        {
            float jacc = TokenJaccard(normalizedA, normalizedB);
            float recallInHaystack = DirectionalCoverage(normalizedB, normalizedA);
            string compactA = normalizedA.Replace(" ", string.Empty);
            string compactB = normalizedB.Replace(" ", string.Empty);
            float dice = BigramDiceCompact(compactA, compactB);
            return Mathf.Clamp01(0.42f * jacc + 0.38f * recallInHaystack + 0.2f * dice);
        }

        private static float TokenJaccard(string normalizedA, string normalizedB)
        {
            HashSet<string> tokensA = Tokenize(normalizedA);
            HashSet<string> tokensB = Tokenize(normalizedB);
            if (tokensA.Count == 0 || tokensB.Count == 0)
            {
                return 0f;
            }

            int intersect = tokensA.Intersect(tokensB).Count();
            int unionCount = tokensA.Union(tokensB).Count();
            return unionCount == 0 ? 0f : (float)intersect / unionCount;
        }

        /// <returns>fraction of reference phrase tokens appearing in the player utterance.</returns>
        private static float DirectionalCoverage(string phraseNormalized, string playerNormalized)
        {
            HashSet<string> refTokens = Tokenize(phraseNormalized);
            HashSet<string> hay = Tokenize(playerNormalized);
            if (refTokens.Count == 0)
            {
                return 0f;
            }

            int hits = refTokens.Count(hay.Contains);
            return (float)hits / refTokens.Count;
        }

        private static HashSet<string> Tokenize(string normalized)
        {
            string[] splits = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            HashSet<string> set = new HashSet<string>();
            foreach (string s in splits)
            {
                if (s.Length < 2)
                {
                    continue;
                }

                set.Add(s);
            }

            return set;
        }

        private static float BigramDiceCompact(string aa, string bb)
        {
            if (string.IsNullOrEmpty(aa) && string.IsNullOrEmpty(bb))
            {
                return 1f;
            }

            if (aa.Length < 2 && bb.Length < 2)
            {
                return string.Equals(aa, bb, StringComparison.Ordinal) ? 1f : 0f;
            }

            if (aa.Length < 2 || bb.Length < 2)
            {
                return bb.IndexOf(aa, StringComparison.Ordinal) >= 0 || aa.IndexOf(bb, StringComparison.Ordinal) >= 0 ? 0.65f : 0f;
            }

            Dictionary<string, int> countA = CountBigrams(aa);
            Dictionary<string, int> countB = CountBigrams(bb);
            int intersections = countA.Sum(kv => Mathf.Min(kv.Value, countB.TryGetValue(kv.Key, out int vB) ? vB : 0));
            int total = countA.Values.Sum() + countB.Values.Sum();
            return total == 0 ? 0f : (2f * intersections) / total;
        }

        private static Dictionary<string, int> CountBigrams(string s)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            for (int i = 0; i < s.Length - 1; i++)
            {
                string bg = s.Substring(i, 2);
                if (!counts.ContainsKey(bg))
                {
                    counts[bg] = 0;
                }

                counts[bg]++;
            }

            return counts;
        }

        private static string NormalizeForMatch(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            string lower = input.ToLowerInvariant().Trim();
            StringBuilder sb = new StringBuilder(lower.Length);
            bool lastWasSpace = false;
            foreach (char c in lower)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    lastWasSpace = false;
                }
                else
                {
                    if (!lastWasSpace && sb.Length > 0)
                    {
                        sb.Append(' ');
                    }

                    lastWasSpace = true;
                }
            }

            return Regex.Replace(sb.ToString().Trim(), @"\s+", " ");
        }

        private static string ComposeReply(GatedQuestionEntrySnap gate, IOuijaGateConditionEvaluator evaluator)
        {
            if (gate.ConditionIdsRequired == null || gate.ConditionIdsRequired.Length == 0)
            {
                return string.IsNullOrWhiteSpace(gate.ResponseWhenEligible) ? string.Empty : gate.ResponseWhenEligible.Trim();
            }

            if (evaluator == null)
            {
                return string.IsNullOrWhiteSpace(gate.ResponseWhenBlocked) ? string.Empty : gate.ResponseWhenBlocked.Trim();
            }

            foreach (string conditionId in gate.ConditionIdsRequired)
            {
                if (string.IsNullOrWhiteSpace(conditionId))
                {
                    continue;
                }

                if (!evaluator.IsConditionMet(conditionId))
                {
                    return string.IsNullOrWhiteSpace(gate.ResponseWhenBlocked) ? string.Empty : gate.ResponseWhenBlocked.Trim();
                }
            }

            return string.IsNullOrWhiteSpace(gate.ResponseWhenEligible) ? string.Empty : gate.ResponseWhenEligible.Trim();
        }

        private async Task<string> ClassifyAmongCandidatesAsync(
            string trimmedPlayerUtterance,
            IReadOnlyList<GatedQuestionEntrySnap> candidateGates,
            OllamaClient ollama,
            string classifierModelName,
            string systemText,
            CancellationToken cancellationToken)
        {
            if (candidateGates.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder candBlock = new StringBuilder();
            for (int i = 0; i < candidateGates.Count; i++)
            {
                GatedQuestionEntrySnap g = candidateGates[i];
                candBlock.AppendLine($"{i + 1}. id={g.QuestionId}");
                foreach (string p in g.MatchPhrases.Where(x => !string.IsNullOrWhiteSpace(x)).Take(3))
                {
                    candBlock.AppendLine($"   phrase: {p.Trim()}");
                }
            }

            string userPrompt =
                "Player said:\n" +
                '\"' + trimmedPlayerUtterance.Replace("\"", "'") + "\"\n\n" +
                "Candidate gate questions:\n" +
                candBlock +
                "\nReturn JSON {\"matched_id\":\"...\",\"confidence\":0.0}";

            List<OllamaMessage> msgs = new List<OllamaMessage>
            {
                new OllamaMessage("system", systemText),
                new OllamaMessage("user", userPrompt),
            };

            OllamaChatRequest requestPayload = new OllamaChatRequest
            {
                model = classifierModelName,
                messages = msgs,
                stream = false,
                keep_alive = _keepAliveString,
            };

            int timeoutSeconds = Mathf.Max(_classifierTimeoutSeconds, _resolveClassifierTimeoutSeconds.Invoke(classifierModelName));
            OllamaChatResponse response = await ollama.SendChatAsync(requestPayload, timeoutSeconds, cancellationToken)
                .ConfigureAwait(false);

            string rawContent = response?.message?.content?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(rawContent))
            {
                return string.Empty;
            }

            if (!TryParseClassifierJson(rawContent, out ClassifierEnvelope env))
            {
                if (_debugLogs)
                {
                    Debug.LogWarning($"Gate classifier parse failed on: {rawContent}");
                }

                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(env.matched_id))
            {
                return string.Empty;
            }

            if (!candidateGates.Any(g => string.Equals(g.QuestionId.Trim(), env.matched_id.Trim(), StringComparison.Ordinal)))
            {
                return string.Empty;
            }

            if (env.confidence < _classifierMinConfidence)
            {
                if (_debugLogs)
                {
                    Debug.Log($"Gate classifier low confidence '{env.confidence:F2}' for '{env.matched_id}'.");
                }

                return string.Empty;
            }

            return env.matched_id.Trim();
        }

        private static bool TryParseClassifierJson(string raw, out ClassifierEnvelope envelope)
        {
            envelope = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string stripped = StripCodeFence(raw);
            Match m = Regex.Match(stripped, @"\{[\s\S]*\}");
            if (!m.Success)
            {
                return false;
            }

            string jsonBlob = m.Value;
            try
            {
                envelope = JsonUtility.FromJson<ClassifierEnvelope>(jsonBlob);
                return envelope != null;
            }
            catch (Exception)
            {
                envelope = null;
                return false;
            }
        }

        private static string StripCodeFence(string raw)
        {
            string t = raw.Trim();
            if (t.StartsWith("```", StringComparison.Ordinal))
            {
                int nl = t.IndexOf('\n');
                if (nl >= 0)
                {
                    t = t.Substring(nl + 1);
                }

                int endFence = t.LastIndexOf("```", StringComparison.Ordinal);
                if (endFence >= 0)
                {
                    t = t.Substring(0, endFence);
                }

                return t.Trim();
            }

            return t;
        }

        /// <summary>Immutable snapshot usable by the matcher without tying it to inspector objects.</summary>
        public sealed class GatedQuestionEntrySnap
        {
            public string QuestionId;
            public bool Enabled;
            public string[] MatchPhrases = Array.Empty<string>();
            public string[] ConditionIdsRequired = Array.Empty<string>();
            public string ResponseWhenBlocked;
            public string ResponseWhenEligible;

            public static GatedQuestionEntrySnap From(OuijaGatedQuestionEntry e)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.QuestionId))
                {
                    return null;
                }

                return new GatedQuestionEntrySnap
                {
                    QuestionId = e.QuestionId.Trim(),
                    Enabled = e.Enabled,
                    MatchPhrases = e.MatchPhrases ?? Array.Empty<string>(),
                    ConditionIdsRequired = e.ConditionIdsRequired ?? Array.Empty<string>(),
                    ResponseWhenBlocked = e.ResponseWhenBlocked ?? string.Empty,
                    ResponseWhenEligible = e.ResponseWhenEligible ?? string.Empty,
                };
            }
        }
    }
}
