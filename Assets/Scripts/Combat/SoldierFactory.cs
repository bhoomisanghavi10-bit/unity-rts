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
    // Creates a "Soldier" unit with combat components. Used by Barracks
    // when training finishes, for whichever faction owns the training
    // Barracks. Milestone 19b: uses the shared Human Character Dummy body
    // (Male) instead of the earlier single-pose Axe Warrior model - gives
    // Idle/Walk/Attack animation via AnimationDriver, and Workers/Soldiers
    // now share one consistent body style (see HumanModelFactory).
    public static class SoldierFactory
    {
        // No default faction value, deliberately: a future call site that
        // forgets to pass one should fail to compile, not silently spawn a
        // Player-owned soldier from an AI Barracks.
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            // Baked in at spawn time - see WorkerFactory's identical note.
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note.
            UnitDefinition def = DataRegistry.GetUnit("soldier");
            if (def == null)
            {
                Debug.LogWarning("SoldierFactory: no generated UnitDefinition for 'soldier' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            // Wave 3 item 9: Infantry tier ladder - baked in at spawn, not
            // retroactive, same convention as every other progression
            // system here (see InfantryLineProgress's own comment).
            InfantryTierData tier = InfantryLineProgress.Current(faction);

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} {tier.Name}"
                : $"Enemy {profile.DisplayName} {tier.Name}";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 4f;

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
                meleeArmor: def != null ? def.meleeArmor : 1f,
                pierceArmor: def != null ? def.pierceArmor : 0f);
            attackable.ConfigureClass(UnitClass.Infantry);
            attackable.EnableUpgradeArmorScaling();
            go.AddComponent<HealthBar>();
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage((def != null ? def.attackDamage : 5f) + tier.DamageBonus);
            attacker.SetRange(def != null ? def.attackRange : 1f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetUnitClass(UnitClass.Infantry);
            attacker.EnableUpgradeDamageScaling();
            go.AddComponent<StanceController>();
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // Purely cosmetic - gives Soldier a silhouette distinct from
            // Archer/Cavalry (previously all three shared the exact same
            // bare-handed model). See WeaponAttachment for why the
            // position/rotation offsets below are approximate: this pack's
            // pivot isn't at the grip, so exact hand placement is tuned by
            // eye rather than derived.
            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.RightHand, "Weapons/Sword/scene",
                targetSize: 1f, localPositionOffset: new Vector3(0.05f, 0.05f, 0f), localEulerOffset: new Vector3(0f, 0f, 100f),
                trimToFirstMesh: true);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
