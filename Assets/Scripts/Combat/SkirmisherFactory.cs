using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Combat
{
    // Creates a "Skirmisher" unit - Wave 4 item 19
    // (docs/IMPLEMENTATION_ROADMAP.md), the second wholly-new Wave 4 unit
    // (see ScoutFactory for the first). The dedicated anti-archer
    // specialist that closes the counter web's last gap (see CombatBonus:
    // 2x vs Archer, 1.25x received from Infantry). Mirrors ArcherFactory's
    // shape almost exactly - same ranged/Pierce attack pattern, same Male
    // body/animation/bow attachment (no dedicated Skirmisher model/weapon
    // exists yet per docs/YOUR_ACTION_ITEMS.md item 19's own spec - a
    // quilted-armor archer with a forearm buckler - flagging this directly:
    // reusing Archer's shared bow model means a Skirmisher currently looks
    // identical to an Archer in the field, a real future asset need, not
    // just a procedural-shape gap) - the actual differentiator is
    // UnitClass.Skirmisher, not a different model.
    public static class SkirmisherFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            UnitDefinition def = DataRegistry.GetUnit("skirmisher");
            if (def == null)
            {
                Debug.LogWarning("SkirmisherFactory: no generated UnitDefinition for 'skirmisher' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            // Wave 4 item 19: Skirmisher tier ladder - baked in at spawn,
            // not retroactive, same convention as every other line.
            SkirmisherTierData tier = SkirmisherLineProgress.Current(faction);

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} {tier.Name}"
                : $"Enemy {profile.DisplayName} {tier.Name}";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 3.8f;

            var unit = go.AddComponent<Unit>();
            unit.IconKey = "train_skirmisher";
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            // General garrisoning system (2026-09-01): lets this unit
            // be ordered to walk to and enter a friendly GarrisonPoint -
            // see GarrisonSeeker.
            go.AddComponent<GarrisonSeeker>();
            go.AddComponent<RelicCarrier>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(((def != null ? def.maxHP : 20f) + tier.HpBonus) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 0f,
                pierceArmor: def != null ? def.pierceArmor : 0f);
            attackable.ConfigureClass(UnitClass.Skirmisher);
            attackable.EnableUpgradeArmorScaling(melee: false, pierce: true);
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage((def != null ? def.attackDamage : 3f) + tier.DamageBonus);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetRange(def != null ? def.attackRange : 5f);
            attacker.SetDamageType(DamageType.Pierce);
            attacker.SetUnitClass(UnitClass.Skirmisher);
            attacker.EnableUpgradeDamageScaling();
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // Placeholder - see the class-level comment above. Held in the
            // off-hand, same convention ArcherFactory already uses.
            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.LeftHand, "Weapons/Bow/scene",
                targetSize: 1f, localPositionOffset: new Vector3(-0.05f, 0f, 0f), localEulerOffset: new Vector3(0f, 90f, 0f));

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
