using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Combat
{
    // Creates an "Archer" unit: AoE-style ranged counterpart to Soldier -
    // lower HP, hits from range instead of needing melee contact, and
    // deals Pierce damage (so it's resisted by pierceArmor, not
    // meleeArmor - see Attackable). Mirrors SoldierFactory's shape almost
    // exactly, sharing the same Male body model since no dedicated archer
    // model/animation exists yet (same "primitive/placeholder until a real
    // pack lands" convention already used elsewhere in this project).
    public static class ArcherFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Archer"
                : $"Enemy {profile.DisplayName} Archer";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = 3.8f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(18f * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(meleeArmor: 0f, pierceArmor: UpgradeProgress.ArmorBonus(faction));
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(4f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetDamageBonus(UpgradeProgress.DamageBonus(faction));
            attacker.SetRange(6f);
            attacker.SetDamageType(DamageType.Pierce);

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(9f);
            }

            return go;
        }
    }
}
