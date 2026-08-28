using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Market foundation (Market + ConstructionSite +
    // FactionMember). Mirrors FarmFactory's shape - used by BuildingPlacer
    // for the Player's mouse-driven placement, and available to AiController
    // the same way every other building factory is, though the AI doesn't
    // use it yet (see roadmap item 39 - self-contained, no AI wiring
    // required for the building/trade mechanic itself to work).
    public static class MarketFactory
    {
        private static readonly Vector3 Size = new Vector3(2.4f, 1.6f, 2.4f);
        private const float MaxHealth = 180f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            GameObject go = BuildingModelFactory.Spawn("Market", civ, point + Vector3.up * (Size.y * 0.5f), Size, profile.PrimaryColor);
            go.name = faction == FactionId.Player ? "Market" : "EnemyMarket";
            BuildingFootprint.Attach(go, BuildingFootprint.Square(BuildingFootprint.MarketTiles), carveObstacle: true);

            go.AddComponent<Market>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<SelectionIndicator>().Configure(1.4f, -Size.y * 0.5f);
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(MaxHealth);
            attackable.ConfigureArmor(meleeArmor: 1f, pierceArmor: 1f);
            attackable.ConfigureClass(UnitClass.Building);
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
