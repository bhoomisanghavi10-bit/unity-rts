# Kingdoms of Bharat — Repository Audit and Development Roadmap

**Prepared:** 26 September 2026 (India time)  
**Repository:** bhoomisanghavi10-bit/unity-rts  
**Reviewed branch:** claude/scaffold-kingdoms-of-bharat  
**Pinned commit:** 49a7e20397bdf32f0d5edd10c5642b480bf13969  
**Runtime-observation commit:** b1262c530bf32e2a6395f2b1a448a28f3d146a09 (same branch; later than the pinned static-audit commit)  
**Engine:** Unity 6000.3.21f1; Universal Render Pipeline package 17.3.0.

**Runtime-verification update, 26 September 2026:** The project was opened and exercised in Unity 6000.3.21f1 on macOS through the live Editor, with Metal rendering under Rosetta. The project compiled and entered Play Mode. I ran the available tests, launched a Chola River Valley skirmish, inspected spawned workers and a mixed combat sample in the Game view, verified the animation clips selected at runtime, and captured Editor Profiler counters. These observations apply to the later runtime-observation commit above, not byte-for-byte to the pinned static-audit commit. They are Editor observations rather than a standalone-player or target-device benchmark.

**Recommendation:** Pause roster and mode expansion. Make the existing single-player skirmish reproducible, saveable, responsive, visually coherent, and measurable. Treat LAN multiplayer as an incomplete integration project until the defects below are resolved. Preserve the useful systems already built; a wholesale rewrite is not justified by this review.

**Review scope and limitations**

The main body remains a static repository audit. I enumerated the complete development-branch tree (5,572 entries, not truncated), retrieved 387 text files covering first-party runtime/editor/test source, design CSVs, documentation, packages and settings, inspected Main.unity, and extracted the 26-sheet master-reference workbook. The runtime contains 257 C# files, approximately 39,133 lines including comments; the test directory contains 79 C# files, all under EditMode. I deeply traced representative gameplay, networking, saving, faction perspective, spawning, animation, rendering support, and test paths. The runtime pass added below is deliberately narrower: it is a smoke playtest and representative battle capture, not a full-match, save/load, LAN, build, or platform-certification pass.

The runtime pass used the checked-out asset payloads in the local project. It did not complete a full economy-to-victory match, exercise save/load, run two-process LAN, build a standalone player, sweep building LODs, or test boats. Performance values are single Editor-frame snapshots with profiling overhead and an unspecified Game-view resolution; they establish scale and obvious hotspots, not shipping percentiles. Numerical asset budgets and milestone workloads elsewhere in this report remain proposed starting points, not published Age of Empires IV specifications.

Evidence labels used below:

- **Confirmed code gap:** Visible directly in the inspected implementation. Runtime impact should still be reproduced in Unity.
- **Risk:** A plausible failure or scalability concern that requires a measured reproduction.
- **Documented/content gap:** Stated in project records or supported by asset/source inventory; visual quality still needs inspection.
- **Recommendation:** A proposed change in design, engineering, or production process.

Source links in the evidence index point to the exact reviewed commit. The root default branch, main, contains only an 11-byte README. The actual project resides on the development branch above.

**Runtime verification ledger (this update)**

| Check | Evidence available now | Editor result | Pass criterion for the next run |
|---|---|---|---|
| Fresh import and compile | Source, package versions, scene text | **Opened and playable.** No blocking compile/Console error was observed. Runtime warnings reported a disabled duplicate `TestAi_MultiFront` controller and missing terrain tree/detail prototypes. | Repeat from a clean checkout/import and retain the Editor log |
| EditMode and PlayMode tests | 79 EditMode test source files; no PlayMode suite found | **EditMode: 694 completed, one failure.** `ScoreProgressTests.Technology_AddsFiftyPoints_OnceUniqueTechResearched` expected 50 and received 0. **PlayMode: zero actual tests discovered/executed** (the runner returned a passing zero-test container). | Fix the score regression and add targeted PlayMode coverage |
| Single-player full match | Static gameplay paths | **Smoke-run only.** Chola/River Valley reached live gameplay, spawned both factions, and sustained a scripted mixed battle; economy progression and victory were not completed. | Start, gather, build, advance, fight and conclude without soft locks |
| Save/load in active match | Confirmed serializer/catalog omissions | **Not run** | Compare every unit/building type, order, queue, technology and resources across reload |
| LAN from two processes | Confirmed networking and bootstrap gaps | **Not run** | Host/client each control own faction, share seed/state, end with same result |
| Unit meshes and textures | Factory-spawned models inspected in Game view | **Partially verified.** Workers used `FemaleVillager`/`MaleVillager` models. Padati, Dhanurdhara, Ashvarohi and Shilakshepaka all resolved to `HumanDummy_M White(Clone)`; cavalry and siege therefore lacked distinct mounted/siege silhouettes on this path. Team colors were readable. | Inspect every roster entry, weapon attachment and LOD at gameplay zoom |
| Building art and LOD | Starting settlement inspected after centering the camera | **Presentation fallback observed.** A bright-red primitive/block-like structure appeared at the player start; its exact hierarchy identity was not captured. No LOD sweep was performed. | Identify the fallback object, then sweep representative buildings and LOD transitions |
| Movement and combat animation | Runtime PlayableGraph clip selection plus Game-view inspection | **Partially verified.** Idle and walk clips played. Fourteen active attackers, including archers, all selected `Armature|Sword_Regular_A`; the suspected archer sword-attack mapping is reproduced. Damage/death and boats were not systematically checked. | Give archers a bow attack and verify every class's idle/walk/attack/death sequence |
| VFX, audio and performance | Representative 24-unit mixed battle in the Editor | **Measured, Editor only.** Battle snapshot: 22.53 ms CPU, 14.79 ms main thread, 11.58 ms render thread, 17.63 ms GPU, 973 batches, 7.39M triangles, 5.52M vertices and 25 visible skinned meshes. A later lighter frame was 14.97 ms CPU/10.20 ms GPU with 922 batches and 7.30M triangles. Total used memory was 3.07 GB, including 1.50 GB textures, 166 MB meshes and 720 MB GC; one sampled frame allocated 22.8 KB. | Capture percentiles in a standalone development player on named target hardware and resolution |

