# Your Action Items — Assets, UI Decisions & Design Calls

Companion to `docs/IMPLEMENTATION_ROADMAP.md` and `docs/KingdomsOfBharat_Model_Spec.xlsx`.
Everything below is work Claude Code CANNOT do for you — art/audio sourcing, or a decision
only you can make. Claude Code's job on each of these is limited to building the code plumbing
(a material slot, a factory hook, a switch statement with a placeholder fallback) that makes
dropping your asset or answer in later a small change, not a rewrite. Nothing here blocks the
code-only waves (0-3) — this list runs on your own schedule alongside them.

**Standing rule (UPDATED, applies project-wide, present and future) — all civs share ONE common
architecture tree through Classical Age, for every building.** This widens the original
Ancient-only version of this rule: civ-specific building architecture now starts at **Durg
Age**, not Classical. Concretely, for Town Center and any other building that gets age-visual
variants — current or added later — **Ancient AND Classical** are each ONE shared, generic,
non-civ-specific model reused by all 5 civilizations; civ-distinct architecture (regional
motifs, materials, silhouette) only begins at **Durg Age** onward (Durg, then Imperial). This
applies automatically to any building added to the roster in the future, not just the ones
tracked in this document today — no need to re-confirm this rule per building. **The one
exception is Tower and Wall**, which follow their own separate, already-confirmed rule below
(shared through Durg, civ-specific starting Imperial only) — do not apply this Classical-cutoff
rule to those two.

**Correction (confirmed with you directly): there are 5 existing Town Center models, one per
civ — not one shared generic model.** These 5 are civ-specific already and are being treated as
the **Imperial-Age** tier per civ going forward (they're the most detailed/final-looking design
already on hand). Under the updated standing rule above: Ancient and Classical are each ONE new
shared, non-civ-specific model (2 new models total, not 6), and only **Durg** still needs a
civ-specific variant per civ (5 models). See item 2's updated scope below.

**Correction (confirmed with you directly): Tower and Wall follow the OPPOSITE pattern from
Town Center — civ differentiation starts later, not earlier.** Per you: Tower and Wall are
**not civ-specific until Imperial Age.** Every tier up through Durg (Ancient, Classical, Durg)
uses ONE shared, generic model per tier, reused by all 5 civs and distinguished only by
player-color tint — not by civ architecture. Civ-specific Tower/Wall art only appears at the
**Imperial** tier, where each civ gets its own distinct design; and when a player-owned
Imperial-tier tower/wall is upgraded further, it settles back onto one shared model per civ
(not a new per-upgrade-step look), again told apart only by player color. This is the reverse
of the Town Center rule directly above — do not apply the "generic-until-Classical" pattern to
Tower/Wall, and do not apply this "generic-until-Imperial" pattern to Town Center. Each building
type's differentiation point is a separate, explicit decision; see item 3's updated scope below.

**Major correction (confirmed by checking `Assets/Resources/buildings/` directly): the
Imperial-tier civ-specific building set is further along than anything above assumed.** Each of
the 5 civ folders (`Chola/`, `Rajput/`, `Maurya/`, `Maratha/`, `Vijayanagara/`) contains a full,
matching set of **9 buildings**, each with its own civ-distinct prefab and concept-art
reference: **Barracks, Dock, Farm, Gate, House, Market, Tower, TownCenter, Wall.** That's 45
Imperial-tier models total, already built for every civ, not the partial/unconfirmed picture
the earlier corrections above described (which only checked Town Center and Tower, and only
found WIP art for 2 of 5 civs on Tower). Concretely, this changes two things: (1) item 3's
Tower AND Wall Imperial tier is now **fully done for all 5 civs**, not just Rajput/Maurya's
Tower — see item 3's corrected scope below; (2) Barracks, Dock, House, and Market also already
have complete per-civ Imperial art, which wasn't previously tracked as a gap anywhere in this
document but is worth knowing — it means Wave 4 item 26 (Trader, which needs Market/Dock
infrastructure) has zero new building art to source if it's greenlit, only the Trader unit
itself (already covered in item 6).

