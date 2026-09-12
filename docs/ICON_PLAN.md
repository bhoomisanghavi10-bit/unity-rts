# Icon Asset Plan — Kingdoms of Bharat

A separate plan from `docs/YOUR_ACTION_ITEMS.md` (which covers 3D models/gear) and
`docs/IMPLEMENTATION_ROADMAP.md`/`docs/KingdomsOfBharat_Model_Spec.xlsx` (which cover code and
mechanics). This one is just for UI icons — the small square/circular badges used in the build
menu, unit portraits, tech tree, and HUD.

## Why not just use the aoe2-icon-resources repo directly

That repo (https://github.com/qwyt/aoe2-icon-resources) states its icons were "scrapped from"
the AoE II Fandom wiki — meaning they're extracted from Age of Empires II: Definitive Edition's
actual game files, copyrighted by Microsoft/World's Edge. No license is stated anywhere in the
repo, so there's no permission to reuse them, and the repo's own author has no rights to grant
even if they claimed to. Dropping these PNGs into Kingdoms of Bharat would mean shipping
Microsoft's copyrighted icon art as your own game's assets — not something to build the icon
system on.

What the repo IS useful for: its **category structure** (civilizations / objects / technologies
/ terrain / maps, following the `aoc-reference-data` naming convention) is a solid checklist of
*what kinds of icons a game like this needs*, which is what the breakdown below is built from —
adapted to Kingdoms of Bharat's actual roster, not copied file-for-file.

## Icon style anchor

Distinct from the 3D-model style anchor in `YOUR_ACTION_ITEMS.md` — icons are flat 2D, read at
~64px, and need one consistent frame/background so the whole set looks like one system:

> *"Age of Empires II: Definitive Edition-style RTS game UI icon — flat 2D illustration (not a
> 3D render), bold clear silhouette readable at small size, warm parchment-and-bronze icon frame
> (a subtle beveled circular or hexagonal badge border), muted earthy color palette with one
> accent color per subject, centered composition with generous padding inside the frame,
> medieval Indian subcontinent visual motifs, no background scene or clutter outside the badge
> frame, single flat icon on a transparent or plain background, square canvas."*

Use Canva's AI image generator with this anchor for every icon below; a 2D icon needs no Meshy
step at all (no 3D conversion, no rigging) — these go straight into Unity as sprites/UI Images.

## The checklist, by category

### 1. Resource & population icons (5 icons — needed immediately, blocks nothing else)

Wood, Food, Stone, Gold, Population. Every other UI element (cost tooltips, the resource HUD bar
itself) references these, so they're the cheapest, highest-leverage icons to do first.

| Icon | Prompt |
|---|---|
| Wood | `[icon anchor] + "a stack of cut timber logs, representing the Wood resource"` |
| Food | `[icon anchor] + "a bundle of grain/wheat sheaves and a clay pot, representing the Food resource"` |
| Stone | `[icon anchor] + "a chiseled grey stone block, representing the Stone resource"` |
| Gold | `[icon anchor] + "a small pile of gold coins with a South Asian coin-motif design, representing the Gold resource"` |
| Population | `[icon anchor] + "a simple South Asian dwelling/hut silhouette with two small figure silhouettes beside it, representing Population capacity"` |

### 2. Building icons (14 icons — one per building type that exists in code today)

Matches the confirmed building roster: TownCenter, Barracks, House, Farm, Market, Dock, Tower,
Wall, Gate, Durg, Karmashala, LumberCamp, MiningCamp, Mill.

| Icon | Prompt |
|---|---|
| Town Center | `[icon anchor] + "a grand multi-tiered South Asian administrative building with a domed roofline, representing the Town Center"` |
| Barracks | `[icon anchor] + "a fortified rectangular training hall with a banner/flag, representing the Barracks"` |
| House | `[icon anchor] + "a modest single-story South Asian dwelling, representing the House"` |
| Farm | `[icon anchor] + "a small farmstead with a plowed field pattern, representing the Farm"` |
| Market | `[icon anchor] + "an open-sided bazaar pavilion with awning, representing the Market"` |
| Dock | `[icon anchor] + "a timber pier and boathouse extending over water, representing the Dock"` |
| Tower | `[icon anchor] + "a tall narrow defensive watchtower, representing the Tower"` |
| Wall | `[icon anchor] + "a straight fortification wall segment, representing the Wall"` |
| Gate | `[icon anchor] + "a fortified gateway set into a wall line, representing the Gate"` |
| Durg | `[icon anchor] + "an imposing fortified stone keep with a raised plinth and watch-post, representing the Durg (fort) building"` |
| Karmashala | `[icon anchor] + "a forge/workshop building with an anvil and hammer motif, representing the Karmashala (blacksmith-equivalent)"` |
| Lumber Camp | `[icon anchor] + "a small timber camp with stacked logs and a lean-to roof, representing the Lumber Camp"` |
| Mining Camp | `[icon anchor] + "a small mining camp with an ore cart and timber headframe, representing the Mining Camp"` |
| Mill | `[icon anchor] + "a small grain mill with a thatched roof and a stone quern or waterwheel, representing the Mill"` |

### 3. Unit icons (roster below — group by what's already live vs Wave 3/4-gated)

**Already-live roster (7 icons):** Worker/Villager, Spearman, Archer, Cavalry, Siege, War
Galley, Fishing Boat.

**Wave 3 upgrade-tier icons (16 icons)** — one per tier per line, since each tier is a distinct
trainable unit once the ladders are built: Infantry ×5 (Padati/Senani/Khandayata/Maha
Khandayata/Vir Yodha), Spearman ×3, Archer ×3, Cavalry/Knight ×3, Elephant ×2.

