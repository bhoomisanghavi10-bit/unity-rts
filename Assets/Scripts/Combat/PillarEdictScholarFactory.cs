using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Combat
{
    // Roadmap Section 5 item 3 unique unit (Maurya, slot 1 of 2): reflects
    // Ashoka's edict pillars/administrative reach, reimagined as a support
    // unit rather than a pure fighter. CSV Category is Infantry with real
    // (if modest, Worker-self-defense-tier) combat stats, so this follows
    // the same combat-factory shape as every other unique unit -
    // UnitClass.Infantry, kept in line with Soldier's own weapon/armor
    // shape - plus one addition: PillarEdictAura, giving nearby friendly
    // Workers +50% gather rate (see PillarEdictAura/Gatherer).
    //
    // Visual closure (2026-08-27 rigging session): a real Meshy-sourced
    // mesh (project-owned) bound onto the same shared Human Character
    // Dummy rig every other human unit uses (Blender, ARMATURE_AUTO
    // weights) - reuses this rig's existing Idle/Walk/Attack clips
    // directly, no new animation source needed. The mesh already sculpts
    // its own held book/robes, so the old cosmetic sword attachment (a
    // placeholder for the shared dummy body) is gone.
    public static class PillarEdictScholarFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            UnitDefinition def = DataRegistry.GetUnit("pillar_edict_scholar");
            if (def == null)
            {
                Debug.LogWarning("PillarEdictScholarFactory: no generated UnitDefinition for 'pillar_edict_scholar' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(
                HumanModelFactory.Gender.Male, position, civilization,
                prefabPathOverride: "UniqueUnits/PillarEdictScholar/PillarEdictScholar", applyPaletteMaterial: false);
            HumanModelFactory.ApplyCustomTexture(go, "UniqueUnits/PillarEdictScholar/PillarEdictScholar_albedo");
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Pillar Edict Scholar"
                : $"Enemy {profile.DisplayName} Pillar Edict Scholar";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 3.5f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 30f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: (def != null ? def.meleeArmor : 0f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Infantry),
                pierceArmor: (def != null ? def.pierceArmor : 0f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Infantry));
            attackable.ConfigureClass(UnitClass.Infantry);
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 4f);
            attacker.SetRange(def != null ? def.attackRange : 1f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetDamageBonus(UpgradeProgress.DamageBonus(faction) + UpgradeProgress.ClassDamageBonus(faction, UnitClass.Infantry));
            attacker.SetUnitClass(UnitClass.Infantry);
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);
            go.AddComponent<PillarEdictAura>().Configure(faction);

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
