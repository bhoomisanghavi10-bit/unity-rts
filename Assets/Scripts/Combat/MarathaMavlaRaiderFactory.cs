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
    // Roadmap Section 5 item 3 unique unit (Maratha, slot 0 of 2): the
    // Ganimi Kava light-cavalry guerrilla raiders, Maratha's actual
    // signature unit type - a fast glass-cannon raider, not a line-holder
    // (CSV: lower HP but much higher move speed than Rajput's Royal
    // Guard). Kept as UnitClass.Cavalry, same reasoning as
    // RajputRoyalGuardFactory - the existing counter triangle already
    // applies correctly with zero new balance entries needed.
    //
    // Visual closure (2026-08-27 rigging session): a real Meshy-sourced
    // mesh (project-owned) bound onto the same shared Human Character
    // Dummy rig every other human unit uses (Blender, ARMATURE_AUTO
    // weights) - reuses this rig's existing Idle/Walk/Attack clips
    // directly. The sourced model is a standing foot-soldier pose (no
    // horse geometry), so the cosmetic horse mount stays attached
    // separately exactly as it did on the old dummy body, to keep this
    // unit reading as Cavalry; the old cosmetic sword attachment is gone
    // since the new mesh already sculpts its own sword/spear/shield.
    public static class MarathaMavlaRaiderFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            UnitDefinition def = DataRegistry.GetUnit("maratha_mavla_raider");
            if (def == null)
            {
                Debug.LogWarning("MarathaMavlaRaiderFactory: no generated UnitDefinition for 'maratha_mavla_raider' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(
                HumanModelFactory.Gender.Male, position, civilization,
                prefabPathOverride: "UniqueUnits/MavlaRaider/MavlaRaider", applyPaletteMaterial: false);
            HumanModelFactory.ApplyCustomTexture(go, "UniqueUnits/MavlaRaider/MavlaRaider_albedo");
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Mavla Raider"
                : $"Enemy {profile.DisplayName} Mavla Raider";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.45f;
            agent.height = 2.1f;
            agent.speed = def != null ? def.moveSpeed : 8.5f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            // General garrisoning system (2026-09-01): lets this unit
            // be ordered to walk to and enter a friendly GarrisonPoint -
            // see GarrisonSeeker.
            go.AddComponent<GarrisonSeeker>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 32f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 1f,
                pierceArmor: def != null ? def.pierceArmor : 0f);
            attackable.ConfigureClass(UnitClass.Cavalry);
            attackable.EnableUpgradeArmorScaling();
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 7f);
            attacker.SetRange(def != null ? def.attackRange : 1f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetUnitClass(UnitClass.Cavalry);
            attacker.EnableUpgradeDamageScaling();
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

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
