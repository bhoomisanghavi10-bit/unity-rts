using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Animations;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Wildlife;

namespace KingdomsOfBharat.Units
{
    // Plays the right animation clip for a unit's current job/action
    // (mirrors UnitStatus.Describe's priority order, but inspects
    // components directly for finer detail - see ResolveClip) and
    // movement speed. Drives the model's Animator/Avatar directly via a
    // PlayableGraph + AnimationClipPlayable
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

            // The Animator lives on the visual model child, not this
            // component's own GameObject - see HumanModelFactory, which
            // parents the model separately from the gameplay root.
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                return;
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

        // Mirrors UnitStatus.Describe's priority order (Attacking >
        // Building > Farming > Milking > Gathering > Idle/Walk) but goes
        // straight to the components for finer-grained detail than that
        // shared status string affords - specifically, splitting
        // Gathering into Mine (Gold/Stone) vs. the generic Gather
        // (Wood/Food) by resource type.
        private AnimationClip ResolveClip()
        {
            if (_unit.TryGetComponent(out MeleeAttacker attacker) && attacker.IsAttacking)
            {
                return _clips.Attack;
            }

            if (_unit.TryGetComponent(out Builder builder) && builder.IsBuilding)
            {
                return _clips.Build;
            }

            if (_unit.TryGetComponent(out FarmWorker farmWorker) && farmWorker.IsFarming)
            {
                return _clips.Farm;
            }

            if (_unit.TryGetComponent(out LivestockWorker livestockWorker) && livestockWorker.IsMilking)
            {
                // No dedicated milking clip in the pack - Farm reads
                // reasonably close (a repetitive hands-on-livestock motion
                // beats standing/walking).
                return _clips.Farm;
            }

            if (_unit.TryGetComponent(out Gatherer gatherer) && gatherer.IsWorking)
            {
                ResourceType? resourceType = gatherer.CurrentResourceType;
                bool mining = resourceType == ResourceType.Gold || resourceType == ResourceType.Stone;
                return mining ? _clips.Mine : _clips.Gather;
            }

            bool moving = _agent != null && _agent.velocity.sqrMagnitude > 0.05f;
            return moving ? _clips.Walk : _clips.Idle;
        }
    }
}
