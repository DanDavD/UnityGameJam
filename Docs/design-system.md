# Design System

Single source of truth for the game's look and feel. `ui-designer` owns this file; every UI change follows it. The USS tokens in `Assets/_Project/UI/Styles/Tokens.uss` must mirror the values here. If one changes, change both.

## Direction

**"A physical board game on a parchment table."** Clean, minimal, calm. The board and its wooden pieces are the stars; UI is quiet ink on paper and only gets loud when the dice speak.

- **Minimal:** few colors, few font sizes, generous spacing. If an element doesn't help the player decide or understand, remove it.
- **Physical:** pieces look like painted wood, panels like paper cards, the die like a real die. No neon, no glassmorphism, no heavy gradients.
- **Readable first:** any game state must be readable in one glance at 1280×720.
- **Color is never the only signal:** every nation-owned thing shows its **icon** too.

## Color

### Nations (Okabe-Ito, colorblind-safe)

| Token | Nation / God | Hex | Icon | Die face |
|---|---|---|---|---|
| `--nation-zeus` | Greece / Zeus | `#56B4E9` | lightning bolt | 1 |
| `--nation-thor` | Norse / Thor | `#D55E00` | hammer | 6 |
| `--nation-ra` | Egypt / Ra | `#E69F00` | sun disc | 2 |
| `--nation-morrigan` | Celts / Morrígan | `#1A1A1A` | crow | 5 |

Each nation also has a **tint** (15% of the color on parchment) for territory fills: `--nation-*-tint`, and pieces get a 2 px `--ink` outline so black and gold stay crisp.

| Token | Hex |
|---|---|
| `--nation-zeus-tint` | `#D6E9F0` |
| `--nation-thor-tint` | `#EBCDB4` |
| `--nation-ra-tint` | `#F2DDB0` |
| `--nation-morrigan-tint` | `#CFC7BA` |

### Neutrals (paper and ink)

| Token | Use | Hex |
|---|---|---|
| `--parchment` | Screen / table background | `#F2E8D5` |
| `--paper` | Panels, cards, tooltips | `#FBF6EC` |
| `--paper-shade` | Pressed states, dividers, empty slots | `#E4D7BF` |
| `--wood` | Board frame, primary button fill | `#8B5E3C` |
| `--wood-dark` | Button hover/pressed, borders | `#6B4529` |
| `--ink` | Primary text, outlines | `#2B2118` |
| `--ink-soft` | Secondary text, hints | `#6E5F4E` |
| `--ink-faint` | Disabled text, placeholders, cursed die border and crack | `#A89580` |
| `--scrim` | Dims the board behind modal cards (end banner) | `rgba(43, 33, 24, 0.45)` (`--ink` at 45%) |
| `--clear` | Transparent fill (secondary buttons) | `rgba(43, 33, 24, 0)` |
| `--shadow` | Flat paper shadow under panels and cards | `#D2C2A4` |

### Roll outcome (sacred / cursed / neutral)

Outcomes are communicated with **shape + label + motion**, not a new color. The nation's own color is used for the glow.

| Outcome | Symbol | Label | Motion |
|---|---|---|---|
| Sacred | star burst icon (★ in sentences) | `SACRED` | die pulses and glows in the nation color (thicker nation border, nation-tint face, star burst icon in the corner) |
| Cursed | cracked die icon (✗ in sentences) | `CURSED` | die shakes, desaturates (`--paper-shade` face, `--ink-faint` border, `--ink-soft` number), diagonal crack, cracked die icon in the corner. Never fade the number |
| Neutral | ● plain | `NEUTRAL` | none, just the number |
| Good fortune (3) | four-leaf clover | `GOOD FORTUNE` | soft rise |
| Bad omen (4) | skull | `BAD OMEN` | soft sink, slight vignette |

Contrast rule: text on `--parchment` / `--paper` uses `--ink` or `--ink-soft` (both ≥ 4.5:1). Never put text directly on a nation color, except white text on `--nation-morrigan` and `--ink` text on the other three.

## Typography

**Numbers never use Cinzel** (its "1" reads like "I"): die faces, round numbers and stats use Inter SemiBold.

Fonts are free under the SIL Open Font License; credit them in `Docs/credits.md`.

| Role | Font | Notes |
|---|---|---|
| Display (titles, god names, big numbers) | **Cinzel** (Bold) | Carved, mythic capitals. Use only at 24 px and up. |
| Body / UI | **Inter** (Regular, SemiBold) | Highly legible at small sizes. |

