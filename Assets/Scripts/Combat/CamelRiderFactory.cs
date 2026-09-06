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
    // Wave 4 item 22 (docs/IMPLEMENTATION_ROADMAP.md): the Camel Rider -
    // "Resolve in Plan Mode whether this ships at all before coding" (user
    // confirmed yes, via AskUserQuestion, before any code was written).
    // Design call made explicitly (the roadmap fixes tier names/ages, not
    // what class this counts as for combat purposes): a genuinely new
    // UnitClass.Camel rather than folded into Spearman or Cavalry, since it
    // needs both traits at once - Spearman's hard-counter-vs-Cavalry
    // combat role (see CombatBonus: Camel->Cavalry 2x, Infantry->Camel
    // 1.25x, values reused directly from Spearman's own pairing) plus
    // Cavalry's own mounted move speed and melee-charge damage type. Base
    // HP/armor/damage/speed reuse CavalryFactory's own fallback values
    // exactly (a camel-mounted unit is stat-equivalent to a horse-mounted
    // one before the counter multiplier kicks in) rather than being
    // independently balanced; cost reuses SpearmanFactory's own
    // Food+Wood-only model (no Gold) rather than Cavalry's Food+Gold one,
    // since this is fundamentally the anti-cavalry-specialist archetype,
    // just mounted. No camel model/pack exists yet - reuses the same Male
    // Human Character Dummy body plus both the Spear (RightHand) and Horse
    // (AttachBeside) props, no dedicated camel mount exists yet - flagging
    // directly per the flag-asset-needs convention: a Camel Rider
    // currently looks identical to Cavalry/Spearman hybrid using existing
    // props, no distinct silhouette.
    public static class CamelRiderFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note.
            UnitDefinition def = DataRegistry.GetUnit("camel_rider");
            if (def == null)
            {
                Debug.LogWarning("CamelRiderFactory: no generated UnitDefinition for 'camel_rider' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            // Camel Rider tier ladder - baked in at spawn, not retroactive,
            // same convention as every other combat-unit tier line.
            CamelRiderTierData tier = CamelRiderLineProgress.Current(faction);

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} {tier.Name}"
                : $"Enemy {profile.DisplayName} {tier.Name}";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 6.5f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            // General garrisoning system (2026-09-01): lets this unit
            // be ordered to walk to and enter a friendly GarrisonPoint -
            // see GarrisonSeeker.
            go.AddComponent<GarrisonSeeker>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(((def != null ? def.maxHP : 40f) + tier.HpBonus) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 1f,
                pierceArmor: def != null ? def.pierceArmor : 0f);
            attackable.ConfigureClass(UnitClass.Camel);
            attackable.EnableUpgradeArmorScaling();
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage((def != null ? def.attackDamage : 6f) + tier.DamageBonus);
            attacker.SetRange(def != null ? def.attackRange : 1f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetUnitClass(UnitClass.Camel);
            attacker.EnableUpgradeDamageScaling();
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // Purely cosmetic - see SpearmanFactory's/CavalryFactory's
            // identical notes. Combines both existing props (spear + horse)
            // so this reads as a mounted spear-wielder at a glance, distinct
            // from bare-handed Cavalry - no camel mount exists yet.
            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.RightHand, "Weapons/Spear/scene",
                targetSize: 2.4f, localPositionOffset: new Vector3(0.05f, 0.3f, 0f), localEulerOffset: new Vector3(-15f, 0f, 0f));
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
