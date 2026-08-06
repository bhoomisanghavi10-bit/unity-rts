using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Animations;
using KingdomsOfBharat.UI;

namespace KingdomsOfBharat.Units
{
    // Plays the right animation clip for a unit's current status
    // (UnitStatus.Describe - the same shared logic SelectedUnitPanel and
    // HoverTooltip already use) and movement speed. Drives the model's
    // Animator/Avatar directly via a PlayableGraph + AnimationClipPlayable
    // rather than an AnimatorController state machine - deliberately, for
    // two reasons: (1) building a .controller asset requires
    // UnityEditor.Animations APIs unavailable at runtime, so a
    // hand-authored .controller YAML would be the only alternative, one of
    // the riskiest Unity asset formats to hand-author blind; (2) the
    // legacy Animation component (the first approach tried here) turned
    // out to be fundamentally incompatible with these clips - Humanoid-rig
    // animation clips store muscle-space curves, not raw bone transforms,
    // and the legacy Animation component can't read that format at all
    // (confirmed by real-Editor testing: units stayed stuck in T-pose).
    // The Playables API plays Humanoid clips correctly since it goes
    // through the same Avatar retargeting Mecanim itself uses.
    public class AnimationDriver : MonoBehaviour
    {
        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private NavMeshAgent _agent;
        private Unit _unit;
        private HumanAnimationSet.Clips _clips;
        private AnimationClip _currentClip;

        public void Configure(HumanAnimationSet.Clips clips, NavMeshAgent agent, Unit unit)
        {
            _clips = clips;
            _agent = agent;
            _unit = unit;

            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = gameObject.AddComponent<Animator>();
            }

            _graph = PlayableGraph.Create($"{name}_Animation");
            _output = AnimationPlayableOutput.Create(_graph, "Animation", _animator);

            SetClip(_clips.Idle);
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        private void Update()
        {
            if (_unit == null || !_graph.IsValid())
            {
                return;
            }

            AnimationClip target = ResolveClip();
            if (target == _currentClip)
            {
                return;
            }

            SetClip(target);
        }

        private void SetClip(AnimationClip clip)
        {
            if (clip == null)
            {
                return;
            }

            var playable = AnimationClipPlayable.Create(_graph, clip);
            _output.SetSourcePlayable(playable);
            if (!_graph.IsPlaying())
            {
                _graph.Play();
            }

            _currentClip = clip;
        }

        private AnimationClip ResolveClip()
        {
            switch (UnitStatus.Describe(_unit))
            {
                case "Gathering":
                case "Farming":
                case "Milking":
                    return _clips.Gather;
                case "Building":
                    return _clips.Build;
                case "Attacking":
                    return _clips.Attack;
            }

            bool moving = _agent != null && _agent.velocity.sqrMagnitude > 0.05f;
            return moving ? _clips.Walk : _clips.Idle;
        }
    }
}
