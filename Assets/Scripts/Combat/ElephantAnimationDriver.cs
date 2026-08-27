using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace KingdomsOfBharat.Combat
{
    // Plays the War Elephant's Idle/Walk/Attack/Die clips - same Playables
    // approach as BoarAnimationDriver (a Generic, non-Humanoid rig, same
    // reasoning: no muscle-space curves to worry about), but driven by the
    // MeleeAttacker/Attackable state a combat unit actually has instead of
    // WildBoar's own IsDead/IsAttacking fields, since the War Elephant is a
    // real roster Siege unit, not wildlife.
    public class ElephantAnimationDriver : MonoBehaviour
    {
        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private NavMeshAgent _agent;
        private Attackable _attackable;
        private MeleeAttacker _attacker;
        private ElephantAnimationSet.Clips _clips;
        private AnimationClip _currentClip;

        public void Configure(ElephantAnimationSet.Clips clips, NavMeshAgent agent, Attackable attackable, MeleeAttacker attacker)
        {
            _clips = clips;
            _agent = agent;
            _attackable = attackable;
            _attacker = attacker;

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
            if (_attackable == null || !_graph.IsValid())
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

        // Attackable.TakeDamage calls Destroy() the instant Health reaches
        // 0, which doesn't take effect until end of frame - same "still
        // guaranteed at least one Update() before removal" convention
        // BoarAnimationDriver's ResolveClip already relies on, so Die still
        // gets a real (if brief) chance to show rather than being dead code.
        private AnimationClip ResolveClip()
        {
            if (_attackable.IsDead)
            {
                return _clips.Die;
            }

            if (_attacker != null && _attacker.IsAttacking)
            {
                return _clips.Attack;
            }

            bool moving = _agent != null && _agent.velocity.sqrMagnitude > 0.05f;
            return moving ? _clips.Walk : _clips.Idle;
        }
    }
}
