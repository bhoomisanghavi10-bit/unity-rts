using UnityEngine;

namespace KingdomsOfBharat.Wildlife
{
    // Loads the "Red Cambala" wild boar pack's animation clips actually
    // used by BoarAnimationDriver (Idle, Walk, Attack1, Death1) -
    // narrower than the pack's full set (also ships Attack2, Death2,
    // Hit1/2, Run, Stun, StunGet, StunOut), same "ship the core states
    // first" reason HumanAnimationSet started narrow too. Each clip is
    // its own individual AnimationClip asset under
    // Resources/AnimalAnimations/WildBoar/ (not bundled inside an FBX),
    // so a plain Resources.Load is enough - no LoadAll/sub-asset lookup
    // needed here.
    public static class BoarAnimationSet
    {
        public readonly struct Clips
        {
            public readonly AnimationClip Idle;
            public readonly AnimationClip Walk;
            public readonly AnimationClip Attack;
            public readonly AnimationClip Death;

            public Clips(AnimationClip idle, AnimationClip walk, AnimationClip attack, AnimationClip death)
            {
                Idle = idle;
                Walk = walk;
                Attack = attack;
                Death = death;
            }
        }

        public static Clips Load()
        {
            return new Clips(
                Load("Idle"),
                Load("Walk"),
                Load("Attack1"),
                Load("Death1"));
        }

        private static AnimationClip Load(string name)
        {
            return Resources.Load<AnimationClip>($"AnimalAnimations/WildBoar/{name}");
        }
    }
}
