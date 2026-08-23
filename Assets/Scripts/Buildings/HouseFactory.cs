using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Creates a House foundation (House + ConstructionSite + FactionMember).
    // Mirrors BarracksFactory/FarmFactory's shape - used by BuildingPlacer
    // for the Player's mouse-driven placement, and AiController for the
    // AI's own population growth.
    public static class HouseFactory
    {
        private static readonly Vector3 Size = new Vector3(2f, 1.6f, 2f);
        private const float BaseHealth = 120f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationProfile profile = CivilizationProfile.For(CivilizationRegistry.For(faction));
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));

            GameObject go = BuildingModelFactory.Spawn("House", point + Vector3.up * (Size.y * 0.5f), Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "House" : "EnemyHouse";

            go.AddComponent<House>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<Attackable>().Configure(BaseHealth * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(6f);
            }

            return go;
        }
    }
}
