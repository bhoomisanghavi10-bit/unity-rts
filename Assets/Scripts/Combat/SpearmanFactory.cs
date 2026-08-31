using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
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
            // General garrisoning system (2026-09-01): lets this unit
            // be ordered to walk to and enter a friendly GarrisonPoint -
            // see GarrisonSeeker.
            go.AddComponent<GarrisonSeeker>();
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

            // Sourced from Sketchfab's "Spear Infantryman" (CC-BY, Avijoy.L
            // - see Assets/importedmodels CREDITS convention) - the full
            // 30-submesh character import wasn't used directly (no
            // animations, unverified rig, same risk this project already
            // flagged for the 2 unused Crusader Knight body swaps), only
            // its spear mesh was extracted out to its own standalone
            // prefab (Assets/Resources/Weapons/Spear/scene.prefab, single
            // renderer) and attached the same way Sword/Bow/Kanabo are.
            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.RightHand, "Weapons/Spear/scene",
                targetSize: 2.4f, localPositionOffset: new Vector3(0.05f, 0.3f, 0f), localEulerOffset: new Vector3(-15f, 0f, 0f));

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
