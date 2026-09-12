# Team Color Art Brief — Kingdoms of Bharat

Companion to `docs/IMPLEMENTATION_ROADMAP.md` Wave 5 item 29 (Player/team colour system,
reopened 2026-09-12). This is a sourcing brief for you (or an artist/AI tool) — Claude Code
doesn't create game art per the project's asset-sourcing rule; this file exists so the exact
technical shape of what's needed is written down once, and future sessions can wire in whatever
you produce without re-deriving the spec.

**Buildings are done — no new art needed for them.** See "Buildings — shipped" below. Only the
**units** section below still needs new art from you.

**Note on the reference images**: the 3 screenshots you supplied are real, copyrighted AoE II:
Definitive Edition art (Microsoft/World's Edge) — same reasoning `UI_ART_BRIEF.md` already
applies to its own AoE-inspired sections. Don't ask an AI tool to reproduce those images
directly; the brief below describes the *mechanism* (a paintable team-color mask region) and
Kingdoms of Bharat's own established motif language, not a copy of AoE2's specific artwork.

---

## Why this exists (read before sourcing anything)

Investigated 2026-09-12: both units and buildings initially looked like they render from
**exactly one flat-color material for the whole model**, with no way to isolate "trim" from
"stone"/"skin" for a code-only fix. That held up for units, but turned out to be only half the
story for buildings — see "Buildings — shipped" below.

- **Units**: `HumanModelFactory.ApplyPaletteMaterial` assigns one shared material (a texture-
  offset swap on a shared trim-sheet) to every renderer on the body — this is how civ identity
  already works (Chola=red mannequin, Rajput=blue, etc.). There's no separate cloth/tunic region
  distinct from skin/armor to recolor independently, and the underlying texture
  (`HumanCharacterDummy_ColorPalette.png`) is a flat 128×128 color swatch with zero shape detail
  at all — there's nothing to build a mask against even in principle.
- **Buildings**: `BuildingModelFactory` confirmed live (Chola TownCenter/Tower/Barracks each
  have exactly 1 `Renderer` and 1 `Material`) and `Assets/Editor/MeshyBuildingImporter.cs` force-
  overwrites every renderer's material slots down to a single shared material at import time.
  **However**, each of those single materials already carries a real metallic/smoothness map
  (standard PBR output from the Meshy pipeline) that separates gilded/metal trim from plain
  stone at the pixel level — see below. That's enough to drive team color without any new art.

The current small pennant/banner system (`Core/TeamColorAccent.cs`) stays running unchanged on
both sides — it's the only team indicator for buildings that lack a metallic map (several
non-civ-specific buildings still use the generic procedural fallback shape) and the primary one
for units until new art lands.

---

## Buildings — shipped (2026-09-12), no new art needed

Every `MeshyBuildingImporter`-sourced building ships a `_metallicSmoothness.png` alongside its
albedo texture, at the exact same UV layout. Opening both side by side (not just checking
renderer/material counts) showed the metallic channel is already bright exactly where the
albedo shows gilded/metal decorative elements (finials, banding, ornament) and dark everywhere
else (plain stone) — a ready-made mask that just happened to already exist.

Implemented in `Core/TeamColorBuildingTint.cs` + `Assets/Resources/Shaders/
TeamColorTrimBlit.shader`: a small offscreen blit shader bakes
`lerp(albedo, teamColor, metallicMask)` into a cached `RenderTexture` (one per unique
albedo+metallic+faction combination, reused across every instance of that combo in a match,
capped at 1024×1024) and applies it via a `MaterialPropertyBlock` on the building's renderer —
no material duplication, no change to the existing civ-color tint (`BuildingModelFactory.
TintMaterials`, which multiplies `Material.color` — a separate channel from whatever texture is
sampled, so both effects compose correctly). Wired into `BuildingModelFactory.BuildVisual` right
after the existing civ tint call; gracefully no-ops for any building with no metallic map
(procedural fallbacks). Cache is released and cleared in `CivilizationSetup.BeginMatch` via
`TeamColorBuildingTint.Reset()`, alongside the existing `DiplomacyRegistry.Reset()`.