**Wave 3 remaining lines (8 icons):** Mangonel/Siege ×3, Galley/Naval ×3, Unique-unit Elite tier
badge (a single "Elite" overlay icon reused across all 7 unique units, not 7 separate icons —
matches AoE's own convention of one elite-star overlay rather than a full redraw).

**Wave 4 new-unit icons (12 icons):** Scout, Skirmisher, Battering Ram, Cavalry Archer, Camel
Rider, Scorpion, Trebuchet, Fire Ship, Trader, Vaidya, Purohita, Maharaja (Maharaja may want 5
civ-specific portrait icons rather than 1 generic, matching the model treatment — your call).

**Unique units (7 icons, one per civ):** Chola Naval Raider, Vijayanagara War Elephant, Rajput
Royal Guard, Maurya War Elephant, Pillar Edict Scholar, Maratha Mavla Raider, Maratha Durg
Garrison.

Example prompts (the pattern is identical for every unit — subject changes, frame doesn't):

| Icon | Prompt |
|---|---|
| Worker/Villager | `[icon anchor] + "a South Asian peasant worker figure carrying a farming tool, representing the Villager unit"` |
| Vir Yodha (Infantry tier 5) | `[icon anchor] + "an elite South Asian infantry swordsman in advanced armor, representing the Vir Yodha (final Infantry tier)"` |
| Rajput Royal Guard | `[icon anchor] + "an ornately armored Rajput cavalry guard, representing the Rajput Royal Guard unique unit"` |
| Elite overlay badge | `[icon anchor] + "a small gold star-and-laurel badge meant to overlay in the corner of an existing unit icon, representing an Elite-tier upgrade, no other subject — just the overlay badge itself"` |

### 4. Technology / upgrade icons (roughly 15-20 icons)

Age-up icons (3: Ancient→Classical, Classical→Durg, Durg→Imperial — each shows the destination
age's emblem), Karmashala stat-upgrade icons (attack/armor tiers ×3 steps, matching AoE's
Forging/Scale-Mail-Armor-style convention of a distinct icon per research), one Unique
Technology icon per civ (5), and a small number of Economy tech icons per
`EconomyTechProgress.cs` — check that file with Claude Code for the exact current list before
generating, since econ techs aren't all named in this workbook yet.

| Icon | Prompt |
|---|---|
| Age-up to Durg | `[icon anchor] + "an upward-pointing arrow or ascending staircase motif overlaid with the Durg fort emblem, representing advancing to the Durg Age"` |
| Karmashala attack upgrade (example) | `[icon anchor] + "a sword being sharpened on a whetstone, representing a weapon-attack research upgrade"` |
| Unique Tech (example, per civ) | `[icon anchor] + "a [civ]-specific ceremonial emblem or scroll, representing that civilization's unique technology"` |

### 5. Civilization icons (5 icons)

One crest/emblem per civilization, used on the civ-select screen and in tooltips — matching each
civ's already-established material/motif language (same source used for buildings and gear):

| Civ | Prompt |
|---|---|
| Chola | `[icon anchor] + "a Chola civilization crest: a bronze temple-gopuram silhouette with naval/maritime motifs"` |
| Vijayanagara | `[icon anchor] + "a Vijayanagara civilization crest: a granite pillared-mandapa silhouette with an elephant motif"` |
| Rajput | `[icon anchor] + "a Rajput civilization crest: a rose-pink sandstone chhatri-dome silhouette with a jharokha-lattice pattern"` |
| Maurya | `[icon anchor] + "a Maurya civilization crest: a four-lion Ashokan capital silhouette in polished cream/gold tones"` |
| Maratha | `[icon anchor] + "a Maratha civilization crest: a dark basalt hill-fort silhouette with a saffron accent"` |

### 6. Command / UI icons (roughly 15-20 icons — lower priority, tied to Wave 5's UI re-anchor)

Attack-move, Stop, Guard, Patrol, Formation (2-3 formation-shape icons), Garrison, Repair, Rally
point/flag, Delete, and per-building "train X" action icons (which mostly reuse the unit icons
from section 3 rather than needing separate art). Hold this category until Wave 5 item 30 (the
bottom-bar UI re-anchor) is actually being built — no point finalizing command-icon art before
the layout that will display it is decided (see decision #9 in `YOUR_ACTION_ITEMS.md`).

## Suggested order

1. Resources + Population (5) — immediate, blocks nothing, used everywhere.
2. Buildings (14) — all 14 buildings already exist in code, so these are usable right away.
3. Already-live units (7) — same reasoning.
4. Civilization crests (5) — needed for any civ-select UI, independent of any wave.
5. Unique units (7) + their single shared Elite-overlay badge (1).
6. Wave 3 upgrade-tier icons (16) and remaining lines (8) — generate alongside each upgrade
   ladder as it's built in Wave 3, not all upfront, so icon and unit ship together.
7. Wave 4 new-unit icons (up to 16, depending on Maharaja's civ-variant count) — same logic,
   alongside each Wave 4 item as you greenlight it.
8. Technology icons (~15-20) — alongside Karmashala/age-up/unique-tech code landing.
9. Command/UI icons (~15-20) — hold until Wave 5's UI layout decision is settled.

## Handing this to Claude Code

Once a batch of icon PNGs is ready, the actual Unity wiring (importing as Sprites, setting the
correct import settings for UI use, and pointing each build-menu/tech-tree button at the right
one) is a small, mechanical Claude Code task — much lighter than the 3D-model pipeline in
`docs/BRING_IN_ART_1-4.md`, since icons need no rigging, no age/civ resource-path logic, just a
straightforward Resources/Sprites folder and a lookup by unit/building/tech name. Worth doing as
its own short session once a full category (e.g. all 14 building icons) is ready, rather than
one-by-one.
