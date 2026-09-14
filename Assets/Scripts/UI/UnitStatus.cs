using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Wildlife;

namespace KingdomsOfBharat.UI
{
    // Shared "what is this unit currently doing" status string, used by
    // both SelectedUnitPanel (bottom-left, for the current selection) and
    // HoverTooltip (follows the cursor over anything) - one place to keep
    // the priority order consistent instead of two copies drifting apart.
    public static class UnitStatus
    {
        public static string Describe(Unit unit)
        {
            if (unit.TryGetComponent(out MeleeAttacker attacker) && attacker.IsAttacking)
            {
                return "Attacking";
            }

            if (unit.TryGetComponent(out Builder builder) && builder.IsBuilding)
            {
                return "Building";
            }

            if (unit.TryGetComponent(out Repairer repairer) && repairer.IsRepairing)
            {
                return "Repairing";
            }

            if (unit.TryGetComponent(out FarmWorker farmWorker))
            {
                // Item 5 (Renewable Resource): mutually exclusive per-tick
                // in FarmWorker, so ordering here only matters for clarity.
                if (farmWorker.IsReseeding)
                {
                    return "Reseeding Farm";
                }

                if (farmWorker.IsFarming)
                {
                    return "Farming";
                }
            }

            if (unit.TryGetComponent(out LivestockWorker livestockWorker) && livestockWorker.IsMilking)
            {
                return "Milking";
            }

            if (unit.TryGetComponent(out Gatherer gatherer) && gatherer.IsWorking)
            {
                return "Gathering";
            }

            return "Idle";
        }

        // Wave 6 item 34 (idle-worker indicator): a unit is "idle" exactly
        // when Describe would fall through to that final default - reuses
        // the same priority chain rather than a second, potentially
        // drifting definition of "doing nothing" (e.g. a worker mid-walk to
        // a resource node is NOT idle, since Gatherer.IsWorking already
        // covers the walk, not just the harvest itself).
        public static bool IsIdle(Unit unit)
        {
            return Describe(unit) == "Idle";
        }
    }
}