**Observed skirmish behavior.** The default match camera began at approximately `(-50, 18, -50)` while the four player workers spawned around `x=-3..3, z=0`. With normal fog enabled, the Game view was almost entirely black except for one rock and the HUD. Revealing the map showed that terrain and props had rendered; centering the camera over the player spawn exposed the workers and settlement. This reproduces a poor initial-camera/fog interaction rather than a missing-world import. The HUD reported four idle workers and 4/10 population before the injected battle. Resource values remained zero during this smoke run because the economy loop was not exercised.

**How to complete the verification.** Re-run from the pinned commit and a clean import, retain the full Editor log and Test Runner XML, and fix the one score test before using the suite as a green baseline. Then run a complete single-player match without inspection cheats, exercise save/load, and record every unit/building class plus idle/walk/attack/death motion. Profile a 100–200-unit battle and representative settlement in a standalone development player on named target hardware and resolution, collecting percentiles rather than isolated frames. Finally run a two-instance LAN match with separate host/client controls and matched seeds. Keep a row per observation with scene, build hash, asset/entity name, reproduction steps, screenshot/video or log, expected/actual behavior and severity.

---

**1. Overall assessment**

You have built a broad RTS implementation, considerably beyond an initial prototype. Code exists for five civilizations, four ages, gathering and drop-offs, construction and repair, land and naval units, counters and armor, formations, garrisoning, trade, support units, relics, diplomacy, scripted scenarios, a tutorial, UI, saving, and TCP networking.

The central problem is uneven integration. New units and features were added faster than shared services such as saving, networking, AI, presentation, and lifecycle management were extended to support them. For example, the expanded roster and the five-unit save/spawn registry describe different games. A serializer supports message kinds that the receiver never dispatches. A command clock exists, but the gameplay simulation still runs on frame-driven updates.

Therefore, I would describe the project as a **feature-rich prototype requiring integration and production hardening**. It is not supported by the inspected evidence to call it nearly complete with only an art-sourcing tail. This is a maturity judgment, not a claim that the current local build cannot be played.

The most valuable work now is to make one complete match trustworthy. Preserve the five civilizations in the project, but select one reference matchup for focused quality work before applying the standard to all five.

| Area | What exists | Readiness assessment |
|---|---|---|
| Economy and construction | Gatherers, resource stockpiles, drop-offs, farms, repair, trade | Substantial implementation; needs end-to-end workload tests and full persistence |
| Combat and progression | Counters, armor, splash, tiers, support, siege | Broad rule coverage; presentation and integration remain incomplete |
| Movement | NavMeshAgent wrapper, destination formations, water mover | Useful baseline; limited failure handling and naval routing |
| AI | Scripted economy, scouting, production, attacks, diplomacy | Functional architecture; narrow roster use and limited adaptation |
| Saving | JSON capture and reconstruction | Incomplete for the current roster and match state |
| Networking | TCP, commands, clock, hashes, snapshots | Several confirmed integration defects; not merely awaiting a second PC |
| Visual pipeline | Imported buildings, humanoid rigs, terrain, water, clutter, UI art | Significant investment; needs consistent game-ready assets and measured budgets |
| Audio | Nine sound effects and playback helper | Basic functional sound; no comparable music/voice production layer found |
| Quality assurance | 79 EditMode test files and extensive session notes | Valuable unit coverage; committed automated full-match/build evidence is missing |
| Production | Design workbook, import tools, narrow-task conventions | Good foundations, but status drift and oversized instructions obscure priorities |

---

**2. Findings that should change the development order**

**F01 — Save/load cannot preserve the current game. Priority P0. Confirmed code gap.**

SaveManager.IdentifyUnitType recognizes Worker, then only Archer, Cavalry, Siege and Infantry combat classes. Other classes can be omitted, while distinct units sharing a class can restore as the wrong generic unit. A cavalry archer, for example, is classified as Archer and loses its distinct identity on this route. IdentifyBuildingType recognizes only TownCenter, Barracks, Farm, House, Wall, Gate, Tower and Market. Durg, Karmashala, Monastery, Dock and specialized drop-offs are not captured by that switch. EntitySpawner reconstructs only five unit types and eight building types. [S1–S3]

MatchSaveData also lacks a complete representation of queued/current orders, production/research state, partial construction, individual tier identity, depleted resources, carried resources/relics, garrisons, building rotation, explored fog, scenario progress, simulation tick and random-generator state. Some of these limitations are acknowledged in SaveManager comments.

**Why it matters:** A mid-game save is not a faithful continuation. New content can vanish or change identity, and existing progress can be lost. The scenario editor shares the narrow EntitySpawner registry, so its placement capabilities are also smaller than the main game's roster.

**Recommended change:** Introduce stable definition IDs and a versioned, explicit match-state schema. Use one entity catalog for factory creation, scenario placement, saving and loading. Capture logical state independently of meshes. Restore in stages: match configuration and registries, entity IDs, entities, references/orders, visibility, AI ownership, then resume simulation. Reject unsupported save versions clearly; do not silently drop unrecognized entities.

**Acceptance:** A fixture containing every supported entity, rotated walls, a half-built building, a production queue, researched tiers, damaged and garrisoned units, a carried relic and depleted nodes survives a save/load round trip. Continue both the original and restored match and compare meaningful outcomes.

**F02 — The lockstep clock does not control the full simulation. Priority P0 for multiplayer. Confirmed architecture gap.**

SimClock gates its own OnTick event, but MeleeAttacker calls Tick(Time.deltaTime) from Update; TownCenter and Barracks decrement training/research timers with Time.deltaTime; WaterMover moves in Update; land units use NavMeshAgent. The runtime OnTick subscribers found are command execution, state hashing and the desync monitor, not the economy/combat/movement systems. [S4–S7]

**Why it matters:** If the network tick stalls, other gameplay can continue. Different frame rates can produce different timing and spawn order. A fixed-rate command queue alone cannot establish deterministic lockstep.

