using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;

namespace KingdomsOfBharat.Buildings
{
    // Item 49: creates a Dock foundation (Dock + ConstructionSite +
    // FactionMember + VisionSource), mirroring BarracksFactory's shape
    // exactly. Used by BuildingPlacer (mouse-driven, Player); AI naval
    // construction is out of scope for this pass (see AiController's class
    // doc comment - a scripted AI, not one that plans a whole new tech
    // branch's worth of build logic on its own yet).
    public static class DockFactory
    {
        private static readonly Vector3 Size = new Vector3(2.2f, 0.6f, 4f);
        private const float MaxHealth = 200f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationProfile profile = CivilizationProfile.For(CivilizationRegistry.For(faction));

            GameObject go = BuildingModelFactory.Spawn("Dock", point + Vector3.up * (Size.y * 0.5f), Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "Dock" : "EnemyDock";

            go.AddComponent<Dock>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<SelectionIndicator>().Configure(2.4f, -Size.y * 0.5f);
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(MaxHealth);
            attackable.ConfigureArmor(meleeArmor: 1f, pierceArmor: 1f);
            attackable.ConfigureClass(UnitClass.Building);
            go.AddComponent<HealthBar>();
            go.AddComponent<FactionMember>().Configure(faction);

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(9f);
            }

            return go;
        }

        // Same "no foundation on top of another building, friend or foe"
        // check every other building's IsClear uses.
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
