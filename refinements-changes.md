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