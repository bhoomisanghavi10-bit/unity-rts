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
    // Phase 6 unique unit (Rajput): an elite Cavalry variant - Rajput
    // warrior-clan culture was historically centered on mounted combat
    // specifically, distinct from their existing profile bonus (general
    // damage/HP, already applies to every unit). Kept as UnitClass.Cavalry
    // rather than a new class - the existing counter triangle
    // (Infantry>Archer>Cavalry>Infantry, CombatBonus.cs) already applies
    // correctly with zero new balance entries needed, matching the "purely
    // additive" scope this item was planned around. Stats are a flat step
    // up from CavalryFactory across the board (HP/damage), not a
    // sidegrade - it's meant to read as "Rajput's best cavalry", not just
    // a reskinned Cavalry. Only ever trained by Barracks.RequestTrain
    // UniqueUnit when the owning faction's civ is actually Rajput (see
    // UniqueUnitDefinition), same as CavalryFactory reused for the Male
    // Human Character Dummy body/animations pending a real horse-rider
    // model.
    public static class RajputRoyalGuardFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note.
            UnitDefinition def = DataRegistry.GetUnit("rajput_royal_guard");
            if (def == null)
            {
                Debug.LogWarning("RajputRoyalGuardFactory: no generated UnitDefinition for 'rajput_royal_guard' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Royal Guard"
                : $"Enemy {profile.DisplayName} Royal Guard";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.45f;
            agent.height = 2.1f;
            agent.speed = def != null ? def.moveSpeed : 6f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            // General garrisoning system (2026-09-01): lets this unit
            // be ordered to walk to and enter a friendly GarrisonPoint -
            // see GarrisonSeeker.
            go.AddComponent<GarrisonSeeker>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 55f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: (def != null ? def.meleeArmor : 2f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Cavalry),
                pierceArmor: (def != null ? def.pierceArmor : 1f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Cavalry));
            attackable.ConfigureClass(UnitClass.Cavalry);
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 9f);
            attacker.SetRange(def != null ? def.attackRange : 1f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetDamageBonus(UpgradeProgress.DamageBonus(faction) + UpgradeProgress.ClassDamageBonus(faction, UnitClass.Cavalry));
            attacker.SetUnitClass(UnitClass.Cavalry);
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.RightHand, "Weapons/Sword/scene",
                targetSize: 1.1f, localPositionOffset: new Vector3(0.05f, 0.1f, 0f), localEulerOffset: new Vector3(0f, 0f, 90f));
            WeaponAttachment.AttachBeside(
                go, "Mounts/Horse/scene",
                targetSize: 2.4f, localPositionOffset: new Vector3(0f, 0f, -0.6f), localEulerOffset: Vector3.zero);

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
