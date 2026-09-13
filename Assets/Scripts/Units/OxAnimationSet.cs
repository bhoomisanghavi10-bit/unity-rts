using UnityEngine;

namespace KingdomsOfBharat.Units
{
    // Loads the retargeted pack-ox clips (see OxModelFactory's own header
    // comment for how these were produced) - narrower than Cow's own set
    // (no Eating clip; a trader has nothing analogous to being milked),
    // same "ship the core states first" convention CowAnimationSet/
    // BoarAnimationSet/HumanAnimationSet all already established. Each
    // clip is a standalone .anim asset (not embedded in an FBX sub-asset
    // like Cow's own pack), since the source FBX's baked clip paths needed
    // rewriting to match the displayed glTF prefab's flatter hierarchy -
    // Resources.Load<AnimationClip> works identically either way.
    public static class OxAnimationSet
    {
        public readonly struct Clips
        {
            public readonly AnimationClip Idle;
            public readonly AnimationClip Walk;

            public Clips(AnimationClip idle, AnimationClip walk)
            {
                Idle = idle;
                Walk = walk;
            }
        }

        public static Clips Load()
        {
            return new Clips(
                Load("A_Ox_Idle_01"),
                Load("A_Ox_Walk_01"));
        }

        private static AnimationClip Load(string fileName)
        {
            return Resources.Load<AnimationClip>($"AnimalAnimations/Ox/{fileName}");
        }
    }
}