**Recommended change:** Make an explicit architecture choice before extending networking. Option A is deterministic simulation with every authoritative system controlled by a shared tick and a deliberately deterministic navigation solution. Option B is host/server-authoritative simulation with state replication and client-side visual interpolation. Prototype both costs against your required army size; the existing NavMesh/frame-driven architecture makes authority-based replication worth evaluating, but it is not automatically cheaper at every scale.

**Acceptance:** Two standalone processes at different frame caps run the same scenario. A deliberate network stall freezes or otherwise handles authoritative gameplay according to the chosen protocol. Resources, combat, production, ownership and outcome agree, not just unit positions.

**F03 — Important network orders are missing or broken end to end. Priority P0 for multiplayer. Confirmed code gaps.**

- NetMessageKind and CommandSerializer support TradeRoute, Heal and Convert, but NetworkDriver.Dispatch only passes Move, Train, Build and Attack through command deserialization. The additional messages are not executed there.
- ForAttack captures a target ID only when the target has a Unit component; ToAttackCommand also resolves only a unit. A building target is therefore lost in this path.
- SelectionManager performs gathering and several other actions directly; boat ground orders call WaterMover.MoveTo directly.
- BuildMenu directly calls age-up, research, market buy/sell and other gameplay operations without the same network command route used for training.
- ToMoveCommand requires UnitMover, so it is not a generic water-movement reconstruction path. [S8–S11]

**Recommended change:** Maintain a command coverage table for every user action: local input, validated command, serialization, receiver dispatch, execution, cancellation, saving and testing. Use typed entity references that distinguish units, buildings and resources. Bind sender identity to a faction rather than trusting an arbitrary faction field.

**Acceptance:** Execute each action from both host and client through the real UI in separate processes. Both worlds must observe it exactly once. Test late, duplicate, invalid and stale commands.

**F04 — Match initialization is not synchronized comprehensively. Priority P0 for multiplayer. Confirmed code gaps and high-confidence runtime risk.**

LanMatchMenu creates a seed for HostHello, then reads Environment.TickCount again when completing the host handshake. The client uses the original transmitted seed. These two reads are not guaranteed to be equal. ResourceNodeSpawner uses map.ResourceSeed or its own Environment.TickCount instead of the negotiated NetworkMatch.PendingSeed; built-in map definitions use ResourceSeed = -1. [S12–S13]

There is also a standard-match spawning risk: AiController.Start returns before spawning the Enemy town center/workers during a network match; UnitSpawner and TownCenterSpawner only create Player starting forces; BeginNetworkMatch calls the normal core setup. I found no replacement Enemy starter-spawn in that traced path. Custom scenarios with explicit placements are a different case. [S14]

**Recommended change:** Create a single immutable MatchConfiguration containing player slots, controllers, civilization choices, seed, map, settings and content version. Both peers acknowledge the same configuration before a dedicated bootstrapper spawns every faction. AI activation must be separate from faction spawning. Give terrain/resources/wildlife reproducible random streams derived from the agreed seed.

**Acceptance:** A normal LAN skirmish starts with both human players owning their intended starting forces and identical world layouts. Repeat with different frame rates and handshake delays. The second player's requested civilization must not be silently changed by distinct-civilization fallback logic.

**F05 — LAN client perspective is still hardcoded to Player. Priority P0 for multiplayer. Confirmed code gap.**

NetworkMatch correctly identifies the joining human as Enemy, but ResourceHUD reads Player resources, civilization, age and population. FogOfWarManager bases its decisions on Player; unit factories such as ArcherFactory add vision only for Player. MatchManager evaluates victory from Player's perspective. [S15–S17]

**Recommended change:** Separate canonical faction simulation from local viewing perspective. Use a viewer/local-faction context for HUD, camera, fog, alerts and victory presentation. Store the winning faction/team in match state, then derive each viewer's result.

**Acceptance:** The joiner sees their own resources and explored area, can see their own army correctly, and gets the correct win/lose result when either side is eliminated. Include same-civilization matches once team-color differentiation is ready.

**F06 — State hashing and resynchronization are incomplete. Priority P0 for multiplayer. Confirmed code gap.**

StateHash folds the tick plus unit positions, faction and health. It does not include building state, resources, technologies, current commands or other strategic state. DesyncRecovery reuses the incomplete SaveManager snapshot. The snapshot does not preserve network IDs or a complete clock/command/RNG state; reconstructed objects receive fresh IDs. [S18–S19]

**Why it matters:** Some consequential divergences are invisible to the hash. Recovery can change the roster, invalidate references or lose progress instead of restoring an equivalent match.

**Recommended change:** Build complete snapshot semantics first. Hash canonical state in a stable order and include entity definition/identity, resources, buildings, technologies, orders, timers and relevant RNG state. Resync at an agreed boundary with explicit pending-command handling and persistent IDs. Keep a bounded hash history for delayed comparisons.

**Acceptance:** Deliberately diverge a resource amount, building HP, research timer, unit order and unit identity separately. Detect every case and restore it without changing entity IDs or losing queued actions. Continue the match afterward.

**F07 — Command ordering and delay need redesign. Priority P1. Confirmed limitation.**

CommandBus executes commands in local insertion order. Two peers can enqueue their own command before the remote command, producing different order within one tick. There is no canonical player/sequence ordering in this class. It also imposes InputDelayTicks = 4 at 20 Hz in single-player, introducing roughly 150–200 ms of scheduled delay depending on frame/tick timing. [S5]

**Recommended change:** Add match ID, tick, player slot and monotonically increasing command sequence; define canonical order and explicit tick-completion semantics. Address late commands instead of leaving them in a bucket whose tick has already passed. Use immediate or next-tick local execution for offline play unless a measured design reason requires delay.

**Acceptance:** Opposite arrival orders yield the same authoritative execution order. Offline commands feel responsive and are measured from input to action acknowledgment and motion.

**F08 — Static progression can leak across matches. Priority P0 for repeatable single-player. Confirmed lifecycle gap.**

