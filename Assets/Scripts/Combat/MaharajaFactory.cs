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
    // Wave 4 item 28: the Hero unit - Maharaja, one per faction (see
    // Durg.RequestTrainHero/HeroProgress for the population-cap-of-1 and
    // Regicide bookkeeping). Unlike the other unique units
    // (UniqueUnitDefinition), Maharaja isn't civ-flavored in stats - a
    // single shared factory read by every civ, matching SoldierFactory's
    // "one factory, civ read via CivilizationRegistry.For" shape rather
    // than RajputRoyalGuardFactory's one-civ-per-file shape. User-confirmed
    // design call: the strongest unit in the game, not AoE's defenseless
    // King - stats sit clearly above every other unit's ceiling (the
    // Elephant line's own Imperial-elite tier, ~130 HP/~17 dmg, was the
    // prior high point). UnitClass.Hero (not Cavalry) - CombatBonus has no
    // entries for Hero in either direction, so it falls through to the 1x
    // default, same as Vaidya/Purohita's own Support class.
    //
    // No dedicated model exists - flagging directly per the flag-asset-
    // needs convention: reuses the shared Human Character Dummy body plus
    // the same Sword/Horse prop combo as RajputRoyalGuardFactory, so a
    // Maharaja is visually identical to a Royal Guard/Cavalry hybrid, no
    // distinct regalia or silhouette.
    public static class MaharajaFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            UnitDefinition def = DataRegistry.GetUnit("maharaja");
            if (def == null)
            {
                Debug.LogWarning("MaharajaFactory: no generated UnitDefinition for 'maharaja' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Maharaja"
                : $"Enemy {profile.DisplayName} Maharaja";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2.2f;
            agent.speed = def != null ? def.moveSpeed : 5f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            go.AddComponent<GarrisonSeeker>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 220f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 6f,
                pierceArmor: def != null ? def.pierceArmor : 5f);
            attackable.ConfigureClass(UnitClass.Hero);
            attackable.EnableUpgradeArmorScaling();
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 24f);
            attacker.SetRange(def != null ? def.attackRange : 1.2f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetUnitClass(UnitClass.Hero);
            attacker.EnableUpgradeDamageScaling();
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.RightHand, "Weapons/Sword/scene",
                targetSize: 1.2f, localPositionOffset: new Vector3(0.05f, 0.1f, 0f), localEulerOffset: new Vector3(0f, 0f, 90f));
            WeaponAttachment.AttachBeside(
                go, "Mounts/Horse/scene",
                targetSize: 2.5f, localPositionOffset: new Vector3(0f, 0f, -0.6f), localEulerOffset: Vector3.zero);

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(9f);
            }

            return go;
        }
    }
}
