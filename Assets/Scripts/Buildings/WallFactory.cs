using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Wall segment (Wall + ConstructionSite + FactionMember +
    // NavMeshObstacle). Mirrors BarracksFactory/FarmFactory's shape - used
    // by BuildingPlacer for the Player's mouse-driven placement. Meant to
    // be placed edge-to-edge one segment at a time (see BuildingPlacer's
    // smaller wallClearance) to form a line, since this project doesn't yet
    // support AoE's click-drag multi-segment chain placement.
    public static class WallFactory
    {
        // Ancient-age modular wall kit (2026-09-28): straight run + 2
        // standalone junction pieces (Corner/EndPost), all mechanically
        // identical Wall buildings (same HP/armor/garrison/repair/
        // fortification bonuses) - only the visual resourceName and
        // gameplay-footprint Size differ per piece. See PieceResourceName/
        // PieceSize below and BuildingPlacer's wall-piece-variant selector
        // (Alpha1-5 while placing a Wall). No dedicated T/X-junction art
        // (2026-09-29, user's explicit call, matching real AoE II - a 3+
        // way junction just reuses the Corner piece, see
        // WallConnectivity.ClassifyPieceKind).
        public enum WallPieceKind { Straight, Corner, EndPost }

        // Straight segment height was a stale 1.8 (shorter than the ~1.9
        // worker unit) until the low-poly Ancient kit landed - now matches
        // the kit's own real proportions (see
        // project_lowpoly_asset_size_conventions memory: straight 3.0-3.5).
        private static readonly Vector3 Size = new Vector3(2.4f, 3.2f, 0.4f);

        // Corner/EndPost pieces read as turret caps rising above the
        // wall line (AoE II convention) - taller than the straight run,
        // per the same memory's table. The gameplay footprint deliberately
        // stays a single wall-tile square (2.4x2.4), not the piece's full
        // visual bounding box - each piece's arms/turret are cosmetic
        // dressing that visually reaches into the neighboring tile a
        // straight segment would otherwise occupy, same idea as a modest
        // roof overhang on any other building; a single NavMeshObstacle box
        // sized to the true L/T/X silhouette would over-block the notch
        // between arms, so the obstacle only ever covers the pillar's own
        // tile, matching this project's existing "square box, not exact
        // shape" footprint convention everywhere else.
        private static readonly Vector3 JunctionSize = new Vector3(2.4f, 6f, 2.4f);

        private const float MaxHealth = 250f;

        private static string PieceResourceName(WallPieceKind kind)
        {
            return kind switch
            {
                WallPieceKind.Corner => "Wall_Ancient_Corner",
                WallPieceKind.EndPost => "Wall_Ancient_EndPost",
                _ => "Wall",
            };
        }

        // Classical age wall kit (2026-09-28, real Meshy glb delivery
        // superseding the same-day flat-sprite version): one shared 3D mesh
        // per WallPieceKind, at Buildings/Wall_Classical[_Kind] - resolved
        // via BuildingModelFactory.Refresh(..., age: AgeId.Classical, ...)
        // exactly like the Ancient kit's own literal-resourceName pieces
        // (see PieceResourceName above), just always passing AgeId.Classical
        // regardless of the piece's actual current age so Durg reuses the
        // identical mesh (darkened by FortificationVisual.DarkenMaterials).
        private static string ClassicalMeshResourceName(WallPieceKind kind)
        {
            return kind switch
            {
                WallPieceKind.Corner => "Wall_Classical_Corner",
                WallPieceKind.EndPost => "Wall_Classical_EndPost",
                _ => "Wall_Classical",
            };
        }

        internal static Vector3 PieceSize(WallPieceKind kind)
        {
            return kind == WallPieceKind.Straight ? Size : JunctionSize;
        }

        // Classical/Durg both resolve to the Classical mesh (see
        // ClassicalMeshResourceName's own comment) - Ancient/Imperial keep
        // resolving through the age-tiered literal resourceName as before.
        private static string ResourceNameFor(WallPieceKind kind, AgeId age)
        {
            return (age == AgeId.Classical || age == AgeId.Durg)
                ? ClassicalMeshResourceName(kind)
                : PieceResourceName(kind);
        }

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            return Place(point, faction, buildTime, Quaternion.identity);
        }

        // Wall system Session A: an overload carrying the segment's
        // rotation, for free-angle drag-placed chains (see
        // BuildingPlacer.ComputeWallChain). The root is created directly
        // here (not via BuildingModelFactory.Spawn) since the visual itself
        // is built by FortificationVisual, given an already-created root -
        // NavMeshObstacle (Box shape) inherits orientation from this same
        // transform automatically, and IsClearForKind's Wall/Gate overlap
        // check is already a rotation-agnostic circular distance test -
        // both need no further changes.
        //
        // pieceKind (2026-09-28 Ancient modular kit, extended 2026-09-28 for
        // a second, Classical-tier modular kit): every piece is genuinely
        // age-tiered - Ancient/Imperial resolve through
        // BuildingModelFactory's existing mesh lookup chain (a junction
        // piece's literal resourceName has no Imperial art, so it correctly
        // falls through to its own bare Ancient-tier path; Straight/Gate
        // resolve to the real per-civ Imperial model as before), Classical/
        // Durg resolve to the Classical-tier mesh kit - see
        // ResourceNameFor/FortificationVisual.
        public static GameObject Place(Vector3 point, FactionId faction, float buildTime, Quaternion rotation, WallPieceKind pieceKind = WallPieceKind.Straight)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);
            Vector3 size = PieceSize(pieceKind);
            AgeId age = AgeProgress.CurrentAge(faction);
            string resourceName = ResourceNameFor(pieceKind, age);

            GameObject go = new GameObject(resourceName);
            go.transform.position = point + Vector3.up * (size.y * 0.5f);
            go.transform.rotation = rotation;
            FortificationVisual.Build(go, resourceName, age, civ, profile.PrimaryColor, faction, size);
            go.name = faction == FactionId.Player ? "Wall" : "EnemyWall";
            go.AddComponent<WallAgeVisual>().ConfigureWall(pieceKind, size);
            // Wall is exempt from BuildingFootprint's square-tile/margin
            // system (it blocks its full footprint edge-to-edge, no
            // passable margin, and keeps its own modular chain-placement
            // NavMeshObstacle below) - only tagged here so other buildings'
            // placement-overlap checks still see its real shape.
            BuildingFootprint.Attach(go, new Vector2(size.x, size.z), carveObstacle: false);

            go.AddComponent<Wall>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<SelectionIndicator>().Configure(1.4f, -size.y * 0.5f);
            var attackable = go.AddComponent<Attackable>();
            // Phase 6: Vijayanagara's unique tech (Hampi Fortifications)
            // multiplies defensive-structure HP - same non-retroactive
            // convention as CivilizationProfile/AgeProfile bonuses (baked
            // in at spawn, not applied to buildings already standing).
            // AoE-parity Phase 3.2: Vijayanagara's team bonus - allied
            // Wall/Gate/Tower get +15% max HP, stacking multiplicatively
            // with the owner's own tech above (unconditional - not gated
            // on the ally having researched anything). Uses `faction`,
            // not FactionId.Player, so this applies to AI-built
            // fortifications too, unlike WoodMultiplierFor's Player-only
            // scope. See TeamBonus.cs.
            float fortificationMultiplier = UniqueTechProgress.HasResearched(faction)
                ? UniqueTechDefinition.For(civ).FortificationHealthMultiplier
                : 1f;
            if (TeamBonus.HasAlly(faction, CivilizationId.Vijayanagara))
            {
                fortificationMultiplier *= TeamBonus.VijayanagaraFortificationHealthMultiplier;
            }
            attackable.Configure(MaxHealth * fortificationMultiplier);
            attackable.ConfigureArmor(meleeArmor: 6f, pierceArmor: 4f);
            attackable.ConfigureClass(UnitClass.Building);
            go.AddComponent<Repairable>();
            go.AddComponent<HealthBar>();
            go.AddComponent<FactionMember>().Configure(faction);
            // General garrisoning system (2026-09-01): Wall keeps its
            // original narrow behavior (durgOnly:true) rather than
            // becoming generally garrisonable - a thin 0.4-unit-deep Wall
            // segment has no real interior for a population to hide in,
            // unlike Tower/TownCenter. Only a siege-immunity-granting
            // unit (the Maratha Durg Garrison unique unit) may enter -
            // see GarrisonPoint.TryGarrison. Unchanged Player-facing
            // behavior from before this class was generalized.
            go.AddComponent<GarrisonPoint>().Configure(1, durgOnly: true);

            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = size;
            obstacle.carving = true;

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (VisionSource.IsTracked(faction))
            {
                go.AddComponent<VisionSource>().Configure(6f);
            }

            return go;
        }

        // Age-up retroactive re-skin entry point - see WallAgeVisual. Only
        // touches the visual + its collider (via FortificationVisual),
        // leaving every gameplay component already on `root` (Attackable,
        // GarrisonPoint, Repairable, ConstructionSite, etc.) untouched,
        // same "pure re-skin, not a re-spawn" contract
        // BuildingModelFactory.Refresh already established.
        public static void RefreshVisual(GameObject root, WallPieceKind pieceKind, AgeId age, CivilizationId civ, Color civColor, FactionId? faction, Vector3 size)
        {
            FortificationVisual.Build(root, ResourceNameFor(pieceKind, age), age, civ, civColor, faction, size);
        }
    }
}