CivilizationSetup resets selected registries, but the progression dictionaries in UpgradeProgress and many unit lines expose only ResetForTests and are not reset in the production match-start path inspected. EconomyTechProgress and UniqueTechProgress also hold static research state. SaveManager restores some tiers by incrementing toward a target, which cannot reduce an already higher value. [S20]

**Recommended change:** Introduce an owned MatchState or a comprehensive production reset lifecycle. Reset command queues, progression, hero flags, fog cheats, diplomacy, IDs and random streams at the correct boundary. Prefer exact restoration to repeated increment APIs.

**Acceptance:** Research upgrades, finish a match, choose Play Again without restarting the application, and verify a clean opening. Load an older/lower-tier save afterward and verify exact restoration. Repeat with domain reload disabled in the Editor and in a standalone player.

**F09 — Fog changes simulation colliders; hostility rules disagree. Priority P1. Confirmed coupling, runtime effects require testing.**

FogOfWarManager.SetRenderersEnabled also disables all child Colliders based on local visibility. Gameplay uses colliders for selection, distance and placement queries. Viewer-dependent visibility should not alter canonical obstacle or hit geometry. StanceController.IsHostile checks only whether factions differ, while other combat paths use diplomacy-aware HostileFilter. [S16, S21]

**Recommended change:** Keep authoritative geometry stable. Filter selection/targeting using explicit visibility queries and separate selection hitboxes if needed. Centralize hostility checks, including alliance changes while an attack is already in progress.

**Acceptance:** Hiding an enemy changes presentation and allowed information, not physical occupancy. Allied units do not auto-acquire one another; a war-to-alliance transition cancels now-invalid attacks according to a documented rule.

**F10 — Map quality and starting-position rules are only partly integrated. Priority P1. Confirmed gaps.**

The latest ResourceNodeSpawner does use SkirmishMapZones.IsSpawnable for foliage classification, so the older note saying the zoning class is entirely unwired is stale. However, resource and relic placement still use a random ring. IsWithinStartingResourceRange has no runtime caller outside its definition. UnitSpawner places Player workers around the world origin, while the Player town center uses a map-specific position. [S13–S14, S22]

**Recommended change:** Spawn the opening settlement as one coordinated group. Guarantee accessible opening food/wood/gold, enforce edge and water restrictions for all gameplay resources, validate walkable approach positions, and distribute contested resources intentionally. Keep cosmetic randomness separate from strategic-resource generation.

**Acceptance:** Run a seeded map-validation batch, such as 100 initial seeds, recording unreachable resources, overlaps, start distances, opening travel time and contested-resource access. This is a proposed gate, not a claim that 100 seeds proves every possible layout. Test large-footprint units and wall/gate chokepoints.

**F11 — Navigation needs failure handling and naval routing. Priority P1. Confirmed limitations.**

UnitMover is essentially a SetDestination wrapper. GroupFormation/FormationController compute destination offsets; they are not a full group route-and-recovery system. WaterMover moves toward a clamped destination without boat avoidance or routes around obstacles. Its unrestricted per-frame movement step also warrants a low-frame-rate overshoot test. [S23]

**Recommended change:** Add destination projection, partial/invalid-path handling, repath budgets, movement intent, arrival tracking and stuck recovery. Let formations compress through chokepoints and reform after transit. Add water navigation if islands, bays or more complex coastlines become part of the scope; do not promise those maps while keeping a rectangle-only mover.

**Acceptance:** Mixed armies pass gates, cross each other, pursue moving targets, retreat through congestion and handle a newly placed blocker without permanent jams. Workers complete long gather/deposit runs from several directions. Boats do not overlap endlessly or cross land.

**F12 — Basic RTS control depth is narrower than the feature list suggests. Priority P1. Confirmed limitations/design decisions.**

TownCenter and Barracks use an active training slot/timer rather than a general multi-item production queue. The inspected selection path issues actions directly or schedules individual commands; I did not find a persistent per-unit shift-order queue there. The method named AttackMove in MeleeAttacker is target-oriented, not evidence of a conventional attack-ground command. StandGround returns without auto-engaging; that is a deliberate current rule but should be evaluated against the experience you want. [S6, S10, S21]

**Recommended change:** Define the control contract: queued movement/work, attack-ground, stop, hold position, cancel/refund, rally behavior and production queues. Implement one shared order state machine with explicit replacement/cancellation rather than extending manual cancel chains. Specify whether existing units receive tier promotions; the current spawn-baked line tiers and live-read blacksmith bonuses have different semantics.

**Acceptance:** Queue gather → build → move; cancel mid-action; destroy the target; exhaust the resource; fill the population cap during training; reopen the queue after capacity returns. Each case must produce a clear, consistent result without duplicate spending or lost work.

**F13 — AI does not exercise the full game. Priority P1 for single-player quality. Confirmed limitation.**

AiController is approximately 1,717 lines including comments. TryTrainSoldiers rotates through seven slots, rather than choosing composition from observed enemy threats. The code explicitly describes a scripted opponent with full internal map/economy knowledge and a separate scouting discovery gate. I found no relic-collection or support-unit strategy in this controller. [S24]

**Recommended change:** Keep the existing working behaviors and extract small decision modules: economy allocation, construction planning, production choice, scouting knowledge, defense, attacks/retreats and recovery. Feed them observations and goals. Use authored build orders plus simple utility scoring before considering learning-based AI.

**Acceptance:** The AI rebuilds a destroyed drop-off/production building, reallocates idle workers, responds to a cavalry-heavy threat, attacks vulnerable expansions, retreats from unfavorable fights, and can use selected support/relic mechanics. Difficulty should vary reaction time and decision quality; disclose any resource advantages separately.

**F14 — Building optimization has not reached a production budget. Priority P1, measure immediately. Documented risk with confirmed permissive test.**

BuildingPolycountTests allows 750,000 triangles per spawned building. Its comments describe decimation toward roughly 500,000 triangles because direct simplification damaged the generated geometry. This is a regression ceiling, not a demonstrated runtime target. As an illustration only, forty simultaneously visible 500,000-triangle buildings would represent 20 million triangles before terrain, units or additional rendering passes. Actual runtime counts and costs were not measured in this audit. [S25]

