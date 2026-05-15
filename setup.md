# Technical Setup Guide

**Project:** Don't Forget to Say GOODBYE  
**Repository:** [INSERT GITHUB LINK]  
**Last updated:** May 2026

This guide is written so a moderator, teammate, or future student can install dependencies, run Ollama, open the Unity project, and troubleshoot common failures without prior knowledge of the codebase.

---

## 1. Overview

| Component | Purpose |
|-----------|---------|
| **Unity 6** | Game client and UI |
| **Ollama** | Local LLM server (`localhost:11434`) |
| **llama3.2** | Instruct model for story + Ouija + gate classifier |
| **Git** | Version control and submission history |
| **Whisper.unity** (in project) | Voice-to-text for Ouija questions |

---

## 2. System requirements

### 2.1 Minimum (development / CPU-only inference)

| Item | Specification |
|------|----------------|
| **OS** | Windows 10/11 64-bit (primary); Linux supported for Ollama via included helpers |
| **CPU** | [INSERT CPU MODEL] — e.g. 6+ cores recommended |
| **RAM** | 16 GB minimum; **32 GB recommended** for Llama 3.2 8B + Unity |
| **GPU** | Optional; integrated graphics works but story generation is slow |
| **Storage** | ~10 GB free (Unity Library + Ollama models) |
| **Unity** | **6000.4.1f1** (see `ProjectSettings/ProjectVersion.txt`) |

### 2.2 Recommended (smooth local AI)

| Item | Specification |
|------|----------------|
| **GPU** | [INSERT GPU MODEL] — NVIDIA with 8+ GB VRAM preferred |
| **RAM** | 32 GB |
| **SSD** | Model load and project import faster on SSD |

### 2.3 Developer machine (fill in for submission)

```
CPU:    [INSERT SYSTEM SPECS]
GPU:    [INSERT SYSTEM SPECS]
RAM:    [INSERT SYSTEM SPECS]
OS:     [INSERT SYSTEM SPECS]
Unity:  6000.4.1f1
```

---

## 3. Required software

1. **Unity Hub** + Editor **6000.4.1f1** (Unity 6)  
   - Install modules: Windows Build Support (IL2CPP/Mono as needed), Visual Studio integration optional.

2. **Git**  
   - Clone: `git clone [INSERT GITHUB LINK]`

3. **Ollama**  
   - Download: https://ollama.com  
   - Windows installer or Linux install per Ollama docs.

4. **Llama 3.2 model** (see Section 5).

5. **Optional:** Python 3 — only if using `Ollama_Setup.py` wrapper in repo root helpers.

---

## 4. Step-by-step: Install Ollama

### Windows

1. Download and run the Ollama installer from https://ollama.com.
2. After install, Ollama usually runs as a background service.
3. Open **PowerShell** or **Command Prompt** and verify:

   ```bash
   ollama --version
   ```

4. If the command is not found, restart the terminal or add Ollama to PATH per installer notes.

### Linux

1. Use Ollama’s official install script or package, **or** run the bundled `Linux_Ollama_Setup` binary from the project root (after build from `OuijaSetup/` if needed).
2. Verify `ollama --version`.

### macOS

- The repo includes C++ setup tooling that could target macOS, but primary testing is on **Windows**. Use https://ollama.com for Mac if demonstrating on Apple hardware.

---

## 5. Pull the model

Target model for documentation and design:

```bash
ollama pull llama3.2
```

Confirm it appears in the local library:

```bash
ollama list
```

You should see `llama3.2` (or similar tag) in the list.

### Align Unity with the model name

At the **project root** (same folder as `Assets/`), edit:

- `StoryModel.txt` — single line: `llama3.2`
- `OuijaModel.txt` — single line: `llama3.2`

Only the **first word** on each line is read by code. These files are copied next to the build via `Assets/Editor/PostBuildCopy.cs`.

---

## 6. Confirm Ollama is running

1. Ensure the Ollama app/service is running.
2. In a browser, open:

   **http://localhost:11434**

   You should get a response (Ollama’s root page or API acknowledgement).

3. Optional CLI smoke test:

   ```bash
   ollama run llama3.2 "Reply with one word: hello"
   ```

4. Start the game from Unity Play mode — `OllamaProcessManager` probes the same host and may start `ollama serve` only if the game session owns that startup.

---

## 7. Open and run the Unity project

1. **Clone the repository** (if not already):

   ```bash
   git clone [INSERT GITHUB LINK]
   cd Ouija-Game
   ```

2. **Open Unity Hub** → **Add** → select the folder containing `Assets/` and `ProjectSettings/`.

3. **Editor version:** Open with **6000.4.1f1**. Allow Unity to import assets (first open can take several minutes).

4. **Open the main menu scene:**  
   `Assets/Scenes/MainMenu.unity`

5. Press **Play**.  
   - Configure audio/settings from the menu if desired.  
   - Start a new game flow that loads **StoryScene** (story + lore generation uses Ollama).

6. **Build order** (see `ProjectSettings/EditorBuildSettings.asset`):

   | Index | Scene |
   |-------|--------|
   | 0 | MainMenu |
   | 1 | StoryScene |
   | 2 | HouseScene |
   | 3 | TarotScene |
   | 4 | RuneScene |
   | 5 | EndScene |

---

