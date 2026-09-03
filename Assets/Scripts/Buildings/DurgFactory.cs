using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Durg foundation (Durg + ConstructionSite + FactionMember +
    // VisionSource + BuildingAttacker + GarrisonPoint). Wave 2 item 7: the
    // strongest defensive structure in the game (user-confirmed design
    // decision) - every stat below is strictly higher than TownCenter's,
    // today's previous strongest (see TownCenterFactory.cs, the template
    // this mirrors).
    public static class DurgFactory
    {
        private static readonly Vector3 Size = new Vector3(3.2f, 2.6f, 3.2f);
        private const float MaxHealth = 700f;
        private const int GarrisonCapacity = 12;
        private const int MaxBonusShots = 6;
        private const float AttackDamage = 12f;
        private const float AttackRange = 9f;
        private const float AttackInterval = 1.2f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            GameObject go = BuildingModelFactory.Spawn("Durg", civ, point + Vector3.up * (Size.y * 0.5f), Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "Durg" : "EnemyDurg";
            BuildingFootprint.Attach(go, BuildingFootprint.Square(BuildingFootprint.DurgTiles), carveObstacle: true);

            go.AddComponent<Durg>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<SelectionIndicator>().Configure(1.9f, -Size.y * 0.5f);
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(MaxHealth);
            attackable.ConfigureArmor(meleeArmor: 4f, pierceArmor: 6f);
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
                go.AddComponent<VisionSource>().Configure(12f);
            }

            return go;
        }

        // Faction-agnostic on purpose, same as BarracksFactory.IsClear -
        // neither side should place a foundation on top of any existing
        // building, friend or foe.
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
