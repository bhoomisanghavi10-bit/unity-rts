# UI Art Brief — Kingdoms of Bharat

Companion to Roadmap Section 4.3 "UI skin." This is a sourcing brief for Canva (or
any 2D tool) — Claude Code doesn't create game art per the project's asset-sourcing
rule (Section 4.4); this file exists so you can hand prompts straight to Canva's
Magic Media / Text-to-Image tool, or to a human artist/commission, without having to
re-derive the spec each time.

Once assets exist, drop them at the paths noted per asset and they wire in
automatically — see "Where it plugs in" per tier.

---

## Master style paragraph (reuse in every prompt for consistency)

> Flat 2D game UI art for a historical Indian-subcontinent real-time-strategy game
> (Chola, Vijayanagara, Rajput, Maurya, Maratha empires). Painted/illustrated style —
> not photorealistic, not flat vector/minimalist — think hand-painted parchment and
> carved stone/metal ornamentation, warm earthy palette (ochre, sandstone, bronze,
> deep maroon, forest green), Indian temple/fort motifs (lotus, gopuram silhouettes,
> mandala borders) used sparingly as accents, not covering the whole surface. High
> contrast and clearly readable at small size — this will be viewed at real-time-
> strategy zoom, not full screen. Transparent background, no drop shadow baked in
> (shadows are applied by the game engine).

Paste the paragraph above at the start of every individual prompt below, then add
the asset-specific line.

---

## Tier 1 — highest visibility (do this tier first)