## 8. Scene setup (what each scene does)

| Scene | Role |
|-------|------|
| **MainMenu** | Settings, user preferences (`user_settings.json`), entry to new game |
| **StoryScene** | `StoryGeneratorScreen` runs `GameManager.StartNewGame` — session lore + story via Ollama; displays generated text |
| **HouseScene** | Main exploration, Ouija board, cryptex, interactions to ritual scenes |
| **TarotScene** / **RuneScene** | Ritual mini-games (separate from board); return via saved player position |
| **EndScene** | Final survival ritual |

**Persistent services** (created at runtime, `DontDestroyOnLoad`):

- `OllamaGameSession`, `StoryAiService`, `GameManager`, `StoryManager`, `MinigameManager`, `SpiritNameManager`, `UserSettingsManager` (menu)

For AI testing, place or confirm `OllamaGameSession` and `StoryAiService` exist in the first loaded scene (or rely on lazy singleton creation — prefer explicit scene objects in MainMenu/StoryScene for stable editor play).

---

## 9. Voice input (optional)

Ouija questions support **typing** and **microphone** (`OuijaPlayerInputController` + Whisper).

- Ensure microphone permissions are granted (OS + Unity).
- First transcription after record may take a few seconds on CPU.
- If Whisper fails, status text shows “Transcription failed” / “Didn't catch that”; typed input still works.

---

## 10. Troubleshooting

### 10.1 Ollama not running

| Symptom | Fix |
|---------|-----|
| Connection refused to `127.0.0.1:11434` | Start Ollama app; run `ollama serve` in terminal if needed |
| Game hangs on “Generating…” | Open http://localhost:11434 in browser; confirm service up |
| Firewall prompt | Allow Ollama on private networks |

### 10.2 Model not found

| Symptom | Fix |
|---------|-----|
| Error mentioning unknown model | Run `ollama pull llama3.2` (or match exact name in `StoryModel.txt` / `OuijaModel.txt`) |
| Typo in model file | One word per file, no spaces — e.g. `llama3.2` not `llama 3.2` |

### 10.3 Slow responses

| Symptom | Fix |
|---------|-----|
| First message very slow | Normal cold load; wait up to `coldStartTimeoutSeconds` (90s default) |
| Every message slow | Use GPU if available; close browsers; try smaller quant if you add one in Ollama |
| Story scene long wait | Expected on CPU-only; show machine specs in report |

### 10.4 Unity compile errors

| Symptom | Fix |
|---------|-----|
| Missing packages | Open Package Manager; restore; ensure **Jinja2.NET.Unity** package from repo manifest |
| Wrong Unity version | Use **6000.4.1f1** per `ProjectVersion.txt` |
| Library corrupt | Close Unity; delete `Library/` folder; reopen project to reimport |

### 10.5 Missing references in scenes

| Symptom | Fix |
|---------|-----|
| Pink materials / missing scripts | Pull latest Git; reimport; check Console for missing script GUIDs |
| Ouija orchestrator unassigned | Open HouseScene; assign `OuijaAiOrchestrator` on scene UI object |
| Null `m_AudioMixer` on settings | Open MainMenu; wire AudioMixer on `UserSettingsManager` |

### 10.6 Connection / API errors

| Symptom | Fix |
|---------|-----|
| `UnityWebRequest` failure | Confirm JSON API at `/api/chat`; no HTTPS required locally |
| Empty model response | Check Console; verify prompt templates exist under `Assets/Resources/Prompts/` |
| Timeout | Increase warm/cold timeouts on `StoryAiService` / `OuijaAiOrchestrator` inspectors |

### 10.7 DontDestroyOnLoad / Play mode errors

| Symptom | Fix |
|---------|-----|
| `DontDestroyOnLoad can only be used in play mode` after stopping Play | Story generation still running — stop Play after generation completes; see `StoryGeneratorScreen` async lifecycle |
| Duplicate singletons | Only one `UserSettingsManager` / session host per run; restart Play mode |

### 10.8 Scrollbar / UI

| Symptom | Fix |
|---------|-----|
| Settings list not scrolling | Content needs **Content Size Fitter** (vertical preferred) and top-anchored content; see UI hierarchy under MainMenu → Settings |

---

## 11. Quick verification checklist

Use this before a demo or submission build:

- [ ] `ollama list` shows `llama3.2` (or configured model)
- [ ] http://localhost:11434 responds
- [ ] `StoryModel.txt` and `OuijaModel.txt` match pulled model
- [ ] Unity **6000.4.1f1** opens project without compile errors
- [ ] Play from **MainMenu** → new game → **StoryScene** completes generation
- [ ] **HouseScene** Ouija accepts typed question and returns a reply
- [ ] No red errors in Console during a 5-minute play session

---

## 12. Build notes

- `PostBuildCopy.cs` copies Ollama helper executables and model txt files beside the player build.
- End users still need Ollama installed and the model pulled unless you ship a custom installer (`Windows_Ollama_Setup.exe` in repo).

---

## 13. Getting help

1. Check Unity **Console** (stack trace).
2. Check Ollama logs / terminal where `ollama serve` runs.
3. Review `ollama-plan.md` for intended AI flow.
4. Review `refinements-changes.md` for recent scope changes.

---

*Replace bracketed placeholders before final submission.*
