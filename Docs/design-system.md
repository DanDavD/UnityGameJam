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

**Glyph rule (WebGL):** use only glyphs that Inter or Cinzel actually contain. The browser build has no OS font fallback, so a missing glyph shows as an empty box. That is why the outcome symbols are ★ and ✗ (in Inter) and fast-forward is ▶▶, not ✦ ✖ ⏩. The font assets are **Dynamic** with every glyph we use pre-filled (ASCII, `· − … × ★ ☆ ✗ ▶ ● ○ í Ö ö` and common Latin accents; Ö / ö for Mjölnir and Ragnarök) and *Clear Dynamic Data on Build* off, so the build ships a ready atlas and can still add a stray glyph at runtime from the included source font. A number going down always uses the real minus "−" (U+2212), never the hyphen ("Thor −2", "Favor: … −1 for each 6"); the board's TextMesh Pro "-1" floats are the one exception. Neither font has ⓘ (or Cinzel ★, which falls back to Inter).

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
| Match HUD | h2 (Destiny line, turn banner, Sacred Road popup title, the prompt bar's instruction, the combat verdict, temple dots, the Wrath's name on its button and on the identity card, the identity Favor stars), body (identity, hint, turn box title, nation names, END TURN), label (buttons, prompt label, turn box status, outcome tags, verdict badges, Wrath state line, nation stats, round counter, nations goal caption, reinforcement formula) |
| Main menu | display (title), h2 (god names), body (tagline, "Choose your god", card lines, sacred-number rule) |
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
| `--die-size` | 140 px | Combat die (keeps the dice panel clear of the board frame at 1280×720) |
| `--die-border` | 4 px | Die outline and cursed crack thickness |
| `--die-border-sacred` | 8 px | Sacred die outline |
| `--dice-panel-width` | 352 px | Fixed width of the combat panel (right). It is the `--board-gutter-right` the board is fitted beside, so it never covers the board |
| `--outcome-tag-width` | 144 px | Width of the short tag under each combat die ("1 · SACRED", icon and text on one line); two tags and their margins fill the dice panel's content width |
| `--board-gutter-left` | 416 px | Free table kept clear on the left of the board: screen padding + nations panel + gap. Equal to the right gutter, so the board is centred on the screen |
| `--board-gutter-right` | 416 px | Same on the right: screen padding + `--dice-panel-width` + gap |
| `--board-gutter-top` | 160 px | Kept clear above the board: the round line and the Day badge under it |
| `--board-gutter-bottom` | 232 px | Kept clear below the board: prompt bar, Wrath and END TURN |
| `--prompt-max-width` | 1100 px | Max width of the prompt bar; longer instructions wrap |
| `--turn-box-width` | 288 px | Width of the turn box (top right) |
| `--turn-box-top` | 96 px | Top of the turn box: under the sound and fast-bots toggles |
| `--wrath-button-height` | 84 px | Height of the Wrath button (taller than END TURN: it is the signature power) |
| `--wrath-star-size` | 36 px | Each of the three Favor stars on the Wrath button |
| `--wrath-halo-offset` | -10 px | How far the ready Wrath's halo ring stands outside the button |
| `--end-card-width` | 480 px | Min width of the end banner card |
| `--draw-place-width` | 128 px | Width of each final-order god card (all four equal) |
| `--draw-card-width` / `--draw-card-height` | 680 / 650 px | Fixed size of the turn-order draw card (never changes between duels, ties, verdicts or the final order) |
| `--draw-verdict-height` | 88 px | Reserved height of the draw's verdict box (fits two lines) |
| `--scrim-light` | `--ink` at 18% | Light dim behind the turn-order draw, so the HUD and board stay readable |
| `--icon-sm` | 20 px | Icon inside the small nation chip of the nations panel |
| `--nations-panel-width` | 300 px | Width of the nations panel (left column) |
| `--god-card-width` | 352 px | Width of each god card on the main menu: every power line (Day, Wrath) fits on one line at body size, so the menu fits 1280×720. Cards sit `--space-2` apart and stretch to the same height |
| `--wrath-button-min-width` | 420 px | Min width of the Wrath button: icon, the Wrath's name in carved h2 capitals over its state line, and three Favor stars, so no state ever resizes it |
| `--action-button-height` | 72 px | Height of END TURN, the main action: taller than every other button |
| `--temple-dot-size` | 32 px | Fixed square box of each temple dot in the nations panel (radius `--radius-lg`), so the ringed goal dot is a true circle around its "3" |
| `--favor-star-size` | 72 px | The star that flies from a sacred combat die to its god's Favor stars |
| `--die-size-destiny` | 200 px | The Destiny Die at each round start |
| `--die-edge-destiny` | 16 px | Bottom edge of the Destiny Die, drawn in `--ink`: the side of a real die seen slightly from above |
| `--turn-banner-top` | 108 px | Turn banner position, just under the round line, in the table's top margin: for its few seconds it covers a Day badge rather than the board's top row |
| `--turn-banner-hidden-offset` | -288 px | How far above its spot the turn banner waits before sliding in |
| `--road-popup-width` | 1020 px | Width of the Sacred Road popup: one slim row (chip, title, one short sentence, "Click to close") |
| `--road-popup-top` | 108 px | Top of the Sacred Road popup: just under the round line (it covers a round-long Day badge rather than leaving a sliver of it). One row tall, so it ends at the top of the board frame and never covers a piece or a ring, at any aspect ratio |
| `--round-line-width` | 800 px | Max width of the round / Destiny line; longer lines wrap so they never reach the identity card |
| `--info-badge-size` | 28 px | The square "i" key on every info toggle (round line, nation rows with powers) |
| `--info-panel-width` | 720 px | Width of the round info panel under the round line |
| `--powers-card-width` | 400 px | Width of a nation's powers card, the flyout beside its row in the nations panel |
| `--counter-token-size` | 72 px | The die-like token of the reinforcement counter |

