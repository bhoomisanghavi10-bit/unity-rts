# Representative benchmark run — 2026-09-26

Status: **not completed**. This is a compact run record, retained so the failed build is not mistaken for a performance measurement.

| Field | Value |
| --- | --- |
| Scenario | `representative_battle_v1` |
| Map / seed | `RiverValley` / `20260926` |
| Requested player | macOS Development player, profiler connection enabled |
| Requested display | 1920×1080 windowed |
| Quality | recorded by the player at runtime; player did not launch |
| Unity | 6000.3.21f1 (`c02631ffc030`) |
| Build commit | `b1262c5` (working tree was already dirty) |
| Hardware | MacBook Air, Apple M4 (10 CPU cores, 8 GPU cores), 16 GB unified memory |
| OS | macOS 26.5.2 (25F84) |

The headless player build compiled scripts but failed before producing an `.app`: Unity repeatedly reported `Failed to write temp shader cache entry to Library/ShaderCache/temp/...`. No standalone player ran, so CPU/GPU percentiles, memory, allocations, batches, triangles, visible skinned meshes, and bottleneck ranking are intentionally absent rather than estimated from Editor data.

After reclaiming space for Unity's shader cache, rerun the command in [REPRESENTATIVE_BENCHMARK.md](REPRESENTATIVE_BENCHMARK.md). The player writes the requested compact JSON and exits after its 15-second warm-up plus 900 steady-state frames.
