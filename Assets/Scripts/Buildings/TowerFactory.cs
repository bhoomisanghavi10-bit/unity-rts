using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Creates a defensive Tower (Tower + ConstructionSite + FactionMember +
    // TowerAttacker + a BuildingFootprint-carved NavMeshObstacle). Small
    // (2x2 tile) footprint but tall/high-HP, and its own ranged auto-attack
    // (see TowerAttacker) - the first building in this project that fights
    // back on its own.
    public static class TowerFactory
    {
        private static readonly Vector3 Size = new Vector3(1.8f, 4.4f, 1.8f);
        private const float MaxHealth = 300f;
        // Matches TowerAttacker's own [SerializeField] default - kept here
        // too so Vijayanagara's +1 bonus below has a base value to add to
        // without reading TowerAttacker's private field.
        private const float BaseAttackRange = 9f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            GameObject go = BuildingModelFactory.Spawn("Tower", civ, point + Vector3.up * (Size.y * 0.5f), Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "Tower" : "EnemyTower";
            BuildingFootprint.Attach(go, BuildingFootprint.Square(BuildingFootprint.TowerTiles), carveObstacle: true);

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
            // Phase 6 gap-close: Vijayanagara's "Towers get +1 attack range" bonus.
            go.AddComponent<TowerAttacker>().Configure(
                civ == CivilizationId.Vijayanagara ? BaseAttackRange + 1f : BaseAttackRange);
            go.AddComponent<FactionMember>().Configure(faction);
            // Roadmap Section 5 item 3: lets a Maratha Durg Garrison unit
            // enter this Tower - see Garrison.
            go.AddComponent<Garrison>();

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
