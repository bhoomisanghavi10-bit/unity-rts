using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Units
{
    // Wave 4 item 27: Vaidya, the healer half of AoE's Monk - heals a
    // friendly damaged unit via the VaidyaHealer component (see
    // VaidyaHealer.cs). Deliberately unarmed, mirroring
    // Vanik/TradeShip's own "completely unarmed utility unit" shape - no
    // dedicated model exists yet, reuses the shared Human Character Dummy
    // body, civ-tinted - flagging directly per the flag-asset-needs
    // convention: Vaidya currently looks like a generic soldier, not a
    // healer, and is visually identical to Purohita.
    public static class VaidyaFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));

            UnitDefinition def = DataRegistry.GetUnit("vaidya");
            if (def == null)
            {
                Debug.LogWarning("VaidyaFactory: no generated UnitDefinition for 'vaidya' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Vaidya"
                : $"Enemy {profile.DisplayName} Vaidya";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 3f;

            var unit = go.AddComponent<Unit>();
            unit.IconKey = "train_vaidya";
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            go.AddComponent<GarrisonSeeker>();
            go.AddComponent<VaidyaHealer>();

            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 25f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureClass(UnitClass.Support);
            go.AddComponent<HealthBar>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
