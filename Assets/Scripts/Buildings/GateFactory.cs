using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Gate segment - same shape as WallFactory, plus the Gate
    // component that toggles the NavMeshObstacle's carving open/closed
    // based on friendly proximity (see Gate.cs).
    public static class GateFactory
    {
        // Ancient-age modular wall kit (2026-09-28): the sourced Gate mesh
        // is a real 3-tile-wide combined piece (two flanking towers + a
        // lintel span - matching the "option 2" design already decided in
        // project_lowpoly_asset_size_conventions memory), not the old
        // single-tile placeholder box. Widening this Size constant is
        // sufficient on its own - Gate has always been a single-click
        // placement (never chain-dragged like Wall), so nothing about the
        // placement flow itself needs a multi-slot concept; the player
        // just leaves a matching gap in a Wall chain by eye, same as
        // before. Depth (Z) is deliberately deeper than a straight wall's
        // 0.4 too - real flanking gate towers project further forward/back
        // than a thin wall run.
        private static readonly Vector3 Size = new Vector3(7.2f, 6f, 2.6f);
        private const float MaxHealth = 220f;

        // Classical age wall-and-gate kit (2026-09-28): the closed/open
        // door-leaf sprite pair - see WallSpriteVisual's live open/close
        // swap (driven by the sibling Gate component's own NavMeshObstacle
        // carving state) and FortificationVisual for the age branch. Durg
        // reuses these exact images with a material darken tint, no
        // separate delivery.
        private const string ClassicalClosedPath = "buildings/WallSprites/Classical/GateClosed";
        private const string ClassicalOpenPath = "buildings/WallSprites/Classical/GateOpen";

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            // Age-aware visuals (matching Wall/Tower/TownCenter's existing
            // convention - see BuildingModelFactory.BuildVisual's age-suffixed
            // lookup chain): only an Ancient-tier Gate mesh exists (Buildings/
            // Gate_Ancient); Classical/Durg resolve to the new sprite kit
            // instead (FortificationVisual), and Imperial still falls
            // through to the original shared/civ-specific Gate lookup
            // unchanged, same graceful-degradation behavior every other
            // age-tiered building already relies on.
            AgeId age = AgeProgress.CurrentAge(faction);
            GameObject go = new GameObject("Gate");
            go.transform.position = point + Vector3.up * (Size.y * 0.5f);
            FortificationVisual.Build(go, "Gate", ClassicalClosedPath, ClassicalOpenPath, age, civ, profile.PrimaryColor, faction, Size);
            go.name = faction == FactionId.Player ? "Gate" : "EnemyGate";
            go.AddComponent<WallAgeVisual>().ConfigureGate(Size);
            // Gate is exempt from BuildingFootprint's square-tile/margin
            // system, same reasoning as Wall (see WallFactory) - it has to
            // match a Wall segment's shape to slot into a chain. Only
            // tagged here so other buildings' placement-overlap checks see
            // its real shape.
            BuildingFootprint.Attach(go, new Vector2(Size.x, Size.z), carveObstacle: false);

            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<SelectionIndicator>().Configure(1.4f, -Size.y * 0.5f);
            var attackable = go.AddComponent<Attackable>();
            // Phase 6: same Vijayanagara fortification bonus as WallFactory.
            // AoE-parity Phase 3.2: same team-bonus stacking as WallFactory -
            // see TeamBonus.cs.
            float fortificationMultiplier = UniqueTechProgress.HasResearched(faction)
                ? UniqueTechDefinition.For(civ).FortificationHealthMultiplier
                : 1f;
            if (TeamBonus.HasAlly(faction, CivilizationId.Vijayanagara))
            {
                fortificationMultiplier *= TeamBonus.VijayanagaraFortificationHealthMultiplier;
            }
            attackable.Configure(MaxHealth * fortificationMultiplier);
            attackable.ConfigureArmor(meleeArmor: 5f, pierceArmor: 3f);
            attackable.ConfigureClass(UnitClass.Building);
            go.AddComponent<Repairable>();
            go.AddComponent<HealthBar>();
            go.AddComponent<FactionMember>().Configure(faction);

            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Size;
            obstacle.carving = true;

            // Gate itself resolves FactionMember lazily (added a line
            // above) - added last among Gate's own dependencies since
            // AddComponent<Gate>() fires Awake() synchronously and Gate's
            // Awake only needs the NavMeshObstacle already added above it.
            go.AddComponent<Gate>();

            if (VisionSource.IsTracked(faction))
            {
                go.AddComponent<VisionSource>().Configure(6f);
            }

            return go;
        }

        // Age-up retroactive re-skin entry point - see WallAgeVisual and
        // WallFactory.RefreshVisual's matching comment.
        public static void RefreshVisual(GameObject root, AgeId age, CivilizationId civ, Color civColor, FactionId? faction, Vector3 size)
        {
            FortificationVisual.Build(root, "Gate", ClassicalClosedPath, ClassicalOpenPath, age, civ, civColor, faction, size);
        }
    }
}
