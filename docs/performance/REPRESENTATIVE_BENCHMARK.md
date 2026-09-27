# Representative Benchmark

`representative_battle_v1` is a repeatable **Development-player diagnostic**. It is not a shipping benchmark and should not be compared directly with the earlier Editor snapshot.

The workload uses the `RiverValley` map, seed `20260926`, and a 24-second deterministic camera orbit. It creates two settlements, each with a Town Center, Barracks, House, Farm, and Market, then sustains a 24-unit battle: each side has 2 Workers, 3 Padati, 3 Dhanurdhara, 2 Cavalry, and 2 Siege units. The Worker, Padati, Dhanurdhara, Town Center, and Barracks use their catalog IDs; Cavalry, Siege, House, Farm, and Market retain their existing factory paths to represent the mixed migration state.

The player warms for 15 seconds so startup, terrain/navmesh setup, asset import, and shader warm-up do not enter the results. It then holds at most 900 frame samples in memory, writes aggregate percentiles only, and exits. It never starts a binary Profiler recording.

## Reproduce

1. Open the project in Unity `6000.3.21f1` and let script compilation finish.
2. Use **BharatRTS → Build Representative Benchmark Player**. This builds `Builds/RepresentativeBenchmark/KingdomsOfBharatBenchmark.app` as a macOS Development player with profiler connection enabled.
3. Run the executable directly, using a fixed display mode and an explicit result path:

   ```sh
   Builds/RepresentativeBenchmark/KingdomsOfBharatBenchmark.app/Contents/MacOS/KingdomsOfBharatBenchmark \
     -representative-benchmark -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 \
     -benchmark-output "$PWD/docs/performance/representative-benchmark-result.json"
   ```

4. Record the hardware, OS, Unity version, quality preset, resolution, and `git rev-parse HEAD` alongside the JSON result. The runtime JSON contains the preset, resolution, Unity version, platform, warm-up, sample counts, and median/p95/p99 summaries. `docs/performance/representative-benchmark-run-2026-09-26.md` records the first run.

The GPU, texture-memory, batch, and triangle counters are queried by `ProfilerRecorder`. A counter is explicitly marked unavailable in the compact JSON when that player/platform does not expose its marker; it is never substituted with a misleading editor-only value.
