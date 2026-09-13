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

## Ornate HUD reskin (top + bottom bars) — 2026-09-07 request

The user asked for the top resource bar and bottom command/info/minimap bar to read
like AoE II: Definitive Edition's own HUD — but that reference is Microsoft/World's
Edge's actual shipped, copyrighted UI art, so it can't be copied or lifted directly
(same reasoning `ICON_PLAN.md` already used to reject the aoe2-icon-resources repo).
This section specs an **original** frame set that matches the reference's *silhouette
and construction* (arched/scalloped carved-wood top edge, twisted-rope border
molding, hanging corner ornaments, a diamond-shaped minimap viewport) while staying
in Kingdoms of Bharat's own established motif language (lotus/mandala/temple carving,
warm ochre-and-bronze palette) instead of AoE2's Western-medieval oak-and-iron
styling. Replaces the plainer Tier 1/2 panel frames below wherever both exist — the
plain frames stay as a fallback until this lands, same "swap the file, no code
change" convention as the rest of this doc.

> Reuse the master style paragraph above, plus: *"...a carved-wood-and-bronze HUD
> panel frame with an arched, gently scalloped top edge (not a plain rectangle) —
> a twisted-rope molding runs along the outer border, and a small hanging leaf/lotus
> ornament drapes from each top corner. Indian temple-carving detail, not European
> medieval oak. Flat readable center for UI content."*

### Top resource bar frame (9-slice) — 1024×80 px
- Plugs in at `Assets/Resources/UI/Panels/panel_resource_bar.png` (same path the
  current plain frame uses — `ResourceHUD.cs` already loads this exact key for both
  its own root background and the `MatchStatus` panel's `matchStatusBackground`, no
  code change needed once the file is replaced).
- Prompt: the shared paragraph above + *"...a single wide horizontal strip spanning
  the top edge of the screen, arched/scalloped along the BOTTOM edge only (the top
  edge is off-screen), symmetric hanging corner ornaments on both ends, landscape
  format."*

### Bottom-left info panel frame (9-slice) — 384×192 px
- Plugs in at `Assets/Resources/UI/Panels/panel_selected_unit.png`
  (`SelectedUnitPanel.cs`'s existing load key).
- Prompt: the shared paragraph + *"...a portrait-adjacent info panel frame, arched
  top-left and top-right corners, a small circular portrait-frame notch on the left
  edge, landscape format, matches the resource bar's border style."*

### Bottom-center command panel frame (9-slice) — 512×256 px
- Plugs in at `Assets/Resources/UI/Icons/CommandCardButton/normal.png` (+
  `hover`/`pressed`/`disabled` — same 4-state set `BuildMenu.cs` already loads) for
  the individual grid-cell buttons, and a NEW wide frame behind the whole grid if
  one doesn't already exist in-scene (check `BuildMenu`'s root panel background
  first — it may only need the button-state art refreshed, not a new panel).
- Prompt: the shared paragraph + *"...a grid-cell button plate with a beveled bronze
  border and a small corner motif, square format, subtle state variants (Normal
  bright, Hover glowing, Pressed recessed, Disabled desaturated)."*

