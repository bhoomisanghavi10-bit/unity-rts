using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Mining Camp foundation (MiningCamp + ConstructionSite +
    // FactionMember) - drop-off for both Gold and Stone. Mirrors
    // LumberCampFactory's shape exactly.
    public static class MiningCampFactory
    {
        private static readonly Vector3 Size = new Vector3(2f, 1.4f, 2f);
        private const float MaxHealth = 200f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            GameObject go = BuildingModelFactory.Spawn("MiningCamp", civ, point + Vector3.up * (Size.y * 0.5f), Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "MiningCamp" : "EnemyMiningCamp";
            BuildingFootprint.Attach(go, BuildingFootprint.Square(BuildingFootprint.DropOffTiles), carveObstacle: true);

            go.AddComponent<MiningCamp>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<SelectionIndicator>().Configure(1.3f, -Size.y * 0.5f);
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(MaxHealth);
            attackable.ConfigureArmor(meleeArmor: 0f, pierceArmor: 1f);
            attackable.ConfigureClass(UnitClass.Building);
            go.AddComponent<Repairable>();
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
