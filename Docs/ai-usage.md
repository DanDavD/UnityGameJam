# AI Usage Disclosure

This game was made for the Official 2026 Unity Game Jam, which requires disclosing any use of generative AI. This document outlines the development workflow, the human role as sole creator/designer, and how AI was used strictly as an implementation tool.

## Creator Statement & Design Authorship

**100% of the game design, creative direction, system architecture, and gameplay concepts were created and directed entirely by the human developer (DanDavD).**

* **The Theme ("Lucky Number"):** Conceived entirely by the developer. The core mathematical insight — that opposite faces of a six-sided die always sum to 7, meaning your lucky number is inherently your rival's curse — was the foundational premise designed before any code was written.
* **Game Rules & Mechanics:** Every rule, from the 13-territory symmetric board layout and turn-order determination, to the Destiny Die modifiers, the combat resolution system, the Offering re-roll mechanic, the Sacred Road march, and the divine Wrath/Favor abilities, was designed, balanced, and directed exclusively by the developer.
* **Architecture & Engineering Leadership:** The developer established the technical architecture: decoupling pure C# domain logic (`GameJam.Core`) from Unity presentation (`GameJam.Gameplay`), demanding deterministic randomness for automated simulations, and defining the turn state machine.
* **The AI as a Tool:** Claude Code and its subagents were used strictly as an assisted implementation tool — akin to an interactive compiler and digital junior engineering team. The developer directed each task, issued precise specifications, reviewed every single file line-by-line, corrected AI hallucinations and logic errors, wrote and edited code by hand whenever necessary, and held sole approval authority for every commit.

---

## Tools

- **Claude Code** (Anthropic) as an AI development assistant, orchestrating a team of 5 specialized subagent personas defined and instructed by the developer:
  - `unity-developer`: Implemented C# systems, algorithms, and unit tests according to the developer's exact design specifications.
  - `code-reviewer`: Scanned for architectural violations, edge cases, and performance regressions.
  - `ui-designer`: Authored UI Toolkit (USS/UXML) styles following the developer's design tokens and layout directives.
  - `ux-reviewer`: Evaluated visual hierarchy, feedback clarity, and contrast against the design system.
  - `jam-producer`: Tracked rubric alignment, milestone deadlines, and scope cuts directed by the developer (e.g. cutting hot-seat mode).
- **Unity MCP & Context7:** Local bridge for running tests, querying logs, and referencing package documentation.

---

## How AI Was Used (Human-Led Implementation)

| Area | Human Role (Design, Direction & Control) | AI Role (Assisted Implementation) |
|---|---|---|
| **Game Design & Theme** | **100% Human.** Conceived the "Lucky Number" d6 duality (opposite faces sum to 7), the pantheon mapping, 13-territory symmetric board, victory conditions, Sacred Road, and combat Offering rules. | None. Implemented the rules as dictated by the developer's design documents. |
| **System Architecture** | Designed the decoupled structure: pure C# model (`GameJam.Core`), headless bot AI (`GameJam.AI`), UI views (`GameJam.UI`), and Unity glue (`GameJam.Gameplay`). | Scaffolded assembly definitions, interfaces, and classes under the developer's architectural constraints. |
| **Code Implementation & Debugging** | Instructed the AI with granular prompts, line-by-line code review, debugging, identifying logic bugs, correcting faulty code, and writing/editing code by hand when needed. Approved all final code. | Wrote C# code and unit tests based on the developer's step-by-step instructions. |
| **Bot AI & Balance Tuning** | Set balance criteria (each god within 21%–29% win rate). Analyzed headless simulation logs, identified balance flaws, and chose the specific levers to adjust (e.g. Sacred Road steps, army movement rules). | Implemented the stateless deterministic bot decision tree and executed automated 2,000-match headless simulation batches. |
| **Visual Identity & UI** | Established the design system: visual tokens, Cinzel/Inter font hierarchy, dark parchment aesthetic, card layouts, and layout responsiveness (1280×720 to 4K). | Generated initial UXML structures and USS stylesheets matching the design system specifications. |
| **Audio FX & Music** | Directed the acoustic vision (modal folk, acoustic instruments, no synths), thematic motifs (the six-note destiny phrase), sound palette, and volume balance. | Authored Python DSP synthesis scripts (`Tools/MusicGen/`) to procedurally synthesize god power sound clips and render the acoustic main theme from the developer's brief. |
| **Documentation & Lore** | Conceived the narrative tone, god personalities, and gameplay help. Reviewed and edited all copy for clarity and brevity. | Drafted initial text strings, tooltips, and rule summaries under the developer's guidance. |

---

## What Was NOT AI-Generated

- **All Game Design & Intellectual Property:** The complete concept of GodBoard, the thematic interpretation of "Lucky Number", the mathematical design of the die faces, the board topology, and all game rules were created 100% by the developer.
- **Architectural & Technical Decisions:** Decoupling rules from engine, stateless bot design, event-driven presenter pattern, and deterministic RNG injection.
- **Human Supervision, Debugging & Manual Coding:** Every single line of code was reviewed, tested in-editor, debugged, and approved by the developer, who frequently intervened by hand to correct code and adjust implementations.
- **Third-Party Fonts:** *Cinzel* (The Cinzel Project Authors) and *Inter* (Rasmus Andersson), both under SIL Open Font License 1.1.
- **Third-Party Sound Effects:** Kenney Casino Audio and Interface Sounds, under Creative Commons Zero 1.0 (CC0 Public Domain).
- **Game Engine & Packages:** Unity 6 (6000.6.5f1), URP 2D, Input System, UI Toolkit, and TextMesh Pro.

---

## Sound FX & Procedural Audio

All procedural god-power stings, wrath cues, and soft UI sounds were generated using Python DSP synthesis scripts in `Tools/MusicGen/`. The acoustic direction, frequency targets, decay envelopes, and timing with dice animations were guided and tuned by the developer.

## Main Theme: "The Six Faces"

The main theme (`Assets/_Project/Audio/music-the-six-faces.wav`) was created entirely from the developer's creative brief: slow modal acoustic folk using plucked strings (bouzouki), wooden flute, cello, and choir, with strictly no electronic synths or third-party audio generation services (e.g. Suno, Udio). The composition script (`Tools/MusicGen/compose_theme.py`) and procedural instrument physical modeling renderer (`Tools/MusicGen/render_theme.py`) were written with Claude, tuned, and approved by the developer. The recurring six-note melody (D-A-G-F-E-D) and its musical inversion directly mirror the sacred/cursed duality of the die.
