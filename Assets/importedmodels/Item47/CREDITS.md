# Item 47 art pass — third-party model credits

All models below are licensed **CC Attribution (CC-BY 4.0)** —
commercial use is allowed, but the author must be credited. This file
is that credit. Keep it up to date if more models are added here.

| Model | Author | Source |
|---|---|---|
| AoE2Barracks | Tommabri | https://sketchfab.com/3d-models/none-24fd547e44894225b98d3bc4a52c07c8 |
| AoE2TownHall | Heferum Games | https://sketchfab.com/3d-models/aoe-ii-townhall-b32bd5845e034464ba42aa484bc818e5 |
| TowerAndCastleWalls | LucasPresoto | https://sketchfab.com/3d-models/none-fca85f8450604e6f98952a2739f92672 |
| MedievalMarketStall | AspectStudios | https://sketchfab.com/3d-models/none-b5d87398b87a4e7ab8959da2c6a47056 |
| MedievalTavern | Katydid | https://sketchfab.com/3d-models/none-2a1669778d1b4b27b899ff9f42b7438f |
| MedievalBackAlley | Katydid | https://sketchfab.com/3d-models/none-661535a75f274a2cb762fe347f76afd8 |
| TemplarKnight | everlasting17th | https://sketchfab.com/3d-models/none-e57edad1bb8e4dbd8573f85695ceecde |
| HospitalierKnight | everlasting17th | https://sketchfab.com/3d-models/none-6a6657a61d694fa59700da7f05d0219b |

## Phase 5 gap-close (2026-08-25) — Wall/Gate real models

Also **CC Attribution (CC-BY 4.0)**, sourced to close the last 2 of the
Farm/Wall/Gate/Market "still procedural fallback" gaps (Farm and Market
turned out to already have real models from the Item 47 pass above -
only Wall and Gate were genuinely missing).

**Superseded twice same-day, both Sketchfab picks below.** First round -
after an in-editor visual check caught a scale mismatch: "Wall 1
low-poly"/"New Castle Door" turned out to be a whole multi-crenellation
rampart and a full 2-tower gatehouse respectively (4-27x oversized).
Second round - replaced with `wall_single`/`door_arc` (extracted from a
generic low-poly props pack), a real improvement on scale but plain/
untextured compared to the rest of the game's buildings. Both left in
this table for provenance/history, neither currently used in
`Assets/Resources/buildings/`.

| Model | Author | Source |
|---|---|---|
| ~~Wall (Wall 1 low-poly)~~ superseded | chrismasmanidis3 | https://sketchfab.com/3d-models/wall-1-low-poly-24adfebb64714e80b9c8f0a5eaeaed03 |
| ~~Gate (New Castle Door)~~ superseded | farooq.smurf | https://sketchfab.com/3d-models/none-c17775043b0a485e844d5f7a2aacce24 |
| ~~Wall (wall_single, Lowpoly Medieval Game Assets Set)~~ superseded | insectscorch (3Dimentional) | https://sketchfab.com/3d-models/none-b6de2aa3b6c1490692f8fccfefcb25f7 |
| ~~Gate (door_arc, Lowpoly Medieval Game Assets Set)~~ superseded | insectscorch (3Dimentional) | https://sketchfab.com/3d-models/none-b6de2aa3b6c1490692f8fccfefcb25f7 |

License terms: http://creativecommons.org/licenses/by/4.0/

## Phase 5 gap-close, final pass (2026-08-25) — Wall/Gate via Unity Asset Store

User added and imported the **"Medieval Castle" (Advance Studios)** pack
from the Unity Asset Store directly (`Assets/Advance Studios/Medieval
Castle/`) - not sourced by this session, so governed by that pack's own
Asset Store EULA rather than a CC-BY attribution requirement; no
CREDITS.md entry needed the way Sketchfab imports get one. Used the
pack's own `Prefabs/Wall.prefab` directly (2.00×2.13×0.77, an excellent
native match for the intended 2.4×1.8×0.4 footprint - no rescale needed)
and `Prefabs/Double Door Frame.prefab` scaled ×2.878 uniformly to match
Wall's height (1.62×2.13×0.56 final). Both wired into
`Assets/Resources/buildings/Wall.prefab`/`Gate.prefab` via
`PrefabUtility.SaveAsPrefabAsset`, replacing the two superseded Sketchfab
attempts above.

**Found and fixed a real compatibility bug along the way:** the pack's
10 materials all use Unity's legacy Built-in-RP "Standard" shader, but
this project runs URP - rendered as solid magenta ("missing shader")
until converted. Fixed directly via the Material API (`shader =
Universal Render Pipeline/Lit`, `_MainTex` → `_BaseMap`, `_Color` →
`_BaseColor`) rather than the Editor's built-in Standard→URP menu
converter, which wasn't reachable by exact menu path across Unity
versions in this environment. Verified via reflection in Play mode
through the real `WallFactory`/`GateFactory` spawn path: correct bounds,
no console errors, and a `manage_camera` screenshot confirming real
stone-brick/wood-door textures render correctly (not magenta) - a proper
crenellated wall segment and a stone-trimmed door archway, both properly
civ-tinted and appropriately scaled next to TownCenter.
