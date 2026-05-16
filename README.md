# Don't Forget to Say GOODBYE

Part 2
-------------------------------------------------------------------------------------------------------------------------------------------------------
High Concept Document - [HighConceptDocument_P2.pdf](https://github.com/user-attachments/files/27807988/HighConceptDocument_P2.pdf)

LLM Integration Report - [LLMReport_P2.pdf](https://github.com/user-attachments/files/27808012/LLMReport_P2.pdf)

Technical Demonstration Video - https://canva.link/qr6vyuiwfg2ttj5 

Showcase Video - 

Game Code Reference & Bibliography List - [Code_References_Bibliography_P2.pdf](https://github.com/user-attachments/files/27813632/Code_References_Bibliography_P2.pdf)

Project Schedule & Scheduled Feedback Sessions -[ProjectSchedule_ScheduledFeedbackSessions_GADS_P2_v40.xlsx](https://github.com/user-attachments/files/27811504/ProjectSchedule_ScheduledFeedbackSessions_GADS_P2_v40.xlsx)

**Genre:** First-person narrative horror / puzzle adventure  
**Platform:** Windows PC (Unity 6 standalone); local AI via Ollama   
**Unity version:** 6000.4.1f1  

---

## Contents

- [Description](#description)
- [Core gameplay](#core-gameplay)
- [Main mechanics](#main-mechanics)
- [AI / LLM features](#ai--llm-features)
- [Installation (summary)](#installation-summary)
- [Dependencies](#dependencies)
- [Project documentation](#project-documentation)
- [Credits](#credits)
- [AI tools used (disclosure)](#ai-tools-used-disclosure)
- [Ethical and safety note](#ethical-and-safety-note)
- [Version tracking and transparency](#version-tracking-and-transparency)
- [**Disclaimer** — Content warning & mental health](#content-warning--mental-health-disclaimer)
- [**Disclaimer** — Legal](#legal-disclaimer)
- [Licence](#licence)

---

## Description

*Don't Forget to Say GOODBYE* is a haunted-house game built for a GADS-style project where the player explores a manor, completes ritual mini-games, and communicates with spirits through a Ouija board. A local **Llama 3.2** model (via **Ollama**) generates the opening backstory and short in-character board replies, while **scripted gates** and **session variables** keep progression answers consistent and reduce harmful or hallucinated plot changes.

The experience is designed around restraint: grief and occult themes are fictional, AI output is constrained by prompts and designer-authored fallbacks, and saying **GOODBYE** closes the board when the session ends.

---

## Core gameplay

1. **Main menu** — Settings (resolution, vsync, volume), start new game.  
2. **Story introduction** — AI generates session lore (names, key reasons) and a readable opening narrative.  
3. **House exploration** — First-person movement, interact with objects, cryptex, and the Ouija board.  
4. **Ritual mini-games** — Tarot and rune challenges in separate scenes; order is shuffled each run.  
5. **Progression** — Spirit name unlocks the cryptex; story questions and mini-games gate deeper Ouija answers.  
6. **Finale** — End survival ritual scene after requirements are met.

---

## Main mechanics

| Mechanic | Summary |
|----------|---------|
| **Ouija question system** | Player asks questions; responses are either **gated scripted lines** or a **short LLM reply** (max five words on the free path). |
| **Text input** | Type questions in the UI; send when not blocked during planchette animation. |
| **Voice-to-text** | Hold record → Whisper transcription → same pipeline as typed text. |
| **Planchette spelling** | Spirit answers move a planchette across A–Z, YES/NO, numbers; text builds on screen letter by letter. |
| **GOODBYE** | Closing phrase ends the board session and returns control to exploration. |
| **Cryptex puzzle** | Rings must match the **random spirit name** for the run. |
| **Ritual mini-games** | Tarot (pairs to 21) and rune sequence memory — **not** played on the board itself. |
| **End survival** | Timed candle / pentagram challenge in the final scene. |

---

## AI / LLM features

- **Local only** — No cloud API; Unity talks to `http://127.0.0.1:11434`.  
- **Three prompt roles** — Session lore (JSON facts), story (opening prose), Ouija voice (very short replies).  
- **Question gates** — Fuzzy phrase matching + optional classifier call route “special” questions to cached answers.  
- **Shared session** — `OllamaGameSession` manages warm/cold timeouts and model unload on quit.  

See **`ollama-plan.md`** for architecture, risks, and mitigations.

---

## Installation (summary)

1. Install **Unity 6000.4.1f1** and clone **[INSERT GITHUB LINK]**.  
2. Install **Ollama** and run: `ollama pull llama3.2`  
3. Confirm `StoryModel.txt` and `OuijaModel.txt` contain `llama3.2` (one line each).  
4. Confirm **http://localhost:11434** responds.  
5. Open project in Unity → play **MainMenu** scene.  

Full steps: **`setup.md`**

---

## Dependencies

| Dependency | Use |
|------------|-----|
| Unity 6 (6000.4.1f1) | Engine |
| Ollama + llama3.2 | Local LLM |
| Jinja2.NET.Unity | Prompt templates (`*.j2`) |
| Whisper.unity | Speech-to-text |
| TextMesh Pro | UI text |
| Unity Input System | Player controls |

---

## Project documentation

| File | Contents |
|------|----------|
| `ollama-plan.md` | AI integration design |
| `setup.md` | Install and troubleshooting |
| `refinements-changes.md` | Scope change log and development history |
| `readme.md` | This overview |

Part 2 submission assets (PDFs, schedule) are linked from the top of the previous README stub in this repo.

---

## Credits

| Role | Name |
|------|------|
| **Design & programming** | David Barker |
| **Design & art** | Tiyah Singh |
| **Institution / module** | [GADS7331] |


Third-party assets include environment/audio packs under `Assets/ThirdPartyAssets/` (see in-editor licenses and `Code_References_Bibliography_P2.pdf` where cited).

---

## AI tools used (disclosure)

| Tool | How it was used |
|------|-----------------|
| **Cursor AI** | Code assistance, refactoring, documentation drafts, debugging Unity/Ollama integration |
| **ChatGPT** | Planning prompts, rubric wording, design reviews |
| **Ollama (Llama 3.2)** | Runtime narrative generation inside the shipped game — not used to write C# automatically at build time |

All generated **game dialogue** at runtime is produced locally on the player machine and is **fictional**. Editor-time AI suggestions were reviewed and tested before commit.

---

## Ethical and safety note

This game includes **fiction** involving grief, death, and occult tropes. It is not medical, therapeutic, or spiritual advice. AI outputs are:

- Constrained by system prompts (no harmful instructions, teen-suitable tone).  
- Overridden for critical story beats by **predefined gates** and **session variables**.  
- Short on the Ouija free path to limit rambling or unsafe elaboration.

Players should treat board text as **in-game fiction**, not factual or personal guidance.

---

## Version tracking and transparency

Development history is stored in **Git** on GitHub:

- Commits and dated entries in **`refinements-changes.md`**  
- Cursor AI session prompts and outcomes referenced in commit messages and the change log where relevant  
- Technical AI design in **`ollama-plan.md`**

Moderators can trace when features (gates, lore pass, voice input, etc.) were added and whether AI assistance was used.

**Latest documented integration milestone:** Session lore pass + gated `wife_*` / `player_name` responses (May 2026).

---

## Content Warning & Mental Health Disclaimer

**Don't Forget to Say GOODBYE** contains themes and depictions related to **suicide, grief, death, emotional trauma, psychological distress, occult practices, and supernatural horror**. The game explores these subjects as part of a fictional narrative experience and **may be emotionally distressing or triggering for some players**.

**Player discretion is advised.** This game is intended for **mature audiences** and is **not recommended for players under the age of 16 (PG16)**.

This experience is **not intended to trivialise, romanticise, or encourage self-harm, suicide, or mental illness**. The narrative is designed to explore themes of loss, emotional closure, and unresolved grief within a fictional horror setting. If at any point the content becomes overwhelming or emotionally distressing, players are encouraged to **pause gameplay** and seek support from a trusted person or professional mental health service.

If you or someone you know is struggling with suicidal thoughts, emotional distress, grief, or mental health challenges, **support is available**.

### South African Mental Health & Crisis Support

#### South African Depression and Anxiety Group (SADAG)

| Service | Contact |
|---------|---------|
| **Suicide Crisis Helpline (24/7)** | **0800 567 567** |
| **Mental Health Helpline** | **0800 456 789** |
| **WhatsApp Support** | **076 882 2775** |

#### FindAHelpline South Africa

Directory of free South African suicide and emotional crisis support services.

#### Lifeline South Africa

Emotional wellness and crisis counselling support.

### Emergency Services

| Service | Contact |
|---------|---------|
| **Ambulance** | **10177** |
| **Police Emergency** | **10111** |

*You are not alone, and support is available.*

---

## Legal Disclaimer

**Don't Forget to Say GOODBYE** is an **original student-developed project** created for **educational, research, and entertainment purposes**. Any references to séance rituals, Ouija boards, occult symbolism, tarot imagery, spiritual communication, folklore, or supernatural practices are **entirely fictional** and are used transformatively within the context of an original horror narrative.

This project is **not affiliated with, endorsed by, sponsored by, or associated with** any existing trademark holders, copyrighted properties, organisations, or brands. All trademarks, logos, software, technologies, and third-party assets referenced or utilised during development remain the intellectual property of their respective owners.

Any third-party assets, tools, models, plugins, audio, fonts, or technologies used within the project are credited appropriately in accordance with their respective licensing agreements and usage permissions. The developer does **not** claim ownership over externally sourced assets or technologies beyond their permitted use.

The AI-generated dialogue systems implemented within the project are powered through **locally hosted Large Language Models** using **Ollama**. AI outputs are generated dynamically and are intended **solely for fictional narrative interaction** within the game environment.

This project does **not** promote harmful occult practices, self-harm, violence, or illegal activity. All supernatural and ritualistic content is presented as **fictional horror material** intended for **mature audiences only**.

