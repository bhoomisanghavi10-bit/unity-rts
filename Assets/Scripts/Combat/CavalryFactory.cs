using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Combat
{
    // Creates a "Cavalry" unit: the third roster addition alongside
    // Soldier/Archer, completing the Infantry > Archer > Cavalry > Infantry
    // counter triangle (see CombatBonus) - fast and hard-hitting against
    // Infantry, vulnerable to Archer fire, dealing plain Melee damage (a
    // mounted charge, not a projectile) so it's resisted by meleeArmor like
    // Soldier. No horse model/pack exists yet, so this reuses the same Male
    // Human Character Dummy body as Soldier/Archer - same "primitive/
    // placeholder until a real pack lands" convention used everywhere else
    // in this project; the speed/cost/damage numbers already make it play
    // distinctly even before it looks distinct.
    public static class CavalryFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Cavalry"
                : $"Enemy {profile.DisplayName} Cavalry";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = 6.5f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(40f * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: 1f + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Cavalry),
                pierceArmor: UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Cavalry));
            attackable.ConfigureClass(UnitClass.Cavalry);
            go.AddComponent<HealthBar>();

            // Phase 6: Rajput's unique tech (Warrior Clans) adds flat
            // Cavalry damage on top of the existing flat/per-class
            // UpgradeProgress bonuses - non-retroactive like every other
            // spawn-time bonus here, only benefits Cavalry trained after
            // the tech finishes.
            float uniqueTechDamageBonus = UniqueTechProgress.HasResearched(faction)
                ? UniqueTechDefinition.For(civilization).CavalryDamageBonus
                : 0f;

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(6f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetDamageBonus(UpgradeProgress.DamageBonus(faction) + UpgradeProgress.ClassDamageBonus(faction, UnitClass.Cavalry) + uniqueTechDamageBonus);
            attacker.SetUnitClass(UnitClass.Cavalry);
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // Purely cosmetic - stands a horse mount behind/around the
            // rider rather than a true seated pose (no rig integration
            // between the human's Idle/Walk clips and the horse's own
            // animations), but reads as "mounted" distinctly from Soldier/
            // Archer's bare-handed silhouette, which is the actual goal.
            WeaponAttachment.AttachBeside(
                go, "Mounts/Horse/scene",
                targetSize: 2.2f, localPositionOffset: new Vector3(0f, 0f, -0.6f), localEulerOffset: Vector3.zero);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
