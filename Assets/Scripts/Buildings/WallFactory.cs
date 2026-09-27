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
        // Ancient-age modular wall kit (2026-09-28): straight run + 4
        // standalone junction pieces (Corner/EndPost/T/X), all mechanically
        // identical Wall buildings (same HP/armor/garrison/repair/
        // fortification bonuses) - only the visual resourceName and
        // gameplay-footprint Size differ per piece. See PieceResourceName/
        // PieceSize below and BuildingPlacer's wall-piece-variant selector
        // (Alpha1-5 while placing a Wall).
        public enum WallPieceKind { Straight, Corner, EndPost, TJunction, XJunction }

        // Straight segment height was a stale 1.8 (shorter than the ~1.9
        // worker unit) until the low-poly Ancient kit landed - now matches
        // the kit's own real proportions (see
        // project_lowpoly_asset_size_conventions memory: straight 3.0-3.5).
        private static readonly Vector3 Size = new Vector3(2.4f, 3.2f, 0.4f);

        // Corner/EndPost/T/X pieces read as turret caps rising above the
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
                WallPieceKind.TJunction => "Wall_Ancient_TJunction",
                WallPieceKind.XJunction => "Wall_Ancient_XJunction",
                _ => "Wall",
            };
        }

        internal static Vector3 PieceSize(WallPieceKind kind)
        {
            return kind == WallPieceKind.Straight ? Size : JunctionSize;
        }

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            return Place(point, faction, buildTime, Quaternion.identity);
        }

        // Wall system Session A: an overload carrying the segment's
        // rotation, for free-angle drag-placed chains (see
        // BuildingPlacer.ComputeWallChain). BuildingModelFactory.Spawn
        // always returns its root at identity rotation - only the
        // model's own child gets any rotation correction (Tower-specific) -
        // so setting the root's rotation here is safe and doesn't fight
        // anything Spawn itself does. NavMeshObstacle (Box shape) inherits
        // orientation from this same transform automatically, and
        // IsClearForKind's Wall/Gate overlap check is already a rotation-
        // agnostic circular distance test - both need no further changes.
        //
        // pieceKind (2026-09-28 Ancient modular kit): only Straight is
        // age-tiered (Wall_Ancient/Classical/Durg all exist or will exist -
        // see BuildingModelFactory's age-suffixed lookup chain); the 4
        // junction pieces are a one-off Ancient-only delivery so far, so
        // they resolve their literal resourceName with no age suffix and
        // deliberately skip AgeTieredBuildingVisual - re-skinning a Corner
        // piece as a plain "Wall" on Age-up would silently swap its whole
        // shape, not just its texture.
        public static GameObject Place(Vector3 point, FactionId faction, float buildTime, Quaternion rotation, WallPieceKind pieceKind = WallPieceKind.Straight)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);
            string resourceName = PieceResourceName(pieceKind);
            Vector3 size = PieceSize(pieceKind);
            AgeId? age = pieceKind == WallPieceKind.Straight ? AgeProgress.CurrentAge(faction) : (AgeId?)null;

            GameObject go = BuildingModelFactory.Spawn(resourceName, civ, point + Vector3.up * (size.y * 0.5f), size, profile.PrimaryColor, age, faction: faction);
            go.transform.rotation = rotation;
            go.name = faction == FactionId.Player ? "Wall" : "EnemyWall";
            if (pieceKind == WallPieceKind.Straight)
            {
                go.AddComponent<AgeTieredBuildingVisual>().Configure("Wall", size);
            }
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
    }
}
