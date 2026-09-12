# Bringing sourced art (items 1-4) into the game

Grounded by reading the actual asset-loading code just now (`BuildingModelFactory.cs`,
`HumanModelFactory.cs`, `TownCenterFactory.cs`, `TowerFactory.cs`, `MillFactory.cs`,
`LumberCampFactory.cs`, `MiningCampFactory.cs`), here's what each item actually needs — it's not
the same amount of work for all four.

## The good news: 2 of the 4 items need ZERO code changes

`BuildingModelFactory.cs` was already built for exactly this moment. Every building factory
calls `BuildingModelFactory.Spawn(resourceName, civId, ...)`, which tries, in order:
`Resources/Buildings/{civId}/{resourceName}` → `Resources/Buildings/{resourceName}` →  two
nested Sketchfab-style fallback paths → and only THEN falls back to a procedural placeholder
shape. That means:

- **Item 4 (Lumber Camp, Mining Camp, Mill)** — these are civ-agnostic, one shared model each
  (confirmed: `Mill.cs`/`LumberCamp.cs`/`MiningCamp.cs` are genuinely separate structures from
  Farm, not an overlap — your earlier flag is resolved, all 3 are still needed). Drop each
  model at `Assets/Resources/Buildings/LumberCamp/`, `Assets/Resources/Buildings/MiningCamp/`,
  `Assets/Resources/Buildings/Mill/` (matching whatever nesting your export tool produces — the
  factory already tries both flat and nested paths) and the existing code picks them up on the
  next spawn. No C# changes needed.
- **Item 1 (Villager)** — same mechanism, one level down: `HumanModelFactory.Spawn` already
  supports a `prefabPathOverride` + `applyPaletteMaterial:false` + `ApplyCustomTexture` path
  (built for 3 other custom-painted units already in the game). `WorkerFactory.cs` is the one
  line that needs editing, to pass your new Villager model's path instead of the generic dummy.

## The real work: items 2 and 3 need a genuine code change first

`BuildingModelFactory.Spawn` currently takes **civId only — no age parameter at all.** Every
building always renders the same model regardless of what age the faction is in. That's fine
today (only Imperial-tier art exists), but Ancient/Classical/Durg art for Town Center and
Tower/Wall has nowhere to plug in until the factory itself learns to ask "which age is this
faction in" and pick a different resource path per age. This is a real, if small, feature — not
a drop-in — and it should happen once, correctly, rather than being hacked per-building. The
project already tracks current age per faction in `Progression/AgeProgress.cs`, so the plumbing
this needs already exists one layer up; it just isn't wired into building visuals yet.

## The kickoff prompt

```
Bring the newly-sourced building/villager art into the game. Read
docs/YOUR_ACTION_ITEMS.md items 1-4 for the asset scope and docs/IMPLEMENTATION_ROADMAP.md
for how this fits the wider plan, then work through these in order:

1. AGE-AWARE BUILDING VISUALS (do this first — items 2 and 3 both depend on it).
   Extend BuildingModelFactory.Spawn (Assets/Scripts/Buildings/BuildingModelFactory.cs) to
   accept an AgeId parameter and build the Resources.Load path accordingly. Read
   Progression/AgeProgress.cs first to see how current age is tracked per faction, and use
   that as the source of truth rather than adding a second one. Decide and document a
   resource-path convention for age-tiered buildings, e.g.:
     - Shared (non-civ) age tiers: Resources/Buildings/{resourceName}_{ageId}
       (e.g. Buildings/TownCenter_Ancient, Buildings/Tower_Classical)
     - Civ-specific age tiers: Resources/Buildings/{civId}/{resourceName}_{ageId}
       (e.g. Buildings/Rajput/TownCenter_Durg)
   and make sure the existing Imperial-tier lookup (civId}/{resourceName} with NO age suffix)
   keeps working unchanged, since all 5 civs' Imperial building art already exists at that
   exact path and must not need re-importing. Wire TownCenterFactory, TowerFactory, and
   WallFactory to pass the faction's current AgeId into Spawn. Also decide (Plan Mode, confirm
   with me before implementing): does an EXISTING building on the field re-skin itself the
   moment a faction ages up, or does the new-age look only apply to buildings built after that
   point? Check whether anything already listens for age-up events to model this on.

2. IMPORT THE ART FILES. I'll tell you where the sourced model files are (ask me if you don't
   see them staged anywhere obvious). Import each into Assets/Resources/Buildings/ using the
   path convention from step 1:
     - Town Center: 1 shared Ancient model, 1 shared Classical model, 5 civ-specific Durg
       models (Imperial already exists, don't touch it)
     - Tower: 3 shared tier models (Ancient/Classical/Durg) — Imperial already exists per civ
     - Wall: 3 shared tier models (Ancient/Classical/Durg) — Imperial already exists per civ
     - Lumber Camp, Mining Camp, Mill: 1 shared model each (no age/civ variants)
     - Villager: the new model(s) — tell me whether you sourced one shared model or per-civ
       variants, since that changes whether WorkerFactory needs a civ switch or just one path

3. FIX IMPORT ARTIFACTS, same checklist BuildingModelFactory's own code comments already
   document from the last import pass (Tower's -90° rotation bug, House's off-origin pivot,
   Gate's missing _Color shader property, TownCenter's baked-in leftover components) — expect
   at least one of these categories to recur: wrong rotation, wrong pivot/origin, oversized
   baked-in collider (should be stripped and replaced by AddBoundsCollider), stray
   MonoBehaviours baked into the imported prefab, or a shader color-property name Unity's
   Material.color wrapper doesn't recognize. Verify each new building/unit visually via a Play
   mode screenshot before moving to the next one, the same way the prior import pass did.

4. WorkerFactory.cs: point it at the new Villager model via HumanModelFactory's existing
   prefabPathOverride parameter. If you sourced per-civ variants, decide whether it still needs
   the trim-sheet palette tint (applyPaletteMaterial:true) or ships its own painted civ texture
   (applyPaletteMaterial:false + HumanModelFactory.ApplyCustomTexture, the same pattern already
   used for the 3 custom unique units) — check with me if the sourced asset's material setup
   isn't obvious from the file itself.

5. Test: spawn each building at each age it now has art for (Ancient/Classical/Durg/Imperial
   Town Center, Ancient/Classical/Durg Tower and Wall — Imperial already verified working), and
   the new Villager for at least 2 civs. Confirm existing gameplay is unaffected — same collider
   sizes read correctly for click/select, same rally/attack-range math, same animations playing
   on the Villager (Idle/Walk/Gather via AnimationDriver).

6. Log this session's work to docs/SESSION_LOG.md per the usual convention, noting the chosen
   resource-path convention explicitly so future sessions (Tower/Wall Imperial civ art
   variants, item 5's gear, item 6's new units) follow the same pattern rather than inventing
   another one.

Use Plan Mode before step 1's code change — the age-up re-skin behavior decision matters and
shouldn't be guessed.
```

One practical note before running this: **where are the sourced files right now?** If they're
on your computer outside the connected `unity-rts` folder, either move them into a subfolder
inside the project first (e.g. `unity-rts/_IncomingArt/`) so the session's tools can reach them,
or attach them to the chat — the kickoff prompt above assumes they're locatable, but doesn't
assume a specific path since I don't know where you saved your Canva/Meshy exports.
