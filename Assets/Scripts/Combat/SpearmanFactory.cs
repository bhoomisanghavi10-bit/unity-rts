using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Combat
{
    // Creates a "Spearman" unit: the anti-cavalry specialist
    // unit_roster_template.csv/counter_matrix_template.csv designed during
    // the Phase 1/2 data migration but which had no live factory until now
    // (unlike every other roster unit, which already existed in code before
    // that migration and just got its stats re-sourced). Mirrors
    // SoldierFactory's shape almost exactly - same Male body/animation,
    // same melee attack pattern - the actual differentiator is
    // UnitClass.Spearman (see CombatBonus: 2x vs Cavalry, weak 0.8x
    // received from Infantry), not a different model or component set.
    public static class SpearmanFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note.
            UnitDefinition def = DataRegistry.GetUnit("spearman");
            if (def == null)
            {
                Debug.LogWarning("SpearmanFactory: no generated UnitDefinition for 'spearman' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Spearman"
                : $"Enemy {profile.DisplayName} Spearman";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 3.5f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 35f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: (def != null ? def.meleeArmor : 1f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Spearman),
                pierceArmor: (def != null ? def.pierceArmor : 0f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Spearman));
            attackable.ConfigureClass(UnitClass.Spearman);
            go.AddComponent<HealthBar>();
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 5f);
            attacker.SetRange(def != null ? def.attackRange : 1f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetDamageBonus(UpgradeProgress.DamageBonus(faction) + UpgradeProgress.ClassDamageBonus(faction, UnitClass.Spearman));
            attacker.SetUnitClass(UnitClass.Spearman);
            go.AddComponent<StanceController>();
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // No spear model exists in Assets/Resources/Weapons/ (only
            // Sword/Bow/Kanabo) - deliberately left bare-handed rather than
            // reusing one of those and implying a false visual distinction
            // (a sword-armed Spearman would look identical to Soldier).
            // Real known gap, same category as Farm/Wall's old procedural-
            // fallback state before a real pack was sourced - worth a
            // targeted ask once/if a spear asset is sourced.

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