---

## 1. Art assets to source or create

| # | Asset | Why it's needed | Scope | Roadmap link |
|---|---|---|---|---|
| 1 | **Villager (Praja) model** | Currently an unclothed generic humanoid — the unit every player looks at most, all match. You flagged this directly. | 1 clothed model (or per-civ variants if you want that level of polish) | **DONE (2026-09-04)** — already wired from an earlier session (Harvest Guardian male/female pair) |
| 2 | **Town Center age-variants** | 5 civ-specific models already exist and are being treated as the Imperial-Age tier (confirmed). Needs a distinct look for the other 3 ages once the 4th age (Durg) exists. **Per the updated standing rule: Ancient AND Classical are each ONE shared model across all 5 civs — civ-specific "architecture sets" only kick in from Durg Age onward.** | **1 new shared Ancient-Age model + 1 new shared Classical-Age model** + 5 civ-specific Durg models = **7 new models total.** (Imperial is done — the existing 5 count as that tier. Only Durg needs to be civ-specific.) | **DONE (2026-09-04)** — see `docs/SESSION_LOG.md`'s matching entry for the resource-path convention |
| 3 | **Tower and Wall upgrade tiers** | AoE shows visible tier progression (Watch Tower → Guard Tower → Keep; Palisade → Stone Wall → Fortified Wall). **Confirmed via `Assets/Resources/buildings/<Civ>/`: the Imperial tier is DONE — all 5 civs have their own Tower.prefab and Wall.prefab, with concept art on file.** Tower/Wall are not civ-specific below Imperial (per the standing rule above) — one shared model per tier, all civs, told apart only by player color. Nothing at those pre-Imperial tiers exists yet, civ-shared or otherwise. | **0 new Imperial models needed (confirmed done for all 5 civs, both structures).** Only the shared pre-Imperial tiers remain: 3 shared Tower tier models (Ancient/Classical/Durg) + 3 shared Wall tier models (Ancient/Classical/Durg) = **6 new models total.** | **DONE (2026-09-04)** |
| 4 | **Drop-off building art** (Lumber Camp, Mining Camp, Mill) | Currently procedural silhouettes — visibly below the bar the other ~45 buildings set. **Note: `Farm.prefab` already has full per-civ Imperial art (confirmed as part of the 9-building set above) — if "Mill" in this project is the Farm's drop-off point rather than a separate structure, this item may really only be Lumber Camp + Mining Camp (2 buildings, not 3). Worth a quick confirm before sourcing a Mill you may not need.** | 2-3 buildings (pending the Mill/Farm check above), 1 model each | **DONE (2026-09-04)** — all 3 sourced and wired (Mill kept as its own drop-off, separate from Farm) |
| 5 | **Per-civ unit gear** (helmet / shield / weapon variants), broken out by age per the standing rule | The attachment system (`WeaponAttachment.cs`) already exists and works — only the art is missing. **Ancient and Classical gear is shared/generic across all 5 civs (no civ markers); civ-specific gear starts at Durg, same cutoff as buildings.** Full civ-by-civ, age-by-age breakdown below. | **2 shared gear sets (Ancient, Classical) + 5 civs × 2 civ-specific ages (Durg, Imperial) = 12 gear sets total, × 3 pieces each (helmet/shield/weapon) = 36 individual pieces.** | Asset-blocked track |
| 6 | **New-unit models**, one per Wave 4 item you confirm | Scout, Skirmisher, Battering Ram, Cavalry Archer, Camel Rider, Scorpion, Trebuchet, Fire Ship, Trader, Vaidya, Purohita, Maharaja (per civ) | Up to 11-16 new models depending on which Wave 4 items you greenlight | Wave 4 |
| 7 | **Trebuchet pack/unpack states** | Needs two mesh states or a fold animation, not just a static model — flagged separately because it's an animation dependency, not only a model. | 1 unit, 2 states | Wave 4 item 24 |

