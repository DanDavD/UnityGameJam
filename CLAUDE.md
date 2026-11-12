# CLAUDE.md

Solo entry for the **Official 2026 Unity Game Jam** (theme **"Lucky Number"**) and a **portfolio piece**: it must show interest in board games, a deliberate AI-assisted workflow, solid Unity/C# skill, and a finished, polished product. Code quality, architecture and commit history are read by studio recruiters.

- **Talk to the developer in Spanish.** Code, comments, commits, docs and in-game text are in **English**.
- Game name: **GodBoard** (repo and project codename `GameJam`). Never put "Unity" in the game's name.
- The developer is an **expert C# developer who reviews and debugs but does not hand-write code**. Claude (through the subagents) writes all code; the developer does the final review, testing and approval.

## Dates (UTC-6)

Feature freeze **Sun Nov 8**. Game finished **Wed Nov 11, midnight**. Nov 12-14: itch page, README, GIF, devlog only. **Hard deadline Sat Nov 14 06:00** (no late submissions, no build changes after).

## Game

Digital **board game of faith and conquest**: 1 human vs 3 bots racing for temples on a small altar-shaped board. The territory/attack skeleton is a familiar genre on purpose (instant readability); the identity is the lucky number. **The die is the pantheon** (d6):

| Face | Meaning | Rival (opposite face) |
|---|---|---|
| 1 | Zeus (Greece) | 6 Thor |
| 6 | Thor (Norse) | 1 Zeus |
| 2 | Ra (Egypt) | 5 Morrígan |
| 5 | Morrígan (Celts) | 2 Ra |
| 3 | Good fortune, everyone | |
| 4 | Bad omen, everyone (4 ≈ "death" in Chinese/Japanese) | |

Opposite faces sum to 7: **your sacred number is your rival's, and it is your cursed number.** Every roll is read per nation as **sacred / cursed / neutral**.

### Rules (approved)

**Board (13 territories, symmetric).** Each nation starts in its corner with a **Capital (3 troops)** and a **Village (2 troops)**. Rivals sit in opposite corners: Zeus top-left, Ra top-right, Thor bottom-right, Morrígan bottom-left. **4 Temples** on the edges (North between Zeus and Ra, East between Ra and Thor, South between Thor and Morrígan, West between Morrígan and Zeus) and the **Altar** in the center. Temples and Altar start empty (no owner, 0 troops).

Adjacency: Capital – own Village. Each Village – the two Temples flanking its corner. Altar – all 4 Temples.

**Round.** At the start of every round the **Destiny Die** is rolled:

| Face | Effect this round |
|---|---|
| A god's face (1, 6, 2, 5) | That god is **favored: +2 reinforcements**. Its rival is **weakened: −2 reinforcements** |
| 3, good fortune | Everyone **+1 reinforcement** |
| 4, bad omen | Each nation **loses 1 troop from its largest army** (only if that territory has 2+ troops; ties: lowest territory id) |

The Destiny Die **never shows the face of an eliminated god**: such a face is re-rolled, so the chance is split among the living gods, 3 and 4.

**Turn order (decided by the Destiny Die):** base cycle Zeus → Ra → Thor → Morrígan. If the Destiny face is a god's face, **that god opens the round** and the others follow in the cycle. On 3 or 4 the opener rotates one step from the previous round's opener (round 1 on 3/4: Zeus). **The god who played last in the previous round cannot open the next one** (no back-to-back turns): the opening spot moves to the next living god after them in the cycle (wrapping); the god on the face keeps their Day and modifiers. Eliminated nations are skipped.

**Turn: Reinforce → Attack → End.**

1. **Reinforce:** receive `2 + territories / 2 (integer) + temples owned + Destiny modifier`, minimum 1. Place them one at a time on any owned territory. When all are placed, the Attack phase starts.
2. **Attack (any number of times):** from an owned territory with **2+ troops** to an adjacent territory not owned by you.
   - **Empty unowned territory:** captured directly, no combat, but you may send only **a third of the source's troops** (`max(1, troops / 3)`, the source always keeps at least 1).
   - **Enemy territory:** both roll a d6 (`CombatResolver`). The roll is shown first and the attacker may make an **Offering**: sacrifice 1 troop from the attacking territory to **re-roll their own die**, once per combat, only **while the attacker is losing** the rolled combat and only if the attacking territory has **3+ troops**. The re-roll replaces the attacker's roll, even if it is worse. Then the combat resolves: **the loser loses 1 troop**.
   - **Capture:** when the defender reaches 0, the attacker takes the territory and moves **all troops but one** into it.
