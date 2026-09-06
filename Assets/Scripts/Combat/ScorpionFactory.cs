using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Combat
{
    // Wave 4 item 23 (docs/IMPLEMENTATION_ROADMAP.md): the Scorpion - a
    // dedicated anti-infantry siege weapon. Distinct from the existing
    // Siege (Mangonel-like) unit in both role and mechanic: Siege is
    // anti-BUILDING (CombatBonus 3x vs Building) with radius splash around
    // the impact point; Scorpion is anti-INFANTRY (CombatBonus 2x vs
    // Infantry, see UnitClass.Scorpion/CombatBonus) with a bolt that
    // pierces THROUGH its primary target and keeps going in a straight
    // line, hitting anyone else hostile standing behind it at full damage
    // (MeleeAttacker.SetPierceThrough) - "a raycast-through code path for
    // pass-through damage," per this item's own roadmap text, not a new
    // damage type (DamageType.Pierce already existed). Weak to Cavalry
    // (CombatBonus 1.5x, reusing Cavalry->Infantry's own value) - a fast
    // unit closes the gap on this unarmored, slow-moving engine before it
    // gets more than a shot or two off, same vulnerability real AoE
    // Scorpions have. Base stats/cost/range mirror SiegeFactory's own
    // shape (a siege-class engine, slow, no GarrisonSeeker - siege units
    // don't garrison, same exclusion Siege itself already has) but with
    // Pierce damage and a longer range like Archer/CavalryArcher. No
    // dedicated siege-engine model exists yet ("machine only, no crew" per
    // docs/YOUR_ACTION_ITEMS.md item 23) - reuses the same Male Human
    // Character Dummy body plus the Bow prop like Archer, since no
    // Scorpion machine model exists - flagging directly per the
    // flag-asset-needs convention: a Scorpion currently looks identical to
    // an Archer in the field, with no wheeled/tripod silhouette at all.
    public static class ScorpionFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note.
            UnitDefinition def = DataRegistry.GetUnit("scorpion");
            if (def == null)
            {
                Debug.LogWarning("ScorpionFactory: no generated UnitDefinition for 'scorpion' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            // Scorpion tier ladder - baked in at spawn, not retroactive,
            // same convention as every other combat-unit tier line.
            ScorpionTierData tier = ScorpionLineProgress.Current(faction);

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} {tier.Name}"
                : $"Enemy {profile.DisplayName} {tier.Name}";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 1.6f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(((def != null ? def.maxHP : 35f) + tier.HpBonus) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 0f,
                pierceArmor: def != null ? def.pierceArmor : 0f);
            attackable.ConfigureClass(UnitClass.Scorpion);
            attackable.EnableUpgradeArmorScaling(melee: false, pierce: true);
            go.AddComponent<Repairable>();
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage((def != null ? def.attackDamage : 12f) + tier.DamageBonus);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetRange(def != null ? def.attackRange : 6f);
            attacker.SetDamageType(DamageType.Pierce);
            attacker.SetUnitClass(UnitClass.Scorpion);
            attacker.EnableUpgradeDamageScaling();
            // The whole point of this unit - see this file's own header
            // comment and MeleeAttacker.SetPierceThrough. 3f lets the bolt
            // reach a second rank standing directly behind the primary
            // target in a typical Line formation (unitSpacing 1.5 by
            // default - see FormationDefinition), without extending so far
            // it reaches a third.
            attacker.SetPierceThrough(3f);
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // Purely cosmetic - see ArcherFactory's identical note on why
            // the offsets are approximate. No dedicated Scorpion machine
            // model exists yet, so this reuses the same bow prop Archer
            // uses rather than a wheeled/tripod siege-engine mesh.
            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.LeftHand, "Weapons/Bow/scene",
                targetSize: 1f, localPositionOffset: new Vector3(-0.05f, 0f, 0f), localEulerOffset: new Vector3(0f, 90f, 0f));

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(7f);
            }

            return go;
        }
    }
}
