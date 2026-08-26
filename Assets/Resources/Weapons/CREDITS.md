# Weapons — third-party model credits

**CC Attribution (CC-BY 4.0)** — commercial use is allowed, but the
author must be credited. This file is that credit for this session's own
Sketchfab import.

| Model | Author | Source |
|---|---|---|
| Spear (extracted from "Spear Infantryman") | Avijoy.L | https://sketchfab.com/3d-models/spear-infantryman-faa173d2494c48fb84f66c1bde28530c |

License terms: http://creativecommons.org/licenses/by/4.0/

## Spear extraction note

The full "Spear Infantryman" import (`scene.gltf`, 30 sub-meshes, no
animations) wasn't used directly - same rig/animation-compatibility risk
this project already flagged for the 2 unused Crusader Knight body swaps
(item 47). Only its spear mesh (identified by isolating each of the 30
renderers and confirming visually - a single long thin mesh matching the
full-figure silhouette's spear) was extracted out to its own standalone
prefab (`Assets/Resources/Weapons/Spear/scene.prefab`) and attached to
the existing shared Human Character Dummy body via `WeaponAttachment`,
the same way Sword/Bow/Kanabo already are. The full import is kept for
provenance at `Assets/importedmodels/SpearInfantryman/` (outside
`Resources/`, since 71 mostly-unused textures and a 346k-face mesh have
no reason to bloat a build - only the single extracted spear mesh needs
to live under `Resources/` where `WeaponAttachment` can `Resources.Load`
it). Mesh/material references between the extracted prefab and the
source import stayed intact across that move (Unity resolves them by
GUID via the `.meta` files, not by path).

## Note on Sword/Bow/Kanabo/Mounts

Those weapon/mount props predate this file and this session - their
original Sketchfab source/author isn't recorded here. If their CC-BY
terms require attribution, that credit still needs to be tracked down
and added; flagging rather than silently leaving it undocumented.