**Where it plugs in**: `Assets/Resources/UI/Icons/` and
`Assets/Resources/UI/Panels/` (folders don't exist yet — create them on import).

### Command-card button background (9-slice)
- Size: 128×128 px source (needs a 9-slice-safe border — keep corner ornamentation
  within the outer ~20px, leave the center flat/plain so it stretches cleanly)
- States needed: Normal, Hover, Pressed, Disabled (4 separate images, same
  composition, only tone/brightness differs)
- Prompt: *"...a square button frame/plate icon for a strategy-game command panel —
  bronze/aged-metal beveled border with a subtle carved geometric pattern at the
  corners, flat dark stone-colored center, square format, tileable/stretchable
  center region."*

### Action icons (18 total, 128×128 px each, transparent background)
Use one shared prompt template, swapping the bracketed subject:

> *"...a single simple icon representing [SUBJECT], centered, bold silhouette
> readable at small size, muted bronze/gold linework on a dark parchment circle
> background, square icon format, no text, no border frame (frame is added
> separately)."*

Subjects needed (maps to `BuildMenu.cs` buttons):
1. Build Barracks (crossed spears over a tent/fort icon)
2. Build Farm (wheat sheaf)
3. Build House (simple hut/roof)
4. Build Wall (stone wall segment)
5. Build Gate (arched gateway)
6. Build Tower (watchtower silhouette)
7. Build Market (scale/balance or stacked goods)
8. Build Dock (anchor or ship prow)
9. Train Worker (a simple worker figure with a tool)
10. Train Soldier (sword and shield)
11. Train Archer (bow and arrow)
12. Train Cavalry (horse head silhouette)
13. Train Siege (a catapult/siege engine silhouette)
14. Train Spearman (a single spear, diagonal)
15. Train Unique Unit — slot 1 (leave generic "star" placeholder; unique per civ
    later if you want one icon per civ's first unique unit)
16. Train Unique Unit — slot 2 (same as above, second slot)
17. Upgrade Attack (a sword with an upward arrow)
18. Upgrade Armor (a shield with an upward arrow)
19. Advance Age (an hourglass or rising-sun icon)

### Resource icons (4 total, 64×64 px each, transparent background)
- Wood — *"...a simple wood-log icon, brown, small leaf accent"*
- Food — *"...a simple wheat-sheaf or rice-bundle icon, gold/tan"*
- Gold — *"...a simple stacked-coin icon, bright gold/yellow"*
- Stone — *"...a simple gray stone-block icon"*

### Resource-bar panel frame (9-slice)
- Size: 256×64 px source, wide horizontal strip
- Prompt: *"...a wide horizontal banner/plate for a resource counter bar, aged
  bronze-and-parchment strip with a subtle carved border top and bottom, flat
  center, landscape format."*

---

## Tier 2 — fortification / frequent HUD

**Where it plugs in**: `Assets/Resources/UI/Panels/` and
`Assets/Resources/UI/Cursors/`.

### Selected-unit panel frame (9-slice) — 256×80 px
- Prompt: *"...a rectangular info-panel frame, same bronze/parchment family as the
  resource bar, portrait-adjacent notch on the left edge, landscape format."*

### HP bar fill + frame (2 sprites) — 128×16 px each
- Frame: *"...a thin horizontal bar frame/track, dark recessed metal groove"*
- Fill: *"...a solid horizontal bar of health-bar red-to-green gradient, flat,
  no border (border is the separate frame sprite)"*

### Hover-tooltip panel frame (9-slice) — 192×96 px
- Prompt: *"...a small compact tooltip frame, same bronze/parchment family,
  simpler/thinner border than the main HUD panels since it's small and
  transient, landscape format."*

### Cursor set (5 total, 32×32 px each, transparent background, hotspot at the
tip/center as noted)
- Default — *"...a simple pointing hand or stylized arrow cursor, hotspot at the
  tip (top-left)"*
- Attack-move — *"...a crossed-swords cursor icon, hotspot centered"*
- Invalid — *"...a red circle-with-slash cursor icon, hotspot centered"*
- Gather — *"...a small sickle/hand-tool cursor icon, hotspot centered"*
- Build-placement — *"...a small hammer-and-nail cursor icon, hotspot centered"*

---

## Tier 3 — menu screens

**Where it plugs in**: assign directly to a `UIStyleTheme.asset`
(`Assets/Resources/UI/UIStyleTheme.asset` — create via
`Kingdoms of Bharat > UI Style Theme` in the Create menu) once made; every
code-generated menu (Settings/Diplomacy/MissionSelect/Objective) picks it up
automatically with no further code change.

### Shared modal panel frame (9-slice) — 512×512 px
- Prompt: *"...a large ornate modal dialog frame/border, bronze-and-carved-stone
  motif with a subtle mandala or lotus pattern along the top edge, flat dark
  parchment center, square format, richer detail than the small HUD panels since
  this is a full-screen focal element."*

### Shared menu-button background (9-slice, 3 states: Normal/Hover/Pressed) —
128×48 px each
- Prompt: *"...a rectangular button plate matching the modal frame's bronze/stone
  family, landscape format, subtle bevel."*

### Civ-select cards (5 total, 256×256 px each, one per civilization — crest/
emblem style, not full character art)
Use the master style paragraph plus the matching civ accent color below (these are
the exact in-engine tint values from `CivilizationProfile.cs`, so the art will read
consistently with each civ's actual in-game color):

| Civ | Accent color | Prompt subject |
|---|---|---|
| Chola | `#8C3314` (deep maroon/rust) | Dravidian temple gopuram silhouette crest |
| Vijayanagara | `#D9991A` (ochre/gold) | Hampi granite pillar/chariot motif crest |
| Rajput | `#264099` (deep blue) | fort/haveli battlement with a chhatri dome crest |
| Maurya | `#807866` (warm gray/stone) | Ashokan pillar capital (lion or lotus) crest |
| Maratha | `#267333` (forest green) | hill-fort silhouette with a saffron pennant crest |

Prompt template: *"...a circular emblem/crest icon, [SUBJECT], rendered mostly in
[ACCENT COLOR] with bronze/gold linework, square format with the crest centered."*

---

## Tier 4 — lower priority (optional, nice-to-have)

**Where it plugs in**: `Assets/Resources/UI/Panels/` (minimap frame) and
`Assets/Resources/UI/Portraits/` (unit/building portraits — new folder).

### Minimap frame/border (9-slice) — 288×288 px
- Prompt: *"...a square ornate viewport frame/border matching the modal-dialog
  bronze/stone family, thick enough to read as a frame around a small map."*

### Unit/building portraits (optional — one per trainable unit type and per
building type if you want these; text-only labels are fully functional without
them)
- Size: 128×128 px each, transparent or simple background
- Prompt template: *"...a bust/three-quarter portrait icon of [UNIT OR BUILDING
  NAME], painted illustration style matching the rest of this UI set, square
  format."*

