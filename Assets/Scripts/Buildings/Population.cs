using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Buildings
{
    // Per-faction population: current headcount vs. cap (AoE-style, raised
    // by completed Houses). Computed fresh from the existing Unit.All/
    // Building.All registries every call rather than tracked as separate
    // incrementing/decrementing state - matches the project's established
    // "recompute, don't incrementally track" convention (see
    // FogOfWarManager), so there's no counter that can drift out of sync
    // with reality after a unit dies or a House is destroyed.
    public static class Population
    {
        private const int BaseCapacity = 10;
        private const int PerHouseCapacity = 5;

        public static int Current(FactionId faction)
        {
            int count = 0;
            foreach (Unit unit in Unit.All)
            {
                if (unit.TryGetComponent(out FactionMember member) && member.Faction == faction)
                {
                    count++;
                }
            }

            return count;
        }

        public static int Cap(FactionId faction)
        {
            int houses = 0;
            foreach (Building building in Building.All)
            {
                if (building is House house
                    && house.IsComplete
                    && building.TryGetComponent(out FactionMember member)
                    && member.Faction == faction)
                {
                    houses++;
                }
            }

            return BaseCapacity + houses * PerHouseCapacity;
        }

        public static bool HasRoom(FactionId faction)
        {
            return Current(faction) < Cap(faction);
        }
    }
}
