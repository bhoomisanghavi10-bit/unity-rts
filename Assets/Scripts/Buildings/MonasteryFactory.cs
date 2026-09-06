using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;

namespace KingdomsOfBharat.Buildings
{
    // Wave 4 item 27: Monastery, where Vaidya (healer) and Purohita
    // (converter) train. Mirrors KarmashalaFactory's shape exactly - same
    // 3-tile/Market-sized footprint, same "raidable" HP/armor.
    public static class MonasteryFactory
    {
        private static readonly Vector3 Size = new Vector3(2.4f, 1.8f, 2.4f);
        private const float MaxHealth = 220f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            GameObject go = BuildingModelFactory.Spawn("Monastery", civ, point + Vector3.up * (Size.y * 0.5f), Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "Monastery" : "EnemyMonastery";
            BuildingFootprint.Attach(go, BuildingFootprint.Square(BuildingFootprint.MonasteryTiles), carveObstacle: true);

            go.AddComponent<Monastery>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<SelectionIndicator>().Configure(1.5f, -Size.y * 0.5f);
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(MaxHealth);
            attackable.ConfigureArmor(meleeArmor: 1f, pierceArmor: 2f);
            attackable.ConfigureClass(UnitClass.Building);
            go.AddComponent<Repairable>();
            go.AddComponent<HealthBar>();
            go.AddComponent<FactionMember>().Configure(faction);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