**Sequencing tip from the roadmap:** item 1 (Villager) may be worth resourcing *ahead of* combat-unit art, since it's the unit under the camera most often. Items 2 and 6 are gated on code waves landing first (Wave 1 and Wave 4 respectively) — no point commissioning a Durg-age Town Center before the Durg age exists in-engine to preview it against.

### Generation prompts — Canva (2D concept/reference) and Meshy (3D model)

A shared style anchor to paste at the front of every prompt, so all seven items read as one
consistent set rather than seven different art styles:

> *"Age of Empires II: Definitive Edition art style — hand-painted, high-detail RTS game asset,
> warm painterly lighting, semi-realistic proportions (not cartoonish/chibi), medieval Indian
> subcontinent setting and material culture (cotton and silk textiles, bronze and wootz-steel
> weapons, carved sandstone and teak architecture), three-quarter isometric game-camera angle,
> clean silhouette readable at RTS zoom distance, no background clutter, orthographic
> turnaround-friendly pose."*

Use Canva's AI image generator (or any similar text-to-image tool) for 2D concept sheets and
texture reference; feed the resulting image into Meshy's image-to-3D mode for a cleaner, more
controllable model than text-to-3D alone — image-to-3D is worth the extra step for anything
that needs to match a specific silhouette (buildings, gear).

| # | Asset | Prompt |
|---|---|---|
| 1 | **Villager (Praja)** | `[style anchor] + "a South Asian peasant villager/worker, both a male and female variant, simple cotton dhoti/lungi or saree tucked for work, bare feet or simple sandals, carrying a farming tool (sickle, basket, or axe), neutral idle pose, front-3/4 and back view for a game character turnaround"` |
| 2 | **Town Center — Ancient + Classical (2 NEW shared models, all civs) + Durg (civ-differentiated). Imperial is already done — the 5 existing civ models cover it.** | **Ancient and Classical — neither exists yet; generate each ONCE, no civ-specific markers, shared by all 5 civs.** Ancient: `[style anchor] + "a Town Center administrative building, simple generic South Asian earthen/timber-and-thatch construction, no civilization-specific ornamentation, deliberately plain — this is the pan-Bharat 'Dark Age' baseline every civilization starts from, centered, orthographic front-3/4 view, game-ready silhouette"`. Classical: `[style anchor] + "a Town Center administrative building, one step more developed than a Dark-Age baseline — fired brick construction with a modest carved gate, still no civilization-specific ornamentation, shared by every civilization at this age, centered, orthographic front-3/4 view, game-ready silhouette"`. **Durg — generate once per civ** (5 runs), this is the only age below Imperial that goes civ-specific, so match each civ's already-established Imperial-tier design language so the progression reads as one building evolving — pull the civ's existing Imperial Town Center screenshot as a style reference image alongside the text prompt if your tool supports image+text input: `[style anchor] + "a Town Center for the [Chola / Vijayanagara / Rajput / Maurya / Maratha — pick one] civilization in its Durg-Age form: fortified stone keep with a raised plinth and watch-post, with civ-distinct ornamentation (regional motifs, [civ]'s emblem/banner colors) consistent with this civilization's existing Imperial-Age Town Center design, centered, orthographic front-3/4 view, game-ready silhouette"` |
| 3 | **Tower & Wall — shared pre-Imperial tiers only (Imperial tier is already done for all 5 civs)** | **Ancient/Classical/Durg — ONE shared model per tier, no civ markers, generate once each:** `[style anchor] + "a defensive [watchtower / wall segment] — [Ancient: wooden palisade/timber tower or fence / Classical: rubble-and-mortar stone tower or wall / Durg: dressed-stone fortified tower or wall with battlements and arrow-slits — pick one], generic South Asian military construction, no civilization-specific ornamentation, deliberately plain, orthographic front-3/4 view, consistent scale for tier comparison"`. Nothing to generate for Imperial — `Assets/Resources/buildings/<Civ>/Tower.prefab` and `Wall.prefab` already exist for all 5 civs. |
| 4 | **Drop-off buildings** (Lumber Camp / Mining Camp / Mill) | `[style anchor] + "a small resource drop-off structure for a [timber lumber camp with stacked logs and a lean-to roof / mining camp with an ore cart and timber headframe / grain mill with a thatched roof and a stone quern or waterwheel — pick one], modest single-purpose building, orthographic front-3/4 view"` |
| 5 | **Per-civ unit gear** — see the full civ-by-civ, age-by-age breakdown with individual prompts in the dedicated subsection right after this table. | *(prompts listed per civ per age below, not summarized here — there are 12 distinct gear sets, not one generic prompt)* |
| 6 | **New-unit models** (per Wave 4 unit you confirm) | `[style anchor] + "a South Asian [Scout on horseback, light armor / Skirmisher archer with a small buckler / battering-ram siege crew with a covered ram / horse archer with a recurve bow / camel-mounted rider / scorpion ballista crew / trebuchet siege crew / fire-ship naval unit with a bow-mounted brazier / merchant trader with a laden pack-ox or cart / healer ascetic (Vaidya) with a satchel of herbs / wandering monk-like convert (Purohita) with a staff / royal Maharaja hero in ornate ceremonial armor — pick one] unit, combat-idle pose, front-3/4 and back view for a game character turnaround"` |
| 7 | **Trebuchet pack/unpack states** | Two separate generations, same seed/style for consistency: `[style anchor] + "a siege trebuchet in its FOLDED/PACKED travel state, wheels down, arm lashed flat"` and `[style anchor] + "the same siege trebuchet in its UNFOLDED/DEPLOYED firing state, arm raised and counterweight visible, braced on the ground"` — hand both to whoever rigs the fold animation, or use Meshy on each separately if you want two discrete meshes blended in Unity instead of a skeletal animation. |

