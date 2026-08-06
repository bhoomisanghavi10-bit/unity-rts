using UnityEngine;

namespace KingdomsOfBharat.Units
{
    // Loads the specific Kevin Iglesias animation clips this project
    // actually uses (Idle, Walk, Gather, Mine, Farm, Build, Attack), for
    // whichever gender's dummy body a unit was spawned with. Deliberately
    // narrow - seven clips, not the whole pack - can extend later as more
    // unit roles/actions need their own animation. "Build" reuses a
    // Hammering clip (the pack ships no dedicated construction
    // animation); "Milking" (Livestock) has no dedicated clip either and
    // falls back to Farm in AnimationDriver.
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

        public static Clips LoadFor(HumanModelFactory.Gender gender)
        {
            string genderFolder = gender == HumanModelFactory.Gender.Male ? "Male" : "Female";
            string tag = gender == HumanModelFactory.Gender.Male ? "HumanM" : "HumanF";
            string basePath = $"Kevin Iglesias/Human Animations/Animations/{genderFolder}";

            AnimationClip idle = Load($"{basePath}/Idles/{tag}@Idle01");
            AnimationClip walk = Load($"{basePath}/Movement/Walk/{tag}@Walk01_Forward");
            AnimationClip gather = Load($"{basePath}/Work/Gathering/{tag}@Gathering01");
            AnimationClip mine = Load($"{basePath}/Work/Mining/{tag}@MiningOneHand01_R - Ground");
            AnimationClip farm = Load($"{basePath}/Work/Farming/{tag}@FarmingWithPlow01_R - Loop");
            AnimationClip build = Load($"{basePath}/Work/Hammering/{tag}@HammeringGround01_R - Loop");
            AnimationClip attack = Load($"{basePath}/Combat/1H/{tag}@Attack1H01_R");

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
    }
}
