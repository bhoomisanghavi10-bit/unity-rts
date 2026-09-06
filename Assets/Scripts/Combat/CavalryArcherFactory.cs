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
    // Wave 4 item (docs/IMPLEMENTATION_ROADMAP.md): the Cavalry Archer -
    // "Mobile ranged raider, fits Rajput/Maratha horse-archer tradition."
    // Design call made explicitly (the roadmap fixes tier names/ages, not
    // what class this counts as for combat purposes): classified as
    // UnitClass.Archer, not a new class - a mounted Archer, not a
    // fundamentally new counter archetype. This keeps it inside the
    // existing counter web for free (see CombatBonus): it still
    // hard-counters Cavalry (Archer->Cavalry 2x) and is still
    // hard-countered by Skirmisher (Skirmisher->Archer 2x) and takes the
    // Infantry->Archer 1.5x bonus, exactly like a foot Archer - only its
    // move speed (borrowed from Cavalry's own value) and cost differ.
    // Deliberately does NOT read CivilizationProfile.FindCategoryMultiplier
    // with UnitClass.Cavalry (the Maratha cavalry-speed/Rajput
    // cavalry-damage civ bonuses) - those are scoped to units whose actual
    // combat class is Cavalry, and this unit's combat class is Archer, so
    // it correctly falls outside them; it just happens to ride a horse.
    // Mirrors ArcherFactory's ranged-attack setup and CavalryFactory's
    // mount/speed setup combined - reuses the same Male Human Character
    // Dummy body plus both the Bow and Horse props (no dedicated mounted-
    // archer model exists yet, same "primitive/placeholder until a real
    // pack lands" convention used everywhere else in this project).
    public static class CavalryArcherFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note.
            UnitDefinition def = DataRegistry.GetUnit("cavalry_archer");
            if (def == null)
            {
                Debug.LogWarning("CavalryArcherFactory: no generated UnitDefinition for 'cavalry_archer' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            // Cavalry Archer tier ladder - baked in at spawn, not
            // retroactive, same convention as every other combat-unit tier
            // line.
            CavalryArcherTierData tier = CavalryArcherLineProgress.Current(faction);

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} {tier.Name}"
                : $"Enemy {profile.DisplayName} {tier.Name}";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 6f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            // General garrisoning system (2026-09-01): lets this unit
            // be ordered to walk to and enter a friendly GarrisonPoint -
            // see GarrisonSeeker.
            go.AddComponent<GarrisonSeeker>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(((def != null ? def.maxHP : 30f) + tier.HpBonus) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 0f,
                pierceArmor: def != null ? def.pierceArmor : 0f);
            attackable.ConfigureClass(UnitClass.Archer);
            attackable.EnableUpgradeArmorScaling(melee: false, pierce: true);
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage((def != null ? def.attackDamage : 5f) + tier.DamageBonus);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetRange(def != null ? def.attackRange : 6f);
            attacker.SetDamageType(DamageType.Pierce);
            attacker.SetUnitClass(UnitClass.Archer);
            attacker.EnableUpgradeDamageScaling();
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // Purely cosmetic - see ArcherFactory's/CavalryFactory's
            // identical notes on why the offsets are approximate. Combines
            // both existing props (bow + horse) so this reads as a mounted
            // archer at a glance, distinct from both a foot Archer and
            // plain Cavalry.
            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.LeftHand, "Weapons/Bow/scene",
                targetSize: 1f, localPositionOffset: new Vector3(-0.05f, 0f, 0f), localEulerOffset: new Vector3(0f, 90f, 0f));
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
