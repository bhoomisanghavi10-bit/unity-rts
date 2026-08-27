using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Town Center (TownCenter + FactionMember + VisionSource).
    // Used by both TownCenterSpawner (the Player's fixed starting base) and
    // AiController (the AI's own base).
    public static class TownCenterFactory
    {
        private static readonly Vector3 Size = new Vector3(3f, 2f, 3f);
        private const float MaxHealth = 500f;

        public static GameObject Place(Vector3 position, FactionId faction)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            GameObject go = BuildingModelFactory.Spawn("TownCenter", civ, position, Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "TownCenter" : "EnemyTownCenter";

            go.AddComponent<TownCenter>();
            go.AddComponent<SelectionIndicator>().Configure(1.9f, -Size.y * 0.5f);
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(MaxHealth);
            attackable.ConfigureArmor(meleeArmor: 3f, pierceArmor: 5f);
            attackable.ConfigureClass(UnitClass.Building);
            go.AddComponent<HealthBar>();
            go.AddComponent<FactionMember>().Configure(faction);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(10f);
            }

            return go;
        }

    }
}
