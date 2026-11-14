# GodBoard — itch.io Submission Page

This document contains the exact text and metadata ready to be copied into the itch.io project edit page.

---

## Metadata

* **Title:** GodBoard
* **Short description / Tagline:** A digital board game of faith and conquest where the die is the pantheon. Opposite faces sum to 7: your lucky number is your rival's curse.
* **Classification:** Games
* **Kind of project:** HTML (playable in the browser)
* **Release status:** Released
* **Genre:** Strategy / Board Game
* **Tags:** 2D, Turn-Based, Strategy, Board Game, Mythology, Dice, Unity, Singleplayer, Minimalist
* **Viewport dimensions:** 1280 × 720 (Default), Embed in page, Support Fullscreen

---

## Description / Body Text (Markdown / HTML)

### The Pantheon is Cast

Four ancient nations. Four gods. One six-sided die.

In **GodBoard**, the six faces of the die belong to the world:

| Face | Nation | God | Rival (Opposite Face) |
|:---:|---|---|:---:|
| ⚀ **1** | Greece | Zeus | ⚅ **6** Thor |
| ⚅ **6** | Norse | Thor | ⚀ **1** Zeus |
| ⚁ **2** | Egypt | Ra | ⚄ **5** Morrígan |
| ⚄ **5** | Celts | Morrígan | ⚁ **2** Ra |
| ⚂ **3** | *Good fortune for all* | | |
| ⚃ **4** | *Bad omen for all* | | |

Opposite faces of a six-sided die always add up to 7: **your sacred number is your rival's, and it is your cursed number.** Every roll in the game — the round's Destiny Die, every combat clash, and reinforcement modifiers — is either **sacred**, **cursed**, or **neutral** for you.

---

### How to Play

#### Conquest Goals (First to achieve wins)
1. **Hold 3 of the 4 Temples** (the 4 diamond-shaped territories on the board: North, South, East, West) at the start of your turn (Primary victory).
2. **Eliminate all rival nations** (Last standing).
3. **Control the most territories after 15 rounds** (Tie-breaker).

#### Round Flow
Each round begins with the rolling of the **Destiny Die**:
* **God's Face (1, 6, 2, 5):** Activates that god's unique **Day power** for the entire round. The favored god opens the round!
* **3 (Good Fortune):** Every living nation gains **+1 reinforcement**.
* **4 (Bad Omen):** Every nation loses 1 troop from its largest army ($\ge 2$).

#### Your Turn (Reinforce → Attack → End Turn)
1. **Reinforce:** Place your allotted troops onto any territory you control to reinforce your borders.
2. **Attack:** Select an owned territory with 2+ troops and strike an adjacent land:
   * **Empty Territory:** Claimed peacefully, moving a third of your forces forward.
   * **Enemy Territory:** Both sides roll d6 combat dice.
     * **Offering (Risk & Faith):** If you are losing a combat and hold 3+ troops, sacrifice 1 troop to re-roll your die!
     * The loser of the final roll loses 1 troop. When the defender drops to 0, advance your army and conquer the territory.
   * **Sacred Road:** Rolling your sacred number opens the Sacred Road, allowing an army to march up to 4 steps across your friendly lands (with an option to skip the march and keep attacking freely).
   * **Wrath of the Gods:** Channel your accumulated Favor ($\bigstar$) to unleash your god's unique divine power (e.g. Zeus's double-strike Thunderbolt).
3. **End Turn:** Conclude your actions and pass the turn to the next living deity.

---

### Controls & Options
* **Mouse Left-Click:** Select territories, reinforce, issue attacks, trigger Offerings, interact with UI buttons.
* **Mouse Right-Click / Escape:** Cancel active selection, close overlays or info panels.
* **Mouse Hover:** Hover over any of the 4 diamond Temple territories to reveal dynamic tooltips with ownership status.
* **SOUND Button:** Open the volume panel to fine-tune music and sound effects independently.
* **▶▶ FAST BOTS:** Toggle fast animations for rapid bot turns.

---

### Jam Theme: "Lucky Number"
Every roll in GodBoard is fundamentally tied to the sacred/cursed duality of the d6. Because opposite faces sum to 7, a roll that blesses you with divine favor directly curses your mortal rival with weakness. Your lucky number is quite literally your rival's curse.

---

### AI Usage Disclosure
* **100% Human Design & Direction:** The complete game design, system architecture, core rules, game flow, and the thematic "Lucky Number" concept (where opposite faces of a die sum to 7, turning a lucky roll into a rival's curse) were conceived and designed entirely by the developer (**DanDavD**).
* **AI as an Implementation Tool:** Claude Code and its 5 specialized developer subagent personas served strictly as an interactive coding tool. The developer instructed the AI with precise specifications, reviewed every single file line-by-line, corrected AI errors, debugged and hand-edited code directly whenever necessary, and approved all commits and assets.
* **Audio & Music:** Procedural audio synthesis scripts and the physical modeling synthesis for the acoustic main theme were authored in Python with AI assistance under the developer's strict musical brief and direction; no third-party generative music services (such as Suno or Udio) were used.
* Full disclosure is documented in the project's [`Docs/ai-usage.md`](Docs/ai-usage.md).

---

### Credits
* **Design, Code & Audio Production:** DanDavD
* **Fonts:** *Cinzel* (The Cinzel Project Authors, SIL OFL 1.1) and *Inter* (Rasmus Andersson, SIL OFL 1.1).
* **Audio Samples:** Kenney Casino Audio & Interface Sounds (CC0 1.0 Public Domain).
* **Main Theme:** *"The Six Faces"* — Original modal acoustic theme composed and synthesized via custom procedural physical modeling.

---

### Legal Notice
GodBoard is not sponsored by or affiliated with Unity Technologies or its affiliates. Unity is a trademark or registered trademark of Unity Technologies or its affiliates in the U.S. and elsewhere.
