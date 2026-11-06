# Design Decisions

Key product, game-design and architecture decisions behind **GodBoard**, with the alternatives considered and the reasoning. Updated as the project evolves.

---

## Game design

### The die is the pantheon
**Decision:** a single d6 where each of four gods owns a face: Zeus 1, Thor 6, Ra 2, Morrígan 5. Faces 3 and 4 are universal omens.
**Why:** opposite faces of a d6 always sum to 7, so **a god's sacred number is its rival's cursed number**. The jam theme ("Lucky Number") becomes the core rule instead of decoration.
**Alternatives:** six gods (one per face) was double the art, powers and AI for a one-week jam. A d4 has no opposite faces, makes curses too frequent (25% per roll) and is harder to read at a glance.

### Real cultural lucky numbers
**Decision:** 3 = good fortune for everyone; 4 = bad omen (in Chinese and Japanese, "four" sounds like "death").
**Why:** it grounds the theme in real superstitions, not just invented ones.

### Ancient mythologies, not living religions
**Decision:** Greek, Norse, Egyptian and Irish mythology.
**Why:** a conquest game where deities destroy each other should not feature currently practiced religions.

### Familiar skeleton, distinct identity
**Context:** territories, reinforcements and attacks are a well-known area-control skeleton. That is good for readability: a player understands it in seconds.
**Decision:** keep the skeleton, but put the lucky number at the front:
1. **Temples are the main win condition**, not world domination. It is a short race to the center (about 10 minutes), not a war of attrition.
2. **The Offering**, a push-your-luck decision unique to this game.
3. **An altar-shaped pantheon board**, never a world map.

### The Offering
**Decision:** while losing a rolled combat, the attacker may sacrifice 1 troop to re-roll their own die (once per combat, 3+ troops in the attacking territory). The re-roll stands even if worse.
**Why:** it is a meaningful risk decision tied directly to the lucky number.
**Change:** initially it was allowed at any time. It was restricted to losing rolls so it reads as a clear "pray" moment, and the button only appears when it matters.

### Combat reads omens before numbers
**Decision:** each roll is read for its own god: Sacred beats Neutral beats Cursed. Only when both are neutral does the higher number win. Every other tie goes to the defender.
**Why:** a high number can still lose to a better omen, so every roll is about *your* lucky number.

### Destiny Die every round
**Decision:** the god on the rolled face gets +2 reinforcements and its rival −2; 3 gives everyone +1; 4 removes one troop from each nation's largest army (never emptying a territory).
**Why:** the lucky number drives the economy too, not just combat, and the whole table feels every roll.

### Balanced with data: the Destiny Die opens the round
**Decision:** the god on the Destiny face plays first that round (on 3 or 4 the opener rotates), and moving into an empty territory sends only a third of the source's troops.
**Why:** 2000 simulated bot-vs-bot matches per variant. With a fixed order and "all but one" moves, one god won 60% of matches: whole armies chained into empty temples, and whoever played last picked off the leftovers. Part of the skew came from the bot's own tie-break, which was made symmetric. Nine variants were compared; this one keeps every god between 22% and 27% with matches of about 7-8 rounds.
**Alternatives:** an opening roll-off ritual was the fairest per god but made matches longer and only mattered once; "favored god plays last" introduced its own seat bias.

### Temple victory checked at the start of your own turn
**Decision:** holding 3 of the 4 temples wins only if you still hold them when your next turn begins.
**Why:** rivals get one full round to respond, which creates tension and comebacks.

### Scope restraint
**Decision:** unique god powers, hot-seat mode and per-god VFX are stretch goals. The game must be complete without them.
**Why:** a finished, polished small game beats an ambitious unfinished one.

---

## UX and visual design

### Colorblind-safe palette, never color alone
**Decision:** Okabe-Ito colors (Zeus `#56B4E9`, Thor `#D55E00`, Ra `#E69F00`, Morrígan `#1A1A1A`), and every nation-owned element also shows its icon (bolt, hammer, sun, crow).
**Alternative rejected:** yellow, orange and red were too close to each other, especially with color vision deficiencies.

### Always explain why
**Decision:** every combat result carries the winner, the reason, both rolls and both omens, so the UI can always say *why* something happened.
**Why:** the game must teach itself within the first minute.

