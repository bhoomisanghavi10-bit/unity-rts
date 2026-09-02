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
    // Creates a "Cavalry" unit: the third roster addition alongside
    // Soldier/Archer, completing the Infantry > Archer > Cavalry > Infantry
    // counter triangle (see CombatBonus) - fast and hard-hitting against
    // Infantry, vulnerable to Archer fire, dealing plain Melee damage (a
    // mounted charge, not a projectile) so it's resisted by meleeArmor like
    // Soldier. No horse model/pack exists yet, so this reuses the same Male
    // Human Character Dummy body as Soldier/Archer - same "primitive/
    // placeholder until a real pack lands" convention used everywhere else
    // in this project; the speed/cost/damage numbers already make it play
    // distinctly even before it looks distinct.
    public static class CavalryFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note.
            UnitDefinition def = DataRegistry.GetUnit("cavalry");
            if (def == null)
            {
                Debug.LogWarning("CavalryFactory: no generated UnitDefinition for 'cavalry' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Cavalry"
                : $"Enemy {profile.DisplayName} Cavalry";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 6.5f;
            // Phase 6 gap-close: Maratha's Cavalry-only move-speed bonus -
            // see CivilizationProfile.FindCategoryMultiplier for why this
            // isn't one of CivilizationProfile's named fields.
            agent.speed *= CivilizationProfile.FindCategoryMultiplier(civilization, StatType.MoveSpeed, UnitCategory.Cavalry);
            // AoE-parity Phase 3.2: Maratha's team bonus - allied Cavalry
            // get +10% move speed, unconditional and stacking with the
            // owner's own civ-wide Cavalry speed bonus above. See
            // TeamBonus.cs.
            if (TeamBonus.HasAlly(faction, CivilizationId.Maratha))
            {
                agent.speed *= TeamBonus.MarathaCavalryMoveSpeedMultiplier;
            }

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            // General garrisoning system (2026-09-01): lets this unit
            // be ordered to walk to and enter a friendly GarrisonPoint -
            // see GarrisonSeeker.
            go.AddComponent<GarrisonSeeker>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 40f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: (def != null ? def.meleeArmor : 1f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Cavalry),
                pierceArmor: (def != null ? def.pierceArmor : 0f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Cavalry));
            attackable.ConfigureClass(UnitClass.Cavalry);
            go.AddComponent<HealthBar>();

            // Phase 6: Rajput's unique tech (Warrior Clans) adds flat
            // Cavalry damage on top of the existing flat/per-class
            // UpgradeProgress bonuses - non-retroactive like every other
            // spawn-time bonus here, only benefits Cavalry trained after
            // the tech finishes.
            float uniqueTechDamageBonus = UniqueTechProgress.HasResearched(faction)
                ? UniqueTechDefinition.For(civilization).CavalryDamageBonus
                : 0f;

            // AoE-parity Phase 3.2: Rajput's team bonus - allied Cavalry
            // get +1 flat damage, unconditional (not gated on the ally
            // having researched Warrior Clans). See TeamBonus.cs.
            if (TeamBonus.HasAlly(faction, CivilizationId.Rajput))
            {
                uniqueTechDamageBonus += TeamBonus.RajputCavalryDamageBonus;
            }

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 6f);
            attacker.SetRange(def != null ? def.attackRange : 1f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetDamageBonus(UpgradeProgress.DamageBonus(faction) + UpgradeProgress.ClassDamageBonus(faction, UnitClass.Cavalry) + uniqueTechDamageBonus);
            attacker.SetUnitClass(UnitClass.Cavalry);
            // Item 3 (Area of Effect / Trample, docs/PARTIAL_ELEMENTS_FIX_PLAN.md):
            // a charging Cavalry hit also lightly damages any other hostile
            // clumped within 1.25 units of the impact point - below
            // SelectionManager's 1.5 formationSpacing, unlike Siege's
            // deliberately spacing-spanning 2.25 (see SiegeFactory), so this
            // catches only units standing right next to the primary target,
            // not a full adjacent formation rank. 35% secondary damage keeps
            // it a minor effect even stacked on Cavalry's existing 1.5x
            // hard-counter bonus vs. Infantry (see CombatBonus).
            attacker.SetSplashRadius(1.25f, 0.35f);
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // Purely cosmetic - stands a horse mount behind/around the
            // rider rather than a true seated pose (no rig integration
            // between the human's Idle/Walk clips and the horse's own
            // animations), but reads as "mounted" distinctly from Soldier/
            // Archer's bare-handed silhouette, which is the actual goal.
            WeaponAttachment.AttachBeside(
                go, "Mounts/Horse/scene",
                targetSize: 2.2f, localPositionOffset: new Vector3(0f, 0f, -0.6f), localEulerOffset: Vector3.zero);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
