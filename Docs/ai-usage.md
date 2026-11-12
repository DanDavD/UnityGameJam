# AI Usage Disclosure

This game was made for the Official 2026 Unity Game Jam, which requires disclosing any use of generative AI. This file is kept up to date during development and summarized on the itch.io page.

## Tools

- **Claude Code** (Anthropic) as a development assistant, with a project-specific setup in `.claude/`:
  - Subagents: `unity-developer`, `code-reviewer`, `ui-designer`, `ux-reviewer`, `jam-producer`
  - Workflow skills: `/feature`, `/review-code`, `/ui-check`, `/scope`
  - Reference skills: Unity's official plugin and selected Nice-Wolf-Studio Unity skills (MIT)
  - MCP: MCP for Unity (Editor control, tests, console), Context7 (documentation)

## How AI was used

| Area | Use | Human role |
|---|---|---|
| Planning and game design | Brainstorming theme interpretations, checking the design against the judging rubric, scoping the week | Final concept, rules and every design decision |
| Project setup | Generating `CLAUDE.md`, the design system document, agent and skill definitions, hooks | Review and approval of every file |

*(Add a row per system as development goes: code, tests, UI, art, audio, text.)*

## Not AI-generated

*(List what was made entirely by hand or comes from credited third-party assets, see `credits.md`.)*

## Power sounds

The god-power and soft result sound effects were produced by Python synthesis scripts written with Claude (`Tools/MusicGen/`); the wiring through `AudioCues` and `MatchPresenter` was also written with Claude and reviewed by the developer. A first set of procedural music loops was tried and removed on Nov 12.

## Main theme

"The Six Faces" (`Assets/_Project/Audio/music-the-six-faces.wav`) was composed with Claude from the developer's brief (slow modal folk, acoustic instruments, no synths): Claude wrote the score as a MIDI file (`Tools/MusicGen/compose_theme.py`) and a pure-Python renderer that models every instrument (plucked-string synthesis for the bouzouki, breath and bow noise for flute and cello, formant voices for the choir) (`Tools/MusicGen/render_theme.py`). No samples, recordings or AI music services were used. The main six-note motif is the die: the "destiny" phrase D-A-G-F-E-D and its inversion for the rival god. The playback code was written with Claude and reviewed by the developer.
