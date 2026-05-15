# Ollama / LLM Integration Plan

**Project:** Don't Forget to Say GOODBYE  
**Document version:** 1.0  
**Last updated:** May 2026  
**Author:** David Barker (GADS submission)

---

## 1. Purpose

This document describes how local large-language-model (LLM) inference is integrated into the Unity game, why **Ollama** and **Llama 3.2** were chosen, how data flows between Unity and the inference server, and how the design limits risk from hallucination, inconsistent tone, slow responses, and harmful outputs.

The goal is not open-ended chat. The LLM supports **controlled narrative generation** at session start and **short, in-character Ouija replies**, while **progress-critical facts** (names, reasons, spirit name, progression answers) are handled by **scripted gates** and **cached variables**.

---

## 2. Model choice: Ollama with Llama 3.2

### 2.1 Why Ollama

| Factor | Rationale |
|--------|-----------|
| **Local inference** | Runs on the player's machine; no API keys or cloud billing for coursework/demo builds. |
| **Simple HTTP API** | Unity calls `http://127.0.0.1:11434/api/chat` via `UnityWebRequest` (`OllamaClient.cs`). |
| **Model management** | `ollama pull llama3.2` keeps setup reproducible for moderators and teammates. |
| **Offline-capable** | Suitable for exhibition/lab machines once the model is downloaded. |
| **Process control** | `OllamaProcessManager` can probe readiness and optionally start `ollama serve` if the game launched the server. |

### 2.2 Why Llama 3.2

Llama 3.2 (8B class) is the **project model** because it balances:

- **Quality** — strong instruction-following for short prompts and JSON-style session-lore output.
- **Hardware** — feasible on student/dev PCs with CPU or modest GPU (see `setup.md` for spec placeholders).
- **Latency** — faster cold starts than larger models while still usable for 180–250 word story generation.
- **Safety tuning** — Meta's instruct variants respond well to explicit rules in system/user prompts (no harmful instructions, teen-suitable tone).

**Configuration:** `StoryModel.txt` and `OuijaModel.txt` at the project root each contain a single line (`llama3.2`). The game reads the first token from those files; inspector fallbacks on `StoryAiService` / `OuijaAiOrchestrator` match `llama3.2` if the files are missing.

```bash
ollama pull llama3.2
```

---

