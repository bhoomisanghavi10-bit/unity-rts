using UnityEngine;

namespace KingdomsOfBharat.Combat
{
    // Loads the War Elephant's 4 animation clips (Idle/Walk/Attack/Die) -
    // baked into the model FBX itself as 4 separate named takes (unlike
    // HumanAnimationSet's standalone-per-clip pack), sourced from the Asian
    // Elephant rig (see Assets/Resources/UniqueUnits/CREDITS.md) and rebound
    // onto the Meshy war-elephant mesh. Resources.LoadAll is required rather
    // than a single Resources.Load, since an FBX with multiple baked takes
    // exposes them all as sub-assets under one path.
    public static class ElephantAnimationSet
    {
        public readonly struct Clips
        {
            public readonly AnimationClip Idle;
            public readonly AnimationClip Walk;
            public readonly AnimationClip Attack;
            public readonly AnimationClip Die;

            public Clips(AnimationClip idle, AnimationClip walk, AnimationClip attack, AnimationClip die)
            {
                Idle = idle;
                Walk = walk;
                Attack = attack;
                Die = die;
            }
        }

        public static Clips Load()
        {
            AnimationClip[] clips = Resources.LoadAll<AnimationClip>("UniqueUnits/WarElephant/WarElephant");
            return new Clips(
                Find(clips, "Idle"),
                Find(clips, "Walk"),
                Find(clips, "Attack"),
                Find(clips, "Die"));
        }

        private static AnimationClip Find(AnimationClip[] clips, string name)
        {
            foreach (AnimationClip clip in clips)
            {
                if (clip.name == name)
                {
                    return clip;
                }
            }

            Debug.LogWarning($"ElephantAnimationSet: no clip named '{name}' found in UniqueUnits/WarElephant/WarElephant.fbx");
            return null;
        }
    }
}
