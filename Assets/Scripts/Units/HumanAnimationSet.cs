using UnityEngine;

namespace KingdomsOfBharat.Units
{
    // Loads and legacy-configures the specific Kevin Iglesias animation
    // clips this project actually uses (Idle, Walk, Gather, Build,
    // Attack), for whichever gender's dummy body a unit was spawned with.
    // Deliberately narrow - five clips, not the whole pack - can extend
    // later as more unit roles/actions need their own animation. "Build"
    // reuses a Hammering clip (the pack ships no dedicated construction
    // animation); "Gather" doubles for Farming/Milking status too.
    public static class HumanAnimationSet
    {
        public readonly struct Clips
        {
            public readonly AnimationClip Idle;
            public readonly AnimationClip Walk;
            public readonly AnimationClip Gather;
            public readonly AnimationClip Build;
            public readonly AnimationClip Attack;

            public Clips(AnimationClip idle, AnimationClip walk, AnimationClip gather, AnimationClip build, AnimationClip attack)
            {
                Idle = idle;
                Walk = walk;
                Gather = gather;
                Build = build;
                Attack = attack;
            }
        }

        public static Clips LoadFor(HumanModelFactory.Gender gender)
        {
            string genderFolder = gender == HumanModelFactory.Gender.Male ? "Male" : "Female";
            string tag = gender == HumanModelFactory.Gender.Male ? "HumanM" : "HumanF";
            string basePath = $"Kevin Iglesias/Human Animations/Animations/{genderFolder}";

            AnimationClip idle = LoadLoop($"{basePath}/Idles/{tag}@Idle01");
            AnimationClip walk = LoadLoop($"{basePath}/Movement/Walk/{tag}@Walk01_Forward");
            AnimationClip gather = LoadLoop($"{basePath}/Work/Gathering/{tag}@Gathering01");
            AnimationClip build = LoadLoop($"{basePath}/Work/Hammering/{tag}@HammeringGround01_R - Loop");
            AnimationClip attack = LoadLoop($"{basePath}/Combat/1H/{tag}@Attack1H01_R");

            return new Clips(idle, walk, gather, build, attack);
        }

        private static AnimationClip LoadLoop(string path)
        {
            AnimationClip clip = Resources.Load<AnimationClip>(path);
            if (clip == null)
            {
                return null;
            }

            // Mecanim-imported clips default to non-legacy; the legacy
            // Animation component (see AnimationDriver) refuses to play
            // anything that isn't flipped to legacy first. Safe to set at
            // runtime regardless of import settings.
            clip.legacy = true;
            clip.wrapMode = WrapMode.Loop;
            return clip;
        }
    }
}
