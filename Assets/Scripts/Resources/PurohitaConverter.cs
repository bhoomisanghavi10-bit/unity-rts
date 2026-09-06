using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer;

namespace KingdomsOfBharat.ResourceGathering
{
    // Wave 4 item 27: Purohita's convert-target ability - full AoE-style
    // chance roll (user-confirmed via AskUserQuestion), not a guaranteed
    // conversion. Same "chase while out of range, act while in range" shape
    // as VaidyaHealer/MeleeAttacker.Tick, but the "act" is a per-tick
    // probability roll rather than a guaranteed effect, resolved once
    // (like GarrisonPoint.TryGarrison) rather than repeated once it lands.
    [RequireComponent(typeof(UnitMover))]
    public class PurohitaConverter : MonoBehaviour
    {
        [SerializeField] private float convertRange = 3f;
        // Not independently balanced - a first-pass number reused loosely
        // from AoE's own Monk conversion pacing (a few seconds against a
        // near-full-health target, faster against a wounded one). Internal
        // (not private) so an EditMode test can force a guaranteed-success
        // roll deterministically instead of depending on
        // DeterministicRandom's actual output - see AssemblyInfo.cs's
        // InternalsVisibleTo grant.
        [SerializeField] internal float baseChancePerSecond = 0.15f;

        private UnitMover _mover;
        private Attackable _target;

        public bool IsConverting => _target != null;

        private UnitMover Mover => _mover != null ? _mover : (_mover = GetComponent<UnitMover>());

        public void ConvertAt(Attackable target)
        {
            _target = target;
            Mover.MoveTo(target.transform.position);
        }

        public void CancelConvert()
        {
            _target = null;
        }

        // internal (not private) so EditMode tests can exercise the
        // exclusion rule directly - Buildings/Siege/other Support-Hero
        // units can't be converted, same "user-confirmed scope" as this
        // item's own design decision.
        internal static bool CanConvert(Attackable target)
        {
            return target != null && !target.IsDead
                && target.Class != UnitClass.Building
                && target.Class != UnitClass.Siege
                && target.Class != UnitClass.Support
                && target.Class != UnitClass.Hero;
        }

        // Pure so it's directly EditMode-testable without a NavMeshAgent -
        // converts a per-SECOND probability (scaled up the more HP the
        // target is missing, AoE's own "wounded units convert faster"
        // rule) into a per-frame probability via the standard
        // 1 - (1-p)^dt compounding formula.
        internal static float ChanceForTick(float baseChancePerSecondValue, float missingHpFraction, float deltaTime)
        {
            float perSecond = Mathf.Clamp01(baseChancePerSecondValue * (1f + missingHpFraction));
            return 1f - Mathf.Pow(1f - perSecond, deltaTime);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // Internal (not private) so EditMode tests can drive this directly
        // with an explicit deltaTime and a controlled DeterministicRandom
        // state - see AssemblyInfo.cs's InternalsVisibleTo grant.
        internal void Tick(float deltaTime)
        {
            if (!CanConvert(_target))
            {
                _target = null;
                return;
            }

            float distance = Vector3.Distance(transform.position, _target.transform.position);
            if (distance > convertRange)
            {
                Mover.MoveTo(_target.transform.position);
                return;
            }

            float missingHpFraction = 1f - (_target.Health / _target.MaxHealth);
            float chance = ChanceForTick(baseChancePerSecond, missingHpFraction, deltaTime);

            // DeterministicRandom (not UnityEngine.Random) so this roll
            // replays identically under the lockstep CommandBus - same
            // convention RajputDefianceHook already established for the
            // project's other gameplay-affecting random roll.
            if (DeterministicRandom.Match.NextFloat01() < chance)
            {
                PerformConversion(_target);
                _target = null;
            }
        }

        private void PerformConversion(Attackable target)
        {
            if (target.TryGetComponent(out FactionMember targetFaction))
            {
                targetFaction.Configure(MyFaction());
            }
        }

        private FactionId MyFaction()
        {
            return TryGetComponent(out FactionMember factionMember)
                ? factionMember.Faction
                : FactionId.Player;
        }
    }
}
