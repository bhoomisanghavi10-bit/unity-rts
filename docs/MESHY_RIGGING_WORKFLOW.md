# Turning the item 6 JPEGs into game-ready rigged models, cheaply

Companion to `docs/YOUR_ACTION_ITEMS.md` item 6. Corrected using your actual Meshy pricing
(rigging is free; the cost lives entirely in which model-generation tier you pick per unit).

## Your real pricing

| Tier | Total credits | Texture credits (included) | Notes |
|---|---|---|---|
| **Meshy-7** | 35 | 10 | "High quality" |
| **Meshy-6** | 30 | 10 | "High quality and stable structure" |
| **Meshy T2 (Smart Topology)** | 5 | unclear — confirm before committing a batch | Cheapest by far |
| **Rigging** | 0 | — | Free regardless of which tier generated the mesh |

Since rigging costs nothing, tier selection is the ONLY real lever for total spend across your
~15 units. That changes the earlier advice: rig freely wherever Meshy supports the skeleton type
you need (no cost penalty either way) — the question that actually matters is which generation
tier to run each unit through.

## Recommendation: default to T2 (Smart Topology), reserve Meshy-6 for the few units that benefit

Run the math for all ~15 units at each tier: all-Meshy-7 is 525 credits, all-Meshy-6 is 450, and
all-T2 is 75 (if texture is included in that 5) — a ~6-7x difference. Two things point toward T2
as the right default here, not just the cheapest:

1. **This is a top-down RTS.** The poly-count budgets already set in `docs/YOUR_ACTION_ITEMS.md`
   (1,500-10,000 tris depending on unit type) are low specifically because the camera rarely
   gets close — any extra fidelity Meshy-6/7 adds over T2 gets thrown away in the decimation
   pass anyway (see step 4 below). Paying 6-7x more for detail the camera will never resolve is
   the classic overspend for this kind of project.
2. **"Smart Topology" is a meaningful name, not just a cheaper tier.** It suggests
   deformation-aware, quad-dominant edge flow — which matters more for anything that needs to
   animate (the mounts, and any human figure) than raw surface detail does. A "high quality"
   dense/triangulated mesh from Meshy-6/7 can actually be WORSE to rig and skin than a clean
   Smart Topology mesh, independent of price.

**Before committing all 15 units to T2:** run 1-2 as a pilot first (5-10 credits total — cheap
enough that testing costs nothing) and check two things: (a) does the output actually look
right at your target poly count after decimation, and (b) does its 5-credit price include
texture, or is that a separate add-on you'd need to add in. If the pilot looks good, run the
rest of the roster through T2.

**Where Meshy-6 over T2 might still be worth it:** the 5 Maharaja hero variants specifically —
each is the single most-detailed, most-looked-at figure for its civilization (a UI portrait
candidate, not just a battlefield unit), so the ~25-credit premium per hero (30 vs 5) is a much
smaller bet than paying it across the whole 15-unit roster. Skip Meshy-7 entirely — Meshy-6 is
cheaper and its "stable structure" framing suggests it's the more reliable pick anyway, not just
a downgrade from 7.

Rough total under this plan: 10 non-hero units × 5 credits (T2) + 5 Maharajas × 30 credits
(Meshy-6) = **200 credits**, versus 525 if everything went through Meshy-7 — well under half the
cost, spent where it actually shows (the heroes) rather than spread evenly across units the
camera barely resolves.

## Image-to-3D, not Text-to-3D

You already have the JPEGs, so every conversion should go through Image-to-3D, not Text-to-3D —
feeding it a real reference image is more likely to match what you already approved than having
it reinterpret a prompt from scratch. If you generated more than one angle per unit (front +
side/back), feed all of them into a single Image-to-3D task rather than running separate tasks
per angle and merging afterward — most tools accept multiple reference images per task at no
extra cost, and it gives the model better information to reconstruct the back/sides correctly.

## Rigging — now a correctness question, not a cost question

Since rigging is free, there's no credit reason to avoid it anywhere. But it's still worth
routing correctly, because a wrong rig wastes the modeling spend around it even at zero
additional cost:

- **Skirmisher, Vaidya, Purohita, Maharaja (×5) — still don't use Meshy's auto-rig, for
  compatibility, not cost.** The game already has one shared, working, animated human skeleton
  (`HumanModelFactory.cs`'s "Human Character Dummy," reused by every soldier/worker/unique unit
  today). A fresh auto-rig — free or not — builds a DIFFERENT skeleton, incompatible with every
  walk/attack/idle animation already authored; using it would mean re-animating each unit from
  scratch. Skin the new mesh onto a copy of the *existing* dummy skeleton in Blender instead, so
  it inherits all the existing animations. This is the one place "free rigging" doesn't mean
  "use the rig feature" — it means the feature produces the wrong skeleton for this specific
  project, regardless of price.
- **Scout's horse, Cavalry Archer's horse, Camel Rider's camel, the pack-ox — rig these with
  Meshy freely, if it supports quadruped skeletons.** These are the only 4 units needing a
  genuinely new skeleton (nothing existing to reuse), and since rigging costs nothing, there's no
  reason to do it manually in Blender unless Meshy's auto-rig doesn't handle quadrupeds well.
  Check its output on one of these 4 before assuming it works — some auto-rig tools are tuned
  for bipeds and produce awkward leg chains on a 4-legged animal.
- **Battering Ram, Scorpion, Trebuchet, Fire Ship, Trade Ship — skip rigging entirely.** These
  need at most one or two simple pivot points (a ram swinging on a chain, a trebuchet arm, a
  wheel spinning), which is a single bone or Unity Animation Clip on a hinge — a few minutes in
  Blender, not a character-rig job either way.

## Decimate to the poly budget yourself

Raw Meshy output (at any tier) is typically denser than the poly targets already set in
`docs/YOUR_ACTION_ITEMS.md`'s Pose and poly-count table. Run Blender's built-in Decimate modifier
(free) to bring each mesh down to its target triangle budget before it goes into Unity, rather
than paying for a retopology credit if one's offered — this is also where any accidental extra
geometry from the reconstruction gets cleaned up.

## The order to do all this in

1. Pilot 1-2 units on T2 (Smart Topology) — confirm quality and whether texture is included.
2. Run the other ~13 non-hero units through T2 (or Meshy-6 for the pilot units and any others
   that didn't come out clean on T2).
3. Run the 5 Maharaja variants through Meshy-6.
4. Decimate every output in Blender to its poly budget (free).
5. Rig: skin the 4 solo human units (Skirmisher, Vaidya, Purohita, Maharaja) onto the existing
   dummy skeleton in Blender; auto-rig the 4 mounts/pack-ox with Meshy if it handles quadrupeds
   well, otherwise a manual Blender quadruped rig; give the 5 machines/ships a single pivot bone
   or Animation Clip each in Blender — no character rig for any of them.
6. Hand the finished, rigged, decimated files to a Claude Code session to import — the same
   `BuildingModelFactory`/`HumanModelFactory` pattern described in `docs/BRING_IN_ART_1-4.md`
   applies here too once the files exist.