Screen padding: `--space-8`. Gap between panel sections: `--space-6`. Gap inside groups: `--space-2` to `--space-3`.

## Components

**Pickable UI (one hover language for every button and god card):** hover → lift (`--hover-lift`), slight grow (`--hover-scale`) and a darker frame; pressed → back down (no lift) and `--press-scale`. All in `--duration-fast` with `--ease-enter`. Disabled buttons never lift or grow.

**Primary button:** fill `--wood` with a 2 px `--wood` border (invisible at rest, so hover can darken it without resizing), text `--paper`, label style, padding `--space-3` × `--space-6`, radius `--radius-md`, min height 48 px. Hover → fill `--wood-dark`, border `--ink`. Disabled → fill `--paper-shade`, text `--ink-faint`.

**Secondary button:** transparent fill, 2 px `--wood` border, text `--wood`. Hover → fill `--paper-shade`, border `--wood-dark`. Same press and disabled states.

**Panel / card:** fill `--paper`, 2 px `--paper-shade` border, radius `--radius-lg`, padding `--space-6`, flat shadow (bottom border `--shadow-offset` in `--shadow`). USS class `.card`.

**Nation chip:** small rounded square in the nation color with the god icon (`--ink`, or `--paper` on Morrígan) and a 2 px `--ink` outline. Used wherever a god is named on screen (identity card, end banner) so the god is never color alone.