### Bottom-right minimap frame (NEW — diamond viewport) — 320×320 px
- No plug-in exists yet (`MinimapController.cs` currently renders the map through a
  plain `RawImage` with no frame at all — this is a genuinely new addition, not a
  reskin of an existing asset). Once sourced, drop at
  `Assets/Resources/UI/Panels/panel_minimap_frame.png` and flag it back for wiring —
  needs one small `MinimapController.cs` change (a new `Image` sibling layered over
  the existing `display` `RawImage`, matching how `panel_resource_bar` is already
  layered under `ResourceHUD`'s own content).
- Prompt: the shared paragraph + *"...a diamond-shaped (rotated square) ornate
  viewport frame for a minimap, thick carved border with a small eye or lotus emblem
  centered on the top point, two small circular icon-slot cutouts flanking the
  bottom point, square canvas with the diamond centered and transparent corners."*

### Corner mask ornaments (2, optional) — 96×96 px each
- The reference's bottom-left and bottom-right outer corners each carry a small
  carved wooden mask/figure hanging off the panel edge, outside the main frame.
  Optional flourish, not required for the reskin to read as complete — matches
  `docs/ICON_PLAN.md`'s own "hold lower-priority polish" precedent.
- Prompt: the shared paragraph + *"...a small hanging corner ornament, a stylized
  carved wooden face or lotus-bud finial, meant to overlay just outside a HUD
  panel's outer corner, transparent background, no other subject."*

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
- Size: 128×128 px each, transparent or simple background.
- Prompt template: *"...a bust/three-quarter portrait icon of [SUBJECT], painted
  illustration style matching the rest of this UI set, square format."*
- Pick and choose freely — do as many or as few as you want. Recommended order
  if prioritizing: generic units first (seen by every civ, highest screen time),
  then buildings, then civ-exclusive units last (rarer on screen, lowest payoff
  per image). Subject list below is derived directly from
  `Assets/Design/Data/unit_roster_template.csv` and `BuildingPlacer.BuildingKind`,
  not guessed — every name matches the actual in-game unit/building exactly.

**Generic units (21) — highest priority, one roster shared by every civ**
1. Worker — a villager figure carrying a simple farming tool
2. Soldier — a sword-and-shield infantryman
3. Spearman — an infantryman gripping a raised spear
4. Archer — a bowman drawing an arrow
5. Cavalry — a mounted warrior with a raised sword
6. Siege (Mangonel) — a wheeled catapult/siege engine with its crew
7. Fishing Boat — a small wooden fishing vessel
8. War Galley — an armed war galley with oars and a single sail
9. Chara (Scout) — a light unarmed rider on a fast horse, no weapon drawn
10. Skirmisher — a lightly-armored archer with a small buckler shield
11. Battering Ram — a wheeled ram with a reinforced peaked roof
12. Cavalry Archer — a mounted archer firing a bow from horseback
13. Camel Rider — a warrior mounted on a camel, spear in hand
14. Scorpion — a torsion-powered bolt-throwing siege engine
15. Fire Ship — a small naval vessel with a burning hull/prow
16. Vanik (Trader) — a merchant figure with a laden pack-cart
17. Trade Ship — a merchant cargo vessel, no weapons
18. Vaidya (Healer) — a robed healer figure holding a medicine pouch
19. Purohita (Converter) — a robed priest figure with a ceremonial staff
20. Maharaja (Hero) — an ornately armored royal warrior mounted on horseback
21. Trebuchet — a large counterweight siege engine

**Buildings (15)**
1. Town Center — a grand fortified central hall with domes/towers
2. Barracks — a fortified courtyard building with a training yard
3. Farm — a plowed, tilled field with a small wooden shelter
4. House — a modest thatched-roof dwelling
5. Wall — a stone rampart segment with crenellations
6. Gate — an arched, reinforced fortified gateway
7. Tower — a tall stone watchtower
8. Market — an open bazaar stall with awnings and stacked goods
9. Dock — a wooden pier with moored boats
10. Lumber Camp — a timber yard with stacked logs and a sawhorse
11. Mining Camp — a quarry outpost with pickaxes and an ore cart
12. Mill — a grain mill with a large grinding wheel
13. Durg — a hill-fort citadel, the strongest defensive structure
14. Karmashala — a stone workshop/forge for weapon and armor upgrades
15. Monastery — a domed shrine/temple complex

**Civ-exclusive unique units (10) — lowest priority, rarest on screen**
1. Chola Naval Raider — an elite bow-armed raider in Dravidian temple-carved
   armor
2. Chola Catamaran — a swift twin-hulled outrigger boat
3. Vijayanagara War Elephant — an armored war elephant carrying a howdah
4. Hampi Temple Guard — a heavily armored infantry guard with a temple-carved
   shield
5. Rajput Royal Guard — an elite armored cavalryman couching a lance
6. Rajputani Archer — a mounted archer in Rajput regalia
7. Maurya War Elephant — the heaviest war elephant, gilded ceremonial harness
8. Pillar Edict Scholar — a robed Mauryan administrator/scribe figure, no
   weapon
9. Maratha Mavla Raider — a light guerrilla cavalry raider with a saffron
   sash
10. Maratha Durg Garrison — a fortress-defense infantryman braced behind a
    shield

---

## Full UI-skin checklist (what "done" looks like)

Use this to track sourcing progress — check off as art lands and gets wired in.

**Tier 1 (ship first — resource bar + command card)**
- [x] Command-card button background × 4 states — **done 2026-09-12.** User supplied
  a self-consistent Canva delivery (ornate carved-bronze frame, 4 states) at
  `/Users/bhoome/Downloads/tier 1/`, replacing the Ornate HUD reskin session's
  own command-card frame per the user's explicit choice to overwrite it.
- [x] 19 action icons (build/train/upgrade/age) — **done 2026-09-12**, same delivery.
- [x] 4 resource icons (Wood/Food/Gold/Stone) — **done 2026-09-12**, same delivery.
- [x] Resource-bar panel frame — **done 2026-09-12**, same delivery, also replacing
  the Ornate HUD reskin's own resource-bar frame per the user's choice.

**Tier 2 (HUD frequency)**
- [x] Selected-unit panel frame — **done 2026-09-12.** User supplied a real Canva
  delivery at `/Users/bhoome/Downloads/tier 2/` matching this spec exactly (a real
  circular portrait notch on the left edge, per the brief). Wired at
  `Assets/Resources/UI/Panels/panel_selected_unit.png`. Uses `Image.Type.Simple`, not
  `Sliced` — see `SelectedUnitPanel.cs`'s own comment: the notch sits in the
  vertical-middle of the left edge, in 9-slice's stretchable middle band rather than a
  non-stretching corner, so `Sliced` squished it into a thin sliver at this panel's
  fixed 220×70 display size (live-verified via UnityMCP before switching).
- [x] HP bar frame + fill (2 sprites) — **done 2026-09-12**, same delivery. Wired at
  `Assets/Resources/UI/Panels/hp_bar_frame.png`/`hp_bar_fill.png`, `Sliced`/`Filled`
  respectively, `pixelsPerUnitMultiplier` freshly measured against the new art's real
  dimensions (not reused from the old placeholder).
- [x] Hover-tooltip panel frame — **done 2026-09-12**, same delivery. Wired at
  `Assets/Resources/UI/Panels/panel_tooltip.png`.
- [x] 5+ cursor states (default/attack-move/invalid/gather×4/build-placement) —
  **re-done 2026-09-12 with real purpose-made art, superseding the 2026-08-28
  approximate-match note below.** Same Tier 2 delivery included genuine crossed-swords/
  circle-slash/hammer-and-nail/pointing-hand icons matching this spec exactly, plus a
  bonus expansion: 4 separate per-resource gather cursors (axe/sickle/pickaxe+gem/
  pickaxe+hammer for Wood/Food/Gold/Stone) instead of one generic sickle icon.
  `HoverTooltip.cs`'s `HoverCursorState` enum and `ResolveCursorState` were extended
  (Gather → GatherWood/Food/Gold/Stone) to pick the cursor by the hovered
  `ResourceNode.ResourceType`, live-verified via reflection against all 4 resource
  types. All 8 files were RGB with no real alpha (same baked-checkerboard defect as
  the Tier 1 delivery) — fixed via `Tools/ui_art_alpha_key.py`, then cropped/padded/
  resized to 32×32. Old single `gather.png` removed (superseded, unreferenced).
  Original 2026-08-28 note, kept for history: sourced from an imported Asset Store
  pack ("Basic RPG Cursors", `Assets/Cursors/`), not commissioned to this spec — none
  of its icons were literal crossed-swords/circle-slash/sickle/hammer-and-nail.
  Closest available substitutes were used instead: Default = plain arrow, Attack-move
  = single sword, Invalid = red arrow, Gather = hand-with-coins, Build-placement =
  axe+hammer.

**Text/ornament overlap, flagged 2026-09-12, fixed same day (follow-up session)**:
the new Selected-unit-panel/Hover-tooltip frames have a much taller decorative top
AND bottom ornament band than the old placeholder art (measured precisely via
pixel-centerline sampling, not eyeballed: ~32%/25% of height for the selected-unit
panel, ~36%/32% for the tooltip — bigger than initially guessed), so the original
label Y-positions (tuned for the old thin-bordered art) put the top text row (unit
name) under the crown ornament in both panels. Fixed by growing both panels'
height (`SelectedUnitPanel` 70→110, `HoverTooltip`'s `Panel` 56→105 — pure scene
data, `RectTransform.sizeDelta`) and repositioning all label rows to sit inside the
now-larger flat zone; `MatchStatus`/`InfoPanel` (stacked above `SelectedUnitPanel`
in the shared bottom-docked bar) shifted/grew by the same delta to absorb the
extra height with zero ripple elsewhere — `HoverTooltip`'s own panel needed no
sibling adjustment at all, since it's a self-contained floating tooltip positioned
at the cursor every frame, not part of any static layout. Live-verified via
UnityMCP: all 3 text rows in both panels now render fully clear of both ornaments
with no overlap (screenshotted before/after). No code changes needed — pure
scene-data change, 510/510 EditMode tests pass unmodified.

**Tier 3 (menu screens) — done 2026-09-13**
- [x] Shared modal panel frame
- [x] Shared menu-button background × 3 states
- [x] 5 civ-select crest/emblem cards

**Tier 4 (optional polish)**
- [x] Minimap frame — **already done, reconciled 2026-09-13.** Shipped under the
  "Ornate HUD reskin" section above (2026-09-12, commit `db9b3b4`), before this Tier
  4 write-up existed as a separate checklist — the box here was just never checked
  off. `panel_minimap_frame.png`/`panel_minimap_frame_border.png` are wired via
  `MinimapController.ApplyDiamondFrame()`
  (`Assets/Scripts/Camera/MinimapController.cs:106`); confirmed both files present and
  the wiring intact as of this session. No further action needed.
- [ ] Unit/building portraits (as many or few as you want — fully optional) — **the
  only item left open in this entire doc.** No art has been sourced yet (no
  `Assets/Resources/UI/Portraits/` folder exists) — needs Canva/commissioned images
  per the prompt template above before any wiring session can start.

**Total for a complete first pass (Tiers 1+2)**: ~34 discrete image files.
**Total including Tiers 3+4 (excluding optional portraits)**: ~42 discrete image
files.

---

## After art lands — wiring notes for the next session

- Icons: straightforward `Sprite` swap into an `Image` field per button/label —
  additive, existing layout doesn't need to change.
- 9-slice panels/buttons: import as Sprite (2D and UI), set **Mesh Type to Full
  Rect** (never Tight — a Tight/alpha-hugging mesh breaks `Image.Type.Sliced`'s
  9-slice math outright, discovered the hard way wiring Tier 3's modal frame: it
  silently produced a warped, crowded render with no flat interior, at every
  `pixelsPerUnitMultiplier` value tried, until the mesh type itself was fixed),
  set Border in the Sprite Editor to match the frame's actual bevel width, assign
  to the `UIStyleTheme.asset` fields (`PanelFrameSprite`/`ButtonBackgroundSprite`)
  — `UIStyleTheme.ApplyPanel`/`ApplyButton` (`Assets/Scripts/UI/UIStyleTheme.cs`)
  already check for a non-null sprite and switch every wired panel/button to
  `Image.Type.Sliced` automatically, no other code change needed.
  `ApplyPanel` (as of the Tier 3 wiring session) also auto-computes
  `pixelsPerUnitMultiplier` from the *panel's own RectTransform height* vs. the
  frame texture's native height, so the border renders at a consistent
  proportion regardless of how differently sized any given caller's panel is —
  every `ApplyPanel` call site must set the Image's RectTransform to its final
  anchors/sizeDelta *before* calling `ApplyPanel`, not after (the order every one
  of the 5 call sites had it in before this fix, which is exactly why the border
  rendered wrong).
- Cursors: **done 2026-08-28.** `HoverTooltip.cs` calls `Cursor.SetCursor(texture,
  hotspot, CursorMode.Auto)` for all 5 states, resolved via the pure/testable
  `HoverTooltip.ResolveCursorState` (see `HoverCursorStateTests.cs`) at
  `BuildingPlacer.IsPlacing`, hostile-target hover (only when the selection can
  actually attack — otherwise Invalid), and resource-node hover (only when the
  selection can actually gather — otherwise Invalid).
- Civ-select cards: swap `CivPicker.cs`'s current flat-color `Image` background for
  the crest sprite (add an `Image` for the crest layered over the existing color
  swatch, or replace the swatch's sprite directly).
