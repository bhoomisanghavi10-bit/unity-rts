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
    // Wave 4 item 27: Vaidya, the healer half of AoE's Monk - heals a
    // friendly damaged unit via the VaidyaHealer component (see
    // VaidyaHealer.cs). Deliberately unarmed, mirroring
    // Vanik/TradeShip's own "completely unarmed utility unit" shape.
    //
    // Visual closure (2026-09-14): a real rigged glTF mesh ("Meshy AI
    // Wandering Sage biped", user-supplied) replaces the generic dummy
    // body - imported the same way as Purohita's own visual closure
    // (HumanoidGltfRigImporter.DirectHumanBoneMap, since this rig's bone
    // names are also literally Unity's own HumanBodyBones names, not
    // Mixamo-style - confirmed by inspecting the raw glTF node names
    // directly before importing). Kept its own embedded material as-is
    // (applyPaletteMaterial:false, no ApplyCustomTexture call), same
    // "single sourced asset, not a trim-sheet to retint" convention as
    // Purohita/the Villager bodies - no team-color mask exists for this
    // geometry yet, so this doesn't call TeamColorUnitTint - flagging
    // directly per the flag-asset-needs convention: a Blender-painted
    // mask for this mesh would need to be authored before that pilot can
    // extend here. No hand-held prop either (this rig has no staff/vessel
    // geometry) - visually just a plain robed sage figure for now,
    // distinct from Purohita's own delivered mesh.
    public static class VaidyaFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));

            UnitDefinition def = DataRegistry.GetUnit("vaidya");
            if (def == null)
            {
                Debug.LogWarning("VaidyaFactory: no generated UnitDefinition for 'vaidya' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(
                HumanModelFactory.Gender.Male, position, civilization,
                prefabPathOverride: "UniqueUnits/Vaidya/Vaidya", applyPaletteMaterial: false, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Vaidya"
                : $"Enemy {profile.DisplayName} Vaidya";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 3f;

            var unit = go.AddComponent<Unit>();
            unit.IconKey = "train_vaidya";
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            go.AddComponent<GarrisonSeeker>();
            go.AddComponent<VaidyaHealer>();

            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 25f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureClass(UnitClass.Support);
            go.AddComponent<HealthBar>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(8f);
            }

            return go;
        }
    }
}