**Identity card (top left, slim):** exactly `--nations-panel-width` wide (the left column, `.hud__left`, is fixed to that width so it can never reach the board), so the left column reads as one card stack. A head line (small nation chip centered with the name, same head as a nation row; never "YOU:") and two short lines under it, nothing else: "ZEUS" (the human's god; body, bold); [sacred icon] "1 sacred" [cursed icon] "6 cursed" (body; drawn omen icons at `--icon-sm`, the cursed pair set `--space-4` apart instead of a "·"); and, with Pantheon powers, **the Wrath band**, the loudest line of the card: the Wrath name in Cinzel bold at h2 ("THUNDERBOLT") and under it the Favor stars "★★☆" (Inter SemiBold h2, `--ink`; stacked so the band never widens the column), on the god's tint with a 2 px nation-color border and a `--space-2` nation-color left edge (like the Day badge: it is that god's power, and the chip beside it keeps it from being color alone). Never "ready": readiness is the Wrath button's job. The goal and the powers' full rules live in the nations panel and the info panels, so the card stays small.

**Info toggle and badge:** the round line and each nation row with powers can be opened for the full explanation. The whole element is the hit area; an "i" key says it can be opened: a **rounded square** (`--info-badge-size`, `--radius-sm`; never round, so it can't be read as a temple dot), `--paper-shade` fill, `--wood` border with a deeper `--shadow-offset` bottom edge (a key on the table), bold `--wood` "i" (label size, Inter SemiBold). In a nation row it sits on the name line, right after the name and seat tag. Hover follows the pickable language on the key (lift, grow, `--paper` fill, `--wood-dark` frame and letter); pressed, it scales to `--press-scale` and its bottom edge flattens to `--border-width`. The round line also frames itself in `--wood` on hover, a nation row in `--ink-faint` (so it never reads as the wood "whose turn" frame). Open, the key turns solid `--wood` with a `--paper` letter. The key is a label, not a Button, so one click never toggles twice. (No pointer cursor: UI Toolkit runtime cursors need a cursor texture, and the hover lift already says "clickable".) Text that names it writes "the **i**" (bold): Inter has no ⓘ glyph.

**Round line (top center):** a slim card with two centered lines and the "i" key at its end: the Destiny line, "Destiny 2: Day of Ra, Morrígan −2" (h2, Inter SemiBold: it is all numbers), and under it the round counter, "ROUND 1/15" (label, `--ink-soft`), so the top never crams both facts into one line. It appears when the Destiny Die lands.

**Round info panel:** a card under the round line (and the Day badge), centered, `--info-panel-width` wide, padding `--space-4` × `--space-6`. Positioned absolute, so it lies over the board and never pushes the prompt and actions down, however long the Day's text (checked at 1280×720 with a Day badge, hint and news line all showing). Full sentences: label headings in `--ink-soft` (what it is: "DESTINY 2 · RA'S SACRED NUMBER", "WHO OPENS", the Day, "GOAL"), body text (what it does), one row per god with its chip so the effect is never color alone. "Click or Esc to close" (label, `--ink-soft`) at the bottom. It closes itself when "YOUR TURN" arrives, so the board top is clear when the player must act; while it is open a bot's turn banner is skipped.

**Nation powers card:** a flyout to the right of its nation row (`--space-4` from the panel), `--powers-card-width` wide: `--paper` fill, `--paper-shade` border with a `--space-2` left edge in the nation color (the row beside it carries the chip and the solid "i", so the color is never alone), flat shadow, radius `--radius-md`. The top two rows (Zeus, Ra) open top-aligned with their row, the bottom two (Thor, Morrígan) bottom-aligned, so no card pushes the panel off screen or reaches the prompt at 1280×720 (an inline card under the row did both). Same info-panel type: "DAY OF ZEUS · DESTINY 1" and the Day in one sentence, "WRATH · AT 3 ★ FAVOR" and the Wrath, then how Favor fills from this god's sacred and cursed numbers (`--ink-soft`). Every number comes from the Core constants.

**Reinforcement counter:** while the human places reinforcements, a card in the center action slot (where END TURN appears once all are placed): a die-like token (`--counter-token-size`, `--paper` face, `--die-border` `--ink` outline, h1 number in Inter SemiBold), "TROOPS TO PLACE" / "TROOP TO PLACE" (label) and, under it, where the troops came from: "2 base + 1 for 2 territories + 2 Destiny" (label size, Inter Regular, `--ink-soft`). The number pops to `--pop-scale` on every placement over `--duration-base`. The prompt line above says only "YOUR TURN" / "ZEUS'S TURN" meanwhile, so the bottom of the screen stays quiet.

**Prompt bar (one component for every prompt):** a paper bar centered above the actions (`.prompt`, up to `--prompt-max-width`), like a note laid on the table: the acting god's **chip** at its start, a `--space-2` left edge in that god's color, and two lines: a small **label** (label size, `--ink-soft`, tracked) over the **instruction** (h2, Inter SemiBold, `--ink`, wraps). The label says what kind of prompt it is: "YOUR TURN" (`MatchText.TurnPrefix`), "SACRED ROAD" in Road mode (the whole frame turns `--ink`: it is the call to act), the Wrath's name ("THUNDERBOLT") while aiming. Refusals ("Pick one of your territories", "Attacks wait: …") replace the instruction in the same bar. The bar shows only on a person's turn and only when there is an instruction (an empty Reinforce prompt leaves the reinforcement counter alone), never on a bot's turn: the turn box says whose turn it is.

**Turn box (top right, one turn indicator):** a paper card under the sound / fast-bots toggles (`--turn-box-top`, `--turn-box-width`, `--space-8` from the edge): the acting god's chip, a `--space-2` left edge in that god's color, the title ("YOUR TURN" for the human, "THOR'S TURN" for a bot; body, SemiBold, `MatchText.TurnBox`) and under it a short status (label size, `--ink-soft`): a bot's "Reinforcing…", "Thinking…", "Attacking…", "Rolling…", "Marching an army…", "Unleashing Thunderbolt…", a person's "Place your troops" / "Attack phase". It slides in from the right (`--duration-slow`) when the turn changes and hides on the menu and the end screen. It replaces the old "Thor is playing…" line at the bottom.

**Action row (bottom):** three slots: the two sides share the leftover width equally, so the center slot (END TURN, or the reinforcement counter, or the Offering pair) sits at the exact bottom center; the Wrath button waits in the left slot, right-aligned, `--space-4` from the center.

**END TURN:** the main action of every turn: primary button, `--action-button-height` tall, `--space-12` side padding, body-size label text.

**Wrath button (the god's signature power):** `--wrath-button-height` tall, at least `--wrath-button-min-width` wide, built like a game token, never a grey "disabled" button (it is never `SetEnabled(false)`: UI Toolkit would fade it). Left to right: the god's icon, the Wrath's name in Cinzel bold at h2 over a state line (label size), and the **three Favor stars** (`--wrath-star-size`, the drawn sacred star: pale `--ink-faint` when empty, the nation color on an `--ink` outline when full). A 4 px frame in the god's color on every state. Four states (classes set by `HudView.SetWrath`, `WrathButtonState`):
- **Locked:** `--paper` face, god-color frame, ink name, "FAVOR 1/3" (or "NO TARGET" when full but nothing to strike); the stars show the progress. It does not lift or grow on hover and does not answer a click.
- **Ready (3/3 and a target):** solid nation color, `--ink` frame with a deeper bottom edge (a painted tile, like the pieces), `--paper` stars on their ink outline, "READY TO UNLEASH", a **halo ring** in the nation color just outside the frame and a slow breath: the button eases to `--wrath-pulse-scale` and back (each half `0.8 s`, `HudView`) while the halo goes from soft to full. Text and icon `--ink`, `--paper` on Morrígan's black. Hover frames it in `--wood-dark` (plus the usual lift and grow). The only state that unleashes.
- **Used this turn:** `--paper-shade` face, god-color frame, `--ink-soft` name, "USED THIS TURN".
- **Aiming:** back to paper, `--wood` frame, "CLICK TO CANCEL" (it cancels the aim); no breath.
Never color alone: the icon, the name and the stars are on every state.

**Sacred Road (the army's march, powers on):** opened by the attacker's own sacred die. It has **no button**: the board itself is the control. When the die's result has been read and the person is back in the attack loop, **Road mode** starts by itself.
- **Popup (a person's Road only; a bot's Road is a discreet news line):** when the sacred die has been read (the same 1.3 s after the Favor news of that die, so the Favor "why" is read first), a paper card slides in at the top center (`.road-popup`: `--paper`, `--paper-shade` border, flat shadow, a `--space-2` left edge in the acting god's color, `--road-popup-width` wide, `--road-popup-top` from the top so it sits under the round line and ends above the north temple). One slim row: the god's small chip, **"SACRED ROAD OPENS"** (Cinzel bold at h2, `--ink`), one short sentence (body, `--ink`): "March an army up to 2 territories", and "Click to close" at the end (label, `--ink-soft`); the prompt bar below says what to click. It rises 16 px into place while scaling 0.94 to 1 and fading in (`--duration-base`, `--ease-enter`; out with `--ease-exit`), stays 2.2 s (`TurnBannerView`, serialized; the same view as the turn banner, no new type), and closes on a click on it, on the person's next action (picking an army, END TURN, a Wrath) or when a new turn starts. It plays the sacred chime. It has no scrim and sits above the board's middle rather than on it, so the rings it talks about are never hidden: it catches clicks only where it is drawn, only while shown (picking is Ignore otherwise), so the board is never blocked longer than it takes to read. It appears together with the rings (Road mode waits for the popup, never the other way round). A Wrath keeps the Road open but does not repeat the popup. It is not shown when no army can march (nothing is blocked either).
- **News line (bots only):** "SACRED ROAD · Zeus may march an army 2 territories" in the existing news area (god tint, drawn sacred icon first), with the sacred chime, same 1.3 s gap. A march's news line carries the ★ and the god tint; floats always carry a sign (-N / +N).
- **Road mode (human only, never for a bot or for another person's turn; Classic has none):** every own territory that can march (2+ troops and a destination along own territories) gets a **thick ring in the acting god's own color** (`--nation-*`, the same color as the pieces it surrounds: the marching army is yours, so your color glows) that breathes (same clock as the target rings, but it stays bright, alpha 0.8 to 1), a **pale `--paper` edge** just outside the ring (a double outline, so a gold or sky-blue ring stays crisp on the tan board and the dark god's black ring never passes for the plain `--ink` selection ring), and a small "ROAD" tag in `--ink` just outside the ring (below the top half of the board, above the bottom half, so it never lands on the wooden frame). The color is never alone: the ring is the thickest on the board, it breathes, it has the paper edge and the ROAD word; it differs from the selection ring (solid, `--ink`, no breath, no paper edge, lifts the piece) and from the attack target ring (thin, dashed, `--wood`). The prompt bar, centre-bottom, is the call to act: its label reads "SACRED ROAD" and its whole frame turns `--ink` (`.prompt--road`): "Click a ringed territory to march its army, or press END TURN". Nothing else reacts on the board except the Wrath button, which still works.
- **Picking:** a click on a ringed territory lifts it (selected ring, ink), shows **all its destinations** with the same ring (god color, paper edge, breath) and the troops that would march as a still "+N" over each, and every other army goes plain (so only what a click can do glows). Prompt: "Now click a ringed destination · click the territory again to pick another". A click on a destination marches (path animation, news line "Zeus's army marched: Capital to Village (+9)"). A click on the picked territory, Esc or right-click drops the pick and returns to the ringed armies (never to normal attacking). Any other click says why in the prompt (replacing it): "Attacks wait: click a ringed territory to march first, or press END TURN", "That army can't march (needs 2+ troops and a path of your own lands) · click a ringed one", "Click a ringed destination, or the picked territory again to pick another". The destinations are looked up once per pick, never per frame.
- **Declining:** only END TURN, which loses the Road. There is no skip button, and attacking is not available until the Road is resolved or the turn ends. If no army can march, Road mode does not start and nothing is blocked. After a march the Road is spent: highlights and the large prompt clear and the normal attack loop continues (a later sacred roll in the same turn can reopen it).
- **Wrath:** pressing the Wrath while Road mode is on suspends it (its highlights are replaced by the Wrath's rings and prompt); cancelling the Wrath brings the ringed armies back, and a unleashed Wrath returns to the attack loop, which re-enters Road mode if the Road is still open.
- **March animation:** see the table (path lit in order, "-N" / "+N"). A bot shows origin (selected ring) and destination (target ring) for one step pause first. Texts use "to", not an arrow, because Inter may not ship the glyph in the UI font asset.
- **Tokens:** no road color: the Road wears the acting god's nation color, `--paper` and `--ink`. New sizes only: `--road-popup-width`, `--road-popup-top`. The rest reuses the news line, selected ring and capture rings. (The first version used an indigo `--road-blue`; playtest said it clashed with the parchment palette and read as a second Zeus blue, so it was removed.)


**Nations panel (left, under the identity card):** headed by the goal, always on screen: "First to hold 3 temples wins" (label size, sentence case, Inter SemiBold, `--ink-soft`). Then one row per god in base cycle order: nation chip, name (body, bold) with a seat tag on every nation a person plays (label, `--paper` on `--wood`, radius `--radius-sm`): YOU (bots get no tag), then the "i" key on the same line; stats (label, `--ink-soft`); the temple line (temple icon + one h2-size dot per temple in a `--temple-dot-size` box, ● held / ○ not; the 3rd dot, the one that wins, is ringed in `--wood` as the goal and shows a small "3" (label, `--wood`) inside its ring while empty, ● once held); and with Pantheon powers the Favor stars "★★☆" alone (body, Inter SemiBold, `--ink`; no "Favor" word, the powers card says it). Whose turn: `--wood` frame, `--paper-shade` fill and a ▶ mark. Eliminated: 50% opacity, "eliminated" text, no temple line.

**End banner:** `--scrim` over the board, centered card whose top edge (`--space-2`) takes the winner's color. Under the verdict (h1) sits the **winner card**: the winner's chip (`--chip-lg`), name (body, SemiBold) and the reason, on the winner's tint with a nation-color border. Then three stats (h1 value, label name) split by `--paper-shade` dividers, then Play again / Main menu. If there is no winner yet (human eliminated), the card stays neutral: chip and name hide and only the reason remains.

| Mode | Verdict (h1) | Winner card name | Stats |
|---|---|---|---|
| Match | "YOU WIN" / "YOU LOSE" | "Zeus wins" | rounds, "Zeus's temples", "combats you won" |

**Destiny Die (modal, round start):** `--scrim`, a card with "ROUND N" (h1), a `--die-size-destiny` die (thick border, `--die-edge-destiny` ink bottom edge) that tumbles and lands. On a god's face the die takes the favored god's color (border) and tint (face) and shows that god's chip on its top-right corner, so the favor is never color alone. 3 and 4 land as a plain paper die. Effect line (body) and "Click to skip" (caption) below.

**Hint toast:** a paper note above the prompt: `--paper` fill, `--paper-shade` border, a `--space-2` `--wood` left edge, flat shadow, body text. One at a time; it hides itself or on the next action.

**Fast bots button:** secondary button, top right. Hidden while the main menu is open (it only means something during a match) and on the end banner.

**Territory piece (board, sprites):** a painted wooden token. Layers from back to front: offset shadow (`--ink` at 22%, offset down-right), 2 px `--ink` outline (keeps gold and blue borders ≥ 3:1 against the board), nation-color border, nation-tint fill, god icon in `--ink` (about 40% of the smallest piece) and the troop count bold in `--ink`. Unowned pieces are `--paper` with an `--ink` border and no icon or count.

**Board highlights (never color alone):**

| State | Ring | Motion | Lift |
|---|---|---|---|
| Selected (attack from) | thick solid `--ink` ring | none | piece scales 1.1 and its shadow lengthens |
| Legal target | thin **dashed** `--wood` ring | gentle breath: alpha 50-100% and +5% scale over 1.4 s, all targets in sync | none |
| Hovered (only if usable now) | the `--ink` outline warms to `--wood` (a soft glow) | eases in and out over `--duration-fast` (120 ms, ease-out) | +8% scale, on top of the selected lift |
| Sacred Road (an army that can march, and where the picked one can go) | thick solid ring in the **acting god's nation color** with a pale `--paper` edge outside it; origins also carry a "ROAD" tag in `--ink`, destinations a still "+N" | breath: alpha 80-100% and +5% scale over 1.4 s, in sync with the target rings | none |

**Hover rule (board):** a piece reacts to the pointer **only when a click on it would do something right now**, so hover itself teaches what is playable. During the human's Reinforce: their own territories. During Attack with nothing selected: own territories that can attack (2+ troops and at least one legal target). With a territory selected: its legal targets. Nothing reacts during bot turns, the Destiny Die, the turn banner, a pending combat, or under a HUD button. The rule is decided by `MatchController` from the same checks the clicks use (`Match.CanAttack`), never duplicated.

**Board frame:** the board sits on a `--wood` frame (0.2 units wide) with a soft `--ink` shadow, like an altar board on a table. Connection lines are `--ink` at 50%, slightly thicker than a hairline.

**Materials (board sprites, `Assets/_Project/Art/Textures/`):** texture is felt, not seen. Colors are baked from the tokens, so each sprite renders with a white tint and the token stays the average color.

| Surface | Sprite | Base token | Texture | Contrast |
|---|---|---|---|---|
| Table (screen background, `Table` object, tiled 48 × 32 units behind everything; the camera's flat `--parchment` is only a fallback) | `parchment_tile` (512 px, 64 px/unit = 8-unit tile, seamless) | `--parchment` | soft mottling + short paper fibers | about ±3% luminance; fibers at most -2% |
| Board inner area (`Outer`) and altar steps (`Steps`) | `board_inner`, `board_steps` (48 px/unit) | `--paper-shade`, `--parchment` | the same parchment pattern, sampled in world space so it lines up with the table | 60% of the table's: troop counts and lines must win every time |
| Board frame (`Frame`) | `board_frame_wood` (48 px/unit) | `--wood`, grain lines toward `--wood-dark` | grain runs along each side; the four sides meet in mitered corners with a thin dark joint | grain lines up to about 1/3 of the way to `--wood-dark` |

Rules: no texture on pieces, icons, lines or UI cards (they stay flat paint and ink). Baked sprites match the scene sizes (frame 17.8 × 10.8, inner 17.4 × 10.4, steps 9 × 9 units, rounded corners matching `rounded_square`); if the board frame is resized, regenerate them. Import: Sprite, Full Rect mesh, no mipmaps, max 1024, default compression (DXT1 for the opaque tile, DXT5 for the rest).

**God card:** same frame for all four gods (consistency is the point). Top band in the nation **tint** with the nation chip (color + icon); god name in Cinzel (h2); sacred and cursed numbers as two small die-like tags in the dice language, each icon + number + word: star burst icon + "1 sacred" on the nation tint with a nation-color border, cracked die icon + "6 cursed" desaturated (`--paper-shade`, `--ink-faint` border); rival line in `--ink-soft`. Hover lifts the card (`--hover-lift`), grows it (`--hover-scale`) and frames it in `--wood`; press drops the lift and scales to `--press-scale`. With Pantheon powers, two power lines at body size ("**Storm:** enemies −1, to Zeus" / "**Thunderbolt:** halve an army"), one line each.

**Menu mode row:** one button pair on one row: PANTHEON POWERS / CLASSIC. The pair is a radio: the selected one is the primary button with a "●" lead, the other secondary with "○" (shape + fill, never fill alone). Buttons also fade their text color, so a switch never shows wood text on a wood fill mid-transition. One line under the row (body, `--ink-soft`) says what the powers choice changes.

**Menu prompt row:** under the mode line, as tall as a button (`--button-min-height`): "Choose your god" (body, SemiBold); a card click starts the match as that god. (Hot-seat, with a START button and HUMAN / BOT seat tags, was removed for scope on Nov 11.)


**Sacred-number rule (main menu, under the god cards):** one fixed sentence, body size, `--ink-soft`, centered over three rows: "Each god has a [sacred icon] sacred number (beats any neutral roll) / and a [cursed icon] cursed number (loses to any neutral roll). / Your cursed number is your rival's sacred one." The drawn omen icons (`.omen-icon`, `--icon-sm`) replace the ★ / ✗ glyphs, so this is built as rows of labels and icons, like the HUD identity line. Teaches the core rule before the first click, without a tutorial.

**Tooltip:** `--paper`, radius `--radius-md`, padding `--space-3`, caption or body size, appears after 300 ms hover, never covers the hovered element.

**Turn banner:** top center, h2, slides down, stays 1.2 s, slides up. Shows the nation chip + "YOUR TURN" (human) / "ZEUS'S TURN" (a bot), with a `--space-2` left edge in the nation color.

**Die:** rounded square, `--paper` face, `--ink` pips/number, 2 px outline. In the roll overlay, both dice side by side with a short boxed tag under each: the number and its outcome in capitals (label size, Inter SemiBold, tracked, centered, one line inside `--outcome-tag-width`): sacred → [sacred icon] "1 · SACRED", cursed → [cursed icon] "6 · CURSED", neutral → "4 · NEUTRAL" (no icon). Whose die it is comes from the god's tint and border, the name over the die and the die's colors, so the tag never repeats the god. Both tags share one height.

**Combat panel (right):** fixed `--dice-panel-width`, vertically centered. Title "Zeus attacks Thor" (label size), the two dice with their tags, then **one boxed verdict** and, only when they apply, badges. Once the dice land the **loser's die and tag fade to 55%** (the winner stays at full strength); no sentence says who won.
- **Verdict box** (`.verdict`): the winner's chip at its start, the winner's tint and border (like the outcome tags) and a `--space-2` left edge, and the rule that decided it as a **headline** in capitals at h2 (Inter SemiBold, wraps): "SACRED BEATS NEUTRAL", "NEUTRAL BEATS CURSED", "SACRED BEATS CURSED"; when both dice read alike a small **kicker** (label size, `--ink-soft`) sits over the headline: "NEUTRAL VS NEUTRAL" over "HIGHER WINS" or "DEFENDER WINS" (equal neutral numbers), "BOTH SACRED" / "BOTH CURSED" over "DEFENDER WINS" (a tie of two sacred or two cursed dice, since each god's sacred number is another god's cursed one), and over "TIES GO TO THOR" under Ragnarök.
- **Badges** (small chips under the verdict: `--paper` fill, `--wood` border, `--wood-dark` label text): "OFFERING MADE · −1 TROOP" on a re-rolled combat, and under Thor's Day "RAGNARÖK" (with " · −2 TROOPS" when his win costs the loser more than one) when it bent the rules.
- "Click to continue" (label size, `--ink-soft`) while the person's result waits.

## Iconography

- One flat, single-color line style for every icon: 2 px stroke at 24 px, rounded caps.
- Icon sizes: 16, 24, 32, 48 px.
- Nation icons: lightning bolt (Zeus), hammer (Thor), sun disc (Ra), crow (Morrígan). Game icons: temple, troop, die, clover, skull, sacred, cursed.
- One icon pack or one hand-drawn set only. Never mix sources. All current icons are flat white silhouettes on transparent (`Assets/_Project/Art/Placeholder/icon_*.png`, 256 px), tinted with `-unity-background-image-tint-color`.
- **Sacred icon** (`icon_sacred.png`): an 8-point star burst (4 long rays, 4 short). **Cursed icon** (`icon_cursed.png`): a rounded die with two pips split by a jagged vertical crack (never an ×, so it can't be read as "wrong" or "close"). Both read at 16 px.
- **Glyph vs icon:** where the sacred / cursed mark **stands on its own** it is the drawn icon: die corner marks, combat outcome tags (icon, then "1 · SACRED"), the menu's sacred-number rule, god card tags (icon + number + word), the HUD identity line ("ZEUS ·" [sacred icon] "1 sacred ·" [cursed icon] "6 cursed": a row of three labels with `.omen-icon` between them, because Inter's ✗ reads as "×" at body size), and the Favor news line ([cursed icon] "6 cursed: −1 Favor (1/3)": the news line is a row with an optional leading `.omen-icon`, set by `HudView.ShowNews`). **Inside hint sentences** it stays the ★ / ✗ text glyph, so it flows with the text. USS: `.omen-icon` (size `--icon-sm`, tint `--ink`) + `.omen-icon--sacred` or `.omen-icon--cursed` (cursed tints `--ink-soft`, desaturated like the cursed die). The word always stays next to the icon: never icon alone.

## Motion and juice

Subtle, fast, purposeful: motion should explain cause and effect.

| Token | Value | Use |
|---|---|---|
| `--duration-fast` | 120 ms | Hover, press |
| `--duration-base` | 200 ms | Panels, banners, highlights |
| `--duration-slow` | 400 ms | Screen transitions, victory |
| `--ease-enter` | `ease-out-cubic` | Things arriving or reacting (hover, press, banners in) |
| `--ease-exit` | `ease-in-cubic` | Things leaving (banners out) |
| `--hover-lift` | `0 -2px` (translate) | Button and god card hover |
| `--hover-scale` | `1.02 1.02` (scale) | Button and god card hover (board pieces use +8%, a serialized field in `TerritoryView`) |
| `--press-scale` | `0.97 0.97` (scale) | Button press |
| `--pop-scale` | `1.15 1.15` (scale) | A count that just changed (reinforcement counter), like a piece that gains a troop |
| `--favor-bump-scale` | `1.3 1.3` (scale) | A Favor meter (identity card or nation row) the moment a star lands on it (`.favor--bump`) |
| `--favor-dim-opacity` | `0.4` | A Favor meter that just lost a star to a cursed roll, for a moment (`.favor--dim`) |
| `--wrath-pulse-scale` | `1.04 1.04` (scale) | The ready Wrath button's slow breath |

- UI motion: USS `transition` on `translate`, `scale`, `opacity`, `background-color`.
- Board motion (sprites): small coroutines in `TerritoryView`, durations in serialized fields. A piece hops (scale 1 → 1.15 → 1, 200 ms) when it gains a troop or changes owner; a loss never hops, it shows the "-1" instead, so one thing moves per piece.
- Capture: a ring in the new owner's color expands (+45%) and fades over 350 ms, drawn above the piece.
- Troop loss: "-1" in `--ink` at 85% of the count size, above the count; rises with an ease-out, stays opaque for the first 40% of 1 s, then fades.
- Dice roll: the tumble lasts exactly as long as the one roll clip (1.3 s), so the dice land on its final clack; then land with a small overshoot. This is the moment of the game, so it gets the most love (sound + motion). On landing (400 ms): a sacred die pulses twice (+8% scale; its tint and thick border are the glow), a cursed die shakes sideways (6 px, dying out). Only the dice that just rolled react.
- **Favor star (sacred → Favor):** when a combat resolves and a side's final die was its sacred number (+1 Favor), a `--favor-star-size` star (the sacred star burst in the nation color over an `--ink` one, its outline) pops over that die (scale 0.4 → 1.2), flies on an arc (`120` panel px high, ease-in-out, shrinking to 0.6) to that god's Favor stars, and bursts there (grows to 1.4 and fades); the meter fills (☆ → ★) and bumps to `--favor-bump-scale` the moment it lands, not before. Destination: the identity card's Wrath band for the human, every other god's nation row. 1 s in all (`HudView`, serialized), a third with ▶▶ on a bot's turn. **Cursed (−1 Favor)** gets the quiet counterpart, no flight: a cracked-die mark (`--icon-md`, `--ink-soft`) appears over the meter, sinks 16 px and fades (opaque for the first 40%, 0.8 s) while the meter dims to `--favor-dim-opacity`. Both are pooled (two stars, two marks: one combat can move both sides) and move by `translate` from the panel scheduler, never allocating per frame. A Wrath or an elimination empties a meter at once, without a star (it has its own news).
- Screen shake only when a territory is taken from a rival (empty captures do not shake), and for a Thunderbolt or a Mjölnir that does not capture: 3 px (2-4), 120 ms, measured in screen pixels.
- **God power VFX** (`GodVfx`, prefab `Prefabs/GodVfx.prefab`, a child of `Board` at its origin): procedural, from the placeholder sprites and two LineRenderers, pooled. Each god has its **own shape** as well as its color, so a power is never told by color alone. Drawn above the pieces (sorting order 8-10). Each effect lasts about 1 s at most; with ▶▶ fast bots a bot's effects run at a third of the time.

| Power | Shape and motion | Length |
|---|---|---|
| Thunderbolt (Zeus Wrath) | zigzag bolt (nation color core, `--ink` outline) falls on the target, flickers once and fades; a nation-color ring spreads from the impact | 0.55 s |
| Storm (Zeus Day) | a small bolt on every struck territory (staggered), then one small bolt icon per struck territory flies on an arc to Zeus's home, where a ring answers | ≤ 1.1 s |
| Mjölnir (Thor Wrath) | the hammer icon swings down onto the target (ease-in), then a nation-color shockwave and a thinner `--ink` ring roll out | 0.6 s |
| Ragnarök (Thor Day, and a Thor win that costs 2) | ember pulse: two quick nation-color rings and a few rising sparks on Thor's pieces (Day) or on the winning piece (2-troop blow) | 0.75 s |
| Sun at noon (Ra Day) | eight short rays turn and spread from every Ra piece over a soft nation-color glow | 0.9 s |
| Eye of Ra (Ra Wrath) | the sun disc flares (grows and fades) over the converted territory with a ray burst and a ring | 0.75 s |
| Sacred Road march | the path lights up territory by territory in the god's color (the capture ring), `0.35 s` each; "-N" floats on the origin as the army leaves and "+N" on the destination as it arrives. Plays alone: the turn loop waits for it | ~0.35 s per step |
| Night of crows / Harvest (Morrígan) | two or three crow icons fly on curved paths from the territory that lost troops to the one that gains them, wings beating, dropping feathers that drift down; at the start of Night of crows they fly in from beyond her corner to her nest | ≤ 1.1 s |

  The presenter decides when: a Day's effect plays after the Destiny Die lands, together with the "-1" / "+1" it explains, and the turn banner waits for it (one strong animation at a time). Wrath and combat effects play with their troop change.
- Pacing (readability first; ▶▶ fast bots divides every bot timing by 3): a bot waits 0.7 s per troop and 0.8 s before and during each attack; a bot combat result stays 2 s; the human's own result stays until a click on the board or 3 s ("Click to continue" under the verdict), or until they choose when an Offering is possible; Destiny holds 1.5 s after landing; the turn banner stays 1.6 s.
- **Attack path (`AttackPathView`):** when an attack starts (a person's click or a bot's attack, before the dice), a march line grows along the board link from the attacking piece's edge to the target's edge: dots (a nation-color core on an outline: `--ink`, or `--paper` for Morrígan's black) popping in one after another, led by an arrowhead (the diamond sprite, drawn long and thin) that rides the head of the line. It takes `0.35 s` (ease-out, times the fast-bots pace on a bot's turn), then stays lit at 85% with a slow breath (1.4 s) while the dice roll, and fades over `0.4 s` when the combat resolves. It does **not** delay the dice: they roll while the line grows. An attack on an empty territory has no dice, so its path holds `0.3 s` and fades by itself. The arrowhead shrinks on a short link (Capital to Village). It sits above the pieces and the capture ring, below the god effects. The match start, a new turn and the end screen clear it.
- Respect restraint: at most one strong animation on screen at once.

### Pacing (playtest 2)

Playtest 2 found everything too quick to read, so timings grew about 1.5-2x. These values replace the older ones above; they live in serialized fields (scene and `GodVfx.prefab`) with matching code defaults. ▶▶ fast bots still divides bot timings by 3; the human's own result hold and Destiny hold are never shortened.

- **Destiny Die**: tumble 1.3 s (the roll clip's length), landing 0.3 s, then a 2.5 s hold. A round with news (a god's **Day** line or the **bad omen**) holds 1.5 s more, 4 s in all. A click skips it at any time.
- **Turn banner**: 2.4 s.
- **Combat dice**: tumble 1.3 s (the roll clip's length), landing 0.25 s, sacred pulse / cursed shake 0.6 s.
- **Results**: the human's own combat result stays until a click or 4.5 s; a bot's result (combat or Wrath) stays 3 s.
- **Bots**: 1 s per placed troop, 1.2 s before and during each attack or Wrath.
- **God power VFX** (approximate total): Thunderbolt 1 s, Mjölnir 1.1 s, Eye of Ra 1.3 s, Ragnarök embers 1.7 s, Sun at noon 1.7 s, Night of crows / Harvest up to 2 s, Storm up to 2.3 s (strikes spread over at most 0.5 s, then the troops fly home in 0.8 s).

## Audio identity (for coherence)

Wooden and tactile: dice clacks on wood, soft paper swishes for panels, a low drum for combat, a bright chime for sacred, a dull thud plus crack for cursed. One short music loop with ancient-instrument flavor (lyre, frame drum), kept low under SFX (not in the jam build: no music yet).

Shipped cues (Kenney CC0, clips and volumes in the `AudioCues` asset; where a cue has variations, one is picked at random; the dice roll never varies):

| Moment | Sound | Sync |
|---|---|---|
| Every roll: combat, Offering re-roll, Destiny Die | `dice-roll`: one 1.3 s clip, always the same: a hard throw hit on frame 0, dice clattering and bouncing on wood with shorter, softer gaps, and a loud landing clack in its last 30 ms | starts on the frame the dice start tumbling and is exactly as long as the tumble, so its clack is the landing; no delays, no variations, no stretching |
| Landing sting | bright chime (sacred) / dull bong (cursed) | on landing; one per roll: a sacred winner chimes, any cursed die bongs, plain numbers stay quiet. For Destiny: chime if it helps the player, bong if it hurts them |
| Troop placed | chip laid (one clip, always the same) | each troop |
| Capture | chips stacked (2 variations) | with the ring |
| Turn change | card slide | with the turn banner |
| Victory / defeat | rising confirmation / falling minimize | with the end screen, once |
| Button / invalid click | soft click / short error | on the click |

Rules: rolls share one voice and one clip (a new roll stops and restarts it; landing never cuts it) so clatter never stacks and a roll always sounds the same; everything else is a one-shot. Mute toggle ("SOUND: ON/OFF") next to fast bots, kept for the session. On the web, audio starts after the first click (choosing a god).

## Layout (1920×1080 reference, must work down to 1280×720)

- **Center:** the board. Always the largest element. **Panels never cover it, at any aspect ratio from 4:3 to 21:9.** The HUD carries an invisible `board-area` rectangle, the screen minus the `--board-gutter-*` margins (left column + gap, combat panel + gap, round line, prompt / actions). `BoardCameraFit` (on the camera) reads that rectangle every time it changes and sets the orthographic size and position so the board frame (17.8 × 10.8 units plus its shadow) fits inside it, centered in it. The left column is exactly `--nations-panel-width` and the combat panel exactly `--dice-panel-width`, so the gutters are true. (The old fixed size, 8.4, fitted the screen *height*: right at 16:9, but at 16:10 and 3:2 the panels, sized to the 1920 px width, reached over the board.) Only the nations' powers flyout and the round info panel, which the person opens on purpose, lie over the board.
- **Top center:** round number, current Destiny Die result and turn banner.
- **Left:** the four nations stacked (icon, territory count, troops); current player highlighted.
- **Bottom center:** the prompt bar ("YOUR TURN / Choose a territory to attack from, or end your turn") and, under it, the Wrath button, END TURN (exact center) and the reinforcement counter.
- **Right:** the turn box under the sound and fast-bots toggles, and the combat panel (vertically centered).
- Sound and fast-bots toggles: top right.
