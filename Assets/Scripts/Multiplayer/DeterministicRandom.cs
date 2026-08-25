using UnityEngine;

namespace KingdomsOfBharat.Multiplayer
{
    // Item 51 (lockstep foundation): a seeded PRNG independent of
    // UnityEngine.Random's hidden global state, so two machines running the
    // same command stream from the same seed produce identical gameplay
    // randomness. xorshift32 - small, fast, and trivially reproducible from
    // just its seed and call count (no hidden engine state to diverge on).
    public class DeterministicRandom
    {
        // Match-wide instance, seeded once per match by SimClock alongside
        // the tick counter reset. Anything gameplay-relevant that currently
        // calls UnityEngine.Random should call this instead so it's covered
        // by the same lockstep replay.
        public static DeterministicRandom Match { get; private set; } = new DeterministicRandom(1);

        private uint _state;

        public DeterministicRandom(int seed)
        {
            // xorshift32 is undefined at state 0 - fold the seed away from
            // it deterministically rather than special-casing 0 at call time.
            _state = (uint)seed == 0 ? 0x9E3779B9u : (uint)seed;
        }

        public static void ReseedMatch(int seed)
        {
            Match = new DeterministicRandom(seed);
        }

        private uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }

        public float NextFloat01()
        {
            return (NextUInt() & 0xFFFFFF) / (float)0x1000000;
        }

        public float Range(float min, float max)
        {
            return min + NextFloat01() * (max - min);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
        }

        public Vector2 InsideUnitCircleNormalized()
        {
            float angle = Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
