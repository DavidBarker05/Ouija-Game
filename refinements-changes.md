# Refinements and Changes

This file tracks the Unity + Ollama integration work completed so far.

## Completed so far

- Added `Assets/OurAssets/Scripts/AI/OllamaProcessManager.cs`
  - AI assisted: yes.
  - Checks whether Ollama is reachable via localhost.
  - Attempts to start `ollama serve` when Ollama is not running.
  - Polls readiness until startup timeout expires.

- Added `Assets/OurAssets/Scripts/AI/OllamaDtos.cs`
  - AI assisted: yes.
  - Defines request/response DTOs for Ollama `/api/chat`.
  - Includes support for `model`, `messages`, `stream`, and `keep_alive`.

- Added `Assets/OurAssets/Scripts/AI/OllamaClient.cs`
  - AI assisted: yes.
  - Sends chat requests to the local Ollama server using `UnityWebRequest`.
  - Parses responses and reports structured failure details.

- Added `Assets/OurAssets/Scripts/Chat/OuijaConversationState.cs`
  - AI assisted: yes.
  - Stores message history in three blocks:
    - constants (system + generated context),
    - player messages,
    - AI messages.
  - Composes outgoing context in this order:
    - constants first,
    - then interleaved `player -> ai -> player -> ai ...`.

- Added `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - AI assisted: yes.
  - Orchestrates two AI roles:
    - Story generator model,
    - Ouija board model.
  - Sends story output into the constants block for the Ouija model.
  - Supports inspector-configured keep-alive seconds conversion to Ollama format (e.g. `120s`).
  - Applies warm vs cold timeout behavior based on keep-alive expiry.

- Updated `Assets/OurAssets/Scripts/AI/OllamaClient.cs` and `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - AI assisted: yes.
  - Added explicit model unload calls to Ollama (`/api/generate` with `keep_alive: "0s"`).
  - Triggers unload when app is paused, loses focus/minimized, or quits.
  - Clears warm-state timestamps immediately so the next model request uses cold-start timeout behavior.

- Updated `Assets/OurAssets/Scripts/AI/OllamaClient.cs` and `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - AI assisted: yes.
  - Moved Ollama server readiness/startup responsibility into `OllamaClient`.
  - `OuijaAiOrchestrator` now delegates server readiness checks to client layer.

- Updated `Assets/OurAssets/Scripts/AI/OllamaProcessManager.cs`, `Assets/OurAssets/Scripts/AI/OllamaClient.cs`, and `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - AI assisted: yes.
  - Tracks whether the current game session started `ollama serve`.
  - On game quit, only stops Ollama if this session started it.
  - If Ollama was already running before launch, game quit leaves it running.

- Updated `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - AI assisted: yes.
  - On quit, attempts to stop owned Ollama server first.
  - If owned server is stopped, skip model unload on quit.
  - If server is not stopped (pre-existing server), still unload models on quit.

- Updated `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs`
  - AI assisted: yes.
  - Added quit guard for Unity lifecycle ordering (`_isQuitting`).
  - Prevents `OnApplicationPause` / `OnApplicationFocus` from triggering unload logic during quit.
