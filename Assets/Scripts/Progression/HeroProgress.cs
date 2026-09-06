using System.Collections.Generic;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Progression
{
    // Wave 4 item 28: per-faction bookkeeping for the Maharaja hero unit -
    // deliberately tiny compared to UniqueUnitEliteProgress/ElephantLine
    // Progress, since Maharaja has no tier ladder. Two independent
    // questions live here: has this faction EVER trained a hero (persistent
    // - needed so Regicide can't spuriously trigger for a faction that
    // never built one), and does this faction have one ALIVE right now
    // (a live scan of Unit.All, same "zero bookkeeping, just recount"
    // idiom Population.Current/MatchManager.FactionHasForces already use -
    // no persistent "alive" flag needed since a dead unit is simply no
    // longer in the registry).
    public static class HeroProgress
    {
        private static readonly Dictionary<FactionId, bool> Trained = new Dictionary<FactionId, bool>();

        public static bool HasTrainedHero(FactionId faction) =>
            Trained.TryGetValue(faction, out bool trained) && trained;

        public static void MarkTrained(FactionId faction)
        {
            Trained[faction] = true;
        }

        public static bool IsAlive(FactionId faction)
        {
            foreach (Unit unit in Unit.All)
            {
                if (!unit.TryGetComponent(out Attackable attackable) || attackable.Class != UnitClass.Hero)
                {
                    continue;
                }

                if (unit.TryGetComponent(out FactionMember member) && member.Faction == faction)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void ResetForTests()
        {
            Trained.Clear();
        }
    }
}