Practical notes: generate 2-3 variants per prompt and pick the best rather than accepting the
first result — these tools are inconsistent at this level of style specificity. For anything
going through Meshy image-to-3D, a clean orthographic front view with a plain/transparent
background converts far more reliably than a busy or angled shot, so it's worth a quick manual
crop/background-removal pass on the Canva output before feeding it into Meshy.

### Per-civ unit gear — full breakdown by age

Same rule as buildings: **Ancient and Classical gear is ONE shared set worn by every civ's
soldiers — no civ markers.** Civ-specific gear begins at **Durg** and carries through to
**Imperial**, where each civ's identity is most elaborate. Each entry below is a full "gear set"
covering the three attachment slots — helmet, shield, weapon — generated together in one Canva
prompt so the pieces read as belonging to the same soldier. Civ material/motif language below is
pulled from each civilization's own already-established building concept art
(`Assets/Resources/buildings/<Civ>/`) so gear matches architecture rather than inventing a
separate visual language.

**Shared baseline — generate ONCE each, used by all 5 civs:**

| Age | Helmet | Shield | Weapon | Prompt |
|---|---|---|---|---|
| **Ancient** (shared) | Simple leather/quilted skull-cap, no metal | Plain wood-and-hide round shield, unpainted | Unadorned iron short sword or spearhead, plain wood haft | `[style anchor] + "a basic soldier's equipment set for the earliest age of a South Asian RTS civilization, deliberately plain and generic — no civilization-specific ornamentation: a simple leather or quilted skull-cap, a plain wood-and-hide round shield with no markings, and an unadorned iron short sword or spearhead with a plain wood haft, laid out as three separate equippable game-asset pieces on a plain background, front and side view each"` |
| **Classical** (shared) | Riveted bronze/iron conical helmet, plain nasal guard | Round wood-and-hide shield, plain metal boss, no civ markings | Iron short sword or spear, leather-wrapped grip | `[style anchor] + "a soldier's equipment set one step more developed than the earliest age, still deliberately generic — no civilization-specific ornamentation: a riveted bronze or iron conical helmet with a plain nasal guard, a round wood-and-hide shield with a plain metal boss and no markings, and an iron short sword or spear with a leather-wrapped grip, laid out as three separate equippable game-asset pieces on a plain background, front and side view each"` |

