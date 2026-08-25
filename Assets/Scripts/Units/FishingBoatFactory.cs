using UnityEngine;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Units
{
    // Item 49: the naval Worker - gathers Food from water (Fish resource
    // nodes) via BoatGatherer instead of walking to a land node. Mirrors
    // WorkerFactory's shape (no combat component - a Fishing Boat can't
    // fight, matching AoE's own convention).
    public static class FishingBoatFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note. Move
            // speed isn't wired here - WaterMover's own default (3f)
            // already matches the CSV's fishing_boat MoveSpeed=3.0 exactly
            // and WaterMover has no public setter to change it per-spawn.
            UnitDefinition def = DataRegistry.GetUnit("fishing_boat");
            if (def == null)
            {
                Debug.LogWarning("FishingBoatFactory: no generated UnitDefinition for 'fishing_boat' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = BoatModelFactory.Spawn("FishingBoat", position, profile.PrimaryColor, isWarGalley: false);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Fishing Boat"
                : $"Enemy {profile.DisplayName} Fishing Boat";

            go.AddComponent<Unit>();
            go.AddComponent<WaterMover>();
            go.AddComponent<SelectionIndicator>();
            go.AddComponent<BoatGatherer>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 15f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureClass(UnitClass.Naval);
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
