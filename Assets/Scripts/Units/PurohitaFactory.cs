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
    // Wave 4 item 27: Purohita, the converter half of AoE's Monk - a
    // chance to flip a living enemy unit to this unit's own faction via the
    // PurohitaConverter component (see PurohitaConverter.cs). Deliberately
    // unarmed (no MeleeAttacker), mirroring VaidyaFactory's own shape.
    //
    // Visual closure (2026-09-14): a real rigged glTF mesh ("Meshy AI
    // Sacred Pilgrim biped", project-owned) replaces the generic dummy
    // body - imported the same way as the Female/Male Villager swap
    // (HumanoidGltfRigImporter, since glTFast doesn't auto-build a
    // Humanoid Avatar). This rig's bone names are literally Unity's own
    // HumanBodyBones names (not Mixamo-style), and it has no separate
    // Spine node - just Hips -> Chest -> UpperChest -> Neck -> Head - so
    // HumanoidGltfRigImporter.DirectHumanBoneMap maps Chest to the
    // mandatory Spine slot and UpperChest to the optional Chest slot.
    // Kept its own embedded material as-is (applyPaletteMaterial:false,
    // no ApplyCustomTexture call), same "single sourced asset, not a
    // trim-sheet to retint" convention as the Villager bodies - no team-
    // color mask exists for this geometry yet (a different UV layout than
    // the earlier discarded delivery), so unlike MarathaMavlaRaiderFactory
    // this doesn't call TeamColorUnitTint yet - flagging directly per the
    // flag-asset-needs convention: a Blender-painted mask for this mesh
    // would need to be authored before that pilot can extend here. No
    // hand-held prop either (this rig has no staff/bell geometry, unlike
    // the earlier discarded delivery) - visually just a plain robed
    // figure for now.
    public static class PurohitaFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));

            UnitDefinition def = DataRegistry.GetUnit("purohita");
            if (def == null)
            {
                Debug.LogWarning("PurohitaFactory: no generated UnitDefinition for 'purohita' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(
                HumanModelFactory.Gender.Male, position, civilization,
                prefabPathOverride: "UniqueUnits/Purohita/Purohita", applyPaletteMaterial: false, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} Purohita"
                : $"Enemy {profile.DisplayName} Purohita";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 3f;

            var unit = go.AddComponent<Unit>();
            unit.IconKey = "train_purohita";
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            go.AddComponent<GarrisonSeeker>();
            go.AddComponent<PurohitaConverter>();

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
