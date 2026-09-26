# Player Command Coverage (audit, 2026-09-26)

Scope: inventory only. Source of truth is the code at the commit that adds this file.
Legend — **Route**: `Bus` = `CommandBus.Enqueue` (4-tick input delay) ; `Direct` = method called from UI/input immediately.
**Net** = serialized by `CommandSerializer` *and* dispatched by `NetworkDriver`. **AI** = AI uses the same route (it never does: `AiController` calls the component methods directly, no `CommandBus`).
Bypass class: **SP** = matters for the single-player reference skirmish, **MP** = multiplayer-only, **OBS** = obsolete.

## Input handlers
| Command | Entry point | Validation | Execution | Target type | Route | Net | Cancel/refund | Tests |
|---|---|---|---|---|---|---|---|---|
| Move | RMB ground, `SelectionManager.HandleMoveInput` | `IsPlayerControllable`, has `UnitMover` | `MoveCommand` -> `UnitMover.MoveTo` | Unit (NetworkId) + Vector3 | Bus | Yes | cancels other tasks via per-component `Cancel*` (Direct, at click time) | CommandSerializer, CommandBusDeterminism |
| Move (boats) | RMB ground | none | `WaterMover.MoveTo` | Unit | **Direct** | **No** (`ToMoveCommand` requires `UnitMover`) | none | none |
| Attack / attack-move (melee, boat) | RMB hostile | `IsHostileTarget` | `AttackCommand` -> `AttackMove` | Unit or Building attacker; **target must be Unit** on the wire | Bus | Partial: building targets serialize as id -1 and are dropped on receive | `CancelAttack` | CommandSerializer |
| Ranged attack | same path (`MeleeAttacker` with range) | same | same | same | Bus | Partial | same | — |
| Heal / Convert | RMB damaged friend / hostile | friend+damaged / `CanConvert` | `AbilityCommand` | Unit | Bus | Yes — **fixed here** (was never dispatched) | `CancelHeal/Convert` | new stale-id test |
| Trade route | RMB Market/Dock | friendly | `TradeRouteCommand` | Unit + Building | Bus | Yes — **fixed here** | `CancelRoute` | new stale-id test |
| Gather (land/boat) | RMB resource node | none beyond hit test | `Gatherer/BoatGatherer.GatherFrom` | ResourceNode (no id type) | **Direct** | **No** | `CancelGather` | Gatherer tests |
| Construct assist | RMB own site | `IsSameFaction` | `Builder.BuildAt` | ConstructionSite | **Direct** | **No** | `CancelBuild` | ConstructionSite tests |
| Farm / livestock staff | RMB Farm / animal | faction | `StaffAt` | Farm / Livestock | **Direct** | **No** | `CancelWork` | Farm tests |
| Repair | RMB damaged own building/unit | faction | `Repairer.RepairAt` | Repairable | **Direct** | **No** | `CancelRepair` (cost paid per HP, no refund) | Repair tests |
| Garrison | RMB own GarrisonPoint | faction | `GarrisonSeeker.GarrisonAt` | Building/Unit | **Direct** | **No** | n/a | GarrisonPoint tests |
| Relic pickup | RMB relic | not held | `RelicCarrier.PickUp` | Relic | **Direct** | **No** | `CancelCarry` | RelicTests |
| Deposit | automatic in `Gatherer` state machine | — | in-component | — | not a command | n/a | n/a | Gatherer tests |
| Rally point | RMB with building selected | — | `RallyPoint.SetPoint` | Building + Vector3 | **Direct** | **No** | n/a | none |
| Stance cycle (V) | `HandleStanceHotkey` | has `StanceController` | `CycleStance` | Unit | **Direct** | **No** | n/a | StanceController tests |
| Ungarrison all | `BuildMenu.UngarrisonAtSelected` | count>0 | `UngarrisonAll` | Building | **Direct** | **No** | n/a | GarrisonPoint tests |
| Town Bell | button/F8 | — | `TownBell.Ring` | Faction | **Direct** (documented local-only) | No | n/a | TownBellTests |
| Place building | `BuildingPlacer.TryConfirmPlacement` | pre-check + re-check in `ExecuteBuild` | `BuildCommand` | position + kind | Bus | Yes (Build) | cost deducted at execute, none refunded on later cancel | CommandBusDeterminism |
| Train (all `EnqueueTrain` sites) | BuildMenu buttons/hotkeys | in `RequestTrain*` at execute | `TrainCommand` | Building | Bus | Yes via `NetTrainKind` (Market/Monastery/Dock/Barracks/Durg/TC) | **Town Center + Barracks (Prompt 9):** shared `ProductionQueue`, 5 slots, cancel-last (HUD button/Backspace, via `CommandBus`, refund 100%), refund on building destruction, not serialized for LAN (button disabled in LAN). Dock/Market/Monastery/Durg still single-slot, no cancel | TrainingAndTrade, ProductionQueueTests, ProductionQueuePlayModeTests |
| Research (Barracks/Dock/Durg/Karmashala tiers, unique tech, elite) | BuildMenu | in `RequestResearch*` | called directly | Building | **Direct** | **No** | none | per-line tests |
| Age advance / Economy tech | `RequestAgeUpAtSelected`, `ResearchEconomyTechAtSelected` | UI pre-check + method | `TownCenter.RequestAgeUp/...EconomyTech` | Building | **Direct** | **No** | none | AgeUpRequirement tests |
| Market buy/sell | BuildMenu | in `Market` | `Market.Sell/Buy` | Market | **Direct** | **No** | n/a | TrainingAndTrade |
| Tribute | DiplomacyMenu | in `Tribute.Send` | direct | Faction | **Direct** | **No** | n/a | Tribute tests |
| Cheat console | `CheatConsole` | refuses when `NetworkMatch.IsActive` | direct | — | Direct by design | n/a | n/a | Cheat tests |

