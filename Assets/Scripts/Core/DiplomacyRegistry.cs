using System.Collections.Generic;

namespace KingdomsOfBharat.Core
{
    // Item 48: alliance/relation state between factions. Unset pairs
    // default to War - this matches every check that existed before this
    // file did (SelectionManager's `== FactionId.Player`, FogOfWarManager's
    // `== FactionId.Enemy`, AiController's hardcoded target): they were
    // really testing "is this the other faction," which is exactly what
    // "not allied" means when nobody has ever set a relation. A match that
    // never calls SetAllied behaves identically to before this file
    // existed.
    public enum RelationState
    {
        War,
        Allied,
    }

    public static class DiplomacyRegistry
    {
        // Keyed by an order-independent pair (smaller enum value first) so
        // (Player, Enemy) and (Enemy, Player) hit the same entry - callers
        // never need to know or care which order they pass factions in.
        private static readonly Dictionary<(FactionId, FactionId), RelationState> Relations =
            new Dictionary<(FactionId, FactionId), RelationState>();

        public static void SetAllied(FactionId a, FactionId b, bool allied)
        {
            if (a == b)
            {
                return;
            }

            Relations[Key(a, b)] = allied ? RelationState.Allied : RelationState.War;
        }

        public static bool AreAllied(FactionId a, FactionId b)
        {
            if (a == b)
            {
                return true;
            }

            return Relations.TryGetValue(Key(a, b), out RelationState state) && state == RelationState.Allied;
        }

        public static bool IsHostile(FactionId a, FactionId b)
        {
            return a != b && !AreAllied(a, b);
        }

        // Called by CivilizationSetup.BeginMatch so a new match doesn't
        // inherit alliance state from whatever the previous match ended
        // with - same "fresh start" convention CivilizationRegistry/
        // AgeProgress already follow at match start.
        public static void Reset()
        {
            Relations.Clear();
        }

        private static (FactionId, FactionId) Key(FactionId a, FactionId b)
        {
            return a.CompareTo(b) <= 0 ? (a, b) : (b, a);
        }
    }
}
