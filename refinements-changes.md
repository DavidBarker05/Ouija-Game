# Refinements and Changes

This file tracks the Unity + Ollama integration work completed so far.

## Completed so far

- Added `Assets/OurAssets/Scripts/AI/OllamaProcessManager.cs`
  - Date: 29/04/2026
  - AI assisted: yes.
  - Checks whether Ollama is reachable via localhost.
  - Attempts to start `ollama serve` when Ollama is not running.
  - Polls readiness until startup timeout expires.

- Added `Assets/OurAssets/Scripts/AI/OllamaDtos.cs`
  - Date: 29/04/2026
  - AI assisted: yes.
  - Defines request/response DTOs for Ollama `/api/chat`.
  - Includes support for `model`, `messages`, `stream`, and `keep_alive`.

- Added `Assets/OurAssets/Scripts/AI/OllamaClient.cs`
  - Date: 29/04/2026
  - AI assisted: yes.
  - Sends chat requests to the local Ollama server using `UnityWebRequest`.
  - Date: 30/04/2026
  - Parses responses and reports structured failure details.

- Added `Assets/OurAssets/Scripts/Chat/OuijaConversationState.cs`
  - Date: 29/04/2026
  - AI assisted: yes.
  - Stores message history in three blocks:
    - constants (system + generated context),
    - player messages,
    - AI messages.
  - Composes outgoing context in this order:
    - constants first,
    - then interleaved `player -> ai -> player -> ai ...`.

- Added `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - Date: 29/04/2026
  - AI assisted: yes.
  - Orchestrates two AI roles:
    - Story generator model,
    - Ouija board model.
  - Sends story output into the constants block for the Ouija model.
  - Supports inspector-configured keep-alive seconds conversion to Ollama format (e.g. `120s`).
  - Applies warm vs cold timeout behavior based on keep-alive expiry.

- Updated `Assets/OurAssets/Scripts/AI/OllamaClient.cs` and `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - Date: 29/04/2026
  - AI assisted: yes.
  - Added explicit model unload calls to Ollama (`/api/generate` with `keep_alive: "0s"`).
  - Triggers unload when app is paused, loses focus/minimized, or quits.
  - Clears warm-state timestamps immediately so the next model request uses cold-start timeout behavior.

- Updated `Assets/OurAssets/Scripts/AI/OllamaClient.cs` and `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - Date: 29/04/2026
  - AI assisted: yes.
  - Moved Ollama server readiness/startup responsibility into `OllamaClient`.
  - `OuijaAiOrchestrator` now delegates server readiness checks to client layer.

- Updated `Assets/OurAssets/Scripts/AI/OllamaProcessManager.cs`, `Assets/OurAssets/Scripts/AI/OllamaClient.cs`, and `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - Date: 29/04/2026
  - AI assisted: yes.
  - Tracks whether the current game session started `ollama serve`.
  - On game quit, only stops Ollama if this session started it.
  - If Ollama was already running before launch, game quit leaves it running.

- Updated `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - Date: 29/04/2026
  - AI assisted: yes.
  - On quit, attempts to stop owned Ollama server first.
  - If owned server is stopped, skip model unload on quit.
  - If server is not stopped (pre-existing server), still unload models on quit.

- Updated `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - Date: 29/04/2026
  - AI assisted: yes.
  - Added quit guard for Unity lifecycle ordering (`_isQuitting`).
  - Prevents `OnApplicationPause` / `OnApplicationFocus` from triggering unload logic during quit.

- Added `OuijaSetup/OuijaSetp.cpp`
  - Date: 30/04/2026
  - AI assisted: no (David).
  - Used to be able to install Ollama and AI models for any system, just needs to be compiled

- Added `StoryModel.txt` and `OuijaModel.txt`
  - Date: 30/04/2026
  - AI assisted: no (David).
  - Stores the models to be used for the game

