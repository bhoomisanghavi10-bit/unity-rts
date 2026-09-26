using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Multiplayer
{
    public enum NetworkIssue
    {
        UnresolvedTarget,
        DuplicateCommand,
        LateCommand,
        WrongSender,
        ConfigMismatch,
    }

    // Counts every rejected/failed network command and writes at most one
    // console line per issue kind per WarnIntervalSeconds, so a bad peer
    // cannot flood the log but nothing fails silently.
    public static class NetworkDiagnostics
    {
        public const float WarnIntervalSeconds = 5f;
        private static readonly int[] _counts = new int[System.Enum.GetValues(typeof(NetworkIssue)).Length];
        private static readonly float[] _lastWarn = new float[_counts.Length];
        private static readonly List<string> _recent = new List<string>();

        static NetworkDiagnostics()
        {
            Reset();
        }

        public static int Count(NetworkIssue issue) => _counts[(int)issue];
        public static IReadOnlyList<string> Recent => _recent;

        public static void Reset()
        {
            System.Array.Clear(_counts, 0, _counts.Length);
            for (int i = 0; i < _lastWarn.Length; i++) _lastWarn[i] = -1000f;
            _recent.Clear();
        }

        public static void Report(NetworkIssue issue, string detail)
        {
            int i = (int)issue;
            _counts[i]++;
            _recent.Add($"{issue}: {detail}");
            if (_recent.Count > 30) _recent.RemoveAt(0);
            float now = Time.realtimeSinceStartup;
            if (now - _lastWarn[i] >= WarnIntervalSeconds)
            {
                _lastWarn[i] = now;
                Debug.LogWarning($"[Network] {issue}: {detail} (count {_counts[i]})");
            }
        }
    }
}
