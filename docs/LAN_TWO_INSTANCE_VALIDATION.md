# LAN two-instance validation (repeatable procedure)

Status: **not yet run.** Everything in the current test suite is code-level or single-process. This document is the procedure and tooling for the real two-process check.

## Setup
1. Build a Mac standalone player (or use one Editor plus one player - two Editors cannot open the same project).
2. Start both processes with a validation log each:
   ```
   ./KingdomsOfBharat.app/Contents/MacOS/KingdomsOfBharat -lanlog /tmp/kob_host.log
   ./KingdomsOfBharat.app/Contents/MacOS/KingdomsOfBharat -lanlog /tmp/kob_join.log
   ```
   (or set `KOB_LAN_LOG=<path>`). Logging is off without the flag.
3. Process A: LAN Match panel (top-left) -> pick civilization/map -> Host. Process B: enter A's address -> Join. Both start the match automatically once each has the other's hello.

## Flow to exercise (each side, through the real HUD)
1. Confirm each screen shows its own faction's resources, Town Center and fog-revealed start.
2. Move units, attack, train (Town Center + Barracks), place a building, order workers to gather.
3. Let it run several minutes; issue commands from both sides at about the same time.
4. Quit both, then compare:
   ```
   python3 Tools/compare_lan_logs.py /tmp/kob_host.log /tmp/kob_join.log
   ```
   Exit 0 means: identical configuration hash/seed/map, identical executed-command logs (canonical tick/faction/sequence order) and identical state hashes at every shared checkpoint (every 20 ticks). Exit 1 prints the first divergence.

## What the logs contain
`CFG <hash> seed <s> map <m> local <faction>` once at start; `CMD <tick> <faction> <seq> <Type>` per executed command; `HASH <tick> <h>` every 20th tick. Rejected commands (duplicate, late, wrong sender, unresolved id) and configuration faults are counted in `NetworkDiagnostics` and logged as `[Network]` warnings/`[NetworkMatch] FAULT`.

## Pass criteria
- compare script exits 0 after a multi-minute session with commands from both sides;
- no `[Network]` warnings other than expected, no FAULT;
- no desync/resync warnings in either console.

Because gameplay is frame-driven (see MULTIPLAYER_DECISIONS.md) state-hash divergence is plausible even when commands match; if so, that is a finding, not a harness bug.
