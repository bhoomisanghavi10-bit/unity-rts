using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Barracks foundation (Barracks + ConstructionSite +
    // FactionMember + VisionSource). Used by both BuildingPlacer
    // (mouse-driven, Player) and AiController (programmatic, Enemy) instead
    // of duplicating the component list in two places.
    public static class BarracksFactory
    {
        private static readonly Vector3 Size = new Vector3(3f, 2f, 3f);
        private const float BaseHealth = 350f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationProfile profile = CivilizationProfile.For(CivilizationRegistry.For(faction));
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));

            GameObject go = BuildingModelFactory.Spawn("Barracks", point + Vector3.up * (Size.y * 0.5f), Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "Barracks" : "EnemyBarracks";

            go.AddComponent<Barracks>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<Attackable>().Configure(BaseHealth * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(10f);
            }

            return go;
        }

        // Faction-agnostic on purpose: neither side should place a
        // foundation on top of any existing building, friend or foe.
        public static bool IsClear(Vector3 point, float clearance)
        {
            foreach (Building building in Building.All)
            {
                if (Vector3.Distance(building.transform.position, point) < clearance)
                {
                    return false;
                }
            }

            return true;
        }

    }
}