**Chola** — Dravidian temple architecture, dark grey granite, bronze relief work, naval/maritime tradition:

| Age | Helmet | Shield | Weapon | Prompt |
|---|---|---|---|---|
| **Durg** | Bronze conical helmet, modest temple-finial spike | Round shield, plain bronze rim | Iron sword, bronze pommel | `[style anchor] + "a Chola civilization soldier's equipment set at a mid-tier fortification age: a bronze conical helmet with a modest temple-finial spike, a round shield with a plain bronze rim, and an iron sword with a bronze pommel — restrained Dravidian temple influence, not yet fully ornate, laid out as three separate equippable game-asset pieces on a plain background, front and side view each"` |
| **Imperial** | Ornate bronze helmet, Nandi-bull or lotus finial crest | Shield with gopuram-motif etched bronze rim | Curved sword (patta-style), bronze basket-hilt, rope-motif engraving | `[style anchor] + "a Chola civilization soldier's equipment set at its most elaborate imperial tier: an ornate bronze helmet crested with a Nandi-bull or lotus finial, a shield with a temple-gopuram motif etched into its bronze rim, and a curved sword with a bronze basket-hilt and naval rope-motif engraving — full Dravidian temple and maritime ornamentation, laid out as three separate equippable game-asset pieces on a plain background, front and side view each"` |

**Vijayanagara** — Hampi-style granite, pillared mandapa, elephant and lotus motifs:

| Age | Helmet | Shield | Weapon | Prompt |
|---|---|---|---|---|
| **Durg** | Granite-grey lacquered helmet, plain lotus boss | Round shield, modest elephant-motif rim | Iron sword, plain hilt | `[style anchor] + "a Vijayanagara civilization soldier's equipment set at a mid-tier fortification age: a granite-grey lacquered helmet with a plain lotus boss, a round shield with a modest elephant-motif rim, and an iron sword with a plain hilt — restrained Hampi-style influence, not yet fully ornate, laid out as three separate equippable game-asset pieces on a plain background, front and side view each"` |
| **Imperial** | Helmet with elephant-tusk crest motif | Shield with full elephant-and-lotus relief medallion, gold/granite tones | Sword with mandapa-pillar-fluted hilt | `[style anchor] + "a Vijayanagara civilization soldier's equipment set at its most elaborate imperial tier: a helmet with an elephant-tusk crest motif, a shield bearing a full elephant-and-lotus relief medallion in gold and granite tones, and a sword with a mandapa-pillar-fluted hilt — full Hampi-style temple ornamentation, laid out as three separate equippable game-asset pieces on a plain background, front and side view each"` |

**Rajput** — rose-pink sandstone, domed chhatri pavilions, jharokha lattice work:

| Age | Helmet | Shield | Weapon | Prompt |
|---|---|---|---|---|
| **Durg** | Iron helmet, modest chhatri-shaped crest | Round shield, rose-sandstone lacquer, simple jharokha-lattice rim | Curved talwar sword, plain hilt | `[style anchor] + "a Rajput civilization soldier's equipment set at a mid-tier fortification age: an iron helmet with a modest chhatri-dome-shaped crest, a round shield lacquered in a rose-sandstone tone with a simple jharokha-lattice pattern at the rim, and a curved talwar sword with a plain hilt — restrained fort-and-haveli influence, not yet fully ornate, laid out as three separate equippable game-asset pieces on a plain background, front and side view each"` |
| **Imperial** | Ornate helmet, full chhatri-dome crest and plume | Shield with painted jharokha-lattice medallion, rose-pink accent | Talwar or katar, gilt hilt, floral chasing | `[style anchor] + "a Rajput civilization soldier's equipment set at its most elaborate imperial tier: an ornate helmet with a full chhatri-dome crest and plume, a shield with a painted jharokha-lattice medallion and rose-pink sandstone accent, and a talwar or katar with a gilt hilt and floral chasing — full fort-and-haveli ornamentation, laid out as three separate equippable game-asset pieces on a plain background, front and side view each"` |

