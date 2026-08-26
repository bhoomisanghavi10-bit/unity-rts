using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Combat
{
    // Roadmap Section 5 item 3 unique unit (Maurya, slot 0 of 2): the
    // heaviest elephant in the roster, reflecting Ashoka-era Maurya's
    // historically signature war-elephant corps. Kept as UnitClass.Siege,
    // same reasoning as VijayanagaraWarElephantFactory (gets CombatBonus's
    // existing 3x Building bonus for free) - a step up from that unit
    // across the board (HP/damage/cost) rather than a sidegrade, matching
    // the CSV's own "the heaviest elephant" framing. No elephant model
    // exists, so this reuses the Male Human Character Dummy body/
    // animations like every other unique unit pending a real pack.
    public static class MauryaWarElephantFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            UnitDefinition def = DataRegistry.GetUnit("maurya_war_elephant");
            if (def == null)
            {
                Debug.LogWarning("MauryaWarElephantFactory: no generated UnitDefinition for 'maurya_war_elephant' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} War Elephant"
                : $"Enemy {profile.DisplayName} War Elephant";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.6f;
            agent.height = 2.4f;
            agent.speed = def != null ? def.moveSpeed : 2f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 100f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: (def != null ? def.meleeArmor : 2f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Siege),
                pierceArmor: (def != null ? def.pierceArmor : 2f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Siege));
            attackable.ConfigureClass(UnitClass.Siege);
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 11f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetDamageBonus(UpgradeProgress.DamageBonus(faction) + UpgradeProgress.ClassDamageBonus(faction, UnitClass.Siege));
            attacker.SetRange(def != null ? def.attackRange : 2.5f);
            attacker.SetUnitClass(UnitClass.Siege);
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.RightHand, "Weapons/Kanabo/scene",
                targetSize: 2.1f, localPositionOffset: new Vector3(0.05f, 0.1f, 0f), localEulerOffset: new Vector3(0f, 0f, 100f));

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(7f);
            }

            return go;
        }
    }
}
