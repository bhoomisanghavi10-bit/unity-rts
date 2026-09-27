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
        // The playable actually feeding _output - tracked so SetClip can
        // destroy the previous one before creating a new one. Without
        // this, every clip switch (Idle<->Walk<->Attack, which happens
        // constantly as a unit starts/stops moving or fighting) leaves the
        // old AnimationClipPlayable orphaned in the graph: disconnecting a
        // source via SetSourcePlayable does not destroy the playable it
        // replaces, so PlayableGraph.GetPlayableCount() grew without bound
        // over a long session - a real, previously-unnoticed leak, not
        // just a hypothetical one (confirmed live: see
        // ArcherPresentationTests.RepeatedClipSwitches_
        // DoNotGrowThePlayableGraphUnbounded).
        private Playable _currentPlayable;

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

            // Destroy the outgoing playable before replacing it - see
            // _currentPlayable's own comment for why this matters.
            if (_currentPlayable.IsValid())
            {
                _currentPlayable.Destroy();
            }

            _currentPlayable = AnimationClipPlayable.Create(_graph, clip);
            _output.SetSourcePlayable(_currentPlayable);
            if (!_graph.IsPlaying())
            {
                _graph.Play();
            }

            _currentClip = clip;
        }

        // Where the currently-playing clip's own local time sits, as a
        // 0..1 fraction of its length - used by MeleeAttacker's opt-in
        // ranged-projectile mode (see SetProjectile) to fire a shot at a
        // specific moment in the swing/draw animation instead of the
        // instant the attack becomes off cooldown. Returns -1 whenever
        // that can't be answered (no graph, a different clip currently
        // playing, or a zero-length clip) so callers have an unambiguous
        // "not available" sentinel rather than a misleading 0.
        public float NormalizedTimeInClip(AnimationClip clip)
        {
            if (clip == null || _currentClip != clip || !_graph.IsValid() || !_currentPlayable.IsValid() || clip.length <= 0f)
            {
                return -1f;
            }

            double loopRelative = _currentPlayable.GetTime() % clip.length;
            return (float)(loopRelative / clip.length);
        }

        // Test-only: lets an EditMode test confirm the graph's own
        // playable count stays bounded across repeated clip switches
        // instead of growing linearly, without depending on real Update()
        // frames to drive it.
        internal void ForceSetClipForTest(AnimationClip clip) => SetClip(clip);
        internal int PlayableCountForTest => _graph.IsValid() ? _graph.GetPlayableCount() : 0;

        // Test-only: lets an EditMode test deterministically position the
        // currently-playing clip's own local time, since the PlayableGraph
        // itself isn't guaranteed to auto-advance outside Play mode -
        // without this, testing NormalizedTimeInClip/marker-crossing
        // behavior would depend on real per-frame graph evaluation this
        // test environment can't reliably drive.
        internal void SetPlayableTimeForTest(double time)
        {
            if (_currentPlayable.IsValid())
            {
                _currentPlayable.SetTime(time);
            }
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

            if (_unit.TryGetComponent(out Gatherer gatherer) && gatherer.IsActivelyGathering)
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