- Updated `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - Date: 30/04/2026
  - AI assisted: no (David).
  - Read the AI models from the text files instead of inspector
  - Inspector models are now used as fallbacks if the text files are missing or empty

- Added `OuijaSetup/CMakeLists.txt`
  - Date: 30/04/2026
  - AI assisted: no (David).
  - Allows the cpp file to be built on any system

- Added `Windows_Ollama_Setup.exe`, `Linux_Ollama_Setup`, and `Ollama_Setup.py`
  - Date: 30/04/2026
  - AI assisted: no (David).
  - Allows Ollama and the models to be installed on Windows and Linux
  - The cpp file does work for MacOS but they require me to own a Mac or illegally download a VM for MacOS which I didn't feel like doing
  - The py file checks the system and runs the correct executable, and the executables are there in case the person running them doesn't have python

- Added `Assets/Editor/PostBuildCopy.cs`
  - Date: 30/04/2026
  - AI assissted: no (David).
  - Copies the Ollama setup files from the project folder to the executable folder on build

- Forked [Jinja2.NET](https://github.com/AlexNek/Jinja2.NET) to create my own package that works with Unity called [Jinja2.NET.Unity](https://github.com/DavidBarker05/Jinja2.NET.Unity)
  - Date: 07/05/2026
  - AI assisted: no (David).
  - Used to read prompts stored in Jinja2 files in Unity C# without python

- Added `Jinja2.NET.Unity` as a package
  - Date: 07/05/2026
  - AI assisted: no (David)
  - See above

- Updated `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs` and added prompt assets in `Assets/Resources/Prompts/`
  - Date: 07/05/2026
  - AI assisted: yes.
  - Replaced serialized string prompt fields with serialized `TextAsset` prompt templates.
  - Added prompt rendering via `Jinja2.NET.Template` before sending requests to Ollama.
  - Added fallback loading from Unity `Resources` paths:
    - `Prompts/OuijaSystemPrompt`
    - `Prompts/StoryPrompt`
  - Story context generation now renders the story template file first, then sends the rendered text to the story model.
  - Ouija system constants now render from the system template file before being added to conversation context.

- Updated `Jinja2.NET.Unity`
  - Date: 07/05/2026
  - AI assisted: no (David)
  - Now .j2 files are read as TextAssets by Unity which makes them easier to work with

- Updated `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - Date: 07/05/2026
  - AI assisted: yes.
  - Added inspector toggle `enableRegularDebugLogs` to control non-critical `Debug.Log` output.
  - Wrapped regular logs with inline `if` checks to preserve original callsite stack traces.
  - Kept `Debug.LogError` and `Debug.LogWarning` always-on for troubleshooting.

- Added `whisper.unity` package (`com.whisper.unity`) to `Packages/manifest.json`
  - Date: 09/05/2026
  - AI assisted: no (David).
  - Provides on-device speech-to-text via whisper.cpp bindings.
  - Used to transcribe player voice input into the Ouija message box.

- Added `Assets/OurAssets/Scripts/Chat/OuijaPlayerInputController.cs`
  - Date: 09/05/2026
  - AI assisted: yes.
  - Drives the on-screen Ouija chat input UI.
  - Text input path:
    - Serialized `TMP_InputField` whose `characterLimit` is set from a serialized `maxMessageCharacters` (default 200) so typing/transcription is capped.
    - Send button (also triggers on `onSubmit` / Enter) calls `OuijaAiOrchestrator.SendPlayerMessageToOuijaAsync`.
    - Send button auto-disables while empty, transcribing, recording, or sending.
    - Optional `clearInputAfterSend` toggle.
  - Voice input path:
    - Toggle button starts/stops recording via `MicrophoneRecord` from whisper.unity.
    - Serialized `maxRecordingSeconds` (default 30) is pushed into `MicrophoneRecord.maxLengthSec` and additionally enforced in `Update()` so the cap is authoritative.
    - On record stop, audio is sent to `WhisperManager.GetTextAsync`; the transcription overwrites the input field text (clamped to the character limit) so the player can edit before sending.
    - Optional UI hooks for record-button label swap, indicator color, status label, and recording-time countdown label.
  - Robustness:
    - `CancellationTokenSource` cancels any in-flight send on disable / re-send.
    - Stops the microphone in `OnDisable` so scene unload / quit doesn't leak the input device.
    - Public events `AiResponseReceived(string)` and `SendFailed(Exception)` for downstream UI to subscribe to.

- Added `Assets/StreamingAssets/Whisper/ggml-tiny.bin` (whisper model weights)
  - Date: 09/05/2026
  - AI assisted: no (David).
  - Shipped under `StreamingAssets` so `WhisperManager` can resolve it via `Application.streamingAssetsPath` in editor and builds.
  - Tracked through Git LFS (see `.gitattributes` change below).

- Updated `.gitattributes`
  - Date: 09/05/2026
  - AI assisted: yes.
  - Added Git LFS tracking for AI/ML model weights: `*.bin`, `*.gguf`, `*.onnx`, `*.safetensors`.
  - Ensures whisper / future ML model files are not committed as plain blobs.

- Updated `Assets/Scenes/SampleScene.unity`
  - Date: 09/05/2026
  - AI assisted: no (David).
  - Added a Canvas with the Ouija chat UI (input field, send button, record button, status / countdown labels).
  - Added GameObjects hosting `WhisperManager`, `MicrophoneRecord`, and `OuijaPlayerInputController`, with serialized references wired up.

- Added `Assets/OurAssets/Scripts/OuijaBoard.cs`
  - Date: 09/05/2026
  - AI assisted: no (David).
  - Basic logic for the Ouija Board that moves a planchette and displays characters to the screen based on the response from the AI

- Added scripted **gated Ouija questions** (preset answers + optional progression conditions, no conversation-history pollution)
  - Date: 10/05/2026
  - AI assisted: yes.

- Added `Assets/OurAssets/Scripts/Chat/IOuijaGateConditionEvaluator.cs`
  - Interface: `bool IsConditionMet(string conditionId)` for minigames, flags, inventory, door state, etc.
  - Wired from `OuijaAiOrchestrator` via a serialized `Component` reference that implements this interface.