Font tokens (UI Toolkit font assets in `Assets/_Project/Fonts/`, applied only through these tokens):

| Token | Asset | Use |
|---|---|---|
| `--font-display` | `Cinzel SDF` | Display, h1, h2 titles and names. Never for numbers (die faces, round line, stats use Inter SemiBold; Cinzel's 1 reads as an I). The HUD round line is Inter at body size so its longest variant fits on one line. Cinzel ships as a variable font; the asset uses its default (Regular) instance and `-unity-font-style: bold` synthesizes the weight. Falls back to Inter Regular for symbols |
| `--font-body` | `Inter-Regular SDF` | Default for the whole HUD (set on `.hud`). Its weight table maps **bold** to Inter SemiBold, so bold body text is a real weight, not a faux one |
| `--font-strong` | `Inter-SemiBold SDF` | Label style (buttons, tags, stat names), menu prompt, winner name |

The board's troop counts (TextMesh Pro) use `Inter-SemiBold TMP`.

**Glyph rule (WebGL):** use only glyphs that Inter or Cinzel actually contain. The browser build has no OS font fallback, so a missing glyph shows as an empty box. That is why the outcome symbols are ★ and ✗ (in Inter) and fast-forward is ▶▶, not ✦ ✖ ⏩. The font assets are **Dynamic** with every glyph we use pre-filled (ASCII, `· − … × ★ ☆ ✗ ▶ ● ○ í` and common Latin accents) and *Clear Dynamic Data on Build* off, so the build ships a ready atlas and can still add a stray glyph at runtime from the included source font.

Type scale (at 1080p reference resolution; the PanelSettings scale mode handles other sizes):

| Token | Size | Font | Use |
|---|---|---|---|
| `--font-size-display` | 64 px | Cinzel Bold | Title screen, victory |
| `--font-size-h1` | 40 px | Cinzel Bold | Screen titles, round banner |
| `--font-size-h2` | 28 px | Cinzel Bold | Panel titles, god names |
| `--font-size-body` | 20 px | Inter Regular | Default text |
| `--font-size-label` | 16 px | Inter SemiBold, uppercase, +1 px letter spacing | Buttons, tags, outcome labels |
| `--font-size-caption` | 14 px | Inter Regular | Hints, secondary info (never below 14 px) |
| `--font-size-die` | 96 px | Cinzel Bold | Die faces in the roll overlay |
| `--font-size-die-destiny` | 128 px | Cinzel Bold | The Destiny Die face |
| `--letter-spacing-label` | 1 px | | Letter spacing of label-style text |

Max 3 sizes visible on one screen at a time, besides the die. Explanations are always sentence case at body size; only labels are uppercase.

| Screen | Sizes |
|---|---|
| Match HUD | h2 (round / Destiny line, turn banner, temple dots), body (identity, goal, prompt, hint, combat "why", nation names), label (buttons, tags, nation stats) |
| Main menu | display (title), h2 (god names), body (tagline, "Choose your god", card lines) |
| Destiny Die (modal) | h1 (round), body (effect line), caption ("Click to skip") |
| End banner (modal) | h1 (verdict, stat values), body (winner name, reason), label (stat names, buttons) |

## Spacing and shape

4 px base grid.

| Token | Value |
|---|---|
| `--space-1` | 4 px |
| `--space-2` | 8 px |
| `--space-3` | 12 px |
| `--space-4` | 16 px |
| `--space-6` | 24 px |
| `--space-8` | 32 px |
| `--space-12` | 48 px |
| `--radius-sm` | 4 px (tags, small chips) |
| `--radius-md` | 8 px (buttons, cards) |
| `--radius-lg` | 16 px (panels, dialogs) |
| `--border-width` | 2 px |
| `--shadow-offset` | 4 px. Flat "paper" shadow, no blur: USS has no box-shadow, so cards draw it as a thicker bottom border in `--shadow` |

Component sizes:

| Token | Value | Use |
|---|---|---|
| `--button-min-height` | 48 px | Every button (touch-friendly at 1280×720) |
| `--icon-md` | 32 px | Icon inside a nation chip |
| `--icon-lg` | 48 px | Nation chip; icon inside the large chip |
| `--chip-lg` | 80 px | Winner chip on the end banner |
| `--die-size` | 112 px | Combat die (keeps the dice panel clear of the board frame at 1280×720) |
| `--die-border` | 4 px | Die outline and cursed crack thickness |
| `--die-border-sacred` | 8 px | Sacred die outline |
| `--dice-panel-text-width` | 240 px | Max width of the combat "why" line (two dice wide) |
| `--end-card-width` | 480 px | Min width of the end banner card |
| `--icon-sm` | 20 px | Icon inside the small nation chip of the nations panel |
| `--nations-panel-width` | 300 px | Width of the nations panel (left column) |
| `--god-card-width` | 200 px | Width of each god card on the main menu |
| `--die-size-destiny` | 200 px | The Destiny Die at each round start |
| `--die-edge-destiny` | 16 px | Bottom edge of the Destiny Die, drawn in `--ink`: the side of a real die seen slightly from above |
| `--turn-banner-top` | 144 px | Turn banner position, below the round line (even when it wraps to two lines) |
| `--turn-banner-hidden-offset` | -240 px | How far above its spot the turn banner waits before sliding in |
| `--round-line-width` | 800 px | Max width of the round / Destiny line; longer lines wrap so they never reach the identity card |

Screen padding: `--space-8`. Gap between panel sections: `--space-6`. Gap inside groups: `--space-2` to `--space-3`.

## Components

**Primary button:** fill `--wood`, text `--paper`, label style, padding `--space-3` × `--space-6`, radius `--radius-md`, min height 48 px. Hover → `--wood-dark` + lift 2 px. Pressed → scale 0.97, shadow removed. Disabled → fill `--paper-shade`, text `--ink-faint`.

**Secondary button:** transparent fill, 2 px `--wood` border, text `--wood`. Same states.

**Panel / card:** fill `--paper`, 2 px `--paper-shade` border, radius `--radius-lg`, padding `--space-6`, flat shadow (bottom border `--shadow-offset` in `--shadow`). USS class `.card`.

**Nation chip:** small rounded square in the nation color with the god icon (`--ink`, or `--paper` on Morrígan) and a 2 px `--ink` outline. Used wherever a god is named on screen (identity card, end banner) so the god is never color alone.

**Identity card (top left):** nation chip + "YOU: ZEUS ·" [sacred icon] "1 sacred ·" [cursed icon] "6 cursed" (body, bold; drawn omen icons at `--icon-sm`) + the goal line (body, `--ink-soft`).

**Nations panel (left, under the identity card):** one row per god in base cycle order: nation chip, name (body, bold) with a YOU tag on the human, stats (label, `--ink-soft`) and the temple line (temple icon + one h2-size dot per temple, ● held / ○ not; the 3rd dot, the one that wins, is ringed in `--wood` as the goal). Whose turn: `--wood` frame, `--paper-shade` fill and a ▶ mark. Eliminated: 50% opacity, "eliminated" text, no temple line.

**End banner:** `--scrim` over the board, centered card whose top edge (`--space-2`) takes the winner's color. Under the verdict (h1) sits the **winner card**: the winner's chip (`--chip-lg`), name (body, SemiBold) and the reason, on the winner's tint with a nation-color border. Then three stats (h1 value, label name) split by `--paper-shade` dividers, then Play again / Main menu. If there is no winner yet (human eliminated), the card stays neutral: chip and name hide and only the reason remains.

**Destiny Die (modal, round start):** `--scrim`, a card with "ROUND N" (h1), a `--die-size-destiny` die (thick border, `--die-edge-destiny` ink bottom edge) that tumbles and lands. On a god's face the die takes the favored god's color (border) and tint (face) and shows that god's chip on its top-right corner, so the favor is never color alone. 3 and 4 land as a plain paper die. Effect line (body) and "Click to skip" (caption) below.

**Hint toast:** a paper note above the prompt: `--paper` fill, `--paper-shade` border, a `--space-2` `--wood` left edge, flat shadow, body text. One at a time; it hides itself or on the next action.

**Fast bots button:** secondary button, top right. Hidden while the main menu is open (it only means something during a match) and on the end banner.

**Territory piece (board, sprites):** a painted wooden token. Layers from back to front: offset shadow (`--ink` at 22%, offset down-right), 2 px `--ink` outline (keeps gold and blue borders ≥ 3:1 against the board), nation-color border, nation-tint fill, god icon in `--ink` (about 40% of the smallest piece) and the troop count bold in `--ink`. Unowned pieces are `--paper` with an `--ink` border and no icon or count.

**Board highlights (never color alone):**

| State | Ring | Motion | Lift |
|---|---|---|---|
| Selected (attack from) | thick solid `--ink` ring | none | piece scales 1.1 and its shadow lengthens |
| Legal target | thin **dashed** `--wood` ring | gentle breath: alpha 50-100% and +5% scale over 1.4 s, all targets in sync | none |

**Board frame:** the board sits on a `--wood` frame (0.2 units wide) with a soft `--ink` shadow, like an altar board on a table. Connection lines are `--ink` at 50%, slightly thicker than a hairline.

**Materials (board sprites, `Assets/_Project/Art/Textures/`):** texture is felt, not seen. Colors are baked from the tokens, so each sprite renders with a white tint and the token stays the average color.

| Surface | Sprite | Base token | Texture | Contrast |
|---|---|---|---|---|
| Table (screen background, `Table` object, tiled 48 × 32 units behind everything; the camera's flat `--parchment` is only a fallback) | `parchment_tile` (512 px, 64 px/unit = 8-unit tile, seamless) | `--parchment` | soft mottling + short paper fibers | about ±3% luminance; fibers at most -2% |
| Board inner area (`Outer`) and altar steps (`Steps`) | `board_inner`, `board_steps` (48 px/unit) | `--paper-shade`, `--parchment` | the same parchment pattern, sampled in world space so it lines up with the table | 60% of the table's: troop counts and lines must win every time |
| Board frame (`Frame`) | `board_frame_wood` (48 px/unit) | `--wood`, grain lines toward `--wood-dark` | grain runs along each side; the four sides meet in mitered corners with a thin dark joint | grain lines up to about 1/3 of the way to `--wood-dark` |

Rules: no texture on pieces, icons, lines or UI cards (they stay flat paint and ink). Baked sprites match the scene sizes (frame 17.8 × 10.8, inner 17.4 × 10.4, steps 9 × 9 units, rounded corners matching `rounded_square`); if the board frame is resized, regenerate them. Import: Sprite, Full Rect mesh, no mipmaps, max 1024, default compression (DXT1 for the opaque tile, DXT5 for the rest).

**God card:** same frame for all four gods (consistency is the point). Top band in the nation **tint** with the nation chip (color + icon); god name in Cinzel (h2); sacred and cursed numbers as two small die-like tags in the dice language, each icon + number + word: star burst icon + "1 sacred" on the nation tint with a nation-color border, cracked die icon + "6 cursed" desaturated (`--paper-shade`, `--ink-faint` border); rival line in `--ink-soft`. Hover lifts the card (`--hover-lift`) and frames it in `--wood`; press scales to `--press-scale`. Later: one-line power text if god powers ship.

**Tooltip:** `--paper`, radius `--radius-md`, padding `--space-3`, caption or body size, appears after 300 ms hover, never covers the hovered element.

**Turn banner:** top center, h2, slides down, stays 1.2 s, slides up. Shows the nation chip + "YOUR TURN" / "ZEUS'S TURN", with a `--space-2` left edge in the nation color.

**Die:** rounded square, `--paper` face, `--ink` pips/number, 2 px outline. In the roll overlay, both dice side by side with the outcome tag under each: sacred / cursed icon + word ("SACRED", "CURSED"); neutral is the word alone.

## Iconography

- One flat, single-color line style for every icon: 2 px stroke at 24 px, rounded caps.
- Icon sizes: 16, 24, 32, 48 px.
- Nation icons: lightning bolt (Zeus), hammer (Thor), sun disc (Ra), crow (Morrígan). Game icons: temple, troop, die, clover, skull, sacred, cursed.
- One icon pack or one hand-drawn set only. Never mix sources. All current icons are flat white silhouettes on transparent (`Assets/_Project/Art/Placeholder/icon_*.png`, 256 px), tinted with `-unity-background-image-tint-color`.
- **Sacred icon** (`icon_sacred.png`): an 8-point star burst (4 long rays, 4 short). **Cursed icon** (`icon_cursed.png`): a rounded die with two pips split by a jagged vertical crack (never an ×, so it can't be read as "wrong" or "close"). Both read at 16 px.
- **Glyph vs icon:** where the sacred / cursed mark **stands on its own** it is the drawn icon: die corner marks, combat outcome tags (icon + `SACRED` / `CURSED`), god card tags (icon + number + word), and the HUD identity line ("YOU: ZEUS ·" [sacred icon] "1 sacred ·" [cursed icon] "6 cursed": a row of three labels with `.omen-icon` between them, because Inter's ✗ reads as "×" at body size). **Inside hint sentences** it stays the ★ / ✗ text glyph, so it flows with the text. USS: `.omen-icon` (size `--icon-sm`, tint `--ink`) + `.omen-icon--sacred` or `.omen-icon--cursed` (cursed tints `--ink-soft`, desaturated like the cursed die). The word always stays next to the icon: never icon alone.

## Motion and juice

Subtle, fast, purposeful: motion should explain cause and effect.

| Token | Value | Use |
|---|---|---|
| `--duration-fast` | 120 ms | Hover, press |
| `--duration-base` | 200 ms | Panels, banners, highlights |
| `--duration-slow` | 400 ms | Screen transitions, victory |
| `--ease-enter` | `ease-out-cubic` | Things arriving or reacting (hover, press, banners in) |
| `--ease-exit` | `ease-in-cubic` | Things leaving (banners out) |
| `--hover-lift` | `0 -2px` (translate) | Button hover |
| `--press-scale` | `0.97 0.97` (scale) | Button press |

- UI motion: USS `transition` on `translate`, `scale`, `opacity`, `background-color`.
- Board motion (sprites): small coroutines in `TerritoryView`, durations in serialized fields. A piece hops (scale 1 → 1.15 → 1, 200 ms) when it gains a troop or changes owner; a loss never hops, it shows the "-1" instead, so one thing moves per piece.
- Capture: a ring in the new owner's color expands (+45%) and fades over 350 ms, drawn above the piece.
- Troop loss: "-1" in `--ink` at 85% of the count size, above the count; rises with an ease-out, stays opaque for the first 40% of 1 s, then fades.
- Dice roll: about 600 ms tumble, then land with a small overshoot. This is the moment of the game, so it gets the most love (sound + motion). On landing (400 ms): a sacred die pulses twice (+8% scale; its tint and thick border are the glow), a cursed die shakes sideways (6 px, dying out). Only the dice that just rolled react.
- Screen shake only when a territory is taken from a rival (empty captures do not shake), and later for Thor/Zeus powers: 3 px (2-4), 120 ms, measured in screen pixels.
- Respect restraint: at most one strong animation on screen at once.

## Audio identity (for coherence)

Wooden and tactile: dice clacks on wood, soft paper swishes for panels, a low drum for combat, a bright chime for sacred, a dull thud plus crack for cursed. One short music loop with ancient-instrument flavor (lyre, frame drum), kept low under SFX (not in the jam build: no music yet).

Shipped cues (Kenney CC0, clips and volumes in the `AudioCues` asset; variations picked at random):

| Moment | Sound | Sync |
|---|---|---|
| Combat roll | dice thrown on wood (3 variations) | starts with the tumble, stops when the dice land |
| Offering re-roll | dice shaken in the hand | same, replaces the throw |
| Destiny Die | single die thrown (2 variations) | starts with the tumble, stops on landing |
| Landing sting | bright chime (sacred) / dull bong (cursed) | on landing; one per roll: a sacred winner chimes, any cursed die bongs, plain numbers stay quiet. For Destiny: chime if it helps the player, bong if it hurts them |
| Troop placed | chip laid (3 variations) | each troop |
| Capture | chips stacked (2 variations) | with the ring |
| Turn change | card slide | with the turn banner |
| Victory / defeat | rising confirmation / falling minimize | with the end screen, once |
| Button / invalid click | soft click / short error | on the click |

Rules: rolls share one voice (a new roll replaces the last, landing stops it) so clatter never stacks; everything else is a one-shot. Mute toggle ("SOUND: ON/OFF") next to fast bots, kept for the session. On the web, audio starts after the first click (choosing a god).

## Layout (1920×1080 reference, must work down to 1280×720)

- **Center:** the board. Always the largest element. The camera frames it (orthographic size 8.4) so the left column and the combat panel keep at least `--space-8` of clear table between them and the board frame.
- **Top center:** round number, current Destiny Die result and turn banner.
- **Left:** the four nations stacked (icon, territory count, troops); current player highlighted.
- **Bottom center:** current action prompt ("Choose a territory to attack") and the main action button.
- **Right:** contextual panel (selected territory or combat preview).
- Pause and settings: top-right icon button.