**Recommended change:** Stop treating the high-poly generated export as the final game mesh. Preserve it as a source, retopologize the silhouette and structural forms, bake relief into texture maps, author progressively cheaper LODs, reduce material proliferation and use simpler shadows where justified. Optimize representative common buildings before rare landmarks. Do not reduce every asset to the same arbitrary number.

A starting experiment could use roughly 5k–20k triangles for common building LOD0 and 20k–60k for major landmarks, with substantially cheaper distant LODs. These are trial budgets to validate against your camera and hardware, not fixed industry limits. Human models should be judged with rig/skin cost and visibility count as well as triangle count.

**Acceptance:** A representative settlement and battle meet CPU/GPU frame-time and memory targets on a named target PC while remaining readable at normal camera distance. Adjust budgets from those measurements. Unity supports explicit LODGroup setup; see the official references at the end.

**F15 — Combat presentation is a major gap to the intended experience. Priority P1. Confirmed implementation limitations.**

ArcherFactory uses the shared HumanAnimationSet; that set selects Sword_Regular_A as its Attack clip. MeleeAttacker applies direct timed damage without a projectile-flight representation in that path. AnimationDriver switches its output to a newly created AnimationClipPlayable without a blend and does not destroy the prior clip playable until the whole graph is destroyed. ConstructionSite scales the visual vertically to show progress. [S7, S26–S28]

**Why it matters:** A detailed model can still feel unfinished when archers use sword motions, attacks lack readable release/impact timing, transitions snap, and buildings stretch into existence. Repeated clip creation also needs lifecycle profiling.

**Recommended change:** Build action-specific presentation: bow draw/release, spear thrust, mounted attacks, siege loading/firing, worker jobs, death and damage feedback. Let authoritative combat emit events consumed by animation, audio and effects. Decide deliberately whether projectiles affect rules or are cosmetic; keep that distinction consistent. Cache/reuse playable nodes or use a bounded mixer/controller, then validate graph size over long sessions. Add construction stages or a suitable reveal effect with scaffolding and readable progress.

**Acceptance:** Observe each role from the gameplay camera. Attack type and ownership are recognizable without selection. Release/impact feedback matches the combat event; animation graph node counts remain bounded after repeated state changes.

**F16 — Asset readiness varies substantially. Priority P1. Inventory and integration findings.**

The repository has a shared combat-body prefab and sample Shared_Classical gear, but the default HumanModelFactory path still uses Human Character Dummy and I found no runtime reference to SharedCombatBody. TeamColorUnitTint is committed now, contrary to older workbook wording, but TryApplyTeamMask is called only by the Mavla Raider factory in the runtime source. The authored-mask helper therefore does not demonstrate roster-wide team tinting. BuildingModelFactory still supports procedural fallbacks; the expected bespoke Durg, Karmashala and Monastery prefab paths were not present in the reviewed asset tree. Specialized drop-off prefabs do exist, so their presence should not be described as missing. [S29–S30]

**Recommended change:** Maintain a production asset matrix keyed by definition ID: final model, UVs, materials, team mask, animations, LODs, portrait, command icon, destruction state, source/provenance and verified runtime path. Validate the actual factory spawn, not just the file in the asset browser. Finish the worker first because it appears constantly, then the most common combat roles and buildings.

**Acceptance:** Every unit in the chosen reference matchup has a distinct role silhouette and readable ownership. Every building has a valid final asset or an explicitly accepted temporary substitute; no silent fallback is counted as finished art.

**F17 — Sound and effects are basic; allocation/lifecycle risks need measurement. Priority P1–P2.**

The tree contains nine gameplay/UI .ogg effects and SfxPlayer offers corresponding generic playback. No dedicated soundtrack or voice layer was found. World sounds use PlayClipAtPoint. VfxFactory creates a GameObject, ParticleSystem and new Material per burst with no corresponding material cleanup visible in that class. Fog texture repaint creates a new Color32 array each update interval, and visibility repeatedly gets child renderer/collider arrays. Stance scanning and splash query global registries. [S16, S21, S31]

**Recommended change:** Profile before replacing systems. Likely targeted improvements include effect/audio pools, bounded voice counts, shared material ownership, cached renderer lists/pixel buffers and a spatial index for nearby-unit queries. Add differentiated weapon/impact sounds, command acknowledgments, alerts, settlement ambience and adaptive music after core correctness. Asset sourcing remains your/artists' responsibility under the existing project instructions; code tasks should integrate approved assets.

**Acceptance:** Large battles remain audible and readable; voices are capped/prioritized, memory stabilizes after repeated battles, and long sessions do not accumulate materials/playables unexpectedly.

**F18 — Tests and documentation overstate readiness in places. Priority P0 process change. Confirmed evidence mismatch.**

There are 79 EditMode test files, but no committed PlayMode test directory or GitHub Actions workflow in the complete tree. CommandBusDeterminismTests explicitly uses synthetic transform-delta commands rather than NavMesh movement. AiControllerNetworkGatingTests checks that Enemy AI disables without spawning, not that a complete two-human match has valid starting forces. These are useful narrow tests, but they cannot establish full-match determinism or playable LAN completion. [S32]

README still mentions two known building test failures while newer records say they were resolved. The workbook describes a corrupted Wall_Durg source; newer BuildingPolycountTests comments describe an importer-settings diagnosis and fix. Several older sheets still list completed tutorial/relic work as open. CLAUDE.md is about 407 KB and its engine section still says the Unity version is to be filled in. [S25, S33]

**Recommended change:** Replace a single Done flag with implementation, integration, automated validation, runtime validation, performance and art readiness. Include the verifying commit/build and artifact link. Keep concise current instructions in CLAUDE.md and chronological detail in SESSION_LOG; preserve the project's deliberate architecture decisions. Do not merge CombatBonus/CounterMatrix or migrate hardcoded formulas merely as a cleanup exercise.