Live-verified via UnityMCP: 3 real Chola TownCenters spawned for Player/Enemy/Enemy2 all show
the same gilded/ornament regions tinted to that faction's exact `TeamColor` (blue/red/green)
while the plain stone is untouched, confirmed by direct screenshot comparison.

**Full-roster visual pass done (2026-09-12), same day**: spawned all 45 civ/building
combinations (`BuildingModelFactory.Spawn` directly, forcing every one onto its own real civ, 0
exceptions) and screenshotted representative samples across all 5 civs. Result: real, genuine
per-civ variance, driven entirely by how much metallic data each civ's source art actually
contains — not a bug in the mechanism. Measured pixel data (mean brightness / % bright pixels)
on the metallic maps directly rather than trusting a screenshot read:

| Building | Metallic map mean | % bright pixels |
|---|---|---|
| Chola TownCenter | 9.2 | 1.72% |
| Maurya TownCenter | 1.4 | 0.00% (but concentrated exactly on the gilded dome) |
| Vijayanagara TownCenter | 0.0 | 0.00% |
| Vijayanagara Tower | 0.5 | 0.02% |
| Vijayanagara Barracks | 0.0 | 0.00% |
| Maurya Tower | 0.0 | 0.00% |
| Maurya Barracks | 0.9 | 0.00% |

