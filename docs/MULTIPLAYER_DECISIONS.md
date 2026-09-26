# LAN multiplayer: authority and simulation model (decision note)

Scope: the existing 2-player LAN foundation only. No matchmaking, relay, lobbies or >2 players.

## Current model (what the code actually does)
- **Both peers run the full simulation.** Each machine executes the same scene, spawners, AI-free economy and combat locally. There is no server world and no client-side prediction: a client never "displays" host state; it computes its own.
- **Player input is turned into commands** (`Command` subclasses) that are scheduled on `CommandBus` for `SimClock.CurrentTick + 4` and sent to the peer as `NetMessageEnvelope` JSON over TCP. Both peers execute every command at the same tick.
- **Deterministic lockstep is the intent, and only partly achieved.** `SimClock` gates tick advance on the peer's acknowledged tick (real lockstep pacing), but gameplay itself (movement, NavMesh agents, gather/combat/training timers) runs on frame-driven `Update()` with `Time.deltaTime`, not on ticks. Identical commands therefore do not guarantee identical state, so the model is **command-synchronised replicated simulation with divergence detection**, not true lockstep.
- **The host owns:** the match configuration and seed (broadcast in `HostHello`), and resynchronisation - on a `StateHash` mismatch the host sends a `SaveManager` snapshot that the client applies.
- **Clients own:** their own faction's inputs. `NetworkMatch.LocalFaction` decides whose orders this process may issue and whose resources/fog/victory it shows.
- **Ordering (after this change):** commands execute per tick in canonical order `(faction slot, per-sender sequence)`, independent of arrival order. Duplicate or replayed sequences and commands whose envelope faction is not the remote peer are rejected. Commands arriving for an already-executed tick are rejected and reported (they would desync).
- **Identity:** units, buildings and resource nodes are addressed by `NetworkId` (spawn-order integers, identical on both peers given the same seed and creation order). An id that fails to resolve is reported once per rate window, never silently dropped.

## Decision
Keep the architecture and label it **experimental**. Do not claim lockstep. Making the simulation tick-driven and NavMesh-deterministic is a separate design decision and is not attempted here.

## Known blockers to "multiplayer complete"
Orders not yet networked: build-assist, farm/livestock, repair, garrison, relic, rally, stance, research/age-up, market, tribute, boat move, cancel-queue. Simulation is frame-driven. Fog/HUD/victory now follow the local faction, but nothing has been validated across two separate processes.
