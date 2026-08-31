using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Combat
{
    // Creates a "Siege" unit: a dedicated anti-building specialist (see
    // CombatBonus's 3x Siege-vs-Building bonus), the whole reason to build
    // one over just massing more Soldiers against a Wall/Tower. Balanced by
    // being slow (half a Worker's speed) and dealing unremarkable damage
    // (flat 1x, no bonus) against every unit class - not a stronger
    // Soldier, a specialist with one job. No dedicated siege-engine model
    // exists yet, so this still reuses the Male Human Character Dummy body
    // like Soldier/Archer/Cavalry - but carries a Kanabo (a real siege
    // engine, e.g. a battering ram, would be a better fit long-term; a
    // hefty wall-breaking weapon reads correctly for now and, more
    // immediately, actually distinguishes this unit from a bare Soldier at
    // all, which it previously didn't).
    public static class SiegeFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note.
            UnitDefinition def = DataRegistry.GetUnit("siege");
            if (def == null)
            {
                Debug.LogWarning("SiegeFactory: no generated UnitDefinition for 'siege' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Siege"
                : $"Enemy {profile.DisplayName} Siege";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 1.8f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 50f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: (def != null ? def.meleeArmor : 0f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Siege),
                pierceArmor: (def != null ? def.pierceArmor : 0f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Siege));
            attackable.ConfigureClass(UnitClass.Siege);
            go.AddComponent<Repairable>();
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 15f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetDamageBonus(UpgradeProgress.DamageBonus(faction) + UpgradeProgress.ClassDamageBonus(faction, UnitClass.Siege));
            attacker.SetRange(def != null ? def.attackRange : 3f);
            attacker.SetUnitClass(UnitClass.Siege);
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // Purely cosmetic - see SoldierFactory's identical note. Larger
            // targetSize than the Soldier's sword since a Kanabo reads as a
            // two-handed, wall-breaking weapon, not a sidearm.
            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.RightHand, "Weapons/Kanabo/scene",
                targetSize: 1.5f, localPositionOffset: new Vector3(0.05f, 0.1f, 0f), localEulerOffset: new Vector3(0f, 0f, 100f));

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(6f);
            }

            return go;
        }
    }
}
