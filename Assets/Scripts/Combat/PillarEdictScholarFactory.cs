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
    // Workers +50% gather rate (see PillarEdictAura/Gatherer). No model
    // exists, so this reuses the Male Human Character Dummy body/
    // animations like every other unique unit pending a real pack.
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

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
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

            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.RightHand, "Weapons/Sword/scene",
                targetSize: 1f, localPositionOffset: new Vector3(0.05f, 0.05f, 0f), localEulerOffset: new Vector3(0f, 0f, 100f),
                trimToFirstMesh: true);

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
