using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Match
{
    public enum MatchOutcome
    {
        Ongoing,
        Victory,
        Defeat,
    }

    // AoE-style conquest victory: a faction is eliminated once it has zero
    // units AND zero buildings left. Polls on a short interval (not every
    // frame - elimination is a rare, coarse-grained event) rather than
    // reacting to individual death events, since Unit/Building already
    // maintain live registries this can just recount cheaply. Freezes the
    // match (Time.timeScale = 0) the instant an outcome is reached; a
    // GameOverScreen (or anything else) just reads Outcome to react.
    public class MatchManager : MonoBehaviour
    {
        [SerializeField] private float checkInterval = 1f;
        [SerializeField] private float graceAfterMatchStart = 2f;

        public static MatchOutcome Outcome { get; private set; } = MatchOutcome.Ongoing;

        private float _timer;
        private float _matchStartedAt = -1f;

        private void OnEnable()
        {
            Outcome = MatchOutcome.Ongoing;
        }

        private void Update()
        {
            if (Outcome != MatchOutcome.Ongoing || !CivilizationSetup.HasMatchStarted)
            {
                return;
            }

            if (_matchStartedAt < 0f)
            {
                _matchStartedAt = Time.unscaledTime;
            }

            if (Time.unscaledTime - _matchStartedAt < graceAfterMatchStart)
            {
                return;
            }

            _timer += Time.unscaledDeltaTime;
            if (_timer < checkInterval)
            {
                return;
            }

            _timer = 0f;
            Evaluate();
        }

        private void Evaluate()
        {
            bool playerAlive = FactionHasForces(FactionId.Player);
            bool enemyAlive = FactionHasForces(FactionId.Enemy);

            if (!playerAlive)
            {
                Declare(MatchOutcome.Defeat);
            }
            else if (!enemyAlive)
            {
                Declare(MatchOutcome.Victory);
            }
        }

        // TargetDummy never counts here: it's tagged Enemy faction as a
        // standing combat test target (see TargetDummySpawner), but it's
        // neither a Unit nor a Building, so it never appears in either
        // registry below - its presence/absence can't affect whether the
        // Enemy reads as eliminated.
        private static bool FactionHasForces(FactionId faction)
        {
            foreach (Unit unit in Unit.All)
            {
                if (unit.TryGetComponent(out FactionMember member) && member.Faction == faction)
                {
                    return true;
                }
            }

            foreach (Building building in Building.All)
            {
                if (building.TryGetComponent(out FactionMember member) && member.Faction == faction)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Declare(MatchOutcome outcome)
        {
            Outcome = outcome;
            Time.timeScale = 0f;
        }
    }
}
