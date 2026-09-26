using System.IO;
using UnityEngine;

namespace KingdomsOfBharat.Multiplayer
{
    // Repeatable two-process validation aid (see docs/LAN_TWO_INSTANCE_VALIDATION.md).
    // Off by default. When the process is started with the command-line flag
    // -lanlog <path> (or KOB_LAN_LOG=<path>) it appends one line per config
    // hash, executed command and state hash, which Tools/compare_lan_logs.py
    // then diffs between the two peers.
    public static class NetworkValidationLog
    {
        private static string _path;
        private static bool _resolved;

        public static bool Enabled
        {
            get
            {
                Resolve();
                return _path != null;
            }
        }

        // Test hook / manual override.
        public static void SetPath(string path)
        {
            _path = path;
            _resolved = true;
        }

        private static void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-lanlog")
                {
                    _path = args[i + 1];
                    return;
                }
            }

            string env = System.Environment.GetEnvironmentVariable("KOB_LAN_LOG");
            _path = string.IsNullOrEmpty(env) ? null : env;
        }

        public static void Record(string line)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                File.AppendAllText(_path, line + "\n");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[NetworkValidationLog] cannot write: " + e.Message);
                _path = null;
            }
        }
    }
}
