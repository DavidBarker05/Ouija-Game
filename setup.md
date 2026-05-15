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

Estimates below are for **Unity 6 + Ollama + Llama 3.2** on a student/dev machine. Actual performance depends on quantisation, background apps, and whether Ollama uses GPU acceleration.

### 2.1 Minimum (playable — CPU inference, slower AI)

| Item | Estimated specification |
|------|-------------------------|
| **OS** | **Windows 10/11** 64-bit (primary target) or **Linux** (64-bit, glibc-based distro) |
| **CPU** | **Intel Core i5-8400** / **AMD Ryzen 5 2600** (or equivalent: **6 physical cores**, 2.8 GHz+) |
| **RAM** | **16 GB** system memory (Llama 3.2 ~4–8 GB + Unity/editor overhead) |
| **GPU** | **Integrated graphics** or any DirectX 11–compatible GPU (**2 GB VRAM**+) — sufficient for the game; Ollama may run on **CPU only** |
| **Storage** | **~12 GB** free (Unity `Library/`, Llama 3.2 model, build cache) |
| **Unity** | **6000.4.1f1** (see `ProjectSettings/ProjectVersion.txt`) |

**Expected experience at minimum:** Story generation and first Ollama reply may take **30–90+ seconds** on cold start; acceptable for testing, not ideal for live demos.

### 2.2 Recommended (smooth play + local AI)

| Item | Estimated specification |
|------|-------------------------|
| **OS** | **Windows 11** 64-bit or **Linux** (Ubuntu 22.04+ / similar) |
| **CPU** | **Intel Core i7-10700** / **AMD Ryzen 7 5800X** (or equivalent: **8+ cores**, 3.0 GHz+) |
| **RAM** | **32 GB** |
| **GPU** | **NVIDIA GeForce RTX 3060** (**8 GB VRAM**) or better — Ollama can offload Llama 3.2 for much faster story/Ouija responses |
| **Storage** | **SSD** with **20 GB+** free |

**Expected experience at recommended:** Warm Ollama replies often **2–15 s**; cold story pass may still take **15–45 s** depending on GPU drivers and load.

### 2.3 macOS (manual setup only)

