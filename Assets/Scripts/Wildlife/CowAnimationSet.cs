using UnityEngine;

namespace KingdomsOfBharat.Wildlife
{
    // Loads the Shepherd Valley cow pack's animation clips actually used
    // by CowAnimationDriver (Idle, Walk, Eating) - narrower than the
    // pack's full set (also ships IdleBreak, Run), same "ship the core
    // states first" reason BoarAnimationSet/HumanAnimationSet started
    // narrow too. Each clip is bundled inside its own per-clip FBX file
    // (unlike the boar pack's standalone .anim assets), so this tries a
    // direct Resources.Load first and falls back to LoadAll's first
    // result if that comes back empty - covers either way Unity ends up
    // indexing the embedded clip sub-asset.
    public static class CowAnimationSet
    {
        public readonly struct Clips
        {
            public readonly AnimationClip Idle;
            public readonly AnimationClip Walk;
            public readonly AnimationClip Eating;

            public Clips(AnimationClip idle, AnimationClip walk, AnimationClip eating)
            {
                Idle = idle;
                Walk = walk;
                Eating = eating;
            }
        }

        public static Clips Load()
        {
            return new Clips(
                Load("A_Cow_Idle_01"),
                Load("A_Cow_Walk_01"),
                Load("A_Cow_Eating_01"));
        }

        private static AnimationClip Load(string fileName)
        {
            string path = $"AnimalAnimations/Cow/{fileName}";
            AnimationClip clip = Resources.Load<AnimationClip>(path);
            if (clip != null)
            {
                return clip;
            }

            AnimationClip[] clips = Resources.LoadAll<AnimationClip>(path);
            return clips.Length > 0 ? clips[0] : null;
        }
    }
}
