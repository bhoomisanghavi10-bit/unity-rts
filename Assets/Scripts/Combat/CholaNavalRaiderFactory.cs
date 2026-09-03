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
    // Phase 6 unique unit (Chola): the Chola Empire's real historical
    // distinction was naval power (their fleet conducted expeditions
    // across the Bay of Bengal) - reflected here as an elite land-based
    // ranged raider rather than a naval unit specifically, since a
    // strictly-naval unique unit would be unusable on RiverValley/
    // Highlands (2 of the 3 maps have no water at all - see
    // MapDefinition.cs), leaving 2/3 of matches without Chola's own
    // identity piece. Kept as UnitClass.Archer (CombatBonus's existing
    // Archer entries - 1.5x vs Cavalry, 0.5x vs Building - apply for free,
    // zero new balance entries), just a stronger version, pairing with
    // their existing economic profile bonus (gather/build cost) rather
    // than duplicating it. No dedicated model exists, so this reuses the
    // Male Human Character Dummy body/animations like every other unit
    // pending a real pack.
    public static class CholaNavalRaiderFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note.
            UnitDefinition def = DataRegistry.GetUnit("chola_naval_raider");
            if (def == null)
            {
                Debug.LogWarning("CholaNavalRaiderFactory: no generated UnitDefinition for 'chola_naval_raider' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Naval Raider"
                : $"Enemy {profile.DisplayName} Naval Raider";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 3.9f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            // General garrisoning system (2026-09-01): lets this unit
            // be ordered to walk to and enter a friendly GarrisonPoint -
            // see GarrisonSeeker.
            go.AddComponent<GarrisonSeeker>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 22f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 0f,
                pierceArmor: def != null ? def.pierceArmor : 1f);
            attackable.ConfigureClass(UnitClass.Archer);
            attackable.EnableUpgradeArmorScaling(melee: false, pierce: true);
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 6f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetRange(def != null ? def.attackRange : 7f);
            attacker.SetDamageType(DamageType.Pierce);
            attacker.SetUnitClass(UnitClass.Archer);
            attacker.EnableUpgradeDamageScaling();
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.LeftHand, "Weapons/Bow/scene",
                targetSize: 1.15f, localPositionOffset: new Vector3(-0.05f, 0f, 0f), localEulerOffset: new Vector3(0f, 90f, 0f));

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(10f);
            }

            return go;
        }
    }
}