| Item | Notes |
|------|--------|
| **OS** | macOS 12+ (Apple Silicon or Intel) — **not** covered by bundled installers in this repo |
| **CPU / RAM** | Apple M1 with **16 GB** unified memory minimum; **M1 Pro / M2 with 32 GB** recommended for comfortable local inference |
| **GPU** | Apple Silicon uses Metal/GPU via Ollama when supported — follow manual install in [Section 4.3](#43-macos-manual-install-no-bundled-executable) |

### 2.4 Developer machine (optional — replace with your test PC)

```
CPU:    [INSERT YOUR CPU — e.g. AMD Ryzen 7 5800X]
GPU:    [INSERT YOUR GPU — e.g. NVIDIA RTX 3060 8 GB]
RAM:    [INSERT YOUR RAM — e.g. 32 GB]
OS:     [INSERT YOUR OS — e.g. Windows 11 64-bit]
Unity:  6000.4.1f1
```

---

## 3. Required software

1. **Unity Hub** + Editor **6000.4.1f1** (Unity 6)  
   - Install modules: Windows Build Support (and Linux/macOS modules only if you build for those targets).

2. **Git** — clone: `git clone [INSERT GITHUB LINK]`

3. **Ollama + Llama 3.2** — use the **bundled setup tools** on Windows/Linux ([Section 4](#4-install-ollama-and-llama-32)) or install manually.

4. **Python 3** (optional) — only needed to run `Ollama_Setup.py`, which launches the platform executable for you.

---

## 4. Install Ollama and Llama 3.2

The repository includes helper programs at the **project root** (also copied next to standalone builds via `PostBuildCopy.cs`):

| File | Platform | Purpose |
|------|----------|---------|
| **`Windows_Ollama_Setup.exe`** | Windows | Installs Ollama and pulls required models |
| **`Linux_Ollama_Setup`** | Linux | Same for Linux (executable; may need `chmod +x`) |
| **`Ollama_Setup.py`** | Windows / Linux | Detects OS and runs the correct executable above |
| **`OuijaSetup/`** | Developer | C++ source + CMake to rebuild the setup tools if missing |

> **macOS:** There is **no** bundled Apple installer in this repo (building for macOS requires Apple hardware). Use [Section 4.3](#43-macos-manual-install-no-bundled-executable) and type the commands yourself.

### 4.1 Windows (recommended — bundled installer)

1. Open the project root folder (same level as `Assets/`).
2. **Either:**
   - Double-click **`Windows_Ollama_Setup.exe`**, **or**
   - Run: `python Ollama_Setup.py` (requires Python 3 on PATH).
3. Wait for the installer to finish (downloads Ollama and pulls models — requires internet).
4. Verify in **PowerShell** or **Command Prompt**:

   ```bash
   ollama --version
   ollama list
   ```

   You should see **`llama3.2`** in the list.

5. Confirm the API: open **http://localhost:11434** in a browser.

**Manual fallback (Windows):** install from https://ollama.com, then run `ollama pull llama3.2`.

### 4.2 Linux (bundled installer)

1. Open a terminal in the **project root**.
2. Make the binary executable (first time only):

   ```bash
   chmod +x Linux_Ollama_Setup
   ```

3. **Either:**
   - Run: `./Linux_Ollama_Setup`, **or**
   - Run: `python3 Ollama_Setup.py`
4. Verify:

   ```bash
   ollama --version
   ollama list
   ```

**Manual fallback (Linux):** follow https://ollama.com download instructions, then `ollama pull llama3.2`.

### 4.3 macOS (manual install — no bundled executable)

Apple builds are **not** shipped with this project. Install and pull the model **manually**:

1. Install Ollama from **https://ollama.com** (macOS download).
2. Open **Terminal** and run:

   ```bash
   ollama pull llama3.2
   ollama list
   ```

3. Confirm **http://localhost:11434** responds.
4. Ensure project root files match the model name:
   - `StoryModel.txt` → `llama3.2`
   - `OuijaModel.txt` → `llama3.2`

Developers with a Mac can rebuild a setup app from `OuijaSetup/` using CMake (see `Ollama_Setup.py` messages if a `MacOS_Ollama_Setup.app` is added locally).

### 4.4 Align Unity with the model name

At the **project root** (same folder as `Assets/`):

- **`StoryModel.txt`** — single line: `llama3.2`
- **`OuijaModel.txt`** — single line: `llama3.2`

Only the **first word** on each line is read by the game. These files are copied beside player builds automatically.

---

## 5. Confirm Ollama is running

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

## 6. Open and run the Unity project

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

## 7. Scene setup (what each scene does)

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

## 8. Voice input (optional)

Ouija questions support **typing** and **microphone** (`OuijaPlayerInputController` + Whisper).

- Ensure microphone permissions are granted (OS + Unity).
- First transcription after record may take a few seconds on CPU.
- If Whisper fails, status text shows “Transcription failed” / “Didn't catch that”; typed input still works.

---

## 9. Troubleshooting

### 9.1 Ollama not running

| Symptom | Fix |
|---------|-----|
| Connection refused to `127.0.0.1:11434` | Start Ollama app; run `ollama serve` in terminal if needed |
| Game hangs on “Generating…” | Open http://localhost:11434 in browser; confirm service up |
| Firewall prompt | Allow Ollama on private networks |

### 9.2 Model not found

| Symptom | Fix |
|---------|-----|
| Error mentioning unknown model | Re-run `Windows_Ollama_Setup.exe` / `Linux_Ollama_Setup`, or `ollama pull llama3.2` |
| Setup executable missing | Clone full repo; or build from `OuijaSetup/` with CMake |
| Typo in model file | One word per file, no spaces — e.g. `llama3.2` not `llama 3.2` |

### 9.3 Slow responses

| Symptom | Fix |
|---------|-----|
| First message very slow | Normal cold load; wait up to `coldStartTimeoutSeconds` (90s default) |
| Every message slow | Use GPU if available; close browsers; try smaller quant if you add one in Ollama |
| Story scene long wait | Expected on CPU-only; show machine specs in report |

### 9.4 Unity compile errors

| Symptom | Fix |
|---------|-----|
| Missing packages | Open Package Manager; restore; ensure **Jinja2.NET.Unity** package from repo manifest |
| Wrong Unity version | Use **6000.4.1f1** per `ProjectVersion.txt` |
| Library corrupt | Close Unity; delete `Library/` folder; reopen project to reimport |

### 9.5 Missing references in scenes

| Symptom | Fix |
|---------|-----|
| Pink materials / missing scripts | Pull latest Git; reimport; check Console for missing script GUIDs |
| Ouija orchestrator unassigned | Open HouseScene; assign `OuijaAiOrchestrator` on scene UI object |
| Null `m_AudioMixer` on settings | Open MainMenu; wire AudioMixer on `UserSettingsManager` |

### 9.6 Connection / API errors

| Symptom | Fix |
|---------|-----|
| `UnityWebRequest` failure | Confirm JSON API at `/api/chat`; no HTTPS required locally |
| Empty model response | Check Console; verify prompt templates exist under `Assets/Resources/Prompts/` |
| Timeout | Increase warm/cold timeouts on `StoryAiService` / `OuijaAiOrchestrator` inspectors |

### 9.7 DontDestroyOnLoad / Play mode errors

| Symptom | Fix |
|---------|-----|
| `DontDestroyOnLoad can only be used in play mode` after stopping Play | Story generation still running — stop Play after generation completes; see `StoryGeneratorScreen` async lifecycle |
| Duplicate singletons | Only one `UserSettingsManager` / session host per run; restart Play mode |

### 9.8 Scrollbar / UI

| Symptom | Fix |
|---------|-----|
| Settings list not scrolling | Content needs **Content Size Fitter** (vertical preferred) and top-anchored content; see UI hierarchy under MainMenu → Settings |

---

## 10. Quick verification checklist

Use this before a demo or submission build:

- [ ] `ollama list` shows `llama3.2` (or configured model)
- [ ] http://localhost:11434 responds
- [ ] `StoryModel.txt` and `OuijaModel.txt` match pulled model
- [ ] Unity **6000.4.1f1** opens project without compile errors
- [ ] Play from **MainMenu** → new game → **StoryScene** completes generation
- [ ] **HouseScene** Ouija accepts typed question and returns a reply
- [ ] No red errors in Console during a 5-minute play session

---

## 11. Build notes

- `PostBuildCopy.cs` copies **`Windows_Ollama_Setup.exe`**, **`Linux_Ollama_Setup`**, **`Ollama_Setup.py`**, **`StoryModel.txt`**, and **`OuijaModel.txt`** next to the player executable.
- Players on **Windows/Linux** can run those helpers from the build folder before first launch.
- **macOS** players must install Ollama manually (Section 4.3).

---

## 12. Getting help

1. Check Unity **Console** (stack trace).
2. Check Ollama logs / terminal where `ollama serve` runs.
3. Review `ollama-plan.md` for intended AI flow.
4. Review `refinements-changes.md` for recent scope changes.

---

*Replace bracketed placeholders before final submission.*
