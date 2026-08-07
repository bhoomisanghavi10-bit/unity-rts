using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace KingdomsOfBharat.Wildlife
{
    // Plays the right animation clip for a cow's current state
    // (Idle/Walk, or Eating while being milked), same Playables API
    // approach as BoarAnimationDriver/AnimationDriver. The Animator lives
    // on the visual model child, not this component's own GameObject -
    // see AnimalModelFactory, which parents the model separately from the
    // gameplay root.
    public class CowAnimationDriver : MonoBehaviour
    {
        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private NavMeshAgent _agent;
        private Livestock _livestock;
        private CowAnimationSet.Clips _clips;
        private AnimationClip _currentClip;

        public void Configure(CowAnimationSet.Clips clips, NavMeshAgent agent, Livestock livestock)
        {
            _clips = clips;
            _agent = agent;
            _livestock = livestock;

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
            if (_livestock == null || !_graph.IsValid())
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
            if (_livestock.IsBeingMilked)
            {
                return _clips.Eating;
            }

            bool moving = _agent != null && _agent.velocity.sqrMagnitude > 0.05f;
            return moving ? _clips.Walk : _clips.Idle;
        }
    }
}
