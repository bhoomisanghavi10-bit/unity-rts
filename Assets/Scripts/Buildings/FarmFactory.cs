using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Farm foundation (Farm + ConstructionSite + FactionMember).
    // Mirrors BarracksFactory's shape - used by BuildingPlacer for the
    // Player's mouse-driven placement.
    public static class FarmFactory
    {
        private static readonly Vector3 Size = new Vector3(2f, 0.6f, 2f);
        private const float MaxHealth = 150f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationProfile profile = CivilizationProfile.For(CivilizationRegistry.For(faction));

            GameObject go = BuildingModelFactory.Spawn("Farm", point + Vector3.up * (Size.y * 0.5f), Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "Farm" : "EnemyFarm";

            go.AddComponent<Farm>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<SelectionIndicator>().Configure(1.3f, -Size.y * 0.5f);
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(MaxHealth);
            attackable.ConfigureArmor(meleeArmor: 0f, pierceArmor: 1f);
            go.AddComponent<HealthBar>();
            go.AddComponent<FactionMember>().Configure(faction);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(6f);
            }

            return go;
        }

    }
}
