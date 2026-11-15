# Space Agency Simulator

Build rockets, manage your agency, launch missions, and keep the budget alive. A Unity space-agency management game playable directly in the browser.

**▶ Play now:** [www.spaceagency.earth](https://www.spaceagency.earth) · [Vercel mirror](https://webpage-build-eight.vercel.app)

> [!IMPORTANT]
> **AI disclosure:** Approximately **60% of all coding work** in this project was done with AI assistance.
> AI was used for implementation, refactoring, debugging, and technical documentation. The remaining **~40%** was written and reviewed by humans. Game design, art direction, and final review remain human-owned.

| Coding contribution | Share |
| --- | --- |
| AI-assisted coding | **~60%** |
| Human coding & review | **~40%** |

## Features

- **Rocket builder** — assemble rockets from parts, save designs, and reuse them.
- **Mission launches** — launch missions with success chances, explosions, and payouts.
- **Agency economy** — budget, mood, daily government grants, and monthly income.
- **Upgrades** — Advertisements, Reusable Rockets, Manned Missions, and Cheaper Parts.
- **Persistent time system** — in-game clock, day/night cycle, and daily payouts at midnight.
- **Save system** — progress, upgrades, economy, and time are stored in `space_agency_save.json`.
- **NASA integration** — live Astronomy Picture of the Day, EPIC Earth imagery, and near-Earth object counts.
- **Overlays** — first-start tutorial, pause menu, and settings without altering the existing game UI.
- **WebGL build** — playable in the browser, no installation required.

## Controls

| Input | Action |
| --- | --- |
| Mouse | Interact with UI and build rockets |
| `F1` | Open/close the NASA briefing |
| `Esc` | Pause/resume, or close the NASA briefing |
| `Enter` / `Space` | Advance tutorial steps |

## Tech stack

- **Unity 6000.6.8f1** with the Universal Render Pipeline (URP)
- **TextMeshPro** for UI text
- **Unity Input System**
- **C#** gameplay, economy, save, and UI systems
- **NASA APIs** — APOD, EPIC, and NeoWs
- **WebGL** deployment (Vercel + custom domain)

## Run locally

1. Install **Unity 6000.6.8f1** (or a compatible Unity 6 version).
2. Open the project folder in Unity Hub.
3. Open `Assets/Scenes/LoadingScreen.unity` or `Assets/Scenes/MainMenu.unity`.
4. Press **Play** in the editor, or create a **WebGL** build via `File → Build Profiles`.

The live browser build is available at [www.spaceagency.earth](https://www.spaceagency.earth).

## Project structure

```
Assets/
├── Scripts/
│   ├── Economy/   # money, mood, grants, upgrades, game time
│   ├── NASA/      # APOD/EPIC/NeoWs client and briefing overlay
│   ├── UI/        # tutorial, pause, settings, runtime UI factory
│   └── ...        # rocket builder, missions, save system
├── Scenes/        # menu, game, rocket builder, overlays
├── Prefabs/       # rockets, parts, UI
└── Fonts/         # custom TMP font
```

## AI usage

This repository is intentionally transparent about AI involvement:

- **~60% of all coding work was AI-assisted.**
- AI contributed to gameplay systems, UI overlays, NASA API integration, refactoring, debugging, and documentation.
- **~40% was human-written and human-reviewed**, including design decisions, art, balancing, testing, and final integration.

If you are evaluating the project, please treat the AI-assisted portion accordingly and review the code before production use.
