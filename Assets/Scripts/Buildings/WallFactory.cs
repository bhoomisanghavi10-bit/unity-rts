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
        private static readonly Vector3 Size = new Vector3(2.4f, 1.8f, 0.4f);
        private const float MaxHealth = 250f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            GameObject go = BuildingModelFactory.Spawn("Wall", civ, point + Vector3.up * (Size.y * 0.5f), Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "Wall" : "EnemyWall";
            // Wall is exempt from BuildingFootprint's square-tile/margin
            // system (it blocks its full footprint edge-to-edge, no
            // passable margin, and keeps its own modular chain-placement
            // NavMeshObstacle below) - only tagged here so other buildings'
            // placement-overlap checks still see its real shape.
            BuildingFootprint.Attach(go, new Vector2(Size.x, Size.z), carveObstacle: false);

            go.AddComponent<Wall>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<SelectionIndicator>().Configure(1.4f, -Size.y * 0.5f);
            var attackable = go.AddComponent<Attackable>();
            // Phase 6: Vijayanagara's unique tech (Hampi Fortifications)
            // multiplies defensive-structure HP - same non-retroactive
            // convention as CivilizationProfile/AgeProfile bonuses (baked
            // in at spawn, not applied to buildings already standing).
            float fortificationMultiplier = UniqueTechProgress.HasResearched(faction)
                ? UniqueTechDefinition.For(civ).FortificationHealthMultiplier
                : 1f;
            attackable.Configure(MaxHealth * fortificationMultiplier);
            attackable.ConfigureArmor(meleeArmor: 6f, pierceArmor: 4f);
            attackable.ConfigureClass(UnitClass.Building);
            go.AddComponent<HealthBar>();
            go.AddComponent<FactionMember>().Configure(faction);
            // Roadmap Section 5 item 3: lets a Maratha Durg Garrison unit
            // enter this Wall - see Garrison.
            go.AddComponent<Garrison>();

            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Size;
            obstacle.carving = true;

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(6f);
            }

            return go;
        }
    }
}
