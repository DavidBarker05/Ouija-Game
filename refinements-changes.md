# Refinements and Changes Log

**Project:** Don't Forget to Say GOODBYE  
**Purpose:** Continuous record of scope changes, limitations addressed, and AI-assisted development decisions for moderation and version tracking.  
**Last updated:** 16 May 2026 (post-build binary copy fix for Ollama setup artifacts)

---

## Change log (summary table)

| Date | Area changed | Original issue / limitation | Change made | Reason for change | Tool / AI assistance | Outcome |
|------|--------------|---------------------------|-------------|-------------------|----------------------|---------|
| Apr 2026 | Ollama connectivity | No local LLM; story was static | Added `OllamaProcessManager`, `OllamaClient`, DTOs for `/api/chat` | Enable offline-capable AI for coursework | Cursor AI | Unity can probe and call localhost Ollama |
| Apr 2026 | Architecture | Single monolithic AI script | Split conversation state, orchestrator, prompts | Maintainable roles for story vs board | Cursor AI | Clearer separation of concerns |
| Apr 2026 | Model configuration | Hard-coded model in inspector only | `StoryModel.txt` / `OuijaModel.txt` beside build | Easy swap without rebuild | No (David) | Moderators can change model name in one line |
| Apr 2026 | Prompt authoring | Prompts embedded in C# strings | Jinja2 templates in `Resources/Prompts/` | Faster iteration on narrative tone | Cursor AI + Jinja2.NET.Unity | Designers edit `.j2` files |
| May 2026 | Narrative control | Free chat could hallucinate plot answers | Gated questions + fuzzy match + classifier JSON | Route critical Q&A to scripted lines | Cursor AI | Progression answers stable |
| May 2026 | Gate accuracy | Typos failed exact string match | `OuijaQuestionGateResolver` fuzzy scoring + top-K classifier | Accept paraphrased player questions | Cursor AI | Better intent matching |
| May 2026 | Gate sampling | Classifier inconsistent on borderline cases | Low temperature, seed, top-k/p on classifier only | Repeatable gate selection | Cursor AI | Fewer mis-filed gates |
| May 2026 | Scene lifecycle | Story AI tied to Ouija scene | `StoryAiService` + temp cache files | Story scene can generate without House loaded | Cursor AI | Cross-scene story context |
| May 2026 | Session persistence | N/A | `OuijaGameCachePaths` under `temporaryCachePath` | Ouija reads story without same scene reference | Cursor AI | Disk-backed session context |
| May 2026 | Model lifetime | Duplicate clients; unload races | `OllamaGameSession` singleton, shared warm/cold timeouts | One Ollama client; consistent unload | Cursor AI | Fewer timeout bugs |
| May 2026 | Voice input | Typing only | Whisper + `OuijaPlayerInputController` | Accessibility and atmosphere | Cursor AI + Whisper.unity | Record → transcribe → send |
| May 2026 | Board presentation | Instant full reply text | Planchette moves letter-by-letter; send blocked while animating | Ouija board fantasy | No (David) | Readable spelled responses |
| May 2026 | Scope — backstory | Every Ouija reply tried to explain lore | Opening **StoryScene** + short board replies (5 words) | Avoid LLM exposition on every question | Design + Cursor AI | Deeper lore at start, not per line |
| May 2026 | Session variables | Names/reasons could drift run-to-run | **Session lore** pass → JSON → story canon + gates | One source of truth per playthrough | Cursor AI | `player_name`, `wife_*` consistent |
| May 2026 | Critical answers | LLM could invent “why she left” | `IOuijaGateResponseResolver` reads cached lore | No hallucination on progression strings | Cursor AI | Gate IDs map to file cache |
| May 2026 | Progression gates | All gates always “eligible” | `OuijaGateConditionEvaluator` + minigame counts | Block answers until rituals progress | No (David) | Pacing aligned with design |
| May 2026 | Spirit / cryptex | Fixed spirit name | `SpiritNameManager` random from list | Replay variety | No (David) | Cryptex solution changes per run |
| May 2026 | Mini-games vs board | Unclear interaction | Separate scenes (Tarot/Rune); `PlayerSceneDataManager` | Rituals not on Ouija UI | No (David) | Clear mode separation |
| May 2026 | Menus / loading | Scene loads froze frame | `LoadingScreen` async load + progress bar | UX during scene change | No (David) | Smoother transitions |
| May 2026 | User settings | Volume not applied on boot | `UserSettingsManager` + mixer; apply after audio ready | Hear saved master volume | Debug + design | Settings persist |
| May 2026 | Ethics / safety | Open LLM could give harmful text | Prompt rules; gates; teen tone; fiction framing | GADS duty-of-care | Cursor AI + design doc | Documented in `ollama-plan.md` |
| May 2026 | Story / session lore | Same names and story text every new game | `run_variant_id` in `SessionLorePrompt.j2` and `StoryPrompt.j2`; Ollama `options` (temperature, top_p, top_k, fresh seed) on lore + story requests in `StoryAiService`; `global::System.Environment.TickCount` avoids clash with `Jinja2.NET.Environment` | Replay variety + builds clean | Cursor AI | Per-run prompt + sampling diversity |
| May 2026 | Opening narrative prompts | Spec drift vs design (board word budget, opener beats) | Rewrote `SessionLorePrompt.j2` (male PC, ≤7-word gate answers) and `StoryPrompt.j2` (70–100 word search/occult/Ouija setup; no spoil of why she left) | Match session flow: facts only on board | Cursor AI + design | Lore/story prompts aligned |
| May 2026 | Documentation | Scattered notes | `ollama-plan.md`, `setup.md`, `readme.md`, this log | Exemplary rubric submission | Cursor AI | Single source for moderators |
| May 2026 | House ↔ minigames | Saved position but view/walk broke after reload; cursor lock leaked from FPS | `FirstPersonCharacter.LoadSceneData` applies `CameraEulerAngles`, CharacterController-safe teleport; `Player.ChangeCharacter` uses `MouseVisible` on active character | Correct spawn + look after Tarot/Rune; FPS cursor hidden again | Cursor AI | Return-to-house matches pre-minigame pose |
| May 2026 | Cryptex persistence | Cryptex beaten then Tarot/Rune reload reset closed door | `CryptexManager` restores door open + hides puzzle when `MinigameManager.IsMinigameBeaten(Cryptex)` on house `Start` | World matches solved state across scene reload | Cursor AI | Door stays open after rituals |
| May 2026 | Minigame queue | `RandomiseMinigames` loop started at index 1 — only one of Tarot/Rune appended | Loop `i = 0 .. Length-1`; append full shuffled pair after Cryptex | `WhichMinigame(2)` valid for `second_task`; both rituals reachable via `CanPlayMinigame` | Cursor AI | `first_task` / `second_task` gated lines populate |
| May 2026 | Windows build extras | `PostBuildCopy` used `ReadAllText` / `WriteAllText` for all files | `File.Copy(..., overwrite: true)` + try/catch for `IOException` | Ship valid `Windows_Ollama_Setup.exe` / Linux binary beside player | Cursor AI | Binaries match repo; clearer copy failures in Console |