3. **End turn.** No separate fortify phase (reinforcements can be placed anywhere).

A nation with no territories is **eliminated**.

**Victory (first that happens):**
1. **Control 3 of the 4 Temples at the start of your turn** (primary goal).
2. **Last nation standing.**
3. **After 15 rounds:** most territories; tie → most troops; tie → earlier in the base cycle (Zeus, Ra, Thor, Morrígan).

### God powers: Favor and the Day of the God (approved Nov 8, phase 6)

Approved by the developer after the feature freeze, as the one deliberate exception. Both layers come from the lucky number and are behind a single on/off flag (`Match` constructor; menu toggle **Pantheon powers / Classic**). Tuning numbers live in Core constants next to `Destiny`, mirrored in data when the presentation needs them.

**1. Day of the God (automatic).** When the Destiny Die shows a god's face, besides the usual +2 / −2, that god's **Day** lasts the whole round. Instant Day effects resolve at round start, like the bad omen, before the opener's turn.

| God | Day effect |
|---|---|
| Zeus | **Storm:** every enemy territory **on the board** with 2+ troops loses 1 troop (never empties a territory); the lost troops **join Zeus's Capital**, or his largest army if he lost it (ties: lowest id). |
| Thor | **Ragnarök:** this round, in every combat Thor is in (attacking or defending), Thor wins ties and, when Thor wins, the loser loses **2** troops instead of 1 (never more than it has; an attacking territory always keeps 1). |
| Ra | **Sun at noon:** +1 troop on every Ra territory, **instead of** Ra's +2 favored reinforcements (Morrígan's −2 still applies). |
| Morrígan | **Night of crows:** this round, troops other nations lose **in rolled combats** (anywhere on the board) join Morrígan's Capital, or her largest army if she lost the Capital (ties: lowest id), **at most 3 per round**. Offering, bad omen, Storm, Thunderbolt and Mjölnir losses do not count. |

**2. Wrath (chosen by the player).** Each nation has **Favor 0–3**. When a combat resolves, each side whose **final** die shows its **sacred** number gains +1 Favor and each side whose final die shows its **cursed** number loses 1 (min 0). The Destiny Die does not change Favor (it gives the Day instead). With **3 Favor**, during its own Attack phase with no combat pending, a nation may unleash its Wrath **once per turn**; Favor returns to 0.

| God | Wrath |
|---|---|
| Zeus | **Thunderbolt:** pick any enemy territory on the board with 2+ troops; it loses half its troops rounded up, never going below 1 (no capture). |
| Thor | **Mjölnir:** an attack (same source/target rules as a normal attack, enemy target only) with no roll: the defender loses 3 troops (or all it has); if it reaches 0 Thor captures it and moves all troops but one. |
| Ra | **Eye of Ra:** **1** enemy territory adjacent to Ra's territories that has **exactly 1 troop** becomes Ra's (troops stay 1). Chosen automatically: Temples first, then the Altar, then lowest id. Needs a candidate. Can eliminate a nation (and win by last nation standing). |
| Morrígan | **Harvest:** for the rest of her turn, the **first 2** troops defenders lose in her attacks join her attacking territory (+1 each), before a capture moves troops. During her Harvest these troops do not also feed Night of crows. |

Balance (2000 bot matches, set B approved Nov 8): Zeus 21.9%, Ra 25.7%, Thor 27.5%, Morrígan 24.9%; Classic stays at ~24/24/26/26%.

