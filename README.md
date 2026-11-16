# GodBoard — Turn-Based Digital Board Game

A turn-based digital board game of territorial conquest and asymmetrical divine powers where the die is the pantheon. Developed solo in a 1-week sprint for the **Official 2026 Unity Game Jam** (Theme: "Lucky Number").

- **Play in Browser (WebGL):** [dandavd.itch.io/god-board](https://dandavd.itch.io/god-board)
- **Game Jam Submission & Player Feedback:** [itch.io/jam/official-unity-game-jam-2026/rate/5357436](https://itch.io/jam/official-unity-game-jam-2026/rate/5357436)
- **Agile Roadmap (Trello):** [trello.com/b/NE6zNufB/godboard-game-jam](https://trello.com/b/NE6zNufB/godboard-game-jam)
- **Repository:** [github.com/DanDavD/UnityGameJam](https://github.com/DanDavD/UnityGameJam)
- **Author:** Daniel David ([@DanDavD](https://github.com/DanDavD))
- **Stack:** Unity 6, C# 12, UI Toolkit (UXML/USS), Universal Render Pipeline (URP 2D), WebGL

---

## The Concept

Four mythic nations (Greece, Norse, Egypt, Celts) compete for divine supremacy across a 13-territory altar board. The mathematical core leverages standard six-sided dice symmetry: opposite faces always sum to 7 (1+6, 2+5, 3+4). Rolling your sacred number activates divine events and initiative, while directly cursing your opposing rival. Matches are fast, tense, and decisive (7 to 10 minutes), balancing board positioning, push-your-luck offering sacrifices, and tactical conquest.

---

## Product & UI/UX Engineering

Built with a strong focus on interface clarity, player onboarding, and tactile audio-visual feedback:

- **Modern UI Toolkit Architecture:** Completely avoids legacy Canvas performance overhead. Built using semantic UXML layouts and a centralized design token system (`Tokens.uss`) for colors, typography scales, border radii, and transitions.
- **Accessibility & Visual Hierarchy:** Designed with an Okabe-Ito colorblind-safe palette, pairing every player color with unique deity iconography and high-contrast dual typography (Cinzel display for divine banners, Inter for clean numerical readability).
- **Progressive Disclosure Onboarding:** Eliminates cognitive overload through an interactive 4-quadrant rulebook modal, dedicated deity selection cards with divine power breakdowns, and dynamic board tooltips.
- **Contextual In-Game Feedback:** Real-time visual banners for turn-order duels ("SACRED BEATS NEUTRAL"), dynamic Destiny Die callouts, animated troop reinforcement badges, and skip-march confirmations.
- **Independent Audio Mixing:** Floating audio settings panel controlling master volume, acoustic Greek-modal score, and WebGL-tuned sound effects with baked echo tails.
- **Responsive Flexbox Layout:** Fluid viewport scaling adapting dynamically from 1280x720 windowed mode up to full-screen 4K displays.

---

## Systems Architecture & Game Engineering

GodBoard was designed as a production-grade software system prioritizing decoupling, testability, and deterministic logic:

```text
+-------------------------------------------------------------+
|               Presentation Layer (Unity 6 & URP)            |
|   UI Toolkit (UXML/USS) · Event Presenter · 2D Lighting    |
+------------------------------+------------------------------+
                               | Events & Presenter Bindings
+------------------------------v------------------------------+
|               Pure C# Domain Engine (GameJam.Core)          |
|   Decoupled from UnityEngine · Turn State Machine          |
|   Deterministic IRandom · Board Graph · Combat Math        |
+------------------------------+------------------------------+
                               | Evaluates State
+------------------------------v------------------------------+
|               Heuristic AI Engine (GameJam.AI)              |
|   Stateless Bot Decision Heuristics · Monte Carlo Runner    |
|   2,000+ Match Headless Balance Simulator                   |
+-------------------------------------------------------------+
```

### 1. Engine-Free Pure C# Domain Core
The entire game state, board graph, combat resolution, and turn state machine are written in pure C# 12 without any dependency on `UnityEngine` or `MonoBehaviour`.
- Allows instant test execution outside the Unity engine lifecycle.
- Injected `IRandom` interface guarantees deterministic simulation, headless test runs, and reproducible match states.

### 2. Headless Simulation & Balance Tuning
Bot players are implemented as stateless heuristic evaluators. To balance the four asymmetrical god powers and the push-your-luck Offering mechanic:
- Built a headless simulation harness executing **2,000+ automated bot-vs-bot matches**.
- Analyzed win distributions, average match length, and seat-order biases.
- Tuned game parameters until win rates across all four nations converged into a competitive **21% to 29%** balanced corridor.

### 3. Automated Test Suite (387 Tests)
Backed by **387 EditMode unit tests** covering the entire game domain:
- **Combat Resolution:** Sacred vs. neutral vs. cursed hierarchy validation and tie-breaking math.
- **Turn State Machine:** Phase transitions (Destiny -> Reinforce -> Attack -> March -> End Turn).
- **Board Graph Connectivity:** Territory adjacency, temple control checks, and pathfinding distances.
- **Balance Invariants:** Mathematical symmetry of dice pairs and god power triggers.

---

## AI-First Engineering Workflow

Developed under an AI-first multiplier approach:
- Architectural constraints, domain models, and game rules were designed and directed by the human engineer.
- Orchestrated specialized AI developer subagent workflows for rapid implementation, test scaffolding, and cross-layer verification.
- Enforced strict human review, manual debugging, and commit approval across all 11,400+ lines of C# and 4,900+ lines of test code.

---

## Quickstart & Verification

### Play in Browser
Open the WebGL build directly: [dandavd.itch.io/god-board](https://dandavd.itch.io/god-board)

### Run Automated Tests via CLI
```bash
/Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app/Contents/MacOS/Unity \
  -projectPath . \
  -quit -batchmode \
  -runTests \
  -testPlatform EditMode \
  -logFile Logs/Tests.log
```

### Build WebGL via CLI
```bash
/Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app/Contents/MacOS/Unity \
  -projectPath . \
  -quit -batchmode \
  -executeMethod GameJam.Editor.BuildScript.BuildWebGL \
  -logFile Logs/Build.log
```