---

## Ethical and content constraints (documented decisions)

| Topic | Decision |
|-------|----------|
| Grief / death | Fiction only; no graphic self-harm instructions in prompts |
| Occult framing | Board and rituals are game mechanics, not real spiritual claims |
| Harmful outputs | System prompts refuse crime/harm coaching; short Ouija replies limit drift |
| AI as authority | UI and docs state dialogue is generated fiction, not advice |
| Sensitive beats | Wife-related facts via session lore + gates, not free-form confession |

---

## Technical milestones (file-level index)

Detailed file history from implementation (chronological). Use with Git commit hashes for verification.

### AI / Ollama stack

- `Assets/OurAssets/Scripts/AI/OllamaProcessManager.cs` — probe/start `ollama serve`, owned-process tracking  
- `Assets/OurAssets/Scripts/AI/OllamaClient.cs` — `UnityWebRequest` chat + unload  
- `Assets/OurAssets/Scripts/AI/OllamaDtos.cs` — request/response types, optional inference options  
- `Assets/OurAssets/Scripts/AI/OllamaGameSession.cs` — shared session, warm/cold timeouts, lifecycle  
- `Assets/OurAssets/Scripts/AI/UnityMainThread.cs` — sync context for async callbacks  

### Chat / narrative

- `Assets/OurAssets/Scripts/Chat/OuijaConversationState.cs` — constants + player/AI transcript  
- `Assets/OurAssets/Scripts/Chat/OuijaAiOrchestrator.cs` — gates + Ouija send path  
- `Assets/OurAssets/Scripts/Chat/StoryAiService.cs` — lore + story generation, cache I/O; `run_variant_id` in Jinja; Ollama narrative sampling per lore/story request  
- `Assets/OurAssets/Scripts/Chat/StorySessionLore.cs` — JSON lore DTO + parser  
- `Assets/OurAssets/Scripts/Chat/OuijaQuestionGateResolver.cs` — fuzzy + classifier  
- `Assets/OurAssets/Scripts/Chat/OuijaGatedQuestionEntry.cs` — inspector gate definitions  
- `Assets/OurAssets/Scripts/Chat/OuijaGateResponseResolver.cs` — `spirit_name`, `player_name`, `wife_*`, `first_task` / `second_task` (`WhichMinigame` indices); warns on invalid order  
- `Assets/OurAssets/Scripts/Chat/OuijaGateConditionEvaluator.cs` — minigame-linked conditions  
- `Assets/OurAssets/Scripts/Chat/OuijaPlayerInputController.cs` — text, voice, send block  
- `Assets/OurAssets/Scripts/Chat/OuijaGameCachePaths.cs` — `session_lore.json`, `story_context.txt`  