**Acceptance:** A task cannot close as release-ready unless all applicable gates have evidence. A fresh checkout can reproduce the build and documented test results. Failed tests require a reproduced finding or a narrowly documented exception, not reliance on old status text.

---

**3. What to keep, finish, defer and reconsider**

| Keep and strengthen | Finish now | Defer during stabilization | Reconsider deliberately |
|---|---|---|---|
| CSV-to-definition pipeline | Complete entity catalog and save state | Additional civilizations and unit lines | Whether online multiplayer belongs in the first release |
| Runtime/editor/test assemblies | Match reset/bootstrap | New scenario-editor capabilities | Lockstep versus authoritative replication |
| Shared factories and logical/visual root split | Orders, queues and cancellation | Deathmatch/King of the Hill expansion until core gates pass | Same-civilization restrictions once team colors work |
| Economy/combat rules and useful tests | Local-faction presentation | Large new map packs | Tier upgrades affecting existing versus newly trained units |
| Terrain, water and clutter work | Representative art/animation set | Additional water polish before a performance baseline | Multi-era fantasy/historical sandbox framing |
| Existing scenario/tutorial foundation | Real onboarding playtest and build automation | More cinematic content | How much naval complexity the initial release needs |

Wonder was explicitly declined in your own roadmap. It is not a mandatory omission to repair. Fish traps, online matchmaking, reconnect and more than two human players should be treated as scope decisions, not prerequisites merely because another RTS has them. Do not delete existing content to narrow the reference milestone; simply limit where new polish work is applied first.

---

**4. Target architecture: incremental changes, not a restart**

Use the existing components as adapters while improving shared foundations in this order:

1. **MatchConfiguration:** player slots, chosen civilizations, map, seed, rules and content version. Initialization owns all starting forces independently of AI.
2. **DefinitionCatalog:** stable unit/building/tech IDs and their runtime creation/presentation bindings. Shared by training, scenarios, saves and tooling.
3. **MatchState:** exact authoritative progression, entities, resources, timers and match outcome, with a complete reset and versioned snapshot contract.
4. **Command pipeline:** validate identity and prerequisites, resolve typed target IDs at execution, execute in canonical order, expose cancellation/queue semantics.
5. **Simulation boundary:** chosen tick/authority model owns economy, movement, combat and production. Presentation cannot change simulation geometry or rules.
6. **Presentation:** renderers, animations, audio, particles and viewer-specific fog consume state and events.
7. **Diagnostics:** command logs, snapshot diffs, subsystem timings and validation tools.

Refactor along actual change pressure. For example, extract a reusable production queue from TownCenter and Barracks before splitting every file. BuildMenu's approximately 2,498 lines, Barracks' 1,222 and SelectionManager's 996 are signs to inspect responsibility boundaries, not proof that line count alone makes code bad. A natural direction is command definitions plus small view binders, a shared production/research service, and reusable order handlers.

Keep Unity and URP. The review does not establish an engine limitation that warrants migration. Likewise, consider Jobs/Burst/ECS only for measured bottlenecks and with a clear migration boundary.

---

**5. A systematic milestone plan**

The order is dependency-driven. Durations are provisional solo-developer planning ranges, assuming focused full-time work and prompt access to needed art. They are not a release estimate; revise them after the first baseline and representative asset are complete. Part-time availability, a networking redesign and outsourcing review time can extend them materially.

| Milestone | Suggested planning window | Deliverable | Required exit evidence |
|---|---|---|---|
| M0 — Reproducible baseline | 3–5 focused days | Fresh checkout instructions, LFS verification, named build, verified test baseline, current backlog | The intended branch opens and builds in the recorded Unity version; no undocumented missing dependency |
| M1 — Reliable match lifecycle | 2–4 weeks | Complete roster catalog, save/load, exact resets, coordinated start spawns | Full-roster round trip; multiple new matches in one process; workers start at their actual base |
| M2 — Reliable commands and movement | 2–4 weeks | Shared order semantics, queues/cancel/refund, offline response, path failures | Repeated worker cycles, crowd/chokepoint scenarios, valid cancellation and no stuck spend state |
| M3 — Representative performance and art | 4–8 weeks for the reference set, variable with art availability | Optimized common assets, role animations, team colors, measured scene | Named hardware benchmark plus gameplay-camera review; this work starts measurement during M0 and iterates throughout |
| M4 — Single-player match quality | 3–6 weeks | Better AI decisions, opening fairness, pacing, onboarding, audio clarity | Independent testers complete matches and understand why they win or lose |
| M5 — Multiplayer vertical slice, if retained | Re-estimate after an architecture prototype | Complete two-process command/state/perspective flow | Sustained two-process match under delay/frame-rate variation, then two physical PCs |
| M6 — Content completion and release candidate | Estimate from measured content throughput | Remaining faction/map art and regression coverage | Every declared supported feature passes the same gates; reproducible release build |

M3 overlaps other stages only as a production track; avoid simultaneous automated code-editing sessions in the same workspace. Your project history explicitly records damage from concurrent sessions. Network-specific design work must start early if multiplayer is a launch promise; M5 describes its quality gate, not permission to postpone all network architecture decisions.

**First ten actionable tickets**

1. Record the current build/test baseline and correct active-branch documentation.
2. Add a production match reset and a Play Again regression scenario.
3. Separate faction startup spawning from AI activation; align Player worker placement with map starts.
4. Create stable entity definition IDs and a complete runtime spawn catalog.
5. Extend the save schema and add a full-roster/state round-trip scenario.
6. Inventory every user command and identify direct calls that bypass the command layer.
7. Fix LAN receive dispatch, typed attack targets and local-faction HUD/fog/outcome.
8. Fix one-seed match configuration and all strategic world-generation consumers.
9. Build a representative battle/settlement benchmark and optimize one common building through the entire art pipeline.
10. Implement one complete ranged-unit presentation path with correct animation, release/impact feedback and bounded effect/playable lifetimes.

Execute these as small tickets with dependencies, not one large “fix all” instruction. The networking model decision should precede investing heavily in a deterministic movement rewrite.

---

**6. Define measurable quality gates**

