using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace KingdomsOfBharat.Units
{
    // Plays Idle/Walk on a pack-ox model purely off NavMeshAgent velocity -
    // same hand-built PlayableGraph approach as CowAnimationDriver, but
    // without that driver's Livestock-specific Eating state, so this is
    // reusable by any NavMeshAgent-driven unit that wears this model (only
    // Vanik today - see VanikFactory/OxModelFactory).
    public class OxAnimationDriver : MonoBehaviour
    {
        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private NavMeshAgent _agent;
        private OxAnimationSet.Clips _clips;
        private AnimationClip _currentClip;

        public void Configure(OxAnimationSet.Clips clips, NavMeshAgent agent)
        {
            _clips = clips;
            _agent = agent;

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
            if (!_graph.IsValid())
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
            bool moving = _agent != null && _agent.velocity.sqrMagnitude > 0.05f;
            return moving ? _clips.Walk : _clips.Idle;
        }
    }
}
