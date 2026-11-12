# GodBoard

A digital **board game of conquest and faith** where the die is the pantheon. Made solo in one week for the **Official 2026 Unity Game Jam**. Theme: **"Lucky Number"**.

▶ **[Play in Browser on itch.io](https://dandavd.itch.io/godboard)** *(WebGL build)*

---

## The Concept

Four ancient nations, four gods, one six-sided die.

| Face | Nation | God | Rival (Opposite Face) |
|:---:|---|---|:---:|
| ⚀ **1** | Greece | Zeus | ⚅ **6** Thor |
| ⚅ **6** | Norse | Thor | ⚀ **1** Zeus |
| ⚁ **2** | Egypt | Ra | ⚄ **5** Morrígan |
| ⚄ **5** | Celts | Morrígan | ⚁ **2** Ra |
| ⚂ **3** | *Good fortune for all* | | |
| ⚃ **4** | *Bad omen for all* | | |

Opposite faces of a six-sided die always add up to 7, so **your lucky number is your rival's curse**. Every roll in the game — the round's Destiny Die, combat rolls, and reinforcement modifiers — is read per nation as **sacred**, **cursed**, or **neutral**.

---

## How to Play

### Objectives (First to achieve wins)
1. **Control 3 of the 4 Temples** at the start of your turn (Primary conquest goal).
2. **Eliminate all rival nations** (Last standing).
3. **Control the most territories after 15 rounds** (Tie-breaker).

### Turn Structure
Each round starts with a roll of the **Destiny Die**:
- **A god's face (1, 6, 2, 5):** That god is **favored (+2 reinforcements)**, while their rival is **weakened (−2 reinforcements)**. The favored god opens the round.
- **3 (Good Fortune):** All nations gain **+1 reinforcement**.
- **4 (Bad Omen):** Every nation loses 1 troop from its largest army (if $\ge 2$ troops).

Then, nations take their turns in clockwise cycle:

1. **Reinforce:** Place your allotted troops on any territory you control to fortify your positions.
2. **Attack:** Select an owned territory with 2+ troops and click an adjacent territory:
   - **Empty Territory:** Captured peacefully, marching a portion of your army into it.
   - **Enemy Territory:** Both sides roll combat dice.
     - **Offering (Risk & Faith):** If you are losing the roll and have 3+ troops, sacrifice 1 troop to re-roll your die!
     - The loser of the final roll loses 1 troop. When the defender reaches 0, you capture the land and advance your army.
   - **Sacred Road:** Rolling your sacred number opens the Sacred Road, allowing an entire army to march up to 2 steps across your connected territories.
   - **Wrath of the Gods:** Channel earned Favor ($\bigstar$) to unleash devastating divine wrath upon your enemies.
3. **End Turn:** Complete your moves and pass the initiative to the next god.

### Controls
* **Left Click:** Select territories, reinforce, issue attacks, trigger Offerings, activate buttons.
* **Right Click / Escape:** Cancel selection, close info cards or sound settings.
* **Sound Button:** Open the volume panel to adjust music and SFX independently.
* **Fast Bots Toggle (▶▶):** Speed up bot turns for rapid-fire play.

---

## Tech Stack & Architecture

- **Engine:** Unity 6 (`6000.6.5f1`), URP 2D, Unity Input System, UI Toolkit (USS/UXML).
- **Target:** WebGL (zero-install browser play via Gzip decompression fallback).
- **Decoupled Architecture:** Game rules written as pure, framework-free C# (`GameJam.Core`), completely independent of Unity's presentation layer (`GameJam.Gameplay`).
- **Deterministic Bot AI:** Stateless AI players (`GameJam.AI`) tested and tuned across 2,000+ headless automated match simulations for balanced win-rates (21%–29% per god).
- **Procedural Audio & Music:** Original modal acoustic theme (*"The Six Faces"*) and sound effects generated via custom Python physical modeling synthesis (`Tools/MusicGen/`).

---

## Creator Statement & AI Usage

**100% of the game design, concept, game flow, system architecture, and the thematic "Lucky Number" mechanic were created and designed entirely by DanDavD.**

[Claude Code](https://claude.com/claude-code) was used strictly as an implementation tool. The developer instructed the AI, orchestrated a team of 5 specialized developer subagent personas, reviewed and debugged all code line-by-line, corrected mistakes by hand, and held sole approval authority over all code and assets. Full disclosure of how AI was used is documented in [`Docs/ai-usage.md`](Docs/ai-usage.md).

---

## Credits & Licenses

* **Design, Programming, Audio Production & Direction:** DanDavD
* **Display Font:** *Cinzel* by The Cinzel Project Authors ([SIL OFL 1.1](Assets/_Project/Fonts/Cinzel-OFL.txt)).
* **Interface Font:** *Inter* by Rasmus Andersson ([SIL OFL 1.1](Assets/_Project/Fonts/Inter-OFL.txt)).
* **Audio FX:** Kenney Casino Audio & Interface Sounds ([CC0 1.0 Public Domain](Docs/credits.md)).
* Complete credits and third-party details are maintained in [`Docs/credits.md`](Docs/credits.md).

---

## Legal

GodBoard is not sponsored by or affiliated with Unity Technologies or its affiliates. Unity is a trademark or registered trademark of Unity Technologies or its affiliates in the U.S. and elsewhere.