Choose one minimum/target hardware specification, resolution, quality preset and camera path. A provisional target is 60 FPS at 1080p for a stated workload, but there is no measured evidence yet that your current project meets it. At 60 FPS the frame interval is about 16.7 ms. CPU and GPU pipelines must both be measured; do not simply add overlapping timings.

| Gate | Proposed scenario | Evidence to retain |
|---|---|---|
| Opening | Fresh match through first military production | Video/build ID, idle time, errors, resource progression |
| Economy | 50 then 100 workers across several resources | Delivery throughput, unreachable targets, allocations, main-thread time |
| Movement | 100 mixed units through gates and crossing crowds | Stuck count, arrival behavior, path-request cost |
| Combat | 200, then 400 total units as stretch workload | CPU/GPU median/p95/p99 frame times, memory, animation/effect cost |
| Settlement | Representative visible building count and wall length | Render statistics, shadow/material cost, LOD transitions |
| Persistence | Save during research, construction, fighting and relic delivery | Snapshot diff and successful continuation |
| Lifecycle | Ten restart/load cycles | Clean progression, stable entity counts and no monotonic memory growth after settling |
| Multiplayer | Two separate players with 30/60/144 FPS caps and induced stalls | Command log, seed/config hashes, state comparisons, outcome agreement |
| Usability | New player plays without developer instructions | Task completion, misunderstood controls, moments of confusion |

These are suggested workloads to expose risk, not claims about Age of Empires IV limits. Select the actual release unit cap and content scope after measuring. Profile a target-platform development player, then validate release-build behavior as well; Editor-only results are insufficient.

A first automated test set should cover high-value integration: full match bootstrap, worker gather/deposit, production queue/cancel/refund, complete save/load, rematch reset, local-faction perspective, network command dispatch and all supported definitions. Retain the existing EditMode unit suite, but add PlayMode and standalone-process checks. Do not replace valuable behavior tests with tests that merely assert the current implementation shape.

---

**7. Art direction and asset production for an Indian RTS**

Your current roster spans distinct historical contexts. Establish an explicit visual/design frame: a historically grounded cross-era sandbox, a narrower period, or deliberate alternate history. The current roster is not automatically wrong; the framing needs to make it intentional.

Create a short art bible for scale, silhouettes, regional architecture, dress/armor, materials, palette, lighting, terrain density, team-color placement and camera distance. Reference Age of Empires IV for readability and production finish while giving Kingdoms of Bharat its own recognizable choices.

Use this asset pipeline:

**Reference/concept → high-resolution source → game mesh/retopology → UVs and baked maps → materials → rig/animations → team mask → LODs → prefab/import validation → actual factory spawn → representative battle review.**

AI-generated geometry can accelerate concept and source production, but the repository's own decimation history shows that automatic simplification alone has not solved the runtime mesh problem. Prioritize a technical artist for retopology, UV/baking, rigs and animation cleanup if you choose to commission help. One finished asset family with a repeatable specification is more useful than a large delivery of inconsistent raw meshes.

For every asset, track author/source, usage terms, source files, target runtime path and replacement status. Where the project itself records unknown provenance, resolve it before release. This review did not independently evaluate asset licenses.

Create an in-engine reference scene with a worker, infantry, archer, mounted/large unit, common building, landmark, wall/gate, terrain, water and combat effects. Review at gameplay zoom, close zoom and reduced quality. Lock the standard before expanding to every civilization/age combination.

---

**8. Improve the Claude Code workflow**

Keep the existing one-scoped-task discipline. The problem is not that tasks are small; it is that completion can stop at one subsystem's boundary while the roadmap describes a complete player-facing feature.

For each ticket, require:

- Current behavior, expected behavior and a reproducible scenario.
- Affected systems and the source of truth for IDs/data.
- Existing interfaces to preserve and explicit non-goals.
- Tests appropriate to the risk, plus a real gameplay verification route.
- Save/load, restart, AI, UI, networking and performance applicability.
- Changed files, actual verification output and limitations.
- One scoped commit and a status update tied to that commit.

A feature completion table should contain: implementation status, player integration, AI integration if applicable, persistence, networking if supported, automated evidence, runtime evidence, performance and asset readiness. Mark unsupported cases explicitly rather than silently calling the whole feature done.

Reduce CLAUDE.md to durable rules, the current engine/package versions, exact build/test instructions, architecture decisions and links to current work. Its approximately 407 KB of chronological detail duplicates SESSION_LOG and makes stale decisions easier to repeat. Archive history rather than deleting it. Keep the deliberate CombatBonus/CounterMatrix split and hardcoded-progression rules unless a separate migration is approved and validated.

For dependencies, document reproducible package resolution and keep packages-lock.json. The manifest points Unity MCP at #main and MeshSimplifier at an unpinned Git URL; use known revisions when updating deliberately. Do not claim this necessarily breaks today's locked install, but avoid accidental tool changes during a gameplay task.

For assets, use Git LFS checkout and verify required payloads before testing. Building files are intentionally LFS-managed, including prefab data. A pointer file is not the model. Source asset packs and demo content currently live under Resources in several places; audit build inclusion and move unused examples out only after checking runtime paths and dependencies. Unity includes Resources assets in player builds even when no scene references them.

**Example first Claude task, ready to adapt:**

> Audit and fix production match-state reset only. Read the current project conventions and relevant progression registries. Reproduce a match in which research is completed, then start a new match in the same application process. Identify every state owner that survives and should reset. Propose the smallest explicit reset lifecycle, implement it, and add a meaningful PlayMode regression covering the normal restart path. Preserve current combat balance and data ownership. Do not add new units or change rendering. Report exact tests run, the standalone/manual verification performed, and anything that could not be verified. Update the current tracker using the actual verifying commit.

Follow with separate catalog, persistence and bootstrap tickets. Do not ask Claude to regenerate the whole project or “make it like AoE IV” in one pass.

---

**9. What success should look like next**

Your next milestone should be a playable, externally testable reference skirmish using existing content. It should have:

- Reliable opening spawns, resource access and responsive controls.
- A complete economy and production loop with understandable queues and cancellation.
- An AI capable of finishing and recovering within the selected matchup.
- Save/load and repeated matches that preserve/reset the correct state.
- Distinct team/role silhouettes, appropriate attack animations and readable feedback.
- A documented performance result on the chosen hardware.
- A reproducible build plus a small set of independent playtest observations.

Only then expand the quality standard across the remaining roster and maps. Keep multiplayer explicitly experimental until a full two-process match satisfies the command, state, initialization and perspective gates. A second physical PC is necessary for broader validation, but it is not the only thing currently standing between this implementation and reliable multiplayer.

---

**Evidence index — exact reviewed source**

Links below are pinned to the audited commit. They support static observations, not claims that this reviewer ran Unity.

[S1]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Core/SaveManager.cs
[S2]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Core/SaveData.cs
[S3]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Core/EntitySpawner.cs
[S4]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Multiplayer/SimClock.cs
[S5]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Multiplayer/CommandBus.cs
[S6]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Buildings/TownCenter.cs
[S7]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Combat/MeleeAttacker.cs
[S8]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Multiplayer/NetworkDriver.cs
[S9]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Multiplayer/CommandSerializer.cs
[S10]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Selection/SelectionManager.cs
[S11]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/UI/BuildMenu.cs
[S12]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Multiplayer/LanMatchMenu.cs
[S13]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Resources/ResourceNodeSpawner.cs
[S14]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Core/CivilizationSetup.cs
[S15]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/UI/ResourceHUD.cs
[S16]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/FogOfWar/FogOfWarManager.cs
[S17]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Match/MatchManager.cs
[S18]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Multiplayer/StateHash.cs
[S19]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Multiplayer/NetworkId.cs
[S20]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Progression/UpgradeProgress.cs
[S21]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Combat/StanceController.cs
[S22]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Core/SkirmishMapZones.cs
[S23]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Units/WaterMover.cs
[S24]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/AI/AiController.cs
[S25]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Tests/EditMode/BuildingPolycountTests.cs
[S26]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Combat/ArcherFactory.cs
[S27]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Units/HumanAnimationSet.cs
[S28]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Units/AnimationDriver.cs
[S29]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Units/HumanModelFactory.cs
[S30]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Core/TeamColorUnitTint.cs
[S31]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Vfx/VfxFactory.cs
[S32]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Tests/EditMode/CommandBusDeterminismTests.cs
[S33]: https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/CLAUDE.md

| Reference | Evidence location |
|---|---|
| S1–S3 | [SaveManager][S1], [SaveData][S2], [EntitySpawner][S3] |
| S4–S7 | [SimClock][S4], [CommandBus][S5], [TownCenter][S6], [MeleeAttacker][S7] |
| S8–S11 | [NetworkDriver][S8], [CommandSerializer][S9], [SelectionManager][S10], [BuildMenu][S11] |
| S12–S14 | [LanMatchMenu][S12], [ResourceNodeSpawner][S13], [CivilizationSetup][S14] |
| S15–S19 | [ResourceHUD][S15], [FogOfWarManager][S16], [MatchManager][S17], [StateHash][S18], [NetworkId][S19] |
| S20–S24 | [UpgradeProgress][S20], [StanceController][S21], [SkirmishMapZones][S22], [WaterMover][S23], [AiController][S24] |
| S25–S30 | [BuildingPolycountTests][S25], [ArcherFactory][S26], [HumanAnimationSet][S27], [AnimationDriver][S28], [HumanModelFactory][S29], [TeamColorUnitTint][S30] |
| S31–S33 | [VfxFactory][S31], [CommandBusDeterminismTests][S32], [CLAUDE.md][S33] |

Additional reviewed evidence: [master-reference workbook](https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/docs/KingdomsOfBharat_Master_Reference.xlsx), [UnitSpawner](https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Units/UnitSpawner.cs), [TownCenterSpawner](https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Buildings/TownCenterSpawner.cs), [Save resync adapter](https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Multiplayer/DesyncRecovery.cs), [AI gating tests](https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Tests/EditMode/AiControllerNetworkGatingTests.cs), [SfxPlayer](https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Audio/SfxPlayer.cs), [ConstructionSite](https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Buildings/ConstructionSite.cs), [BuildingModelFactory](https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Assets/Scripts/Buildings/BuildingModelFactory.cs), [manifest](https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Packages/manifest.json), [package lock](https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/Packages/packages-lock.json), [Unity version](https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/ProjectSettings/ProjectVersion.txt), [build scene list](https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/ProjectSettings/EditorBuildSettings.asset), [.gitattributes](https://github.com/bhoomisanghavi10-bit/unity-rts/blob/49a7e20397bdf32f0d5edd10c5642b480bf13969/.gitattributes).

Official technical references checked for recommendations:

- [Unity: Resources assets included in builds](https://docs.unity.com/en-us/engine/6000.0/manual/building-and-publishing/build-content-output).
- [Unity 6000.3: configure LOD levels](https://docs.unity.com/en-us/engine/6000.3/manual/analysis/graphics-performance-profiling/lod/lod-group/lod-group-configure).
- [Unity: profile a target-platform player](https://docs.unity.com/en-us/engine/6000.5/manual/analysis/profiler/profiling-applications/profiling-target-device). UI details should be checked against the project's pinned 6000.3 editor.

No repository files were changed, no commits were created, and no runtime success is claimed by this report.

---

## Implementation status (verified results)

| Ticket | Status | Evidence |
|---|---|---|
| Prompt 8 — player command pipeline audit | Done (inventory + 1 small fix) | `docs/COMMAND_COVERAGE.md`. Fixed `NetworkDriver.Dispatch` dropping TradeRoute/Heal/Convert. EditMode batch run (`CommandSerializerTests|CommandBusDeterminismTests|NetworkIdTests|TrainingAndTradeTests`): 30/30 passed, incl. new `ToCommand_AbilityKinds_StaleIds_ReturnNullWithoutThrowing`. Not verified: PlayMode/two-process dispatch, full EditMode suite. Verifying commit: see git log for "Audit player command pipeline". |