### Prompts

- `Assets/Resources/Prompts/SessionLorePrompt.j2`  
- `Assets/Resources/Prompts/StoryPrompt.j2`  
- `Assets/Resources/Prompts/OuijaSystemPrompt.j2`  

### Gameplay / UI

- `Assets/OurAssets/Scripts/GameManager.cs` — `StartNewGame` pipeline  
- `Assets/OurAssets/Scripts/StoryManager.cs` — answered-question flags  
- `Assets/OurAssets/Scripts/OuijaBoard.cs` — planchette spelling  
- `Assets/OurAssets/Scripts/UI/StoryGeneratorScreen.cs` — story scene generation UI  
- `Assets/OurAssets/Scripts/Cryptex/CryptexManager.cs` — door open pose cached; restore after reload if Cryptex beaten (`MinigameManager`)  
- `Assets/OurAssets/Scripts/MinigameManager.cs` — Cryptex + **both** shuffled Tarot+Rune in `m_MinigameOrder` (`RandomiseMinigames` append fix)  
- `Assets/OurAssets/Scripts/Player/FirstPersonCharacter.cs`, `Player.cs`, `PlayerSceneDataManager.cs` — scene data restore (body + camera rig), cursor sync on character swap  
- `Assets/OurAssets/Scripts/Cryptex/*` (other), `Tarot/*`, `Rune/*`, `EndMinigame/*`  
- `Assets/OurAssets/Scripts/GameUserSettings/UserSettingsManager.cs`  

### Build / setup helpers

- `OuijaSetup/`, `Windows_Ollama_Setup.exe`, `Ollama_Setup.py`  
- `Assets/Editor/PostBuildCopy.cs` — `IPostprocessBuildWithReport`; **`File.Copy(..., overwrite: true)`** for model txt + Ollama setup binaries (no text round-trip)  

---

## Completed so far (detailed entries)

### April 2026 — Ollama foundation

- Added `OllamaProcessManager.cs` — reachability check, optional `ollama serve`, startup timeout.  
- Added `OllamaDtos.cs` — `/api/chat` payload types, `keep_alive`, streaming flag.  
- Added `OllamaClient.cs` — send chat, parse errors, unload model.  
- Added `OuijaConversationState.cs` — constants + interleaved history.  
- Added `OuijaAiOrchestrator.cs` — story + Ouija roles (later split).  
- Lifecycle: unload on pause/focus loss/quit; respect pre-existing Ollama process.  
- Model names from `StoryModel.txt` / `OuijaModel.txt` with inspector fallbacks.  

### May 2026 — Gates and classifier

- `IOuijaGateConditionEvaluator`, `IOuijaGateResponseResolver`, gated question entries.  
- Two-stage gate: fuzzy instant (threshold) → optional classifier on top-K candidates.  
- Classifier JSON: `matched_id`, `confidence`; camelCase/snake_case parsing.  
- Gate replies do not append to conversation history.  
- Inference options (temperature, seed) **only** on classifier requests.  

### May 2026 — Story pipeline split

- `StoryAiService` DontDestroyOnLoad; temp cache for story text.  
- `OllamaGameSession` shared client and warm state.  
- `GameManager.StartNewGame` orchestrates cache clear, spirit, minigames, AI steps.  

### May 2026 — Session lore (14 May)

- `GenerateSessionLoreAsync` before story; writes `session_lore.json`.  
- Story Jinja uses session names (full lore still drives gated Ouija answers for leave/sad reasons).  
- Gate response IDs: `player_name`, `wife_name`, `wife_left_reason`, `wife_sad_reason`.  

### May 2026 — Story prompts and sampling (16 May)

