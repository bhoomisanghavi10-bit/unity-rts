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
    // Roadmap Section 5 item 3 unique unit (Maurya, slot 0 of 2): the
    // heaviest elephant in the roster, reflecting Ashoka-era Maurya's
    // historically signature war-elephant corps. Kept as UnitClass.Siege,
    // same reasoning as VijayanagaraWarElephantFactory (gets CombatBonus's
    // existing 3x Building bonus for free) - a step up from that unit
    // across the board (HP/damage/cost) rather than a sidegrade, matching
    // the CSV's own "the heaviest elephant" framing.
    //
    // Visual closure (2026-08-27 rigging session): a real armored-elephant
    // mesh (Meshy AI, project-owned) bound onto a real elephant skeleton -
    // not the shared Human Dummy body, and not a generic rig either. The
    // skeleton and its Idle/Walk/Attack/Die clips come from a separate,
    // real, third-party rigged model (CC-BY, see
    // Assets/Resources/UniqueUnits/CREDITS.md); only the rig/animation was
    // reused, the source model's own mesh was discarded and this mesh
    // bound onto it instead (Blender, ARMATURE_AUTO weights). Generic
    // (non-Humanoid) rig type, driven by ElephantAnimationDriver - a
    // Playables setup mirroring BoarAnimationDriver's, not AnimationDriver's
    // (this rig has no muscle-space Humanoid Avatar). See
    // docs/SESSION_LOG.md for the full rigging methodology and known
    // caveats (the Die clip's automatic-weight bind strains under its more
    // extreme late-clip poses - a real first-pass limitation, not fixed
    // this session).
    public static class MauryaWarElephantFactory
    {
        private const float ModelHeight = 2.4f;

        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            UnitDefinition def = DataRegistry.GetUnit("maurya_war_elephant");
            if (def == null)
            {
                Debug.LogWarning("MauryaWarElephantFactory: no generated UnitDefinition for 'maurya_war_elephant' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject root = new GameObject(faction == FactionId.Player
                ? $"{profile.DisplayName} War Elephant"
                : $"Enemy {profile.DisplayName} War Elephant");
            root.transform.position = position;

            GameObject prefab = Resources.Load<GameObject>("UniqueUnits/WarElephant/WarElephant");
            GameObject model = Object.Instantiate(prefab, root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            HumanModelFactory.ApplyCustomTexture(model, "UniqueUnits/WarElephant/WarElephant_albedo");

            float groundY = GroundReference.TryGetHeight(position, out float height) ? height : position.y - 1f;
            AlignFeetToGround(model, groundY);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.6f;
            agent.height = ModelHeight;
            agent.speed = def != null ? def.moveSpeed : 2f;

            var unit = root.AddComponent<Unit>();
            root.AddComponent<UnitMover>();
            root.AddComponent<SelectionIndicator>();
            // General garrisoning system (2026-09-01): lets this unit be
            // ordered to walk to and enter a friendly GarrisonPoint - see
            // GarrisonSeeker.
            root.AddComponent<GarrisonSeeker>();
            var attackable = root.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 100f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 2f,
                pierceArmor: def != null ? def.pierceArmor : 2f);
            attackable.ConfigureClass(UnitClass.Siege);
            attackable.EnableUpgradeArmorScaling();
            root.AddComponent<HealthBar>();

            var attacker = root.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 11f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetRange(def != null ? def.attackRange : 2.5f);
            attacker.SetUnitClass(UnitClass.Siege);
            attacker.EnableUpgradeDamageScaling();
            // Wave 0 item 4 (docs/IMPLEMENTATION_ROADMAP.md): a trampling
            // war elephant, not just a heavy melee hit - DamageType.Trample
            // was declared and unused until this item. Reuses the splash-
            // radius mechanic CavalryFactory's own trample already
            // established (item 3, PARTIAL_ELEMENTS_FIX_PLAN.md) rather
            // than inventing a second one: a radius below GroupFormation's
            // 1.5 default unit spacing (still "clumped tight around the
            // impact point", not a full adjacent rank) at a reduced
            // secondary-damage multiplier, kept a shade larger/heavier than
            // Cavalry's own 1.25/0.35 to read as a bulkier animal's
            // footprint without turning this into Siege-tier splash.
            attacker.SetDamageType(DamageType.Trample);
            attacker.SetSplashRadius(1.4f, 0.4f);
            root.AddComponent<StanceController>();

            root.AddComponent<FactionMember>().Configure(faction);
            root.AddComponent<ElephantAnimationDriver>().Configure(ElephantAnimationSet.Load(), agent, attackable, attacker);

            var collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 0.6f;
            collider.height = ModelHeight;
            root.AddComponent<GroundFollower>().Configure(model.transform);

            if (faction == FactionId.Player)
            {
                root.AddComponent<VisionSource>().Configure(7f);
            }

            return root;
        }

        private static void AlignFeetToGround(GameObject model, float groundY)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float correction = groundY - bounds.min.y;
            model.transform.position += new Vector3(0f, correction, 0f);
        }
    }
}