- Added `Assets/OurAssets/Scripts/Chat/OuijaGatedQuestionEntry.cs`
  - Inspector-friendly entry per special question:
    - Stable `questionId` (also used when the classifier returns JSON).
    - `matchPhrases[]` — paraphrases to fuzzy-match player wording locally.
    - `conditionIdsRequired[]` — if non-empty, all must pass evaluator checks for the **eligible** response; otherwise **blocked** response. Empty list means always **eligible**.
    - `responseWhenBlocked` / `responseWhenEligible` preset lines (spirit voice is still up to wording).

- Added `Assets/OurAssets/Scripts/Chat/OuijaGateConditionEvaluatorStub.cs`
  - MonoBehaviour stub: toggle “treat all conditions as met” for testing before real gameplay hooks exist.

- Added `Assets/OurAssets/Scripts/Chat/OuijaQuestionGateResolver.cs`
  - **Stage 1 (no AI):** normalizes text, scores similarity per gate (token Jaccard, directional overlap, compact bigram Dice), auto-resolves if best score ≥ `gatedFuzzyStrongThreshold`.
  - **Stage 2 (minimal Ollama call):** if not auto-resolved, sends only **top-K** fuzzy candidates (`gatedMaxClassifierCandidates`, floor `gatedFuzzyMinAiCandidateScore`) to a **single-shot** chat — system rules + candidate ids/phrases + player line — **no** story or `OuijaConversationState` history. Expects JSON `{ "matched_id":"...", "confidence":0.0 }`; `confidence` must be ≥ `gatedClassifierMinConfidence` and `matched_id` must be one of the candidates.

- Updated `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - New inspector block **“Gated scripted questions”**: `enableQuestionGate`, `gatedQuestions[]`, optional `gateConditionEvaluator`, optional `gateClassifierInstructions` (`TextAsset`), optional `gateClassifierModelOverride` (blank = same model as normal Ouija chat), thresholds/timeouts/`enableGateDebugLogs`.
  - `SendPlayerMessageToOuijaAsync` resolves gates **before** `_conversationState.AddPlayerMessage`; successful gate replies **never** add player or AI turns to conversation state.
  - When the classifier path runs (`InvokedClassifier`), marks the classifier model warm for timeout bookkeeping (same as ouija unless override set).

- Updated `Assets/OurAssets/Scripts/Chat/OuijaQuestionGateResolver.cs` and `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - Date: 10/05/2026
  - AI assisted: yes.
  - Added detailed gated-question debug logs behind `enableGateDebugLogs`:
    - Per-request fuzzy breakdown including normalized player text, thresholds, and per-gate/per-phrase scores.
    - Similarity component values (`jacc`, token coverage, bigram Dice) for each phrase entry.
    - Classifier candidate-pool summary and explicit “no candidates above floor” logging.
    - `enableGateDebugLogs` is now independent of `enableRegularDebugLogs` so gate diagnostics can be enabled by themselves.

- Added `Assets/OurAssets/Scripts/AI/UnityMainThread.cs`
  - Date: 10/05/2026
  - AI assisted: yes.
  - Captures the Unity `SynchronizationContext` and provides `SwitchToMainThreadAsync()` so async continuations can post back to the main thread.

- Updated `Assets/OurAssets/Scripts/AI/OllamaClient.cs`
  - Date: 10/05/2026
  - AI assisted: yes.
  - `SendChatAsync` and `UnloadModelAsync` await `UnityMainThread.SwitchToMainThreadAsync()` before creating `UnityWebRequest` (fixes “Create can only be called from the main thread” when chat code resumes off the main thread after `await`).

- Updated `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - Date: 10/05/2026
  - AI assisted: yes.
  - Registers the main-thread context in `Awake` via `UnityMainThread.RegisterMainThreadContext` before creating `OllamaClient`.

- Updated `Assets/OurAssets/Scripts/Chat/OuijaQuestionGateResolver.cs`
  - Date: 10/05/2026
  - AI assisted: yes.
  - Classifier JSON parsing hardened for real model outputs: camelCase envelope (`matchedId`), regex fallbacks, `NormalizeClassifierMatchedId` (strip `id=`, list prefixes), numeric / index mapping to `QuestionId`, and flooring `confidence` when it is exactly `0` but a valid gate id was returned (models often misuse zero).

- Updated `Assets/OurAssets/Scripts/AI/OllamaDtos.cs`, `OuijaQuestionGateResolver.cs`, `OuijaAiOrchestrator.cs`
  - Date: 10/05/2026
  - AI assisted: yes.
  - `OllamaChatInferenceOptions` (`temperature`, `top_p`, `top_k`, `seed`) attached to `OllamaChatRequest.options` **only for the gate classifier** so sampling is near-greedy by default (temperature 0, `top_k` 1, fixed seed) and repeats the same player line classify more consistently; main Ouija/story chat still omits `options` and keeps server defaults.
  - Inspector fields on `OuijaAiOrchestrator`: `gateClassifierTemperature`, `gateClassifierTopP`, `gateClassifierTopK`, `gateClassifierSeed`.
  - Default classifier preamble adds an explicit rule that the same player line must map to the same `matched_id` each time.