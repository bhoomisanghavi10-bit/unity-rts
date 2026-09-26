using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Combat
{
    // Creates an "Archer" unit: AoE-style ranged counterpart to Soldier -
    // lower HP, hits from range instead of needing melee contact, and
    // deals Pierce damage (so it's resisted by pierceArmor, not
    // meleeArmor - see Attackable). Shares SoldierFactory's body/rig
    // (no dedicated archer body model is sourced yet - flagging directly,
    // per this project's own convention for real art gaps) but now has its
    // own presentation binding on top of that shared body: a ranged
    // draw/release Attack clip instead of the melee sword swing
    // (HumanAnimationSet.AttackStyle.Ranged), a bow held in the off-hand,
    // and a visible arrow (MeleeAttacker.SetProjectile/Projectile) that
    // flies to the target and only applies the hit on arrival, synced to
    // the draw/release animation rather than firing instantly. See
    // Projectile.cs and MeleeAttacker's own comments for the full flow.
    public static class ArcherFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            if (CivilizationRegistry.For(faction) == CivilizationId.Chola && ArcherLineProgress.Tier(faction) == 0)
                return DefinitionCatalog.Default.Spawn(DefinitionCatalog.CholaDhanurdhara, position, faction);
            return SpawnCore(position, faction, ArcherLineProgress.Current(faction));
        }

        internal static GameObject SpawnCore(Vector3 position, FactionId faction, ArcherTierData tier)
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

            // Wave 3 item 11: Archer tier ladder - baked in at spawn, not
            // retroactive, same convention as InfantryLineProgress/
            // SpearmanLineProgress.

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} {tier.Name}"
                : $"Enemy {profile.DisplayName} {tier.Name}";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 3.8f;

            var unit = go.AddComponent<Unit>();
            unit.IconKey = "train_archer";
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            // General garrisoning system (2026-09-01): lets this unit
            // be ordered to walk to and enter a friendly GarrisonPoint -
            // see GarrisonSeeker.
            go.AddComponent<GarrisonSeeker>();
            go.AddComponent<RelicCarrier>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(((def != null ? def.maxHP : 18f) + tier.HpBonus) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 0f,
                pierceArmor: def != null ? def.pierceArmor : 0f);
            attackable.ConfigureClass(UnitClass.Archer);
            attackable.EnableUpgradeArmorScaling(melee: false, pierce: true);
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage((def != null ? def.attackDamage : 4f) + tier.DamageBonus);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetRange(def != null ? def.attackRange : 6f);
            attacker.SetDamageType(DamageType.Pierce);
            attacker.SetUnitClass(UnitClass.Archer);
            attacker.EnableUpgradeDamageScaling();
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);

            // Archer gets its own ranged presentation binding, not the
            // shared melee one every other human unit uses - see
            // HumanAnimationSet.AttackStyle.Ranged's own comment for why
            // the Attack clip specifically differs (OverhandThrow instead
            // of a sword swing). Keeping the returned Clips/driver so
            // MeleeAttacker's projectile mode can read the exact same
            // Attack clip's live playback position below.
            HumanAnimationSet.Clips clips = HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male, HumanAnimationSet.AttackStyle.Ranged);
            var animationDriver = go.AddComponent<AnimationDriver>();
            animationDriver.Configure(clips, agent, unit);

            // Purely cosmetic - see SoldierFactory's identical note on why
            // the offsets are approximate. Held in the off-hand so a
            // Soldier (sword, right hand) and Archer (bow, left hand)
            // silhouette differently even at a glance.
            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.LeftHand, "Weapons/Bow/scene",
                targetSize: 1f, localPositionOffset: new Vector3(-0.05f, 0f, 0f), localEulerOffset: new Vector3(0f, 90f, 0f));

            // Presentation: fire a visible arrow synced to the draw/release
            // animation instead of resolving the hit the instant the
            // cooldown allows it - see MeleeAttacker.SetProjectile and
            // Projectile. Release marker (0.55) is roughly the midpoint of
            // OverhandThrow's own forward-thrust phase, tuned by eye
            // against the clip in Play mode (same "tuned by eye, not
            // derived" convention WeaponAttachment's own offsets already
            // use) - retune here if a purpose-made bow-draw clip is ever
            // sourced. Projectile speed (16) is fast enough to read as an
            // arrow, not so fast it arrives before the release pose is
            // even visible at this unit's own attack range.
            attacker.SetProjectile(animationDriver, clips.Attack, projectileSpeed: 16f, releaseNormalizedTime: 0.55f);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (VisionSource.IsTracked(faction))
            {
                go.AddComponent<VisionSource>().Configure(9f);
            }

            return go;
        }
    }
}
