using UnityEngine;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Units
{
    // Wave 4 item 26: Trade Ship, the naval Trader - shuttles Gold between
    // owned/allied Docks via the BoatTrader component (see BoatTrader.cs).
    // Mirrors FishingBoatFactory's shape (no combat component - can't
    // fight, matching AoE's own Trade Cog). No dedicated model exists yet -
    // reuses the same hull FishingBoat/WarGalley use - flagging directly
    // per the flag-asset-needs convention: a Trade Ship currently looks
    // identical to a Fishing Boat in the field.
    public static class TradeShipFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));

            UnitDefinition def = DataRegistry.GetUnit("trade_ship");
            if (def == null)
            {
                Debug.LogWarning("TradeShipFactory: no generated UnitDefinition for 'trade_ship' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = BoatModelFactory.Spawn("TradeShip", position, profile.PrimaryColor, isWarGalley: false, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Trade Ship"
                : $"Enemy {profile.DisplayName} Trade Ship";

            go.AddComponent<Unit>();
            // No icon art exists for this unit yet (per BuildMenu.cs's
            // own tradeShipButton, also null) - falls back to the UI's
            // placeholder icon, same convention BuildMenu itself uses.
            go.AddComponent<WaterMover>();
            go.AddComponent<SelectionIndicator>();
            go.AddComponent<BoatTrader>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 20f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureClass(UnitClass.Naval);
            go.AddComponent<KingdomsOfBharat.Combat.Repairable>();
            go.AddComponent<HealthBar>();
            go.AddComponent<FactionMember>().Configure(faction);

            var collider = go.AddComponent<CapsuleCollider>();
            collider.radius = 0.4f;
            collider.height = 1.5f;

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(7f);
            }

            return go;
        }
    }
}