**Maurya** — polished cream Chunar sandstone, lion-capital pillars, mirror-shine finish:

| Age | Helmet | Shield | Weapon | Prompt |
|---|---|---|---|---|
| **Durg** | Polished bronze/iron helmet, plain lion-capital crest nub | Shield with modest Ashokan-lion boss | Iron sword, plain hilt | `[style anchor] + "a Maurya civilization soldier's equipment set at a mid-tier fortification age: a polished bronze or iron helmet with a plain lion-capital crest nub, a shield with a modest Ashokan-lion boss at its center, and an iron sword with a plain hilt — restrained Mauryan imperial influence, not yet fully ornate, laid out as three separate equippable game-asset pieces on a plain background, front and side view each"` |
| **Imperial** | Mirror-polished helmet, full four-lion Ashokan capital crest | Shield with stupa-dome medallion and lion motif | Sword with lion-headed pommel | `[style anchor] + "a Maurya civilization soldier's equipment set at its most elaborate imperial tier: a mirror-polished helmet crested with a full four-lion Ashokan capital, a shield bearing a stupa-dome medallion and lion motif, and a sword with a lion-headed pommel — full Mauryan imperial mirror-shine ornamentation, laid out as three separate equippable game-asset pieces on a plain background, front and side view each"` |

**Maratha** — Deccan hill-fort, dark basalt, deliberately minimal ornamentation (function over display):

| Age | Helmet | Shield | Weapon | Prompt |
|---|---|---|---|---|
| **Durg** | Plain dark-iron helmet | Minimal basalt-toned shield, simple burj-tower motif | Iron sword, plain hilt | `[style anchor] + "a Maratha civilization soldier's equipment set at a mid-tier fortification age: a plain dark-iron helmet with no crest, a minimal basalt-toned shield with a simple hill-fort watch-burj motif, and an iron sword with a plain hilt — deliberately understated Deccan hill-fort influence, function over display, laid out as three separate equippable game-asset pieces on a plain background, front and side view each"` |
| **Imperial** | Dark-iron helmet, modest saffron-accented plume | Shield with hill-fort bastion motif | Curved sword (firangi-style), saffron-and-black hilt wrap | `[style anchor] + "a Maratha civilization soldier's equipment set at its most elaborate imperial tier, still deliberately restrained compared to other civilizations per its hill-fort identity: a dark-iron helmet with a modest saffron-accented plume, a shield bearing a hill-fort bastion motif, and a curved firangi-style sword with a saffron-and-black hilt wrap — minimal ornamentation even at its peak, function over display, laid out as three separate equippable game-asset pieces on a plain background, front and side view each"` |

### Pose and poly-count targets

These budgets assume a top-down/isometric RTS camera that rarely gets close, with 20-100 units
and 10-40 buildings visible on screen at once — the same reasoning AoE-style games use to keep
per-unit poly counts low even when the overall scene looks detailed. Treat these as the game
budget (what should ship in Unity after retopology/decimation), not the raw Meshy output, which
is typically much denser and needs a manual or automated poly-reduction pass first. If any
existing unit or building in the project already has a known tris count, match new assets to it
rather than these numbers — internal consistency matters more than any specific target.

