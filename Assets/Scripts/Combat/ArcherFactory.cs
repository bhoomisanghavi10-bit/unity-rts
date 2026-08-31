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
    // Creates an "Archer" unit: AoE-style ranged counterpart to Soldier -
    // lower HP, hits from range instead of needing melee contact, and
    // deals Pierce damage (so it's resisted by pierceArmor, not
    // meleeArmor - see Attackable). Mirrors SoldierFactory's shape almost
    // exactly, sharing the same Male body model since no dedicated archer
    // model/animation exists yet (same "primitive/placeholder until a real
    // pack lands" convention already used elsewhere in this project).
    public static class ArcherFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note.
            UnitDefinition def = DataRegistry.GetUnit("archer");
            if (def == null)
            {
                Debug.LogWarning("ArcherFactory: no generated UnitDefinition for 'archer' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Archer"
                : $"Enemy {profile.DisplayName} Archer";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 3.8f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            // General garrisoning system (2026-09-01): lets this unit
            // be ordered to walk to and enter a friendly GarrisonPoint -
            // see GarrisonSeeker.
            go.AddComponent<GarrisonSeeker>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 18f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 0f,
                pierceArmor: (def != null ? def.pierceArmor : 0f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Archer));
            attackable.ConfigureClass(UnitClass.Archer);
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 4f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetDamageBonus(UpgradeProgress.DamageBonus(faction) + UpgradeProgress.ClassDamageBonus(faction, UnitClass.Archer));
            attacker.SetRange(def != null ? def.attackRange : 6f);
            attacker.SetDamageType(DamageType.Pierce);
            attacker.SetUnitClass(UnitClass.Archer);
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // Purely cosmetic - see SoldierFactory's identical note on why
            // the offsets are approximate. Held in the off-hand so a
            // Soldier (sword, right hand) and Archer (bow, left hand)
            // silhouette differently even at a glance.
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
