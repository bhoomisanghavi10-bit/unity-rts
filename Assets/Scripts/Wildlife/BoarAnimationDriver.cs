using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace KingdomsOfBharat.Wildlife
{
    // Plays the right animation clip for a wild boar's current state
    // (Idle/Walk/Attack/Death), driven by the Playables API the same way
    // AnimationDriver does for human units - this pack's Generic rig type
    // doesn't have the muscle-space-vs-legacy-Animation-component problem
    // that caused the Humanoid dummy's T-pose bug, but the Playables
    // approach is reused anyway for consistency with an already-proven
    // working setup. The Animator lives on the visual model child, not
    // this component's own GameObject - see AnimalModelFactory, which
    // parents the model separately from the gameplay root.
    public class BoarAnimationDriver : MonoBehaviour
    {
        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private NavMeshAgent _agent;
        private WildBoar _boar;
        private BoarAnimationSet.Clips _clips;
        private AnimationClip _currentClip;

        public void Configure(BoarAnimationSet.Clips clips, NavMeshAgent agent, WildBoar boar)
        {
            _clips = clips;
            _agent = agent;
            _boar = boar;

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
            if (_boar == null || !_graph.IsValid())
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
            if (_boar.IsDead)
            {
                return _clips.Death;
            }

            if (_boar.IsAttacking)
            {
                return _clips.Attack;
            }

            bool moving = _agent != null && _agent.velocity.sqrMagnitude > 0.05f;
            return moving ? _clips.Walk : _clips.Idle;
        }
    }
}