| # | Asset | Pose to generate/request | Target poly count (triangles, game-ready) |
|---|---|---|---|
| 1 | **Villager (Praja)** | **T-pose or A-pose**, arms extended, neutral expression — this is a rig-ready pose, not an action pose, since it needs idle/walk/gather/chop/mine/build animations. | **1,500–3,000 tris.** Cheap and high-instance-count (many on screen gathering at once) matters more than fine detail here. |
| 2 | **Town Center — 4 age variants** | **Static, no pose** — architectural asset. Model centered, front-3/4 hero angle as generated, no animation needed beyond maybe a flag/banner flutter. | **3,000–6,000 tris per age variant.** It's the largest, most-looked-at building, so it can sit at the top of the building budget — but the camera still never gets close enough to justify more. |
| 3 | **Tower & Wall — shared pre-Imperial tiers only** | **Static, no pose.** | **Towers 800–2,000 tris per tier, Wall segments 300–800 tris per tier** (cheapest assets on the list — reused by all civs and walls repeat many times per base perimeter). If the existing Imperial-tier `Tower.prefab`/`Wall.prefab` per civ run higher than this, that's fine to leave as-is — match the new shared tiers to each other for consistency, not necessarily down to the existing Imperial tier's budget. |
| 4 | **Drop-off buildings** (Lumber Camp / Mining Camp / Mill) | **Static, no pose.** | **1,000–2,500 tris each.** Small single-purpose buildings — should read clearly as an upgrade from the current procedural silhouettes without approaching Town Center-level budget. |
| 5 | **Per-civ unit gear** (all 12 gear sets — 2 shared + 5 civs × 2 civ-specific ages) | **Static prop, no pose** — modeled to attach at the existing hand/head bone via `WeaponAttachment.cs`, not as part of a posed character. Applies uniformly to every gear set, shared or civ-specific. | **200–600 tris per piece** (helmet, shield, or weapon each counted separately, across all 12 sets / 36 pieces). Imperial-tier civ-specific pieces can lean toward the top of that range given the extra ornamentation described above; Ancient/Classical shared pieces should stay at the bottom since they're deliberately plain. These stack on top of a unit's own budget across every instance of that unit, so keep them the leanest items on the list regardless of age or civ. |
| 6 | **New-unit models** (per Wave 4 unit) | **T-pose or A-pose** for anything that needs a combat rig (all of them except the Trebuchet crew-only details) — do NOT generate these in an action/combat pose, since a rigger needs a neutral pose to bind animations to. | **Infantry-scale units (Skirmisher, Vaidya, Purohita, Trader-on-foot): 2,000–4,000 tris. Mounted units (Scout, Cavalry Archer, Camel Rider): 4,000–7,000 tris including mount. Siege crews (Battering Ram, Scorpion): 3,000–6,000 tris including the machine. Fire Ship (hull + rigging + brazier): 4,000–8,000 tris. Maharaja hero (per civ): 6,000–10,000 tris** — heroes are rarer on screen (typically one per player) so they can afford noticeably more detail than the rank-and-file roster. |
| 7 | **Trebuchet pack/unpack states** | **Static, no pose** — this is a mechanical fold, not a character pose. If a rigger will blend the two states as a skeletal animation, generate both in a T-pose-equivalent neutral orientation (centered, no camera-relative tilt) so they align; if instead you'll hard-swap two meshes in Unity, orientation match matters less. | **4,000–7,000 tris per state** (folded and deployed) — on the higher end because the arm/frame silhouette is what sells the pack/unpack read, and it's a single-instance siege unit, not something duplicated 20 times per army. |

---

## 2. Audio assets to source

| # | Asset | Why it's needed | Scope |
|---|---|---|---|
| 8 | **Soundtrack** | Already scoped in a prior session: self-serve CC0 tracks from Kenney.nl, same sourcing approach as the existing `Audio/SfxPlayer.cs` pass. | Pure sourcing — Claude Code only wires whatever tracks you provide into the existing audio system, it doesn't compose anything. |

---

## 3. UI layout — visual decisions, not just code

