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
        private static readonly Vector3 Size = new Vector3(2.4f, 1.8f, 0.4f);
        private const float MaxHealth = 220f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            GameObject go = BuildingModelFactory.Spawn("Gate", civ, point + Vector3.up * (Size.y * 0.5f), Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "Gate" : "EnemyGate";
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

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(6f);
            }

            return go;
        }
    }
}