## Not present in the codebase
No Stop, Hold-position command, Patrol, shift-queued orders, production queue cancel, or research cancel exists. "Hold position" is only `UnitStance.StandGround`. Any such expectation is a missing feature, not a bypass.

## Save/load and orders
`UnitSaveData` stores type, faction, position, health, stance, networkId. **No active order is saved** (gather/attack/build/route/garrison/repair state is dropped); units resume idle. In-flight `CommandBus` entries are also not saved. Town Center/Barracks training queues (all items, remaining time, paid cost) are saved and restored (Prompt 9). Research timers other than Infantry tier: not saved.

## AI
`AiController` calls `RequestTrain*`, `RequestAgeUp`, `RequestResearch*`, `GatherFrom`, `BuildAt`, `MoveTo`, `AttackMove` directly, never through `CommandBus`. Consequence: AI actions execute immediately while equivalent player actions are delayed 4 ticks; fine offline, and the AI is disabled for the Enemy faction in LAN matches.

## Bypass findings and practical effect
1. **Player Direct orders (gather, build assist, farm, livestock, repair, garrison, relic, rally, stance, ungarrison, research, age-up, market, tribute, boat move)** — SP: works, executes with zero delay (inconsistent feel vs Move/Attack, and cancel-then-act ordering vs queued orders is unspecified). MP: never reach the peer -> desync. Classification: MP-only defect.
2. **AI direct calls** — SP: expected; not a defect. MP: N/A (AI disabled for human Enemy slot).
3. **Attack on a building over the wire** — MP-only; target id is -1 for buildings.
4. **`NetworkDriver.Dispatch` dropped TradeRoute/Heal/Convert** — MP-only; **fixed in this ticket**.
5. **Refund/cancel** — Town Center/Barracks training now cancellable with refund (Prompt 9); still absent for other production buildings, research, and placed sites.
6. **Unsaved orders** — SP-relevant on load.
7. Obsolete: none identified.

## Fix made in this ticket
`NetworkDriver.Dispatch` now routes `TradeRoute`, `Heal`, `Convert` through `CommandSerializer.ToCommand` (the serializer already handled them).

## Recommended follow-ups
1. Command-per-action tickets for Direct player orders: gather, build-assist, repair, garrison, rally, research/age-up (with typed ResourceNode/ConstructionSite/Farm ids in `NetMessageEnvelope`).
2. Typed target ids (unit/building/resource) so building attack targets survive the wire.
3. Cancel + refund for training/research/placed sites, with tests for spend-state.
4. Persist active orders in `UnitSaveData`.
5. Decide whether offline play keeps the 4-tick delay (audit F07) and whether the AI should share the command route.
6. Two-process test that dispatches each `NetMessageKind` (Dispatch is private and untested).
