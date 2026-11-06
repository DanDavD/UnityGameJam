# CLAUDE.md

Guidance for Claude Code working in this repository.

## Context

- **What:** Solo entry for the **Official 2026 Unity Game Jam**. Theme: **"Lucky Number"**.
- **Why:** Portfolio piece. It must show (1) interest in board games, (2) strong, deliberate AI-assisted workflow, (3) solid Unity + C# skill, (4) a finished, polished product, even if small.
- **Target readers of this repo:** recruiters/engineers at studios (digital board games, action games). Code quality, clear architecture and clean commit history matter as much as the game.
- **Working title:** TBD (repo codename `GameJam`). Never put the word "Unity" in the game's name.
- **Talk to the developer in Spanish.** Code, comments, commits, README and in-game text are in **English**.

## Key dates (developer timezone UTC-6)

| Milestone | Date |
|---|---|
| Jam start | Thu 2026-11-05 |
| Feature freeze | Sun 2026-11-08 (end of day) |
| Game finished | Wed 2026-11-11, midnight |
| Buffer: itch page, README, GIF, devlog only | 2026-11-12 → 2026-11-14 |
| **Hard deadline** (no late submissions) | **Sat 2026-11-14 06:00** (12:00 UTC) |

After the deadline the build must not change (disqualification from voting).

## Game design

Digital **board game of conquest** with dice. One human vs 3 bots (hot-seat is a stretch goal).

### The die is the pantheon (d6)

| Face | Meaning | Color | Icon |
|---|---|---|---|
| 1 | **Zeus** (Greece) | Sky blue `#56B4E9` | lightning ⚡ |
| 6 | **Thor** (Norse), rival of Zeus | Red `#D55E00` | hammer |
| 2 | **Ra** (Egypt) | Gold `#E69F00` | sun |
| 5 | **Morrígan** (Irish/Celtic), rival of Ra | Black `#1A1A1A` | crow |
| 3 | **Good fortune**: small boon for everyone | | |
| 4 | **Bad omen**: small penalty for everyone (4 ≈ "death" in Chinese/Japanese) | | |

- **Opposite faces of a d6 sum to 7.** A nation's **sacred number** is its god's face; its **cursed number** is the opposite face, which is its rival's sacred number.
- Every roll is read per nation as **sacred / cursed / neutral**.

### Core rules (MVP)

- **Destiny Die:** rolled once at the start of each round. The god on that face is **favored** for the round; its rival is **weakened**. 3/4 apply the universal boon/penalty.
- **Combat:** attacker and defender each roll a d6. Sacred = activate god power. Cursed = automatic loss. Neutral = higher number wins (tie → defender).
- **Reinforcements:** troops per turn based on territories; extra if the Destiny Die shows your sacred number.
- **Win:** control the 3 central **temples**, eliminate all rivals, or have the most territories after 15 rounds. Target match length is **about 10 minutes**.
- **Map:** 12 to 15 territories, one nation per corner, temples in the center.

### God powers (stretch goal, the game must work without them)

- **Zeus:** lightning destroys one enemy troop anywhere.
- **Thor:** wins the combat regardless of numbers.
- **Ra:** doubles troop production this turn.
- **Morrígan:** enemy troops killed in this combat join her side.

### Design principles (from the jam judging rubric)

- **Theme is 40% of the score** (x2 weight). Test every feature against: *"if the lucky number were removed, would this still work?"* Prefer designs where the answer is no.
- **Teaches itself.** A player must know what to do within the first minute without reading. Always show *why* something happened (which die face, sacred/cursed/neutral, power used).
- **Restraint.** No feature that doesn't reinforce the core idea. Scope must match one week, one person.
- **Coherent presentation.** Everything looks like a **physical board game**: wooden pieces, parchment board, every god as a card with the same frame. Nations differ only by color + icon.
- **Never rely on color alone.** Every nation-owned element shows its icon too (colorblind-safe Okabe-Ito palette).

## Architecture

Unity **6000.6.5f1**, template **Universal 2D (URP)**, new **Input System**, target **WebGL** (playable in the browser on itch.io).

Planned layout (create as needed):

```
Assets/_Project/
  Scripts/
    Core/        Pure C# rules: dice, nations, combat, turns, game state. NO UnityEngine references (asmdef with noEngineReferences).
    AI/          Bot decision-making. Pure C#, works only on Core.
    Gameplay/    MonoBehaviours that drive Core and react to its events.
    UI/          HUD, dice display, tooltips, menus.
  Tests/
    EditMode/    Unit tests for Core and AI + headless bot-vs-bot simulations for balancing.
  Art/ Audio/ Prefabs/ Scenes/ ScriptableObjects/
```

- **Rules live in Core and never depend on visuals.** Presentation subscribes to events (`OnDiceRolled`, `OnCombatResolved`, ...). This keeps the rules testable and allows running thousands of headless matches.
- **Randomness goes through an injectable `IRandom`** so tests and simulations are deterministic (seeded).
- **Turn flow is an explicit state machine**, not scattered booleans.
- Nation/god data in **ScriptableObjects** (name, face, color, icon, power).

## C# conventions

- One public type per file; file name = type name. Namespaces mirror folders (`GameJam.Core`, `GameJam.AI`, ...).
- `PascalCase` types/methods/properties, `_camelCase` private fields, `camelCase` locals/parameters.
- `[SerializeField] private` over public fields. No `FindObjectOfType` / `GameObject.Find` in gameplay code.
- Avoid allocations in `Update`; prefer events over polling.
- Small, readable methods. Comment the *why*, not the *what*.
- The developer must understand and be able to explain every line that lands in the repo. When generating non-trivial code, briefly explain the design choice.

## Git

- Small, focused commits with imperative messages (`Add combat resolution`, `Fix tie-breaking in combat`).
- **Never add `Co-Authored-By` trailers or any Claude/AI attribution to commits or PRs.** The developer is the sole author.
- Commit `.meta` files together with their assets. Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, builds.
- Ask before pushing.

## Jam compliance checklist

- [ ] WebGL build marked "playable in browser" on itch.io (test a WebGL build from day 2)
- [ ] itch page: description, controls, credits for every external asset, team member
- [ ] **Generative AI disclosure** on the itch page describing how AI was used (required for prize eligibility); keep `docs/ai-usage.md` updated as we go
- [ ] Trademark notice in credits if "Unity" is mentioned: *"<Game> is not sponsored by or affiliated with Unity Technologies or its affiliates. Unity is a trademark or registered trademark of Unity Technologies or its affiliates in the U.S. and elsewhere."*
- [ ] Only the official, unmodified "Made with Unity" logo; never the Unity cube logo
- [ ] Record the source and license of every asset when it is added (`docs/credits.md`)
