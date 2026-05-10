using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <summary>How many match phrases per gate to include in the classifier user message (more context for paraphrase / intent).</summary>
        private const int ClassifierPromptMaxPhrasesPerGate = 8;

        [Serializable]
        private sealed class ClassifierEnvelope
        {
            public string matched_id;
            public float confidence;
        }

        /// <summary>Many models return camelCase JSON; Unity JsonUtility does not map snake_case fields to these.</summary>
        [Serializable]
        private sealed class ClassifierEnvelopeCamel
        {
            public string matchedId;
            public float confidence;
        }

        public struct ResolveResult
        {
            public bool MatchedGate;
            public string Reply;
            public bool InvokedClassifier;
        }

        /*
         * =============================================================================
         * GATE CLASSIFIER — TRIAL-AND-ERROR LOG (verbatim past prompts for reports)
         * =============================================================================
         * The live strings sent to Ollama are composed at runtime: system = optional
         * gateClassifierInstructions (TextAsset) + DefaultClassifierPreamble; user =
         * player line + candidate block + footer (see ClassifyAmongCandidatesAsync).
         * Optional TextAsset in OuijaAiOrchestrator can prepend rules. Parsing fixes
         * (camelCase JSON, matched_id cleanup, etc.) are in C# code, not listed here.
         *
         * ---------- PAST DEFAULT SYSTEM PREAMBLES (word-for-word, superseded) -------
         *
         * --- v0 (first default; minimal JSON contract) ----------------------------
         * You classify whether the player's line is asking essentially the SAME intent as ONE of the listed gate questions.
         * Rules:
         * - Use only semantic match; wording may differ wildly (e.g. asking for a spirit's name matches a name gate).
         * - If none fit, matched_id MUST be "" (empty string).
         * - confidence MUST be a decimal from 0.0 to 1.0 meaning how sure you are (e.g. 0.85 = 85 percent sure). It is NOT a candidate index, NOT a row number, and NOT a percentage integer like 85 — use 0.85.
         * - matched_id MUST be copied EXACTLY from the line "id=..." in the candidate list (e.g. id=spirit_name -> "spirit_name"). Do NOT use "1" or "2" to mean first or second row — use the literal id string.
         * - Output VALID JSON ONLY, one object. Keys (snake_case):
         *   {"matched_id":"<exact id from list or empty>","confidence":0.0}
         *
         * --- v1 (v0 + anti–flip-flop line; still greedy-decoding era) ---------------
         * You classify whether the player's line is asking essentially the SAME intent as ONE of the listed gate questions.
         * Rules:
         * - Use only semantic match; wording may differ wildly (e.g. asking for a spirit's name matches a name gate).
         * - If none fit, matched_id MUST be "" (empty string).
         * - confidence MUST be a decimal from 0.0 to 1.0 meaning how sure you are (e.g. 0.85 = 85 percent sure). It is NOT a candidate index, NOT a row number, and NOT a percentage integer like 85 — use 0.85.
         * - matched_id MUST be copied EXACTLY from the line "id=..." in the candidate list (e.g. id=spirit_name -> "spirit_name"). Do NOT use "1" or "2" to mean first or second row — use the literal id string.
         * - Be consistent: the same player line (same words and intent) must map to the same matched_id every time.
         * - Output VALID JSON ONLY, one object. Keys (snake_case):
         *   {"matched_id":"<exact id from list or empty>","confidence":0.0}
         *
         * --- v2 (experiment: treat near-identical phrase as mandatory match) --------
         * You classify whether the player's line is asking essentially the SAME intent as ONE of the listed gate questions.
         * Rules:
         * - Use only semantic match; wording may differ wildly (e.g. asking for a spirit's name matches a name gate).
         * - If the player line is the same intent as (or only trivial punctuation/case differs from) one of the phrase: lines, that gate MATCHES — set matched_id to that gate's id and confidence 0.95 or higher. Never use "" in that case.
         * - If none fit, matched_id MUST be "" (empty string).
         * - confidence MUST be a decimal from 0.0 to 1.0 meaning how sure you are (e.g. 0.85 = 85 percent sure). It is NOT a candidate index, NOT a row number, and NOT a percentage integer like 85 — use 0.85.
         * - matched_id MUST be copied EXACTLY from the line "id=..." in the candidate list (e.g. id=spirit_name -> "spirit_name"). Do NOT use "1" or "2" to mean first or second row — use the literal id string.
         * - Be consistent: the same player line (same words and intent) must map to the same matched_id every time.
         * - Output VALID JSON ONLY, one object. Keys (snake_case):
         *   {"matched_id":"<exact id from list or empty>","confidence":0.0}
         *
         * --- v3 (first "intent routing" rewrite; before location / MY-vs-YOUR bullets) -
         * You are the last routing check before a free-form Ouija model answers. Your job is intent, not string equality.
         * Decide whether the player's line has the SAME conversational intent as ONE of the listed scripted gates (the phrase: lines are hints, not magic words).
         * Semantic match examples:
         * - "What is thy name?" matches a name gate whose phrases use "your" — archaic/pronoun swaps still count.
         * - Typos, contractions, and reordered words still match if the goal is the same (e.g. asking identity vs location).
         * Rules:
         * - Prefer choosing a gate when a typical player would expect the scripted reply; be generous with paraphrase.
         * - matched_id MUST be copied EXACTLY from "id=..." in the candidate list (e.g. id=spirit_name -> "spirit_name"). Never use "1" or "2" as row numbers.
         * - confidence is 0.0–1.0 for how sure you are of that intent fit. Use middling values (e.g. 0.55–0.75) when wording differs but intent is clear; reserve high values (0.9+) for obvious fits.
         * - If no candidate gate fits the intent, matched_id MUST be "".
         * - Output VALID JSON ONLY, one object (snake_case keys):
         *   {"matched_id":"<exact id from list or empty>","confidence":0.0}
         *
         * ---------- PAST USER-ROLE FOOTERS (after candidate phrase block) -----------
         *
         * --- user template u0 (older; "Candidate gate questions") -------------------
         * Player said:
         * "<player line>"
         *
         * Candidate gate questions (each line shows the gate id you must copy verbatim into matched_id):
         * <numbered id=... + phrase lines>
         *
         * Return JSON only: {"matched_id":"<exact id= value or empty>","confidence":0.0} where confidence is a float 0.0..1.0 (probability), not an index.
         *
         * --- user template u1 (current logic; INTENT in header + wording tweak) -------
         * Player said:
         * "<player line>"
         *
         * Candidate gates (phrase: lines are reference wording — match INTENT, not exact letters):
         * <numbered id=... + phrase lines>
         *
         * Return JSON only: {"matched_id":"<exact id= value or empty>","confidence":0.0} where confidence is a float 0.0..1.0 (your estimated probability of intent fit), not an index.
         *
         * ---------- SAMPLING DEFAULTS TRIED (Ollama options on classifier only) -----
         * - Tight: temperature 0, top_p 0.1, top_k 1, seed 42 (stable but sometimes
         *   brittle empty JSON or low paraphrase tolerance).
         * - Looser (current inspector defaults): temperature ~0.25, top_p ~0.9,
         *   top_k ~40; gatedClassifierMinConfidence was lowered (~0.52) so semantic
         *   near-matches were not all dropped by threshold alone.
         *
         * ---------- NON-PROMPT MITIGATIONS (for assignment “what else we tried”) -----
         * - gateClassifierMaxFuzzyLeaderGap: reject classifier if pick disagrees with
         *   top lexical score in pool by more than a margin (wrong “where” → name).
         * - ShouldRejectSelfDirectedNameToSpiritFacingGate: "What is MY name?" vs
         *   spirit-only "your name" phrases.
         * - gateClassifierLexicalExactFallback (default off): exact normalized phrase
         *   recovery when the model returns empty/broken JSON only.
         * - Archaic thy→your in NormalizeForMatch: tried, reverted (author testing).
         *
         * ---------- CURRENT DEFAULT SYSTEM PREAMBLE — v4 (verbatim; also in const) ---
         * You are the last routing check before a free-form Ouija model answers. Your job is intent, not string equality.
         * Decide whether the player's line has the SAME conversational intent as ONE of the listed scripted gates (the phrase: lines are hints, not magic words).
         * Semantic match examples:
         * - "What is thy name?" matches a name gate whose phrases use "your" — archaic/pronoun swaps still count.
         * - Typos, contractions, and reordered words still match if the goal is the same.
         * IMPORTANT — do not confuse intent categories:
         * - Questions about PLACE / LOCATION / "where" (e.g. "Where are you?", "Where is the spirit?", "Are you here?") are NOT the same as asking for a NAME or "Who are you?" unless this candidate gate's phrases are clearly about location.
         * - Name / identity / "what is your name" / "who are you" belong with identity-style gates, not location-style gates.
         * - Asking about the PLAYER's own name ("What is MY name?", "Who am I?") is NOT the same intent as asking the SPIRIT for ITS name ("your name", "who are you") unless the gate is explicitly about the player.
         * Rules:
         * - Prefer choosing a gate when a typical player would expect the scripted reply; be generous with paraphrase within the SAME intent category.
         * - matched_id MUST be copied EXACTLY from "id=..." in the candidate list (e.g. id=spirit_name -> "spirit_name").
         *   Never use "1" or "2" as row numbers.
         * - confidence is 0.0–1.0 for how sure you are of that intent fit. Use middling values (e.g. 0.55–0.75) when wording differs but intent is clear; reserve high values (0.9+) for obvious fits.
         * - If no candidate gate fits the intent, matched_id MUST be "".
         * - Output VALID JSON ONLY, one object (snake_case keys):
         *   {"matched_id":"<exact id from list or empty>","confidence":0.0}
         * =============================================================================
         */
        private const string DefaultClassifierPreamble =
            "You are the last routing check before a free-form Ouija model answers. Your job is intent, not string equality.\n" +
            "Decide whether the player's line has the SAME conversational intent as ONE of the listed scripted gates (the phrase: lines are hints, not magic words).\n" +
            "Semantic match examples:\n" +
            "- \"What is thy name?\" matches a name gate whose phrases use \"your\" — archaic/pronoun swaps still count.\n" +
            "- Typos, contractions, and reordered words still match if the goal is the same.\n" +
            "IMPORTANT — do not confuse intent categories:\n" +
            "- Questions about PLACE / LOCATION / \"where\" (e.g. \"Where are you?\", \"Where is the spirit?\", \"Are you here?\") are NOT the same as asking for a NAME or \"Who are you?\" unless this candidate gate's phrases are clearly about location.\n" +
            "- Name / identity / \"what is your name\" / \"who are you\" belong with identity-style gates, not location-style gates.\n" +
            "- Asking about the PLAYER's own name (\"What is MY name?\", \"Who am I?\") is NOT the same intent as asking the SPIRIT for ITS name (\"your name\", \"who are you\") unless the gate is explicitly about the player.\n" +
            "Rules:\n" +
            "- Prefer choosing a gate when a typical player would expect the scripted reply; be generous with paraphrase within the SAME intent category.\n" +
            "- matched_id MUST be copied EXACTLY from \"id=...\" in the candidate list (e.g. id=spirit_name -> \"spirit_name\"). " +
            "Never use \"1\" or \"2\" as row numbers.\n" +
            "- confidence is 0.0–1.0 for how sure you are of that intent fit. Use middling values (e.g. 0.55–0.75) when wording differs but intent is clear; reserve high values (0.9+) for obvious fits.\n" +
            "- If no candidate gate fits the intent, matched_id MUST be \"\".\n" +
            "- Output VALID JSON ONLY, one object (snake_case keys):\n" +
            "  {\"matched_id\":\"<exact id from list or empty>\",\"confidence\":0.0}\n";

        private readonly float _fuzzyStrongThreshold;
        private readonly float _fuzzyMinAiCandidateScore;
        private readonly int _maxAiCandidates;
        private readonly float _classifierMinConfidence;
        private readonly int _classifierTimeoutSeconds;
        private readonly Func<string, int> _resolveClassifierTimeoutSeconds;
        private readonly string _keepAliveString;
        private readonly bool _debugLogs;
        private readonly bool _skipFuzzyInstantResolve;
        private readonly bool _lexicalExactFallback;
        /// <summary>When &gt; 0: if classifier pick differs from highest-fuzzy gate in the pool by more than this score margin, reject (reduces mis-routes). 0 = off.</summary>
        private readonly float _classifierMaxFuzzyLeaderGap;
        private readonly OllamaChatInferenceOptions _classifierInferenceOptions;

        public OuijaQuestionGateResolver(
            float fuzzyStrongThreshold,
            float fuzzyMinAiCandidateScore,
            int maxAiCandidates,
            float classifierMinConfidence,
            int classifierTimeoutSeconds,
            Func<string, int> resolveClassifierTimeoutSeconds,
            string keepAliveString,
            bool debugLogs,
            bool skipFuzzyInstantResolve,
            bool lexicalExactFallback,
            float classifierMaxFuzzyLeaderGap,
            OllamaChatInferenceOptions classifierInferenceOptions)
        {
            _fuzzyStrongThreshold = Mathf.Clamp01(fuzzyStrongThreshold);
            _fuzzyMinAiCandidateScore = Mathf.Clamp01(fuzzyMinAiCandidateScore);
            _maxAiCandidates = Mathf.Max(1, maxAiCandidates);
            _classifierMinConfidence = Mathf.Clamp01(classifierMinConfidence);
            _classifierTimeoutSeconds = Mathf.Max(5, classifierTimeoutSeconds);
            _resolveClassifierTimeoutSeconds = resolveClassifierTimeoutSeconds ?? (_ => classifierTimeoutSeconds);
            _keepAliveString = keepAliveString ?? "120s";
            _debugLogs = debugLogs;
            _skipFuzzyInstantResolve = skipFuzzyInstantResolve;
            _lexicalExactFallback = lexicalExactFallback;
            _classifierMaxFuzzyLeaderGap = Mathf.Clamp01(classifierMaxFuzzyLeaderGap);
            _classifierInferenceOptions = classifierInferenceOptions ?? CreateDefaultClassifierInferenceOptions();
        }

        private static OllamaChatInferenceOptions CreateDefaultClassifierInferenceOptions()
        {
            return new OllamaChatInferenceOptions
            {
                temperature = 0.25f,
                top_p = 0.9f,
                top_k = 40,
                seed = 42,
            };
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
            if (_debugLogs)
            {
                LogFuzzyMatchBreakdown(normalizedPlayer, gates);
            }

            if (rankings.Count == 0)
            {
                return new ResolveResult { MatchedGate = false, Reply = string.Empty, InvokedClassifier = false };
            }

            ScoredGate best = rankings[0];
            bool allowFuzzyInstant = !_skipFuzzyInstantResolve && best.Score >= _fuzzyStrongThreshold;
            if (allowFuzzyInstant)
            {
                if (_debugLogs)
                {
                    Debug.Log(
                        $"[OuijaGate] Fuzzy-only gate match (semantic classifier skipped): id={best.Entry.QuestionId} " +
                        $"fuzzy={best.Score:F3} ≥ instant-threshold={_fuzzyStrongThreshold:F3}");
                }

                string replyLocal = ComposeReply(best.Entry, evaluator);
                return new ResolveResult { MatchedGate = true, Reply = replyLocal, InvokedClassifier = false };
            }

            if (_debugLogs)
            {
                if (_skipFuzzyInstantResolve)
                {
                    Debug.Log(
                        "[OuijaGate] Inspector forces semantic classifier when candidate pool is non-empty " +
                        $"(fuzzy instant match disabled). Best fuzzy={best.Score:F3}.");
                }
                else if (best.Score < _fuzzyStrongThreshold)
                {
                    Debug.Log(
                        $"[OuijaGate] Best fuzzy {best.Score:F3} below instant threshold {_fuzzyStrongThreshold:F3} — will try semantic classifier.");
                }
            }

            List<ScoredGate> aiPool = rankings
                .Where(r => r.Score >= _fuzzyMinAiCandidateScore)
                .Take(_maxAiCandidates)
                .ToList();

            if (aiPool.Count == 0)
            {
                if (_debugLogs)
                {
                    Debug.Log("[OuijaGate] No phrases above classifier floor; skipping gate classifier.");
                }

                return new ResolveResult { MatchedGate = false, Reply = string.Empty, InvokedClassifier = false };
            }

            if (_debugLogs)
            {
                string poolSummary = string.Join(
                    ", ",
                    aiPool.Select(p => $"{p.Entry.QuestionId}={p.Score:F3}"));
                Debug.Log($"[OuijaGate] Classifier candidate pool ({aiPool.Count}): {poolSummary}");
                Debug.Log("[OuijaGate] Calling Ollama for gate semantic classification…");
            }

            string classifiedId = await ClassifyAmongCandidatesAsync(
                    playerMessage.Trim(),
                    aiPool,
                    ollama,
                    classifierModelName,
                    string.IsNullOrWhiteSpace(optionalClassifierInstructions)
                        ? DefaultClassifierPreamble
                        : optionalClassifierInstructions.Trim() + "\n\n" + DefaultClassifierPreamble,
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(classifiedId))
            {
                if (_debugLogs)
                {
                    Debug.Log(
                        "[OuijaGate] Semantic classifier (gate-matching model) returned no accepted gate id " +
                        "(empty / low confidence / parse error — see earlier warnings). " +
                        "Next: main Ouija conversation model.");
                }

                return new ResolveResult { MatchedGate = false, Reply = string.Empty, InvokedClassifier = true };
            }

            GatedQuestionEntrySnap chosen = gates.FirstOrDefault(g =>
                string.Equals(g.QuestionId, classifiedId, StringComparison.Ordinal));

            if (chosen == null)
            {
                if (_debugLogs)
                {
                    Debug.LogWarning(
                        $"[OuijaGate] Semantic classifier returned id \"{classifiedId}\" not present in configured gates — check spelling vs QuestionId.");
                }

                return new ResolveResult { MatchedGate = false, Reply = string.Empty, InvokedClassifier = true };
            }

            if (_debugLogs)
            {
                Debug.Log($"[OuijaGate] Semantic classifier matched gate id '{chosen.QuestionId}' — using preset reply (main Ouija model will not run for this message).");
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

        private void LogFuzzyMatchBreakdown(string normalizedPlayer, IReadOnlyList<GatedQuestionEntrySnap> allGates)
        {
            StringBuilder sb = new StringBuilder(1024);
            sb.Append("[OuijaGate] Fuzzy similarity (player vs each match phrase)\n");
            sb.Append("  Player normalized: \"").Append(normalizedPlayer).Append("\"\n");
            sb.AppendFormat(
                CultureInvariant(),
                "  Thresholds — fuzzy-only instant match if best ≥ {0:F3} (unless forced classifier is on); pool floor ≥ {1:F3}; top {2} gates for classifier.\n",
                _fuzzyStrongThreshold,
                _fuzzyMinAiCandidateScore,
                _maxAiCandidates);
            if (_skipFuzzyInstantResolve)
            {
                sb.Append("  Forced semantic classifier: fuzzy never decides a gate alone.\n");
            }

            List<(GatedQuestionEntrySnap gate, float bestScore)> rankedForLog = CollectGateScoresForLog(normalizedPlayer, allGates);
            rankedForLog.Sort((a, b) => b.bestScore.CompareTo(a.bestScore));

            if (rankedForLog.Count == 0)
            {
                sb.Append("  (No enabled gated questions with match phrases.)");
                Debug.Log(sb.ToString());
                return;
            }

            foreach ((GatedQuestionEntrySnap gate, float bestScore) in rankedForLog)
            {
                sb.AppendFormat(CultureInvariant(), "  Gate \"{0}\" — best phrase score {1:F3}\n", gate.QuestionId, bestScore);
                foreach (string phrase in gate.MatchPhrases)
                {
                    if (string.IsNullOrWhiteSpace(phrase))
                    {
                        continue;
                    }

                    string phraseNorm = NormalizeForMatch(phrase);
                    if (string.IsNullOrEmpty(phraseNorm))
                    {
                        continue;
                    }

                    SimilarityCombinedWithParts(normalizedPlayer, phraseNorm, out float combined, out float jacc,
                        out float recall, out float dice);
                    sb.AppendFormat(
                        CultureInvariant(),
                        "    {0:F3} ← \"{1}\" | norm:\"{2}\" | jacc {3:F2}, token-cover {4:F2}, bigramDice {5:F2}\n",
                        combined,
                        TruncateForLog(phrase.Trim(), 64),
                        TruncateForLog(phraseNorm, 64),
                        jacc,
                        recall,
                        dice);
                }
            }

            Debug.Log(sb.ToString());
        }

        private static List<(GatedQuestionEntrySnap gate, float bestScore)> CollectGateScoresForLog(
            string normalizedPlayer,
            IReadOnlyList<GatedQuestionEntrySnap> allGates)
        {
            List<(GatedQuestionEntrySnap gate, float bestScore)> rows = new List<(GatedQuestionEntrySnap gate, float bestScore)>();

            foreach (GatedQuestionEntrySnap gate in allGates)
            {
                if (!gate.Enabled || string.IsNullOrWhiteSpace(gate.QuestionId) || gate.MatchPhrases.Length == 0)
                {
                    continue;
                }

                float best = 0f;
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

                    SimilarityCombinedWithParts(normalizedPlayer, np, out float combined, out _, out _, out _);
                    best = Mathf.Max(best, combined);
                }

                rows.Add((gate, best));
            }

            return rows;
        }

        private static string TruncateForLog(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text.Length <= maxChars ? text : text.Substring(0, maxChars) + "...";
        }

        /// <summary>Emits full text to the Unity console in chunks (very long <see cref="Debug.Log"/> lines can be truncated).</summary>
        private void DebugLogFullGateClassifierString(string label, string text)
        {
            if (!_debugLogs)
            {
                return;
            }

            const int maxChunk = 12000;
            if (string.IsNullOrEmpty(text))
            {
                Debug.Log($"[OuijaGate] {label}\n(empty)");
                return;
            }

            int totalParts = (text.Length + maxChunk - 1) / maxChunk;
            for (int i = 0; i < totalParts; i++)
            {
                int start = i * maxChunk;
                int len = Mathf.Min(maxChunk, text.Length - start);
                string part = text.Substring(start, len);
                string seg = totalParts > 1 ? $" (part {i + 1}/{totalParts})" : string.Empty;
                Debug.Log($"[OuijaGate] {label}{seg}\n{part}");
            }
        }

        private static IFormatProvider CultureInvariant()
        {
            return System.Globalization.CultureInfo.InvariantCulture;
        }

        private static float SimilarityCombined(string normalizedA, string normalizedB)
        {
            SimilarityCombinedWithParts(normalizedA, normalizedB, out float combined, out _, out _, out _);
            return combined;
        }

        private static void SimilarityCombinedWithParts(
            string normalizedA,
            string normalizedB,
            out float combined,
            out float jaccard,
            out float tokenCoverage,
            out float bigramDice)
        {
            jaccard = TokenJaccard(normalizedA, normalizedB);
            tokenCoverage = DirectionalCoverage(normalizedB, normalizedA);
            string compactA = normalizedA.Replace(" ", string.Empty);
            string compactB = normalizedB.Replace(" ", string.Empty);
            bigramDice = BigramDiceCompact(compactA, compactB);
            combined = Mathf.Clamp01(0.42f * jaccard + 0.38f * tokenCoverage + 0.2f * bigramDice);
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
            IReadOnlyList<ScoredGate> fuzzyRankedPool,
            OllamaClient ollama,
            string classifierModelName,
            string systemText,
            CancellationToken cancellationToken)
        {
            if (fuzzyRankedPool == null || fuzzyRankedPool.Count == 0)
            {
                return string.Empty;
            }

            IReadOnlyList<GatedQuestionEntrySnap> candidateGates = fuzzyRankedPool.Select(p => p.Entry).ToList();

            StringBuilder candBlock = new StringBuilder();
            for (int i = 0; i < candidateGates.Count; i++)
            {
                GatedQuestionEntrySnap g = candidateGates[i];
                candBlock.AppendLine($"{i + 1}. id={g.QuestionId}");
                foreach (string p in g.MatchPhrases.Where(x => !string.IsNullOrWhiteSpace(x)).Take(ClassifierPromptMaxPhrasesPerGate))
                {
                    candBlock.AppendLine($"   phrase: {p.Trim()}");
                }
            }

            string userPrompt =
                "Player said:\n" +
                '\"' + trimmedPlayerUtterance.Replace("\"", "'") + "\"\n\n" +
                "Candidate gates (phrase: lines are reference wording — match INTENT, not exact letters):\n" +
                candBlock +
                "\nReturn JSON only: {\"matched_id\":\"<exact id= value or empty>\",\"confidence\":0.0} " +
                "where confidence is a float 0.0..1.0 (your estimated probability of intent fit), not an index.\n";

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
                options = _classifierInferenceOptions,
            };

            if (_debugLogs)
            {
                OllamaChatInferenceOptions opt = _classifierInferenceOptions;
                Debug.Log(
                    "[OuijaGate] Classifier request meta — model=\"" + classifierModelName + "\", keep_alive=\"" +
                    _keepAliveString + "\", options: temperature=" + opt.temperature.ToString(CultureInfo.InvariantCulture) +
                    ", top_p=" + opt.top_p.ToString(CultureInfo.InvariantCulture) + ", top_k=" + opt.top_k + ", seed=" + opt.seed);
                DebugLogFullGateClassifierString("Classifier LLM — FULL system message", systemText);
                DebugLogFullGateClassifierString("Classifier LLM — FULL user message", userPrompt);
            }

            int timeoutSeconds = Mathf.Max(_classifierTimeoutSeconds, _resolveClassifierTimeoutSeconds.Invoke(classifierModelName));
            OllamaChatResponse response = await ollama.SendChatAsync(requestPayload, timeoutSeconds, cancellationToken);

            string rawContent = response?.message?.content?.Trim() ?? string.Empty;
            if (_debugLogs)
            {
                DebugLogFullGateClassifierString("Classifier LLM — FULL raw assistant content", rawContent);
                if (string.IsNullOrEmpty(rawContent))
                {
                    Debug.LogWarning("[OuijaGate] Classifier returned empty assistant content (nothing to parse).");
                }
            }

            if (string.IsNullOrEmpty(rawContent))
            {
                if (_lexicalExactFallback &&
                    TryGetLexicalOverrideGateId(trimmedPlayerUtterance, candidateGates, "model returned empty content", out string gidEmpty))
                {
                    return gidEmpty;
                }

                return string.Empty;
            }

            if (!TryParseClassifierResponse(rawContent, out ClassifierEnvelope env, out string parseDetail))
            {
                if (_debugLogs)
                {
                    Debug.LogWarning(
                        "[OuijaGate] Gate classifier JSON could not be parsed (model may use different keys than matched_id). " +
                        $"Detail: {parseDetail}. Full raw content was logged above as \"Classifier LLM — FULL raw assistant content\".");
                }

                if (_lexicalExactFallback &&
                    TryGetLexicalOverrideGateId(trimmedPlayerUtterance, candidateGates, "JSON parse failed", out string gidParse))
                {
                    return gidParse;
                }

                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(env.matched_id))
            {
                if (_debugLogs)
                {
                    Debug.Log("[OuijaGate] Semantic classifier returned empty matched_id (no gate) — expected for unrelated questions.");
                }

                if (_lexicalExactFallback &&
                    TryGetLexicalOverrideGateId(trimmedPlayerUtterance, candidateGates, "model returned empty matched_id", out string gidNullId))
                {
                    return gidNullId;
                }

                return string.Empty;
            }

            string normalizedId = NormalizeClassifierMatchedId(env.matched_id);
            if (normalizedId != env.matched_id.Trim() && _debugLogs)
            {
                Debug.Log(
                    $"[OuijaGate] Normalized classifier matched_id from \"{env.matched_id.Trim()}\" to \"{normalizedId}\".");
            }

            if (!TryResolveClassifierGateSelection(
                    normalizedId,
                    candidateGates,
                    out string resolvedGateId,
                    out bool usedNumericIndex,
                    out string resolveFailReason))
            {
                if (_debugLogs)
                {
                    Debug.LogWarning(
                        $"[OuijaGate] Classifier output \"{normalizedId}\" (raw \"{env.matched_id.Trim()}\") could not be mapped to a gate id. {resolveFailReason} " +
                        "Reminder: use the exact text after id=, not the row number (1,2,…).");
                }

                if (_lexicalExactFallback &&
                    TryGetLexicalOverrideGateId(trimmedPlayerUtterance, candidateGates, "classifier id could not be resolved", out string gidResolve))
                {
                    return gidResolve;
                }

                return string.Empty;
            }

            if (usedNumericIndex && _debugLogs)
            {
                Debug.Log(
                    $"[OuijaGate] Classifier returned \"{normalizedId}\" — interpreted as list index, mapped to gate id \"{resolvedGateId}\".");
            }

            env.matched_id = resolvedGateId;

            if (Mathf.Approximately(env.confidence, 0f) && !string.IsNullOrWhiteSpace(env.matched_id))
            {
                env.confidence = _classifierMinConfidence;
                if (_debugLogs)
                {
                    Debug.Log(
                        "[OuijaGate] Classifier reported confidence 0.0; models often misuse 0 or omit a 0-1 probability. " +
                        $"Using floor={env.confidence:F2} so a matching gate id can still count when intent is right.");
                }
            }

            if (env.confidence < _classifierMinConfidence)
            {
                if (_debugLogs)
                {
                    Debug.Log(
                        $"[OuijaGate] Semantic classifier low confidence ({env.confidence:F2} < {_classifierMinConfidence:F2}) for id '{env.matched_id}' — treating as no gate match.");
                }

                if (_lexicalExactFallback &&
                    TryGetLexicalOverrideGateId(trimmedPlayerUtterance, candidateGates, "classifier confidence below minimum", out string gidLowConf))
                {
                    return gidLowConf;
                }

                return string.Empty;
            }

            GatedQuestionEntrySnap chosenForPronounCheck =
                candidateGates.FirstOrDefault(g => string.Equals(g.QuestionId, env.matched_id.Trim(), StringComparison.Ordinal));
            if (chosenForPronounCheck != null &&
                ShouldRejectSelfDirectedNameToSpiritFacingGate(trimmedPlayerUtterance, chosenForPronounCheck))
            {
                if (_debugLogs)
                {
                    Debug.Log(
                        "[OuijaGate] Classifier pick rejected — line asks about the human's own name (my/I), " +
                        "but this gate's phrases only ask the spirit (your/you) for identity.");
                }

                return string.Empty;
            }

            if (!TryValidateClassifierAgainstFuzzyLeader(env.matched_id.Trim(), fuzzyRankedPool, out string fuzzyRejectReason))
            {
                if (_debugLogs)
                {
                    Debug.Log(
                        "[OuijaGate] Classifier pick rejected — " + fuzzyRejectReason +
                        " (raise gateClassifierMaxFuzzyLeaderGap or set to 0 to disable).");
                }

                return string.Empty;
            }

            if (_debugLogs)
            {
                Debug.Log(
                    $"[OuijaGate] Semantic classifier accepted id \"{env.matched_id.Trim()}\" (confidence {env.confidence:F2}).");
            }

            return env.matched_id.Trim();
        }

        /// <summary>
        /// Blocks routing self-directed name questions ("What is MY name?") into gates whose phrases only ask the entity for THEIR
        /// name ("your name", "who are you") — distinct intents in most Ouija setups.
        /// </summary>
        private static bool ShouldRejectSelfDirectedNameToSpiritFacingGate(
            string trimmedPlayerUtterance,
            GatedQuestionEntrySnap gate)
        {
            if (gate?.MatchPhrases == null || gate.MatchPhrases.Length == 0)
            {
                return false;
            }

            string np = NormalizeForMatch(trimmedPlayerUtterance);
            if (string.IsNullOrEmpty(np))
            {
                return false;
            }

            bool playerAsksOwnIdentity = Regex.IsMatch(
                np,
                @"\b(my\s+name|who\s+am\s+i|what\s+am\s+i\s+called)\b",
                RegexOptions.IgnoreCase);
            if (!playerAsksOwnIdentity)
            {
                return false;
            }

            bool playerAlsoAsksSpiritIdentity =
                Regex.IsMatch(np, @"\b(your|thy)\s+name\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(np, @"\bwho\s+are\s+you\b", RegexOptions.IgnoreCase);
            if (playerAlsoAsksSpiritIdentity)
            {
                return false;
            }

            foreach (string p in gate.MatchPhrases)
            {
                if (string.IsNullOrWhiteSpace(p))
                {
                    continue;
                }

                string n = NormalizeForMatch(p);
                if (Regex.IsMatch(n, @"\bmy\s+name\b", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(n, @"\bwho\s+am\s+i\b", RegexOptions.IgnoreCase))
                {
                    return false;
                }
            }

            foreach (string p in gate.MatchPhrases)
            {
                if (string.IsNullOrWhiteSpace(p))
                {
                    continue;
                }

                string n = NormalizeForMatch(p);
                if (Regex.IsMatch(n, @"\b(your|thy)\s+name\b", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(n, @"\bwho\s+are\s+you\b", RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// When the classifier picks a different gate than the pool's lexical leader, reject if the fuzzy-score gap is too large.
        /// Reduces false positives (e.g. location phrasing routed to a name gate). Disabled when <paramref name="pool"/> empty or max gap is 0.
        /// </summary>
        private bool TryValidateClassifierAgainstFuzzyLeader(string chosenGateId, IReadOnlyList<ScoredGate> pool, out string detail)
        {
            detail = string.Empty;
            if (_classifierMaxFuzzyLeaderGap <= 0f || pool == null || pool.Count == 0)
            {
                return true;
            }

            ScoredGate top = pool[0];
            ScoredGate pick = null;
            foreach (ScoredGate sg in pool)
            {
                if (string.Equals(sg.Entry.QuestionId.Trim(), chosenGateId.Trim(), StringComparison.Ordinal))
                {
                    pick = sg;
                    break;
                }
            }

            if (pick == null)
            {
                return true;
            }

            if (string.Equals(top.Entry.QuestionId.Trim(), chosenGateId.Trim(), StringComparison.Ordinal))
            {
                return true;
            }

            float gap = top.Score - pick.Score;
            if (gap > _classifierMaxFuzzyLeaderGap)
            {
                detail =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "fuzzy leader \"{0}\" ({1:F3}) vs classifier choice \"{2}\" ({3:F3}), gap {4:F3} > max {5:F3}",
                        top.Entry.QuestionId.Trim(),
                        top.Score,
                        chosenGateId.Trim(),
                        pick.Score,
                        gap,
                        _classifierMaxFuzzyLeaderGap);
                return false;
            }

            return true;
        }

        /// <summary>
        /// When the LLM returns empty/wrong output but the player line matches a configured phrase (same normalization as fuzzy matching)
        /// for exactly one gate in the current pool, use that gate. Compares all match phrases per gate, not only the three copied into the LLM prompt.
        /// </summary>
        private bool TryGetLexicalOverrideGateId(
            string trimmedPlayerUtterance,
            IReadOnlyList<GatedQuestionEntrySnap> candidateGates,
            string reason,
            out string gateId)
        {
            gateId = string.Empty;
            if (!TryUnambiguousNormalizedPhraseMatch(trimmedPlayerUtterance, candidateGates, out string id))
            {
                return false;
            }

            gateId = id;
            if (_debugLogs)
            {
                Debug.Log(
                    "[OuijaGate] Lexical phrase override (" + reason + "): normalized player text equals a matchPhrase for a single gate — using id \"" +
                    gateId + "\".");
            }

            return true;
        }

        private static bool TryUnambiguousNormalizedPhraseMatch(
            string trimmedPlayerUtterance,
            IReadOnlyList<GatedQuestionEntrySnap> candidateGates,
            out string gateId)
        {
            gateId = string.Empty;
            string np = NormalizeForMatch(trimmedPlayerUtterance);
            if (string.IsNullOrEmpty(np))
            {
                return false;
            }

            string winnerId = null;
            foreach (GatedQuestionEntrySnap g in candidateGates)
            {
                if (g.MatchPhrases == null)
                {
                    continue;
                }

                foreach (string phrase in g.MatchPhrases)
                {
                    if (string.IsNullOrWhiteSpace(phrase))
                    {
                        continue;
                    }

                    string nph = NormalizeForMatch(phrase);
                    if (!string.Equals(nph, np, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string id = g.QuestionId.Trim();
                    if (winnerId != null && !string.Equals(winnerId, id, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    winnerId = id;
                }
            }

            if (string.IsNullOrEmpty(winnerId))
            {
                return false;
            }

            gateId = winnerId;
            return true;
        }

        /// <summary>
        /// Models often paste the prompt line into JSON, e.g. matched_id "id=spirit_name" instead of "spirit_name".
        /// </summary>
        private static string NormalizeClassifierMatchedId(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            string t = raw.Trim().Trim('"', '\'');

            // Strip leading "1. " list numbering if copied from the candidate block
            t = Regex.Replace(t, @"^\d+\.\s*", string.Empty);

            // Repeatedly strip id= prefix (handles "id = x" variants)
            for (int guard = 0; guard < 3; guard++)
            {
                if (t.StartsWith("id=", StringComparison.OrdinalIgnoreCase))
                {
                    t = t.Substring(3).Trim();
                    continue;
                }

                if (t.StartsWith("id =", StringComparison.OrdinalIgnoreCase))
                {
                    t = t.Substring(4).Trim();
                    continue;
                }

                break;
            }

            return t.Trim().Trim('"', '\'');
        }

        /// <summary>
        /// Maps model output to a configured QuestionId: exact string first, then 1-based row index, then 0-based index.
        /// </summary>
        private static bool TryResolveClassifierGateSelection(
            string rawMatchedFromModel,
            IReadOnlyList<GatedQuestionEntrySnap> candidateGates,
            out string resolvedGateId,
            out bool usedNumericIndexMapping,
            out string failReason)
        {
            resolvedGateId = string.Empty;
            usedNumericIndexMapping = false;
            failReason = string.Empty;

            if (candidateGates == null || candidateGates.Count == 0)
            {
                failReason = "no candidates.";
                return false;
            }

            string t = NormalizeClassifierMatchedId(rawMatchedFromModel);

            foreach (GatedQuestionEntrySnap g in candidateGates)
            {
                if (string.Equals(g.QuestionId.Trim(), t, StringComparison.Ordinal))
                {
                    resolvedGateId = g.QuestionId.Trim();
                    return true;
                }
            }

            if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out int asIndex))
            {
                if (asIndex >= 1 && asIndex <= candidateGates.Count)
                {
                    resolvedGateId = candidateGates[asIndex - 1].QuestionId.Trim();
                    usedNumericIndexMapping = true;
                    return true;
                }

                if (asIndex >= 0 && asIndex < candidateGates.Count)
                {
                    resolvedGateId = candidateGates[asIndex].QuestionId.Trim();
                    usedNumericIndexMapping = true;
                    return true;
                }
            }

            failReason = "Not equal to any QuestionId and not a valid row index for this candidate list.";
            return false;
        }

        /// <summary>
        /// JsonUtility only binds identical field names; models often emit camelCase or extra text.
        /// </summary>
        private static bool TryParseClassifierResponse(string raw, out ClassifierEnvelope envelope, out string detail)
        {
            envelope = new ClassifierEnvelope();
            detail = string.Empty;

            if (string.IsNullOrWhiteSpace(raw))
            {
                detail = "empty model content";
                return false;
            }

            string stripped = StripCodeFence(raw);
            Match m = Regex.Match(stripped, @"\{[\s\S]*\}");
            if (!m.Success)
            {
                detail = "no JSON object found in model output";
                return false;
            }

            string jsonBlob = m.Value;

            ClassifierEnvelope snake = null;
            try
            {
                snake = JsonUtility.FromJson<ClassifierEnvelope>(jsonBlob);
            }
            catch (Exception e)
            {
                detail = $"JsonUtility snake: {e.Message}";
            }

            ClassifierEnvelopeCamel camel = null;
            try
            {
                camel = JsonUtility.FromJson<ClassifierEnvelopeCamel>(jsonBlob);
            }
            catch
            {
                // ignored
            }

            bool snakeHadId = snake != null && !string.IsNullOrWhiteSpace(snake.matched_id);
            bool camelHadId = camel != null && !string.IsNullOrWhiteSpace(camel?.matchedId);

            if (snakeHadId)
            {
                envelope.matched_id = snake.matched_id.Trim();
                envelope.confidence = snake.confidence;
                detail = "parsed matched_id + confidence (snake_case JSON)";
                return true;
            }

            if (camelHadId)
            {
                envelope.matched_id = camel.matchedId.Trim();
                envelope.confidence = camel.confidence;
                detail = "parsed matchedId + confidence (camelCase JSON)";
                return true;
            }

            ExtractClassifierFieldsWithRegex(stripped, envelope, out bool regexNonEmptyId);
            if (regexNonEmptyId)
            {
                detail = "parsed matched id / confidence via regex fallback (model JSON shape not Unity-friendly)";
                return true;
            }

            if (TryExtractNumericMatchedId(stripped, envelope))
            {
                detail = "parsed numeric matched_id / matchedId (unquoted number in JSON)";
                return true;
            }

            if (HasExplicitEmptyMatchedIdInJson(stripped))
            {
                envelope.matched_id = string.Empty;
                envelope.confidence = snake?.confidence ?? camel?.confidence ?? 0f;
                TryFillConfidenceFromRegex(stripped, envelope);
                detail = "explicit empty matched id (model says no gate)";
                return true;
            }

            detail = string.IsNullOrEmpty(detail) ? "could not read matched_id, matchedId, or parseable JSON" : detail;
            return false;
        }

        private static bool HasExplicitEmptyMatchedIdInJson(string text)
        {
            return Regex.IsMatch(text, @"""(?:matched_id|matchedId)""\s*:\s*""""", RegexOptions.IgnoreCase);
        }

        private static bool TryExtractNumericMatchedId(string source, ClassifierEnvelope target)
        {
            Match mm = Regex.Match(source, @"""(?:matched_id|matchedId)""\s*:\s*(\d+)", RegexOptions.IgnoreCase);
            if (!mm.Success)
            {
                return false;
            }

            target.matched_id = mm.Groups[1].Value.Trim();
            TryFillConfidenceFromRegex(source, target);
            if (target.confidence <= 0f)
            {
                target.confidence = 1f;
            }

            return true;
        }

        private static void TryFillConfidenceFromRegex(string source, ClassifierEnvelope target)
        {
            Match confMatch = Regex.Match(
                source,
                @"""confidence""\s*:\s*([0-9]+\.?[0-9]*(?:[eE][-+]?[0-9]+)?)",
                RegexOptions.IgnoreCase);
            if (confMatch.Success &&
                float.TryParse(
                    confMatch.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float c))
            {
                target.confidence = c;
            }
        }

        private static void ExtractClassifierFieldsWithRegex(
            string fullText,
            ClassifierEnvelope target,
            out bool foundNonEmptyId)
        {
            foundNonEmptyId = false;
            target.matched_id = string.Empty;
            target.confidence = 0f;

            string source = fullText;
            string[] patterns =
            {
                @"""matched_id""\s*:\s*""([^""]*)""",
                @"""matchedId""\s*:\s*""([^""]*)""",
                @"""matched_id""\s*:\s*'([^']*)'",
                @"'matched_id'\s*:\s*""([^""]*)""",
            };

            foreach (string pattern in patterns)
            {
                Match mm = Regex.Match(source, pattern, RegexOptions.IgnoreCase);
                if (mm.Success)
                {
                    target.matched_id = mm.Groups[1].Value.Trim();
                    foundNonEmptyId = !string.IsNullOrWhiteSpace(target.matched_id);
                    break;
                }
            }

            TryFillConfidenceFromRegex(source, target);
            if (foundNonEmptyId && target.confidence <= 0f)
            {
                target.confidence = 1f;
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