The UI Layout sheet in the model-spec workbook (and Wave 5 item 30 in the roadmap) calls for
re-anchoring the build menu, resource panel, and selected-unit panel into one AoE-style
bottom-docked bar (command panel / info panel / minimap, left to right) instead of the current
build-menu-right / resource-panel-top-left split. The roadmap sizes the *code* side of this as
Large but doable by Claude Code. What's on you:

| # | Decision needed | Notes |
|---|---|---|
| 9 | **Approve (or redesign) the bottom-bar composition** before it's built | Do you want AoE's exact 3-region split, or a variant? A quick mockup or reference screenshot from you avoids a re-anchor-then-redo cycle — this is exactly the kind of thing worth a Plan Mode conversation with visuals before code starts. |
| 10 | **Icon set for anything not already covered** | The existing build/train icon language mostly carries over, but new Wave 4 units and the Karmashala/Durg buildings will need icons that don't exist yet — same sourcing shape as the 3D assets above, just 2D. |

---

## 4. Design decisions requiring a yes/no or a direction from you

These aren't assets — they're calls the roadmap explicitly stops and waits on rather than
guessing. Each blocks specific downstream work until you answer.

| # | Decision | What it blocks | Roadmap link |
|---|---|---|---|
| 11 | **UnitClass vs UnitCategory: extend or collapse?** | Every Support/Hero unit (Vaidya, Purohita, Maharaja) has no runtime class until this is resolved. | Wave 0 item 1 |
| 12 | **Durg building's garrison capacity and what it trains** | The building itself, and everything downstream that assumes it exists (unique units, unique techs). | Wave 2 item 7 |
| 13 | **Elephant ladder: one shared line for Maurya+Vijayanagara, or two divergent ones?** | Wave 3 item 13 | |
| 14 | **Camel Rider — ship it at all?** | Currently listed as "design decision," not a confirmed build. | Wave 4 item 22 |
| 15 | **Trader / trade routes — wanted at all?** | This is new economic machinery (Market-to-Market, Dock-to-Dock routes), not just a unit — sizeable scope commitment either way. | Wave 4 item 26 |
| 16 | **Which building houses Vaidya/Purohita** (Durg building, or a dedicated Monastery-equivalent?) | Wave 4 item 27 | |
| 17 | **Build the Maharaja hero at all — and if so, which victory condition does it serve?** | A hero with no win-condition consumer is pure cost; roadmap explicitly says confirm the victory condition first. | Wave 4 item 28 |
| 18 | **Relics + Monastery-equivalent — wanted at all?** | Needs a carrier unit and a storage building; depends on #16 too if the carrier is Vaidya/Purohita. | Wave 6 item 34 |
| 19 | **Which victory conditions beyond Conquest do you actually want?** (Wonder / Relic / Regicide / Time Limit) | Building all of AoE's isn't automatically right-scoped for this project — the roadmap asks you to pick. | Wave 6 item 36 |
| 20 | **Building upgrade-tier system — do you want visual tiers for Towers/Walls the way units now get unit tiers?** | Not yet scoped in the roadmap at all; flagged as a "Wave 3.5 candidate" only if you say yes. This decision also determines whether asset item #3 above is even needed. | Asset-blocked track note |

---

## How to use this alongside the roadmap

Nothing above blocks Wave 0-3 (the foundation fixes and upgrade ladders) — those are pure code
and can proceed immediately. Start sourcing/deciding in roughly this order to stay ahead of the
code waves rather than behind them:

1. **Now:** decisions #11-13 (cheap, unblock the most downstream work) and asset #1 (Villager) —
   can start today, nothing to wait on.
2. **Once Wave 1 lands:** asset #2 (Town Center variants) becomes previewable in-engine.
3. **Before Wave 4 starts:** decisions #14-19 — the roadmap will otherwise stall on each item
   waiting for your answer mid-wave.
4. **Before Wave 5 item 30:** UI decision #9 — get this settled before the re-anchor session so
   it isn't redone.
5. **Whenever convenient, fully parallel:** asset #8 (soundtrack), decision #20 (building tiers).
