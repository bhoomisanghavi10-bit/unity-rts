using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Units
{
    // Wave 4 item 26: Vanik, the land Trader - shuttles Gold between owned/
    // allied Markets via the Trader component (see Trader.cs). Deliberately
    // unarmed, unlike Worker's weak self-defense MeleeAttacker - matches
    // AoE's own Trade Cart (fragile, must be escorted, can't fight back).
    //
    // Visual closure (2026-09-14): a real rigged pack-ox model (user-
    // supplied Meshy AI GLB) replaces the placeholder Human Character
    // Dummy body this unit previously reused - a beast of burden fits a
    // Trader far better than a soldier-shaped human, and matches AoE's own
    // Trade Cart being pulled/carried rather than walked on foot. See
    // OxModelFactory's own header comment for the model/animation pipeline
    // (a Cow-pack walk clip retargeted onto this rig's legs/tail in
    // Blender, since the two skeletons share no bone names).
    public static class VanikFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));

            UnitDefinition def = DataRegistry.GetUnit("vanik");
            if (def == null)
            {
                Debug.LogWarning("VanikFactory: no generated UnitDefinition for 'vanik' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = OxModelFactory.Spawn(position);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Vanik"
                : $"Enemy {profile.DisplayName} Vanik";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.6f;
            agent.height = 1.6f;
            agent.speed = def != null ? def.moveSpeed : 3.5f;

            var unit = go.AddComponent<Unit>();
            unit.IconKey = "train_vanik";
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            go.AddComponent<GarrisonSeeker>();
            go.AddComponent<Trader>();

            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 25f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureClass(UnitClass.Support);
            go.AddComponent<HealthBar>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<OxAnimationDriver>().Configure(OxAnimationSet.Load(), agent);

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
