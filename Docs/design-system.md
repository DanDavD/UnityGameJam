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
| `--ink-faint` | Disabled text, placeholders | `#A89580` |

### Roll outcome (sacred / cursed / neutral)

Outcomes are communicated with **shape + label + motion**, not a new color. The nation's own color is used for the glow.

| Outcome | Symbol | Label | Motion |
|---|---|---|---|
| Sacred | ✦ star burst | `SACRED` | die pulses and glows in the nation color |
| Cursed | ✖ cracked die | `CURSED` | die shakes, desaturates, crack overlay |
| Neutral | ● plain | `NEUTRAL` | none, just the number |
| Good fortune (3) | four-leaf clover | `GOOD FORTUNE` | soft rise |
| Bad omen (4) | skull | `BAD OMEN` | soft sink, slight vignette |

Contrast rule: text on `--parchment` / `--paper` uses `--ink` or `--ink-soft` (both ≥ 4.5:1). Never put text directly on a nation color, except white text on `--nation-morrigan` and `--ink` text on the other three.

## Typography

Fonts are free under the SIL Open Font License; credit them in `Docs/credits.md`.

| Role | Font | Notes |
|---|---|---|
| Display (titles, god names, big numbers) | **Cinzel** (Bold) | Carved, mythic capitals. Use only at 24 px and up. |
| Body / UI | **Inter** (Regular, SemiBold) | Highly legible at small sizes. |

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

Max 3 sizes visible on one screen at a time, besides the die.

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
| `--shadow` | 0 4 px 0 `--paper-shade` (flat "paper" offset, no blur) |

Screen padding: `--space-8`. Gap between panel sections: `--space-6`. Gap inside groups: `--space-2` to `--space-3`.

## Components

**Primary button:** fill `--wood`, text `--paper`, label style, padding `--space-3` × `--space-6`, radius `--radius-md`, min height 48 px. Hover → `--wood-dark` + lift 2 px. Pressed → scale 0.97, shadow removed. Disabled → fill `--paper-shade`, text `--ink-faint`.

**Secondary button:** transparent fill, 2 px `--wood` border, text `--wood`. Same states.

**Panel / card:** fill `--paper`, 2 px `--paper-shade` border, radius `--radius-lg`, padding `--space-6`, `--shadow`.

**God card:** same frame for all four gods (consistency is the point). Top band in the nation color with the icon; god name in Cinzel; sacred and cursed numbers shown as two small die faces with ✦ / ✖ symbols; one-line power text.

**Tooltip:** `--paper`, radius `--radius-md`, padding `--space-3`, caption or body size, appears after 300 ms hover, never covers the hovered element.

**Turn banner:** top center, h1, slides down 200 ms, stays 1.2 s, slides up. Shows nation icon + "ZEUS'S TURN" / "ROUND 3".

**Die:** rounded square, `--paper` face, `--ink` pips/number, 2 px outline. In the roll overlay, both dice side by side with the outcome label under each.

## Iconography

- One flat, single-color line style for every icon: 2 px stroke at 24 px, rounded caps.
- Icon sizes: 16, 24, 32, 48 px.
- Nation icons: lightning bolt (Zeus), hammer (Thor), sun disc (Ra), crow (Morrígan). Game icons: temple, troop, die, clover, skull.
- One icon pack or one hand-drawn set only. Never mix sources.

## Motion and juice

Subtle, fast, purposeful: motion should explain cause and effect.

| Token | Value | Use |
|---|---|---|
| `--duration-fast` | 120 ms | Hover, press |
| `--duration-base` | 200 ms | Panels, banners, highlights |
| `--duration-slow` | 400 ms | Screen transitions, victory |
| Easing | `ease-out-cubic` in, `ease-in-cubic` out | |

- UI motion: USS `transition` on `translate`, `scale`, `opacity`, `background-color`.
- Board motion (sprites): a small tween helper in code. Pieces hop when moved (scale 1 → 1.15 → 1, 200 ms).
- Dice roll: about 600 ms tumble, then land with a small overshoot. This is the moment of the game, so it gets the most love (sound + motion).
- Screen shake only for captures and Thor/Zeus powers: 2-4 px, under 150 ms.
- Respect restraint: at most one strong animation on screen at once.

## Audio identity (for coherence)

Wooden and tactile: dice clacks on wood, soft paper swishes for panels, a low drum for combat, a bright chime for sacred, a dull thud plus crack for cursed. One short music loop with ancient-instrument flavor (lyre, frame drum), kept low under SFX.

## Layout (1920×1080 reference, must work down to 1280×720)

- **Center:** the board. Always the largest element.
- **Top center:** round number, current Destiny Die result and turn banner.
- **Left:** the four nations stacked (icon, territory count, troops); current player highlighted.
- **Bottom center:** current action prompt ("Choose a territory to attack") and the main action button.
- **Right:** contextual panel (selected territory or combat preview).
- Pause and settings: top-right icon button.
