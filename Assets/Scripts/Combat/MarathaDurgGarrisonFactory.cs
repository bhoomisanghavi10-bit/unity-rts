using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Buildings;

namespace KingdomsOfBharat.Combat
{
    // Roadmap Section 5 item 3 unique unit (Maratha, slot 1 of 2): the
    // Maratha durg (mountain-fort) network was central to their strategy -
    // reflected as a fortress-defense specialist rather than a line
    // fighter. CSV stats are shaped exactly like Soldier (Infantry, 35 HP,
    // 5 Melee), so this follows the same combat-factory shape as every
    // other unique unit - UnitClass.Infantry - plus one addition:
    // DurgGarrisonWorker, letting it enter an owned Wall/Tower and strip
    // Siege's 3x anti-building bonus while inside (see Garrison/
    // Attackable.SiegeImmune).
    //
    // Visual closure (2026-08-27 rigging session): a real Meshy-sourced
    // mesh (project-owned) bound onto the same shared Human Character
    // Dummy rig every other human unit uses (Blender, ARMATURE_AUTO
    // weights) - reuses this rig's existing Idle/Walk/Attack clips
    // directly. The mesh already sculpts its own sword/shield, so the old
    // cosmetic sword attachment (a placeholder for the shared dummy body)
    // is gone.
    public static class MarathaDurgGarrisonFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            UnitDefinition def = DataRegistry.GetUnit("maratha_durg_garrison");
            if (def == null)
            {
                Debug.LogWarning("MarathaDurgGarrisonFactory: no generated UnitDefinition for 'maratha_durg_garrison' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(
                HumanModelFactory.Gender.Male, position, civilization,
                prefabPathOverride: "UniqueUnits/DurgGarrison/DurgGarrison", applyPaletteMaterial: false);
            HumanModelFactory.ApplyCustomTexture(go, "UniqueUnits/DurgGarrison/DurgGarrison_albedo");
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Durg Garrison"
                : $"Enemy {profile.DisplayName} Durg Garrison";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 3.5f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 35f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: (def != null ? def.meleeArmor : 1f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Infantry),
                pierceArmor: (def != null ? def.pierceArmor : 0f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Infantry));
            attackable.ConfigureClass(UnitClass.Infantry);
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 5f);
            attacker.SetRange(def != null ? def.attackRange : 1f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetDamageBonus(UpgradeProgress.DamageBonus(faction) + UpgradeProgress.ClassDamageBonus(faction, UnitClass.Infantry));
            attacker.SetUnitClass(UnitClass.Infantry);
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);
            go.AddComponent<DurgGarrisonWorker>();

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
