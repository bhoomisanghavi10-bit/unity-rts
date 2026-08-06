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

            if (unit.TryGetComponent(out FarmWorker farmWorker) && farmWorker.IsFarming)
            {
                return "Farming";
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
    }
}