---

## Full UI-skin checklist (what "done" looks like)

Use this to track sourcing progress — check off as art lands and gets wired in.

**Tier 1 (ship first — resource bar + command card)**
- [ ] Command-card button background × 4 states
- [ ] 19 action icons (build/train/upgrade/age)
- [ ] 4 resource icons (Wood/Food/Gold/Stone)
- [ ] Resource-bar panel frame

**Tier 2 (HUD frequency)**
- [ ] Selected-unit panel frame
- [ ] HP bar frame + fill (2 sprites)
- [ ] Hover-tooltip panel frame
- [x] 5 cursor states (default/attack-move/invalid/gather/build-placement) — **done
  2026-08-28, via approximate matches, not purpose-made art.** Sourced from an
  imported Asset Store pack ("Basic RPG Cursors", `Assets/Cursors/`), not commissioned
  to this spec — none of its icons are literal crossed-swords/circle-slash/sickle/
  hammer-and-nail. Closest available substitutes used instead: Default = plain arrow,
  Attack-move = single sword, Invalid = red arrow (no prohibition/slash icon exists in
  the pack), Gather = hand-with-coins, Build-placement = axe+hammer. Cropped to
  content and resized to the spec'd 32×32 (Python/Pillow, not in-Editor
  `Texture2D.GetPixels` — see the known Editor-crash gotcha). If a closer-fitting set
  is ever sourced, swap the 5 files at `Assets/Resources/UI/Cursors/` — no code change
  needed.

**Tier 3 (menu screens)**
- [ ] Shared modal panel frame
- [ ] Shared menu-button background × 3 states
- [ ] 5 civ-select crest/emblem cards

**Tier 4 (optional polish)**
- [ ] Minimap frame
- [ ] Unit/building portraits (as many or few as you want — fully optional)

**Total for a complete first pass (Tiers 1+2)**: ~34 discrete image files.
**Total including Tiers 3+4 (excluding optional portraits)**: ~42 discrete image
files.

---

## After art lands — wiring notes for the next session

- Icons: straightforward `Sprite` swap into an `Image` field per button/label —
  additive, existing layout doesn't need to change.
- 9-slice panels/buttons: import as Sprite (2D and UI), set Border in the Sprite
  Editor to match the frame's actual bevel width, assign to the `UIStyleTheme.asset`
  fields (`PanelFrameSprite`/`ButtonBackgroundSprite`) — `UIStyleTheme.ApplyPanel`/
  `ApplyButton` (`Assets/Scripts/UI/UIStyleTheme.cs`) already check for a non-null
  sprite and switch every wired panel/button to `Image.Type.Sliced` automatically,
  no other code change needed.
- Cursors: **done 2026-08-28.** `HoverTooltip.cs` calls `Cursor.SetCursor(texture,
  hotspot, CursorMode.Auto)` for all 5 states, resolved via the pure/testable
  `HoverTooltip.ResolveCursorState` (see `HoverCursorStateTests.cs`) at
  `BuildingPlacer.IsPlacing`, hostile-target hover (only when the selection can
  actually attack — otherwise Invalid), and resource-node hover (only when the
  selection can actually gather — otherwise Invalid).
- Civ-select cards: swap `CivPicker.cs`'s current flat-color `Image` background for
  the crest sprite (add an `Image` for the crest layered over the existing color
  swatch, or replace the swatch's sprite directly).
