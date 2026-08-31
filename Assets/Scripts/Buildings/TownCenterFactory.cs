using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Town Center (TownCenter + FactionMember + VisionSource +
    // BuildingAttacker + GarrisonPoint). Used by both TownCenterSpawner
    // (the Player's fixed starting base) and AiController (the AI's own
    // base). General garrisoning system (2026-09-01): TownCenter had no
    // Attacker at all before this item - it's the Keep/TC-equivalent
    // structure, so it gets the highest garrison capacity and bonus-shot
    // count of any building (see GarrisonPoint/BuildingAttacker), plus a
    // modest baseline defense even ungarrisoned.
    public static class TownCenterFactory
    {
        private static readonly Vector3 Size = new Vector3(3f, 2f, 3f);
        private const float MaxHealth = 500f;
        private const int GarrisonCapacity = 8;
        private const int MaxBonusShots = 4;
        private const float AttackDamage = 8f;
        private const float AttackRange = 8f;
        private const float AttackInterval = 1.4f;

        public static GameObject Place(Vector3 position, FactionId faction)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            GameObject go = BuildingModelFactory.Spawn("TownCenter", civ, position, Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "TownCenter" : "EnemyTownCenter";
            BuildingFootprint.Attach(go, BuildingFootprint.Square(BuildingFootprint.TownCenterTiles), carveObstacle: true);

            go.AddComponent<TownCenter>();
            go.AddComponent<SelectionIndicator>().Configure(1.9f, -Size.y * 0.5f);
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(MaxHealth);
            attackable.ConfigureArmor(meleeArmor: 3f, pierceArmor: 5f);
            attackable.ConfigureClass(UnitClass.Building);
            go.AddComponent<Repairable>();
            go.AddComponent<HealthBar>();
            var garrisonPoint = go.AddComponent<GarrisonPoint>();
            garrisonPoint.Configure(GarrisonCapacity, durgOnly: false);
            var buildingAttacker = go.AddComponent<BuildingAttacker>();
            buildingAttacker.ConfigureStats(AttackDamage, AttackRange, AttackInterval);
            buildingAttacker.ConfigureGarrisonBonus(garrisonPoint, MaxBonusShots);
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
