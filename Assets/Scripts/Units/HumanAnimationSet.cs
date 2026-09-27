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
            return LoadFor(gender, AttackStyle.Melee);
        }

        // Which Attack clip LoadFor should bind - Melee (a sword swing, the
        // long-standing default for every non-ranged human unit) or Ranged
        // (see below). Kept as a small enum rather than a bool so a future
        // 3rd style (e.g. a thrown-weapon animation for Siege/Scorpion,
        // per this ticket's "pattern for later work" note) has somewhere
        // to go without another parameter rename.
        public enum AttackStyle { Melee, Ranged }

        // Ranged units (Archer - see ArcherFactory) previously loaded the
        // exact same Attack clip as every melee unit (Sword_Regular_A, a
        // sword swing), which is why an archer's "attack" animation read
        // as a soldier miming a sword strike with no sword in hand. Neither
        // UAL pack ships a dedicated bow-draw/release clip, so this uses
        // OverhandThrow instead (UAL2's own throwing-motion clip: the arm
        // draws back then thrusts forward) - the closest available motion
        // to a bow draw-and-release, and categorically closer than a sword
        // swing for a unit holding a bow. Flagging directly, per this
        // project's own "always flag when a task needs a real art asset"
        // convention: a purpose-made bow-draw/release clip would read
        // better and should replace this if one is ever sourced.
        public static Clips LoadFor(HumanModelFactory.Gender gender, AttackStyle attackStyle)
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
            AnimationClip attack = attackStyle == AttackStyle.Ranged
                ? LoadNamed(Ual2Path, "Armature|OverhandThrow")
                : LoadNamed(Ual2Path, "Armature|Sword_Regular_A");

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
