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

**Turn order:** Zeus → Ra → Thor → Morrígan (eliminated nations are skipped).

**Turn: Reinforce → Attack → End.**

1. **Reinforce:** receive `2 + territories / 2 (integer) + temples owned + Destiny modifier`, minimum 1. Place them one at a time on any owned territory. When all are placed, the Attack phase starts.
2. **Attack (any number of times):** from an owned territory with **2+ troops** to an adjacent territory not owned by you.
   - **Empty unowned territory:** captured directly, no combat.
   - **Enemy territory:** both roll a d6 (`CombatResolver`). The roll is shown first and the attacker may make an **Offering**: sacrifice 1 troop from the attacking territory to **re-roll their own die**, once per combat, only **while the attacker is losing** the rolled combat and only if the attacking territory has **3+ troops**. The re-roll replaces the attacker's roll, even if it is worse. Then the combat resolves: **the loser loses 1 troop**.
   - **Capture:** when the defender reaches 0, the attacker takes the territory and moves **all troops but one** into it.
3. **End turn.** No separate fortify phase (reinforcements can be placed anywhere).

A nation with no territories is **eliminated**.

**Victory (first that happens):**
1. **Control 3 of the 4 Temples at the start of your turn** (primary goal).
2. **Last nation standing.**
3. **After 15 rounds:** most territories; tie → most troops; tie → earlier in turn order.

**Design filter:** theme is 40% of the jam score. For every feature ask *"if the lucky number were removed, would this still work?"*. Prefer no. Restraint over scope. The player must understand what to do in the first minute without reading. Always show *why* a result happened. Visual and UI rules live in [`Docs/design-system.md`](Docs/design-system.md).

### Phases

| Phase | Content | Status |
|---|---|---|
| 1 | Core: dice, pantheon, combat resolution | Done |
| 2 | Core: board, territories, turn state machine, reinforcements, Destiny Die, Offering, victory | Done |
| 3 | Presentation: altar-shaped parchment board, pieces, dice view, input; simple bots (AI) | |
| 4 | UI Toolkit HUD, combat panel with "why", self-teaching onboarding, main menu, end screen | |
| 5 | Audio, juice, balance via headless bot-vs-bot simulations | |
| 6 | WebGL build, bug fixing, polish (finish by Wed Nov 11) | |

**If time allows:** unique god powers (Zeus lightning, Thor auto-win, Ra double production, Morrígan converts the dead), hot-seat mode, per-god VFX.

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

## Tests and builds

- **Editor open (preferred):** run tests through the Unity MCP (`run_tests`, EditMode/PlayMode), or Window → General → Test Runner.
- **Editor closed (batchmode):**
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
