using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Town Center (TownCenter + FactionMember + VisionSource).
    // Used by both TownCenterSpawner (the Player's fixed starting base) and
    // AiController (the AI's own base).
    public static class TownCenterFactory
    {
        private static readonly Vector3 Size = new Vector3(3f, 2f, 3f);
        private const float BaseHealth = 500f;

        public static GameObject Place(Vector3 position, FactionId faction)
        {
            CivilizationProfile profile = CivilizationProfile.For(CivilizationRegistry.For(faction));
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));

            GameObject go = BuildingModelFactory.Spawn("TownCenter", position, Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "TownCenter" : "EnemyTownCenter";

            go.AddComponent<TownCenter>();
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<Attackable>().Configure(BaseHealth * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(10f);
            }

            return go;
        }

    }
}
