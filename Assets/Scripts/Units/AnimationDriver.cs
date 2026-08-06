using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.UI;

namespace KingdomsOfBharat.Units
{
    // Plays the right animation clip for a unit's current status
    // (UnitStatus.Describe - the same shared logic SelectedUnitPanel and
    // HoverTooltip already use) and movement speed. Deliberately built on
    // the legacy Animation component rather than Mecanim's
    // AnimatorController: building a state-machine .controller asset
    // requires UnityEditor.Animations APIs that don't exist at runtime, so
    // a hand-authored .controller YAML would be the only alternative - one
    // of the riskiest Unity asset formats to hand-author blind, and this
    // project has no Editor access to build one interactively either.
    // AnimationClip.legacy can be flipped at runtime via script (see
    // HumanAnimationSet), so this whole system stays procedural.
    public class AnimationDriver : MonoBehaviour
    {
        private Animation _animation;
        private NavMeshAgent _agent;
        private Unit _unit;
        private string _currentClip;

        public void Configure(HumanAnimationSet.Clips clips, NavMeshAgent agent, Unit unit)
        {
            _animation = gameObject.AddComponent<Animation>();
            _animation.playAutomatically = false;
            AddIfPresent(clips.Idle, "Idle");
            AddIfPresent(clips.Walk, "Walk");
            AddIfPresent(clips.Gather, "Gather");
            AddIfPresent(clips.Build, "Build");
            AddIfPresent(clips.Attack, "Attack");

            _agent = agent;
            _unit = unit;
        }

        private void AddIfPresent(AnimationClip clip, string clipName)
        {
            if (clip != null)
            {
                _animation.AddClip(clip, clipName);
            }
        }

        private void Update()
        {
            if (_animation == null || _unit == null)
            {
                return;
            }

            string clipName = ResolveClipName();
            if (clipName == _currentClip || _animation.GetClip(clipName) == null)
            {
                return;
            }

            _animation.CrossFade(clipName, 0.15f);
            _currentClip = clipName;
        }

        private string ResolveClipName()
        {
            switch (UnitStatus.Describe(_unit))
            {
                case "Gathering":
                case "Farming":
                case "Milking":
                    return "Gather";
                case "Building":
                    return "Build";
                case "Attacking":
                    return "Attack";
            }

            return _agent != null && _agent.velocity.sqrMagnitude > 0.05f ? "Walk" : "Idle";
        }
    }
}
