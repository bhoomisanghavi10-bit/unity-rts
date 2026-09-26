using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Units
{
    // Detects "the agent is not getting any closer": progress toward the
    // target has not improved by minProgress for `limit` seconds. Pure
    // (no Unity objects) so the state machines' recovery ladder is testable.
    public sealed class StuckWatchdog
    {
        private float _best = float.MaxValue;
        private float _timer;

        public void Reset()
        {
            _best = float.MaxValue;
            _timer = 0f;
        }

        public bool Tick(float deltaTime, float distance, float minProgress = 0.3f, float limit = 3f)
        {
            if (distance < _best - minProgress)
            {
                _best = distance;
                _timer = 0f;
                return false;
            }

            _timer += deltaTime;
            return _timer >= limit;
        }
    }

    public enum WorkerFailure
    {
        None,
        ResourceUnreachable,
        DropOffUnreachable,
        NoDropOff,
        BuildSiteUnreachable,
    }

    // Failed-order diagnostics without log spam: every failure is counted
    // and remembered; the console gets at most one warning per failure kind
    // per WarnIntervalSeconds, carrying how many were suppressed since.
    public static class WorkerDiagnostics
    {
        public const float WarnIntervalSeconds = 10f;

        private static readonly int[] _counts = new int[System.Enum.GetValues(typeof(WorkerFailure)).Length];
        private static readonly float[] _lastWarn = new float[_counts.Length];
        private static readonly int[] _suppressed = new int[_counts.Length];
        private static readonly List<string> _recent = new List<string>();

        public static int Count(WorkerFailure kind) => _counts[(int)kind];
        public static int Total
        {
            get
            {
                int n = 0;
                for (int i = 1; i < _counts.Length; i++) n += _counts[i];
                return n;
            }
        }
        public static IReadOnlyList<string> Recent => _recent;

        public static void Reset()
        {
            System.Array.Clear(_counts, 0, _counts.Length);
            System.Array.Clear(_suppressed, 0, _suppressed.Length);
            for (int i = 0; i < _lastWarn.Length; i++) _lastWarn[i] = -1000f;
            _recent.Clear();
        }

        static WorkerDiagnostics()
        {
            Reset();
        }

        public static void Report(WorkerFailure kind, string who, string detail)
        {
            int k = (int)kind;
            _counts[k]++;
            string line = $"{kind}: {who} - {detail}";
            _recent.Add(line);
            if (_recent.Count > 20) _recent.RemoveAt(0);

            float now = Time.realtimeSinceStartup;
            if (now - _lastWarn[k] >= WarnIntervalSeconds)
            {
                Debug.LogWarning($"[Worker] {line}" + (_suppressed[k] > 0 ? $" (+{_suppressed[k]} similar suppressed)" : ""));
                _lastWarn[k] = now;
                _suppressed[k] = 0;
            }
            else
            {
                _suppressed[k]++;
            }
        }
    }

    public static class WorkerNav
    {
        // Nearest point on the target's collider bounds (falls back to its
        // origin). A worker can only ever reach the edge of a big node/site,
        // never its centre, so range checks and move targets use this.
        public static Vector3 ClosestPoint(Component target, Vector3 from)
        {
            if (target.TryGetComponent(out Collider collider))
            {
                return collider.bounds.ClosestPoint(from);
            }

            return target.transform.position;
        }
    }
}