### Identity always on screen
**Decision:** a fixed HUD card shows the player's god, sacred and cursed numbers and the goal ("hold 3 temples at your turn start").
**Why:** a UX review found that first-time players never connected the die to *their* god. The lucky number must be readable before the first combat.

### Readable pieces and highlights without color
**Decision:** pieces are layered like painted wooden tokens (offset shadow, ink outline, nation border, tint fill) with the god icon in ink. "Selected" is a thick solid ring with a slight lift; "legal target" is a thin dashed ring that gently pulses. Both are checked in a grayscale capture.
**Why:** gold-on-gold or blue-on-blue icons were hard to read, and two rings that differed only by color failed the "never color alone" rule.

### Explain every number
**Decision:** the reinforcement prompt shows its breakdown ("2 base + 1 for 2 territories + 2 Destiny"), every combat shows both omens and the reason, and a round-limit victory says which tie-break decided it.
**Why:** the game must always show *why* something happened.

### UI Toolkit with design tokens
**Decision:** UI Toolkit (UXML + USS) for all screen-space UI. The colors, sizes, spacing and motion values in [`design-system.md`](design-system.md) map 1:1 to USS variables.
**Why:** the design system and the implementation cannot drift apart.

---

## Architecture

### Three layers, rules first
**Decision:** **Core** (pure C# rules, no `UnityEngine`, enforced by the assembly definition) → **AI** (bots) → **Presentation** (MonoBehaviours, UI Toolkit). Dependencies only point down.
**Why:** rules are testable without the engine, bots use exactly the same rules as the player, and the presentation can change freely.

### Model–view with events
**Decision:** `Match` is the only class that changes game state. The presentation reads state, calls commands (`PlaceTroop`, `Attack`, `MakeOffering`, `ConfirmCombat`, `EndTurn`) and reacts to events raised **after** each state change. `Territory` setters are `internal`.
**Why:** a single source of truth, and no way for the UI to bypass a rule.

### Legality has one source of truth
**Decision:** `CanAttack` and `CanMakeOffering` are used by the commands, the UI highlights and the bots alike. They never allocate or throw.

### Explicit turn state machine
**Decision:** `TurnPhase` (NotStarted → Reinforce → Attack ⇄ CombatPending → MatchOver), and every command validates the phase first.
**Why:** no scattered booleans, and illegal sequences fail loudly.

### Deterministic randomness
**Decision:** all dice go through an injectable `IRandom`, the only interface in Core.
**Why:** deterministic tests and seeded headless simulations of full matches, used for balancing.

### Derived rules over hand-written tables
**Decision:** a cursed face is computed as `Die.Opposite(SacredFace(god))`, and the enum order of `RollOutcome` is the combat rank.
**Why:** the core rule lives in one line, and data cannot contradict itself.

### Bots use the same rules as the player
**Decision:** `BotPlayer` is a stateless, deterministic static class in its own assembly (AI → Core only). It reads only `CanAttack` / `CanMakeOffering` and returns choices; the presentation issues the commands. Ties are broken as if the board were seen from the bot's own corner.
**Why:** bots can't cheat, can be tested headlessly, and the same code drives thousands of balance simulations. An earlier "lowest id" tie-break made two corners fight over the same temple and skewed win rates.

### Commands only from the game loop
**Decision:** a single coroutine in `MatchController` issues every command for humans and bots; Core event handlers only update visuals, and UI buttons only set flags the loop consumes.
**Why:** no re-entrancy into `Match` from inside its own events, and one flow for every player.

### Simplicity first
**Decision:** no frameworks, DI containers, event buses, reflection or complex generics. Static functions for pure rules, enums and plain classes elsewhere. If something can be deleted without losing behavior, it is deleted.
**Why:** it is a small game, and the code should read in minutes.

---

## Process

- **Every phase is reviewed before merging:** correctness, architecture, and unnecessary complexity.
- **Short-lived branch per system, merged through a documented PR.** `main` always builds.
- **Rules are verified headlessly** (netstandard2.1, C# 9, Unity's NUnit) and in the Unity Test Runner.
- **AI-assisted development is disclosed** in [`ai-usage.md`](ai-usage.md).
