using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Karmashala foundation (Karmashala + ConstructionSite +
    // FactionMember) - Wave 2 item 8, AoE's Blacksmith-equivalent. Mirrors
    // MillFactory's shape (no attacker/garrison, nothing trains or spawns
    // here - it only researches). Deliberately "raidable": between Mill's
    // 200 HP and Barracks' 300 HP, per the roadmap item's own wording.
    public static class KarmashalaFactory
    {
        private static readonly Vector3 Size = new Vector3(2.4f, 1.8f, 2.4f);
        private const float MaxHealth = 220f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            GameObject go = BuildingModelFactory.Spawn("Karmashala", civ, point + Vector3.up * (Size.y * 0.5f), Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "Karmashala" : "EnemyKarmashala";
            BuildingFootprint.Attach(go, BuildingFootprint.Square(BuildingFootprint.KarmashalaTiles), carveObstacle: true);

            go.AddComponent<Karmashala>();
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
