using System.Collections.Generic;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.UI
{
    // Wave 6 item 34 (idle-worker indicator): finds every currently-idle
    // Worker belonging to one faction. "Worker" is identified by Gatherer
    // presence, the same marker TownBell.Ring already uses to find every
    // Worker owned by a faction - Gatherer is exclusive to WorkerFactory's
    // spawn, so this deliberately never flags an idle Soldier/Archer/etc.
    // (real AoE's own idle-villager button doesn't either). A garrisoned
    // worker is automatically excluded - Unit.OnDisable removes it from
    // Unit.All the instant GarrisonPoint deactivates it, so no special-case
    // check is needed here.
    public static class IdleWorkerFinder
    {
        public static List<Unit> FindAll(FactionId faction)
        {
            var result = new List<Unit>();
            foreach (Unit unit in Unit.All)
            {
                if (unit == null || !unit.TryGetComponent(out Gatherer _))
                {
                    continue;
                }

                if (!unit.TryGetComponent(out FactionMember member) || member.Faction != faction)
                {
                    continue;
                }

                if (UnitStatus.IsIdle(unit))
                {
                    result.Add(unit);
                }
            }

            return result;
        }
    }
}
