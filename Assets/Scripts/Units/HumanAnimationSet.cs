using UnityEngine;

namespace KingdomsOfBharat.Units
{
    // Loads the animation clips this project actually uses (Idle, Walk,
    // Gather, Mine, Farm, Build, Attack). Idle/Walk/Gather/Farm/Attack come
    // from a shared, gender-agnostic UAL clip pack (UAL1_Standard.fbx /
    // UAL2_Standard.fbx, both under Assets/Resources/human/Human
    // Animations/) retargeted via each clip's own Humanoid Avatar - this
    // project's retargeting is Avatar-based, not skeleton-name-based, so
    // the same clip plays correctly on both the Male and Female dummy
    // bodies. Mine and Build stay on the original per-gender Kevin
    // Iglesias clips (no equivalent exists in the UAL pack). "Milking"
    // (Livestock) has no dedicated clip either and falls back to Farm in
    // AnimationDriver.
    public static class HumanAnimationSet
    {
        public readonly struct Clips
        {
            public readonly AnimationClip Idle;
            public readonly AnimationClip Walk;
            public readonly AnimationClip Gather;
            public readonly AnimationClip Mine;
            public readonly AnimationClip Farm;
            public readonly AnimationClip Build;
            public readonly AnimationClip Attack;

            public Clips(AnimationClip idle, AnimationClip walk, AnimationClip gather, AnimationClip mine, AnimationClip farm, AnimationClip build, AnimationClip attack)
            {
                Idle = idle;
                Walk = walk;
                Gather = gather;
                Mine = mine;
                Farm = farm;
                Build = build;
                Attack = attack;
            }
        }

        private const string Ual1Path = "human/Human Animations/UAL1_Standard";
        private const string Ual2Path = "human/Human Animations/UAL2_Standard";

        public static Clips LoadFor(HumanModelFactory.Gender gender)
        {
            string genderFolder = gender == HumanModelFactory.Gender.Male ? "Male" : "Female";
            string tag = gender == HumanModelFactory.Gender.Male ? "HumanM" : "HumanF";
            string basePath = $"human/Human Animations/Animations/{genderFolder}";

            AnimationClip idle = LoadNamed(Ual1Path, "Armature|Idle_Loop");
            AnimationClip walk = LoadNamed(Ual1Path, "Armature|Walk_Loop");
            AnimationClip gather = LoadNamed(Ual2Path, "Armature|TreeChopping_Loop");
            AnimationClip mine = Load($"{basePath}/Work/Mining/{tag}@MiningOneHand01_R - Ground");
            AnimationClip farm = LoadNamed(Ual2Path, "Armature|Farm_Harvest");
            AnimationClip build = Load($"{basePath}/Work/Hammering/{tag}@HammeringGround01_R - Loop");
            AnimationClip attack = LoadNamed(Ual2Path, "Armature|Sword_Regular_A");

            return new Clips(idle, walk, gather, mine, farm, build, attack);
        }

        private static AnimationClip Load(string path)
        {
            AnimationClip clip = Resources.Load<AnimationClip>(path);
            if (clip == null)
            {
                // A failed load here means AnimationDriver silently keeps
                // playing whatever the last successfully-set clip was
                // (Idle, most likely) instead of switching - looks exactly
                // like "stuck in place while moving" if this is Walk.
                Debug.LogWarning($"HumanAnimationSet: failed to load clip at Resources path '{path}'");
                return null;
            }

            clip.wrapMode = WrapMode.Loop;
            return clip;
        }

        // UAL1_Standard.fbx / UAL2_Standard.fbx each bundle dozens of takes
        // into one multi-clip FBX, so a plain Resources.Load<AnimationClip>
        // on the FBX path resolves ambiguously (it picks whichever clip
        // happens to load first, not the one we want) - this loads every
        // clip at that path and picks the exact-named one instead.
        private static AnimationClip LoadNamed(string path, string clipName)
        {
            AnimationClip[] clips = Resources.LoadAll<AnimationClip>(path);
            foreach (AnimationClip clip in clips)
            {
                if (clip.name == clipName)
                {
                    clip.wrapMode = WrapMode.Loop;
                    return clip;
                }
            }

            Debug.LogWarning($"HumanAnimationSet: failed to find clip '{clipName}' at Resources path '{path}'");
            return null;
        }
    }
}
