using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Creates a defensive Tower (Tower + ConstructionSite + FactionMember +
    // TowerAttacker + NavMeshObstacle). Tall footprint, high HP, and its
    // own ranged auto-attack (see TowerAttacker) - the first building in
    // this project that fights back on its own.
    public static class TowerFactory
    {
        private static readonly Vector3 Size = new Vector3(1.8f, 4.4f, 1.8f);
        private const float MaxHealth = 300f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            GameObject go = BuildingModelFactory.Spawn("Tower", point + Vector3.up * (Size.y * 0.5f), Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "Tower" : "EnemyTower";

            go.AddComponent<Tower>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<SelectionIndicator>().Configure(1.4f, -Size.y * 0.5f);
            var attackable = go.AddComponent<Attackable>();
            // Phase 6: same Vijayanagara fortification bonus as WallFactory.
            float fortificationMultiplier = UniqueTechProgress.HasResearched(faction)
                ? UniqueTechDefinition.For(civ).FortificationHealthMultiplier
                : 1f;
            attackable.Configure(MaxHealth * fortificationMultiplier);
            attackable.ConfigureArmor(meleeArmor: 4f, pierceArmor: 6f);
            attackable.ConfigureClass(UnitClass.Building);
            go.AddComponent<HealthBar>();
            go.AddComponent<TowerAttacker>();
            go.AddComponent<FactionMember>().Configure(faction);

            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Size;
            obstacle.carving = true;

            // Towers see further than any other building - that's their
            // whole point as a forward-defense/vision structure.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(12f);
            }

            return go;
        }
    }
}