- **`Assets/Resources/Prompts/SessionLorePrompt.j2`** — Male player character; `wifeLeftReason` / `wifeSadReason` as single short sentences (≤7 words for Ouija); `{{ run_variant_id }}` appended so each generation request is not identical to the last.  
- **`Assets/Resources/Prompts/StoryPrompt.j2`** — Short opening hook: search for wife, occult house, missing wife, nearby board; 70–100 words, teen (16+); explicit rule not to reveal why she left yet; `{{ run_variant_id }}` for variation.  
- **`Assets/OurAssets/Scripts/Chat/StoryAiService.cs`** — Passes `run_variant_id` into session lore Jinja render; merges lore bindings with a new `run_variant_id` for story render; sets `OllamaChatRequest.options` for both lore and story calls (inspector: narrative temperature / top_p / top_k + random seed per request); `NextNarrativeSeed` uses `global::System.Environment.TickCount` to fix `CS0104` vs Jinja2.NET.  

### May 2026 — Gameplay polish (12–15 May)

- Player character switching (explore, Ouija, cryptex, tarot, rune, pause, menu).  
- `MinigameManager` order + completion tracking.  
- End survival minigame (candles / pentagrams).  
- Interactions for tarot, rune, end, cryptex; `PlayerSceneDataManager`.  
- `LoadingScreen`, `StoryGeneratorScreen`, audio mixer routing.  

### May 2026 — House return after minigames + Cryptex door (16 May)

- AI assisted: yes (Cursor).  
- **`FirstPersonCharacter.LoadSceneData`** — Applies saved **`CameraEulerAngles`** to **`CameraTarget`** (was saved but previously ignored); disables **`CharacterController`** briefly when teleporting so position applies reliably after async scene load.  
- **`Player.Awake`** — Comment clarifies load order: **`ChangeCharacter`** then **`LoadSceneData`** so FirstPerson is initialised before restore.  
- **`Player.ChangeCharacter`** — Cursor visibility follows **`m_PlayerCharacter.MouseVisible`** (was incorrectly tied to **`PauseCharacter.MouseVisible`**, always true, breaking FPS lock).  
- **`PlayerSceneDataManager.SaveSceneData`** — Null-safe **`CameraTarget`**; comment on load path behaviour.  
- **`CryptexManager`** — Caches door closed **`localRotation`** in **`Awake`**; on **`Start`**, if **`MinigameManager.Instance.IsMinigameBeaten(Minigames.Cryptex)`**, applies same open rotation as solve (`initial * Quaternion.Euler(m_DoorRotation)`), disables interaction, destroys cryptex object — no duplicate **`OnMinigameBeaten`**. Solve path uses shared **`ApplyCryptexDoorOpenRotation()`**.

### May 2026 — Minigame order and `second_task` gate (16 May)

- AI assisted: yes (Cursor).  
- **`MinigameManager.RandomiseMinigames`** — Off-by-one append: loop began at **`i = 1`**, so with a length-2 Tarot/Rune array only **`minigames[1]`** was added. Queue was **`[Cryptex, single ritual]`** — **`WhichMinigame(2)`** threw → **`OuijaGateResponseResolver`** **`second_task`** returned empty; the omitted ritual never became **`CurrentMinigameToBeat`**. Fixed by appending **`i = 0 .. minigames.Length - 1`**.  
- **`OuijaGateResponseResolver`** — Documented index map (0=Cryptex, 1/2 optional rituals); **`Debug.LogWarning`** on resolver failure instead of silent empty catch.

### May 2026 — Post-build copy binary safety (16 May)

- AI assisted: yes (Cursor).  
- **`Assets/Editor/PostBuildCopy.cs`** — Replaced **`ReadAllText` / `WriteAllText`** with **`File.Copy`** so **`Windows_Ollama_Setup.exe`**, **`Linux_Ollama_Setup`**, and other non-UTF8 payloads are not corrupted beside the player executable; **`IOException`** logged if copy fails (e.g. file locked).

---

## Known limitations (open / accepted)

| Item | Notes |
|------|--------|
| Model documentation | Docs and `StoryModel.txt` / `OuijaModel.txt` all use `llama3.2` |
| `StoryGeneratorScreen` async `OnEnable` | Can throw if Play mode stops mid-generation |
| Settings scroll view | Content needs Content Size Fitter for scrollbar visibility |
| macOS setup | Not primary target; use ollama.com installer on Mac |

---

## How to extend this log

When making a submission-worthy change, add a row to the **summary table** with:

1. Date (DD Mon YYYY)  
2. Area (AI, UI, gameplay, docs, …)  
3. What was wrong or missing before  
4. What you changed (files optional)  
5. Why (player experience, rubric, bug)  
6. Whether Cursor / ChatGPT / none helped  
7. Result (works / partial / reverted)

Commit to Git with a message that references the same change for traceability.

---

*This log complements Git history and `ollama-plan.md`. For install steps see `setup.md`; for overview see `readme.md`.*
