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
    // Wave 4 item 26: Vanik, the land Trader - shuttles Gold between owned/
    // allied Markets via the Trader component (see Trader.cs). Deliberately
    // unarmed, unlike Worker's weak self-defense MeleeAttacker - matches
    // AoE's own Trade Cart (fragile, must be escorted, can't fight back).
    // No dedicated model exists yet - reuses the shared Human Character
    // Dummy body, civ-tinted, same as Scout/CavalryArcher - flagging
    // directly per the flag-asset-needs convention: a Vanik currently looks
    // like a generic soldier, not a merchant.
    public static class VanikFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));

            UnitDefinition def = DataRegistry.GetUnit("vanik");
            if (def == null)
            {
                Debug.LogWarning("VanikFactory: no generated UnitDefinition for 'vanik' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Vanik"
                : $"Enemy {profile.DisplayName} Vanik";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 3.5f;

            var unit = go.AddComponent<Unit>();
            unit.IconKey = "train_vanik";
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            go.AddComponent<GarrisonSeeker>();
            go.AddComponent<Trader>();

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
