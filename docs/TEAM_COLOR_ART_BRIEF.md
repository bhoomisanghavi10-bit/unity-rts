# Team Color Art Brief — Kingdoms of Bharat

Companion to `docs/IMPLEMENTATION_ROADMAP.md` Wave 5 item 29 (Player/team colour system,
reopened 2026-09-12). This is a sourcing brief for you (or an artist/AI tool) — Claude Code
doesn't create game art per the project's asset-sourcing rule; this file exists so the exact
technical shape of what's needed is written down once, and future sessions can wire in whatever
you produce without re-deriving the spec.

**Note on the reference images**: the 3 screenshots you supplied are real, copyrighted AoE II:
Definitive Edition art (Microsoft/World's Edge) — same reasoning `UI_ART_BRIEF.md` already
applies to its own AoE-inspired sections. Don't ask an AI tool to reproduce those images
directly; the brief below describes the *mechanism* (a paintable team-color mask region) and
Kingdoms of Bharat's own established motif language, not a copy of AoE2's specific artwork.

---

## Why this exists (read before sourcing anything)

Investigated 2026-09-12: both units and buildings currently render from **exactly one flat-color
material for the whole model**.

- **Units**: `HumanModelFactory.ApplyPaletteMaterial` assigns one shared material (a texture-
  offset swap on a shared trim-sheet) to every renderer on the body — this is how civ identity
  already works (Chola=red mannequin, Rajput=blue, etc.). There's no separate cloth/tunic region
  distinct from skin/armor to recolor independently.
- **Buildings**: confirmed live (Chola TownCenter/Tower/Barracks each have exactly 1 `Renderer`
  and 1 `Material`) and in `Assets/Editor/MeshyBuildingImporter.cs` — every imported model's
  renderer material slots are force-overwritten down to a single shared material at import time,
  regardless of the source FBX's own material count.

Because of this, team color can't be isolated to "just the trim" or "just the tunic" with a
code change alone — there's no existing region to target. The fix is a **mask texture + shader**
approach (below), which needs new art per model. The current small pennant/banner system
(`Core/TeamColorAccent.cs`) stays running as the fallback until mask art exists — this brief
describes additive future work, not a replacement to build blind.

---

## The technical shape (what makes this wireable in code later)

For any given model (a unit body or a building):

1. **Keep the existing albedo/base texture unchanged.**
2. **Add one new mask texture**, same resolution and UV layout as the base texture: white/light
   where team color should show through (dome trim, roof edge, banner cloth, parapet ornament
   for buildings; tunic, cloth wrap, saddle blanket for units), black/dark everywhere else
   (stone, skin, metal, wood — anything that should stay its natural material color).
3. **A small shader** (URP Shader Graph, or a hand-written URP-compatible shader) samples both
   textures and blends: `finalColor = lerp(baseAlbedo, teamColor, maskValue)`, where `teamColor`
   is a per-instance property set at spawn time from `Core/TeamColor.For(faction)` (already
   exists, already returns Player=blue/Enemy=red/Enemy2=green).
4. **For buildings specifically**, once mask textures exist, `MeshyBuildingImporter.cs` will
   also need to stop collapsing every renderer down to one material/texture set — it currently
   throws away any per-submesh distinction the source FBX might have had. This is a real
   code-side follow-up for whoever picks this back up, not something to build speculatively now
   before the art exists to test it against.

This mirrors the general pattern the project's UI art already uses for masked/9-slice assets —
a base image plus a role-specific overlay — just applied to 3D materials instead of UI sprites.

---

## Sourcing guidance

Producing a mask is realistically two options:

- **AI-assisted**: feed a model's existing baked albedo texture into an AI image tool (the same
  Canva/AI workflow already used for the UI art deliveries) and ask it to generate a black/white
  region mask isolating "cloth/fabric" or "roofline/banner/trim" areas from "stone/metal/skin."
  Quality will vary — check the result against the actual UV layout before treating it as final.
- **Hand-painted**: open the existing texture in any image editor (GIMP, Photoshop, Krita) and
  paint a new grayscale layer at the same resolution, using the albedo as a guide for where
  trim/cloth regions actually sit in UV space.

**Recommend piloting on one unit + one building first** (Soldier body + TownCenter, the two
most-viewed models) before committing to the full roster below — this is a substantial asset
lift (roughly 20 unit-body variants + 45 civ-building combinations), and validating the
shader/mask pipeline on 2 assets is far cheaper than discovering a problem after 65.

---

## Per-asset checklist

Mark an item done once its mask texture exists at the matching path. Nothing here is wired in
yet — wiring is separate future code work once art starts landing.

### Units (one mask per body/model, shared across civs — civ identity stays on the existing
palette-material swap; team color is a second, independent channel)

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

### Buildings (per civ — 9 buildings × 5 civs = 45 combos; TownCenter and Tower are the
highest-visibility pick if piloting a subset)

- [ ] Chola: TownCenter, Barracks, Tower, Market, Farm, House, Wall, Gate, Dock
- [ ] Vijayanagara: TownCenter, Barracks, Tower, Market, Farm, House, Wall, Gate, Dock
- [ ] Rajput: TownCenter, Barracks, Tower, Market, Farm, House, Wall, Gate, Dock
- [ ] Maurya: TownCenter, Barracks, Tower, Market, Farm, House, Wall, Gate, Dock
- [ ] Maratha: TownCenter, Barracks, Tower, Market, Farm, House, Wall, Gate, Dock
- [ ] Durg, Karmashala, Monastery, Lumber Camp, Mining Camp, Mill (non-civ-specific — shared
      procedural/shared-model buildings, lower priority since several still use the generic
      procedural fallback shape anyway)

---

## What stays as-is until this lands

`Core/TeamColor.cs` (the Player/Enemy/Enemy2 → Color mapping) and `Core/TeamColorAccent.cs`
(the current banner/pennant geometry) are unaffected by this brief and keep running exactly as
they do today. Nothing needs to be removed or disabled while mask art is sourced.