Chola's buildings (moderate, real metallic content) get clean, localized ornament highlighting.
Maurya's TownCenter dome — despite an almost-zero average — gets a strong, correct recolor
because its handful of bright pixels are concentrated exactly on the gilded dome (a genuinely
metallic surface), so that specific region recolors fully while the rest of the building is
untouched — dramatic, but working as intended. **A first look at Vijayanagara's TownCenter
looked "over-tinted" (a broad pink wash) and was initially misdiagnosed as such** — direct
pixel/texture inspection showed that's wrong: its metallic map is essentially all-zero, so this
mechanism does virtually nothing there; the pink is baked directly into the existing albedo
texture and entirely unrelated to team color. The real, corrected finding: **Vijayanagara's
whole building set and Maurya's Tower/Barracks have essentially no usable metallic signal at
all** — not a bug, a content gap in the source art. Per the user's explicit choice, this is
accepted as-is rather than "fixed" with a shader-side clamp (which would only suppress signal
further, not add signal that isn't there) — these buildings simply fall back to the pennant
alone for team color, exactly as they did before this feature existed. Fixing it for real would
need new metallic-map art for those civs/buildings, tracked here as a known content gap, not a
code task.

---

## Units — still needs new art (Blender, not Canva)

**Correction from the first pass of this brief**: a Canva/AI-image-tool prompt cannot produce a
usable mask here, for a reason beyond "there's no separate cloth region" — the actual texture
files (checked directly, not assumed) are **UV atlases**: the model's surface is chopped into
fragments and scattered arbitrarily across the 2D image, with no spatial coherence at all. You
cannot look at the flat texture and tell which scattered fragment corresponds to a sleeve versus
a boot — no human or text-to-image tool can align a mask to it from the 2D image alone.

**The correct tool is Blender** (free) or an equivalent 3D texture-paint tool (Substance
Painter), because it lets you paint directly on the *visible 3D model in a viewport* — you paint
where you can see "this is the tunic," and the software automatically writes that paint into the
correct position in the underlying UV-mapped texture for you. No manual UV alignment needed.

**Workflow**:
1. Import the unit's source FBX into Blender (e.g. `Assets/Resources/human/Human Character
   Dummy/Models/HumanCharacterDummy_M.fbx`, or one of the 4 unique-unit models under
   `Assets/Resources/UniqueUnits/*/` — see the checklist below for which model backs which unit).
2. Switch to Texture Paint mode, create a new image at the same resolution as the model's
   existing albedo texture (128×128 for the generic body is too coarse to be useful — consider
   authoring a proper detailed body texture as part of this, not just a mask, since the current
   one has no real surface detail to begin with; 2048×2048 for the 4 unique units matches their
   existing albedo resolution).
3. Paint **white** over any region that should take team color (tunic, cloth wrap, sash, saddle
   blanket), **black** everywhere else (skin, metal, wood, leather).
4. Export the painted image as a new PNG at the paths noted in the checklist below.

Once a mask exists for a given unit, wiring it in follows the same pattern already shipped for
buildings (`Core/TeamColorBuildingTint.cs` is a template, not unit-specific — a unit-side
equivalent reading the new mask the same way is a small, well-understood follow-up once real
mask art exists to test it against).

**Recommend piloting on one of the 4 unique units first** (they have real 2048×2048 painted
textures already, unlike the generic body) rather than the full roster below.

---

## Per-asset checklist

### Units (still needed — see workflow above)

- [ ] Worker (female body)
- [ ] Worker (male body)
- [ ] Soldier / Spearman / Archer / Cavalry / Siege / Skirmisher / Battering Ram / Cavalry
      Archer / Camel Rider / Scorpion / Trebuchet / Scout (shared Human Character Dummy body —
      one mask likely covers all of these)
- [ ] Vaidya / Purohita (shared Human Character Dummy body — same mask as above likely applies)
- [ ] Maharaja (shared Human Character Dummy + Horse combo)
- [ ] Chola Naval Raider (unique unit, own model)
- [ ] Rajput Royal Guard (unique unit, own model)
- [ ] Maurya Pillar Edict Scholar (unique unit, own model)
- [ ] Maratha Mavla Raider (unique unit, own model)
- [ ] Maratha Durg Garrison (unique unit, own model)
- [ ] War Galley / Fishing Boat / Fire Ship (naval hull)
- [ ] Maurya War Elephant / Vijayanagara War Elephant (own rig)

### Buildings — no action needed (code-only fix shipped and visually passed 2026-09-12, see
"Buildings — shipped" above)

Every Meshy-imported civ building (9 buildings × 5 civs = 45 combos) automatically gets the
metallic-trim team tint the moment it's spawned with a `faction` — nothing to source. Full
45-combo visual pass done 2026-09-12: mechanism runs cleanly everywhere (0 exceptions across all
45), with real visual quality varying by how much metallic content each civ's own source art
happens to contain — see the pixel-measured table above. No further action needed on the code
side; the only way to improve the under-tinted civs/buildings below is new metallic-map art
(a content gap, not a bug):

- [x] All 45 civ/building combinations spawned, screenshotted, and reviewed (2026-09-12)
- [x] Chola: clean, localized ornament highlighting confirmed (real metallic content)
- [x] Maurya: TownCenter dome recolors correctly (its one real metallic region); Tower/Barracks
      have effectively no metallic signal — tint invisible there, pennant is the only indicator
- [x] Vijayanagara: effectively no metallic signal anywhere in its building set — tint invisible,
      pennant is the only indicator (**not** a bug — its metallic maps measured all-zero; an
      unrelated pink patch baked into its TownCenter's own albedo was initially misread as
      over-tinting and corrected after direct pixel inspection)
- [ ] Rajput / Maratha: spawned and screenshotted in the group pass, not individually
      pixel-measured — worth a closer look if either reads oddly in a real match, but nothing
      flagged so far

Buildings with **no** metallic map get no benefit and keep relying on the pennant alone:
- [ ] Durg, Karmashala, Monastery, Lumber Camp, Mining Camp, Mill (non-civ-specific — shared
      procedural/shared-model buildings, several still use the generic procedural fallback shape
      with no PBR maps at all)

---

## What stays as-is until this lands

`Core/TeamColor.cs` (the Player/Enemy/Enemy2 → Color mapping) and `Core/TeamColorAccent.cs`
(the current banner/pennant geometry) are unaffected by this brief and keep running exactly as
they do today. Nothing needs to be removed or disabled while mask art is sourced.
