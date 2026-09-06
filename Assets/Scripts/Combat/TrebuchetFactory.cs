using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Combat
{
    // Wave 4 item 24 (docs/IMPLEMENTATION_ROADMAP.md): the Trebuchet
    // ("Maha Yantra") - a long-range anti-building specialist with a hard
    // minimum range (see MeleeAttacker.SetMinRange), Imperial-only, and the
    // first single-tier trainable combat unit in the project (no
    // TrebuchetLineProgress - unlike Siege/Scorpion/BatteringRam, which all
    // turned out to be tier lines despite reading like flat units at a
    // glance). Also the first live consumer of DamageType.Siege (declared
    // in Wave 0 item 4, unused by any attacker until now - Attackable's
    // UsesPierceArmor already routes Siege to meleeArmor identically to
    // Melee, so this needed no engine change). Base stats/shape mirror
    // SiegeFactory/ScorpionFactory (a siege-class engine, slow, no
    // GarrisonSeeker - siege units don't garrison, same exclusion those two
    // already have) but with a far longer range and the new minimum-range
    // gate instead of splash/pierce-through. No dedicated siege-engine
    // model exists yet (docs/YOUR_ACTION_ITEMS.md items 6/7/24 already
    // track the real pack/unpack mesh-state need) - reuses the same Male
    // Human Character Dummy body plus the Bow prop like Archer/Scorpion -
    // flagging directly per the flag-asset-needs convention: a Trebuchet
    // currently looks identical to an Archer/Scorpion in the field, with no
    // wheeled frame/counterweight silhouette and no packed/deployed states
    // at all.
    public static class TrebuchetFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            UnitDefinition def = DataRegistry.GetUnit("trebuchet");
            if (def == null)
            {
                Debug.LogWarning("TrebuchetFactory: no generated UnitDefinition for 'trebuchet' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Maha Yantra"
                : $"Enemy {profile.DisplayName} Maha Yantra";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 1.0f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 60f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 0f,
                pierceArmor: def != null ? def.pierceArmor : 0f);
            attackable.ConfigureClass(UnitClass.Trebuchet);
            attackable.EnableUpgradeArmorScaling();
            go.AddComponent<Repairable>();
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 20f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetRange(def != null ? def.attackRange : 10f);
            attacker.SetMinRange(4f);
            attacker.SetDamageType(DamageType.Siege);
            attacker.SetUnitClass(UnitClass.Trebuchet);
            attacker.EnableUpgradeDamageScaling();
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // Purely cosmetic - see ArcherFactory's identical note. No
            // dedicated Trebuchet machine model exists yet, so this reuses
            // the same bow prop Archer/Scorpion use rather than a
            // wheeled/counterweight siege-engine mesh.
            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.LeftHand, "Weapons/Bow/scene",
                targetSize: 1f, localPositionOffset: new Vector3(-0.05f, 0f, 0f), localEulerOffset: new Vector3(0f, 90f, 0f));

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(9f);
            }

            return go;
        }
    }
}