Interactions: Ragnarök changes who wins, so the Offering rule ("only while losing") follows the new result. Temple victory is still checked only at the start of your turn. Eliminated nations keep no Favor. If the human is eliminated in a solo match, the match stops on YOU LOSE (bots don't finish it).

### Sacred Road: the army's march (approved Nov 9, phase 6; behind the powers/Pantheon toggle)

Problem found in playtest 3: troops pile up in one territory (usually the Capital) with no way out. Fix, driven by the lucky number (repeated Offerings and a fortify phase were rejected):

- **Trigger.** When the **attacker's final die in a combat shows the attacker's own sacred number** (the same moment it earns Favor), the gods open the road: until the end of that turn the attacker has **one Road**. Only the attacker earns it, in its own turn (a defender's sacred die never interrupts another nation's turn). At most one Road at a time; an unused Road is lost at End turn; a Road that was already used can be reopened by another sacred roll in the same turn. It is not granted if that combat ended the match.
- **The march.** Optional, during the Attack phase with no combat pending: pick one of your territories with 2+ troops (the origin) and a destination of your own that is **at most `SacredRoadMaxSteps` (=2) territories away along a path made only of your own territories**. **All the origin's troops but one** move to the destination. The road never crosses enemy or empty territories (that would be an attack).
- **Tuning levers if the simulations say it is too strong** (try in this order): cap the troops moved (`SacredRoadMaxTroops`, e.g. 6), reduce steps to 1, or require the destination to touch an enemy/empty territory.
- **Bots** march their biggest stack toward the owned territory with the best attack target (empty Temples/Altar first, then weaker enemies).
- **Classic mode** has no Road. It must be re-balanced with the headless simulations (target: every god 21–29%), also tracking the largest troop stack per match to confirm hoarding drops.

**Design filter:** every mechanic must be **fun, easy to understand, simple to implement and unique**, and it should be decided by the lucky number whenever possible. Theme is 40% of the jam score. For every feature ask *"if the lucky number were removed, would this still work?"*. Prefer no. Restraint over scope. The player must understand what to do in the first minute without reading. Always show *why* a result happened. Visual and UI rules live in [`Docs/design-system.md`](Docs/design-system.md).

### Phases

| Phase | Content | Status |
|---|---|---|
| 1 | Core: dice, pantheon, combat resolution | Done |
| 2 | Core: board, territories, turn state machine, reinforcements, Destiny Die, Offering, victory | Done |
| 3 | Presentation: altar-shaped parchment board, pieces, dice view, input; simple bots (AI); first WebGL build | Done |
| 4 | UI Toolkit HUD, combat panel with "why", self-teaching onboarding, main menu, end screen | Done |
| 5 | Audio, juice, balance via headless bot-vs-bot simulations; playtest round 1 fixes | Done (playtest fixes pending a listen/play check) |
| 6 | God powers, per-god VFX, WebGL build, bug fixing, polish (finish by Wed Nov 11) | In progress |

#### Phase 3 plan (approved)

- One scene `Match`. The 13 territory positions live in a `BoardLayout` ScriptableObject so they can be moved in the Editor.
- Placeholder art: simple rounded shapes on a parchment background, already using the design-system colors and god icons. Final art in phase 5.
- Each territory shows owner color + god icon (never color alone) + troop count; temples and the Altar have distinct shapes.
- Input: Reinforce = click an own territory for +1 troop. Attack = click an own territory, legal targets highlight (`CanAttack`), click a target to roll. If losing and `CanMakeOffering`, show **Offering** and **Accept** buttons; otherwise the combat confirms after the dice animation. An **End turn** button. Buttons are minimal UI Toolkit placeholders (full HUD in phase 4).
- Dice view: two dice that tumble and land showing the number and SACRED / CURSED / NEUTRAL in each god's color.
- Bots (`Scripts/AI`, pure C#, tested headlessly): reinforce next to the best target; attack priority empty temples → empty territories → enemies with fewer troops; keep attacking while they have the troop advantage; make an Offering when losing with 4+ troops. Bot turns play with short pauses so the player can follow.
- `MatchController` wires `Match` to the scene, listens to events and runs bot turns. The human plays Zeus for now.
- Finish the phase with the first WebGL build uploaded to itch as a draft.

#### Phase 4 plan (approved)

- **Main menu with god choice**, as a panel inside the `Match` scene (no extra scene): title GodBoard and four god cards with the same frame (icon, name, ★ sacred, ✗ cursed, rival). Clicking a card starts the match as that god.
- **Nations panel** (left): each god with icon, territories, troops and temples held (●●○○); current turn highlighted, eliminated greyed out.
- **Turn banner**: "YOUR TURN" / "THOR'S TURN" with the god icon slides in at the top for about 1.2 s.
- **Destiny Die animation** at each round start: a large die tumbles in the center and lands with its effect line ("Zeus favored +2 · Thor weakened −2 · Zeus opens"), about 1.5 s, click to skip.
- **Onboarding without a text tutorial**: one short contextual hint the first time each situation happens in a match (first reinforce, first attack, first combat, first Offering available, first Destiny); empty temples show a small temple icon.
- **Combat panel**: kept, polished with the design system.
- **End screen**: YOU WIN / YOU LOSE, winner card, reason, 3 stats (rounds, temples, combats won), **Play again** and **Main menu**.
- **Fast bots**: a ▶▶ toggle speeds up bot turns (x3).
- **Fonts**: Cinzel (display) and Inter (body), SIL Open Font License, in `Assets/_Project/Fonts/`, credited in `Docs/credits.md`.
- Out of scope: audio, juice, final art (phase 5).

#### Phase 5 plan (approved; feature freeze: no new rules)

- **Audio**: Kenney CC0 packs (Casino Audio, Interface Sounds), selected clips in `Assets/_Project/Audio/` with their licenses. Cues: dice roll, sacred result (bright chime), cursed result (dull bong), Destiny Die, troop placed, capture, turn change, Offering, victory, defeat, UI click. A first set of procedural music loops was tried and removed on Nov 12 (the result was poor). On Nov 12 the developer approved a single main theme instead, "The Six Faces" (slow modal folk, composed and rendered by `Tools/MusicGen/compose_theme.py` and `render_theme.py`), looping under menu and match and fading out before the victory/defeat sting. A mute toggle next to the fast-bots button.
- **Juice** (subtle, per the design system): pieces hop when they gain a troop or are captured, a small flash on a captured territory, a 2–4 px screen shake only on captures, sacred die glows and cursed die shakes on landing; one strong animation at a time.
- **Art polish**: keep the current style; procedural parchment texture on the background and wood grain on the frame (no downloads); cleaner "-1".
- **Balance**: Zeus wins ~22% vs 25–27%; only the bot may be tuned (rules are frozen).

#### Playtest round 1 (developer feedback, fixes applied on `feature/playtest-fixes`)

1. Pacing was too fast → slower defaults; the human's combat result stays until a click (max 3 s) with "Click to continue"; longer bot pauses, Destiny hold and turn banner. Fast bots ▶▶ still speeds bots up.
2. Sounds too short → longer clips; sacred, cursed, victory and defeat are mixes of Kenney CC0 clips with a baked echo (WebGL has no audio filters). Roll sounds are no longer cut on landing.
3. Placing troops played several sounds → one clip only.
4. Hover feedback → only territories usable right now react (slight scale + outline), all buttons and god cards share hover/pressed states.
5. Sacred numbers not self-explanatory → explanation line in the menu; each die tag reads "1 = Zeus's sacred number" / "4 = neutral for Ra" / "6 = Zeus's cursed number".

#### Phase 6 plan (approved Nov 8, full scope: nothing cut)

Window: Sun Nov 8 night – Wed Nov 11 (game finished Wed midnight). Priority order: **god powers** (rules above: Core + AI + tests, balance sims, then presentation: Favor meter, Wrath button, Day banner, "why" lines, per-god VFX), then **hot-seat** (human or bot per god in the menu), then the polish items below. Every new feature is behind a toggle; anything not merged and playtested by **Tue Nov 10 23:59** ships toggled off. Wednesday is fixes only.
- **Verify playtest round 1** in a fresh WebGL build: listen to every cue with headphones, check pacing, tune volumes in `AudioCues.asset`. Confirm `AudioSource.PlayDelayed` works in WebGL.
- **Bug bash**: full matches with each of the four gods; edge cases (human eliminated, round-15 limit, Offering, bad omen, temple win, Play again / Main menu); 1280×720 and 1920×1080; first click unlocks audio; all glyphs render.
- **Playtest round 2** with someone who has never seen the game (first-minute clarity), then fix what they stumble on.
- **WebGL optimization** (`/unity:optimize-web`): download size, load time, compression, managed stripping.
- **Code tidy if time allows**: split presentation logic out of `MatchController` (~750 lines; code review LOW).
- **Final build** uploaded to itch.io as a draft.
- Nov 12–14 (no build changes after Nov 14 06:00): itch page (description, controls, credits, screenshots, GIF, AI disclosure — complete `Docs/ai-usage.md` then), README rewrite, submit.

**Visual identity (never like a world-map war game):** a parchment board shaped like an altar, the four gods in their corners, temples around the center. No world map, no toy soldiers, no political-map colors.

## Project structure

```
Assets/_Project/
  Scripts/Core/        Pure C# rules (asmdef, noEngineReferences: true)
  Scripts/AI/          Bots. Pure C#, depends only on Core
  Scripts/Gameplay/    MonoBehaviours that drive Core and react to events
  Scripts/UI/          UI Toolkit controllers (views only)
  Tests/EditMode/      Core + AI tests, headless bot-vs-bot simulations
  Tests/PlayMode/      Only what truly needs the engine
  UI/                  UXML, USS (theme tokens in UI/Styles/Tokens.uss), panel settings
  Art/  Audio/  Fonts/  Prefabs/  Scenes/  ScriptableObjects/
Docs/                  design-system.md, ai-usage.md, credits.md
```

## Architecture

- **Game rules live in plain C# classes in Core**, not in MonoBehaviours. They are testable and can run thousands of matches headlessly.
- Core exposes **C# events** (`event Action<CombatResult> CombatResolved`). Gameplay and UI **only subscribe**; they never mutate rules state directly except through Core commands.
- **Randomness through an injectable `IRandom`** (seeded in tests and simulations).
- **Turn flow is an explicit state machine.**
- **Data and tuning in ScriptableObjects** (nations, colors, icons, balance numbers). No magic numbers in logic.
- **UI: UI Toolkit** (UXML + USS) for all screen-space UI. Design tokens are USS variables mirroring `Docs/design-system.md`. Board and pieces are sprites (URP 2D).

## Simplicity first

This is a small, simple game. **Prefer the most direct solution.**

- No elaborate patterns or unnecessary abstractions: no interfaces with a single implementation (except `IRandom`, needed for deterministic tests), no service locators, DI frameworks, event buses, reflection, complex generics or advanced math when a plain solution exists.
- Static helper classes are fine for pure rules (for example reading a die face).
- Enums and plain classes over class hierarchies. A `switch` is fine.
- If a reviewer can delete code or a type without losing behavior, delete it.

**Layers (keep consistent in every file):**

| Layer | Folder | Contains | May reference |
|---|---|---|---|
| Model + Rules | `Scripts/Core` | Data types and game rules, plain C# | nothing (no `UnityEngine`) |
| Bots | `Scripts/AI` | Bot decisions | Core |
| Presentation | `Scripts/Gameplay`, `Scripts/UI` | MonoBehaviours, UI Toolkit views | Core, AI |

## C# conventions

- One class per file; file name = type name; namespaces mirror folders (`GameJam.Core`, `GameJam.UI`, ...).
- `PascalCase` types/methods/properties/events, `_camelCase` private fields, `camelCase` locals/params.
- `[SerializeField] private` instead of public fields.
- No `Find*`, `GetComponent` or allocations (LINQ, string concat, `new` lists) in `Update`/hot paths: cache references in `Awake`.
- Prefer events over polling. Unsubscribe in `OnDisable`/`OnDestroy`.
- Small methods, guard clauses, comment the *why*.
- C# files are auto-formatted with **CSharpier** by a hook after every edit (`dotnet tool restore` once after cloning).

## Don't break the project

- **Never edit `.meta` files by hand** (blocked in settings). Never edit `Library/`, `Temp/`, `Logs/`, `UserSettings/`.
- **Create, rename and move assets only inside the Unity Editor** (or via the Unity MCP), so GUIDs and `.meta` files stay consistent. New `.cs` files created on disk are fine; Unity generates their `.meta`.
- **Ask before touching `ProjectSettings/` or `Packages/manifest.json`.**
- Scenes and prefabs are YAML: keep one person/agent editing a scene at a time, prefer prefabs over big scene edits, never hand-merge scene conflicts (take one side, redo the change in the Editor).
- Commit `.meta` together with its asset.

## Setting up a new machine

See [`Docs/development-setup.md`](Docs/development-setup.md) (Windows and macOS).

## Tests and builds

- **Editor open (preferred):** run tests through the Unity MCP (`run_tests`, EditMode/PlayMode), or Window → General → Test Runner.
- **Editor closed (batchmode)** (Windows path below; on macOS use `/Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app/Contents/MacOS/Unity`):
  ```
  "C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml -logFile Logs/test.log
  ```
  (fails if the project is already open in the Editor.)
- **WebGL build for itch.io:** File → Build Profiles → Web → Build, output to `Builds/WebGL/` (gitignored). Compression: Gzip with *Decompression Fallback* enabled (itch serves without the right headers). Zip the folder contents (with `index.html` at the root) and upload as "playable in browser". Test a build from day 2.

## Tooling

- **Unity MCP** (`unityMCP`, MCP for Unity by CoplayDev): reads the console, inspects/creates GameObjects, runs tests and takes screenshots. Needs the Editor open with Window → MCP for Unity → server running.
- **Plugins:** `unity` (Unity's official skills, for example `/unity:ui-uitk`, `/unity:optimize-web`, `/unity:2d-pixel-perfect`), `context7` (up-to-date package docs: add "use context7"), `csharp-lsp` (C# diagnostics; needs Unity to have generated the `.csproj`/`.sln`: Preferences → External Tools → Regenerate project files).
- **Project skills** in `.claude/skills/`: our workflow commands plus vetted Unity reference skills from Nice-Wolf-Studio (MIT, see `LICENSE-nice-wolf-studio.txt`).

## Agent workflow

Subagents in `.claude/agents/`:

| Agent | Role | Writes code? |
|---|---|---|
| `unity-developer` | Implements systems in C#, writes EditMode tests | Yes |
| `code-reviewer` | Bugs, performance, architecture. Prioritized findings | No (read-only) |
| `ui-designer` | Owns the look: tokens, USS/UXML, typography, juice, the design system | Yes (UI only) |
| `ux-reviewer` | Clarity, feedback, onboarding, readability, contrast | No (read-only) |
| `jam-producer` | Scope, time, priorities vs the rubric | No (read-only) |

**Standard flow (every phase):**

1. `unity-developer` writes all the code and tests.
2. **Before the developer sees anything**, `code-reviewer` checks compile errors, logic bugs, architecture violations and unnecessary complexity. When possible, also compile and run the Core tests (with the Unity Test Runner or a throwaway `dotnet test` project outside the repo). If UI/UX is touched, `ui-designer` + `ux-reviewer` validate too.
3. Fix every finding, then review again if the fixes were significant.
4. Deliver to the developer, who reviews, tests and approves. Commit after approval.

**Delivery format for each phase** (in Spanish):

- Short summary of what was implemented and why
- Complete files (never fragments), each with its path
- What the reviewers found and how it was fixed
- How to test it
- Proposed next phase

Commands: `/feature <description>` (whole flow), `/review-code` (review current changes), `/ui-check <screen or system>`, `/scope` (what's left, what to cut).

## Git

- `main` always holds a playable build. Short-lived branches per system (`feature/combat`), merged with a PR that explains what and why. Tiny fixes may go straight to `main`.
- Small, focused commits with imperative messages (`Add combat resolution`).
- **Never add `Co-Authored-By` trailers or any Claude/AI attribution to commits or PRs.** The developer is the sole author. AI usage is disclosed in `Docs/ai-usage.md` and on the itch page instead.
- Ask before pushing.

## Jam compliance

- WebGL build marked "playable in browser"; itch page with description, controls, credits, team.
- **Generative AI disclosure** on the itch page (required for prizes). Keep `Docs/ai-usage.md` updated.
- Record source and license of every external asset when added, in `Docs/credits.md`.
- If "Unity" is mentioned, include: *"<Game> is not sponsored by or affiliated with Unity Technologies or its affiliates. Unity is a trademark or registered trademark of Unity Technologies or its affiliates in the U.S. and elsewhere."* Only the official unmodified "Made with Unity" logo; never the cube logo.