## 3. Local inference overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Unity Player (C#)                       │
│  GameManager → StoryAiService / OuijaAiOrchestrator              │
│       ↓                    ↓                                     │
│  OllamaGameSession (singleton, DontDestroyOnLoad)                │
│       ↓                                                          │
│  OllamaClient → UnityWebRequest POST /api/chat                   │
└────────────────────────────┬────────────────────────────────────┘
                             │ HTTP (localhost)
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│              Ollama server (ollama serve)                          │
│              http://127.0.0.1:11434                                │
│              Loaded model: llama3.2 (from StoryModel/OuijaModel) │
└─────────────────────────────────────────────────────────────────┘
```

- **No streaming** in the current build: `stream: false` on chat requests; Unity awaits the full reply.
- **`keep_alive`** (e.g. `120s`) keeps the model warm between story, Ouija, and gate-classifier calls to reduce repeat load time.
- **Unload on pause/quit** — `OllamaGameSession` can send `keep_alive: "0s"` to free VRAM when the app backgrounds or exits (behaviour depends on whether the game started Ollama).

---

## 4. Expected inference timing and performance

| Phase | Typical behaviour | Config levers |
|-------|-------------------|---------------|
| **Cold start** (first request after load) | ~15–90+ s depending on CPU/GPU and RAM | `coldStartTimeoutSeconds` (default 90) on story/Ouija services |
| **Warm request** (model still in memory) | ~2–20 s for short Ouija lines; longer for story | `warmRequestTimeoutSeconds` (default 20), `keepAliveSeconds` (120) |
| **Session lore JSON** | One chat call; small output | Same story model as narrative |
| **Story generation** | One chat call; ~180–250 words | `StoryPrompt.j2` length constraints |
| **Ouija reply** | One chat call; prompt caps at **5 words**, short letters | `OuijaSystemPrompt.j2` |
| **Gate classifier** | Optional extra call; top-K candidates only | `gateClassifierTimeoutSeconds`, low temperature (~0.25) |

**Performance considerations**

- Run Ollama with GPU acceleration when available (Ollama uses system GPU drivers automatically when supported).
- Close other heavy applications during demo; 8B models need several GB RAM/VRAM.
- Story generation runs in **StoryScene** so the player sees progress text while waiting (`StoryGeneratorScreen`).
- Gate classifier is scoped to **at most five** fuzzy candidates to limit prompt size and latency.

---

## 5. Data flow: Unity ↔ Ollama

### 5.1 Session bootstrap (`GameManager.StartNewGame`)

Executed when a new run begins (e.g. from `StoryGeneratorScreen`):

1. Clear temp cache (`TempCacheManager`).
2. Reset `StoryManager`, pick random **spirit name** (`SpiritNameManager`), assign **minigame order** (`MinigameManager`).
3. **Session lore AI** — `StoryAiService.GenerateSessionLoreAsync()` → JSON cached at `Application.temporaryCachePath/OuijaGame/session_lore.json`.
4. **Story AI** — `StoryAiService.GenerateStoryContextAsync()` → Jinja-rendered `StoryPrompt.j2` → text cached as `story_context.txt`.

### 5.2 Ouija play (`OuijaAiOrchestrator.SendPlayerMessageToOuijaAsync`)

For each player message:

1. Rebuild **constants** (system prompt + cached story context from disk).
2. **Question gate** (if enabled):
   - **Stage A — Fuzzy match** (`OuijaQuestionGateResolver`): token overlap / Jaccard-style scoring on `matchPhrases`; optional instant match above `gatedFuzzyStrongThreshold`.
   - **Stage B — Classifier** (if needed): single Ollama call with candidate ids/phrases only; expects JSON `{ "matched_id", "confidence" }`.
   - If a gate matches: return **scripted** text via `IOuijaGateResponseResolver` / inspector fallbacks — **no** addition to conversation history.
3. Otherwise: append player message to `OuijaConversationState`, POST full message list to Ollama, append AI reply, persist `ouija_conversation.json`.

### 5.3 API payload shape

Defined in `OllamaDtos.cs`:

- `model` — from `StoryModel.txt` or `OuijaModel.txt`
- `messages[]` — role/content pairs
- `stream: false`
- `keep_alive` — e.g. `"120s"`
- Optional `options` — temperature/top_p/top_k/seed **only** for the gate classifier request

---

## 6. Prompt structure and prompt engineering

Prompts are **Jinja2 templates** (`Assets/Resources/Prompts/*.j2`), rendered with **Jinja2.NET.Unity** before each request.

| Template | Role | Output |
|----------|------|--------|
| `SessionLorePrompt.j2` | Session facts generator | Strict JSON: `playerName`, `wifeName`, `wifeLeftReason`, `wifeSadReason` |
| `StoryPrompt.j2` | Opening backstory | 180–250 words; fixed location, spirits, cursed object; canon block from lore |
| `OuijaSystemPrompt.j2` | Ouija “spirit” voice | Max 5 words; no environment description; safety rules |
| Gate classifier instructions (optional `TextAsset`) | Intent routing | JSON gate id + confidence |

**Engineering principles used**

- **Separate passes** for facts vs prose vs board voice (reduces one model “making up” names mid-game).
- **Hard output shapes** where possible (JSON for lore; word count for Ouija).
- **Constants block** prepended once per Ouija session so story context is stable without re-sending the full backstory every turn in the history slots.
- **Low temperature** on the classifier for repeatable gate selection.
- **Teen-suitable tone** and explicit “no harmful instructions” lines in templates.

---

## 7. Narrative variables: generation and reuse

### 7.1 Generated once per playthrough

| Variable | Source | Storage | Reuse |
|----------|--------|---------|--------|
| `playerName`, `wifeName`, `wifeLeftReason`, `wifeSadReason` | Session lore AI | `session_lore.json` | Story prompt (canon), gated Ouija answers (`responseId`: `player_name`, `wife_name`, `wife_left_reason`, `wife_sad_reason`) |
| Opening backstory | Story AI | `story_context.txt` | Ouija constants block |
| Spirit name | Random from fixed list | `SpiritNameManager` | Cryptex solution, gated `spirit_name` |
| Minigame order | Shuffled | `MinigameManager` | Progression gates |

### 7.2 Parsed and validated

`StorySessionLoreParser` extracts JSON from model output (handles markdown fences; camelCase or snake_case keys). Incomplete fields cause generation to fail fast rather than silently continuing.

### 7.3 Progression flags

`StoryManager` tracks which story questions were “answered” via gates (`WifeLeft`, `WifeSad`, `WifeDead`, `WhereWife`). `OuijaGateConditionEvaluator` ties gates to minigame completion counts.

---

## 8. Player input checks before Ouija AI

| Layer | Mechanism | Purpose |
|-------|-----------|---------|
| **Input UI** | `OuijaPlayerInputController` | Typing, voice (Whisper), send blocking during planchette animation |
| **Empty / busy** | `CanSend()` | Blocks send while recording, transcribing, sending, or external `PushSendBlock` |
| **Question gate — fuzzy** | `OuijaQuestionGateResolver.RankGates` | Catches paraphrases of designer-authored questions before free chat |
| **Question gate — classifier** | Secondary Ollama call | Disambiguates near matches; confidence threshold + fuzzy leader gap |
| **Gate conditions** | `IOuijaGateConditionEvaluator` | Blocks or allows branches based on minigame/story state |
| **Scripted responses** | `IOuijaGateResponseResolver` | Returns exact strings from cache (no model) for critical answers |
| **Free-form path only if no gate** | Orchestrator order | General Ouija model never sees progress-critical Q&A if gate matched |

Voice path: microphone → Whisper transcription → same text pipeline as typed input (then gates apply identically).

---

## 9. Risks and mitigations

| Risk | Description | Mitigation in this project |
|------|-------------|----------------------------|
| **Hallucination** | Model invents names, plot, or answers | Session lore + gates for facts; story canon block; spirit name from code |
| **Inconsistent tone** | Board voice drifts or breaks character | Short Ouija system prompt; 5-word cap; constants include story context |
| **Slow responses** | Timeouts on weak hardware | Warm/cold timeouts; keep_alive; progress UI in StoryScene; classifier limited to top-K |
| **Harmful outputs** | Unsafe instructions, graphic grief content | Prompt rules; teen tone; gate + short replies; no open-ended “advice” design |
| **Technical failure** | Ollama down, parse errors, empty reply | Exceptions logged; gate falls through to main model or fails send; user sees status strings on mic path |
| **Wrong gate match** | Classifier mislabels intent | Fuzzy floor/ceiling, confidence minimum, fuzzy leader gap, optional lexical fallback (off by default) |
| **Conversation pollution** | Scripted answers affecting later turns | Gate replies **not** added to `OuijaConversationState` |
| **Ethical / grief themes** | Wife death, occult framing | Fiction framing; constrained prompts; progression via designed questions not free confession |

### 9.1 Ethical constraints (design intent)

- Game fiction presents grief and occult tropes; prompts avoid graphic violence, self-harm instruction, and real-world harm facilitation.
- AI dialogue is **not** presented as factual or therapeutic.
- Sensitive story beats (why she left, why she was sad) are **authored channels** (gates + lore JSON), not left to unconstrained improvisation at the board.

---

## 10. Architecture components (reference)

| Component | Responsibility |
|-----------|----------------|
| `OllamaGameSession` | Shared client, warm/cold timing, lifecycle unload |
| `OllamaClient` / `OllamaProcessManager` | HTTP chat, server probe/start |
| `StoryAiService` | Session lore + story generation + cache I/O |
| `OuijaAiOrchestrator` | Gates + Ouija chat + conversation persistence |
| `OuijaQuestionGateResolver` | Fuzzy + classifier |
| `OuijaGateResponseResolver` | Maps `responseId` to cached lore/spirit strings |
| `OuijaGameCachePaths` | Temp-file paths under `Application.temporaryCachePath` |

---

## 11. Future improvements (out of scope unless scheduled)

- Document VRAM benchmarks on target machine in `setup.md` after testing with `llama3.2`.
- Retry/backoff wrapper on `OllamaClient` with user-visible “spirit silent” fallback string.
- Cancel async generation on scene unload (`StoryGeneratorScreen`) to avoid `DontDestroyOnLoad` errors when exiting Play mode mid-request.
- Optional streaming UI for story text display.

---

*This plan should be read together with `setup.md`, `readme.md`, and `refinements-changes.md`.*
