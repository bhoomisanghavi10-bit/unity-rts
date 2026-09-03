using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Wave 1 item 6 (docs/IMPLEMENTATION_ROADMAP.md): AoE-style "own enough
    // buildings" gate on advancing past the Classical Age, layered on top of
    // the existing Wood/Stone cost check rather than replacing it. No
    // per-age building taxonomy exists in this codebase (that would be a
    // materially bigger feature - see the roadmap item's own note), so
    // "2 buildings" here means the simplest faithful reading: 2 completed,
    // non-TownCenter buildings currently owned, not gated by which building
    // types they are. TownCenter is excluded because every faction starts
    // with exactly one and it would make the check meaningless. Recomputed
    // fresh from Building.All every call - same "recompute, don't
    // incrementally track" convention as Population.Cap, so there's no
    // counter to drift out of sync when a building is destroyed.
    public static class AgeUpRequirement
    {
        public const int RequiredBuildingCount = 2;

        // Ancient->Classical is deliberately exempt (user-confirmed design
        // decision): a player's very first age-up may genuinely only have
        // the starting TownCenter, and gating that transition would block
        // the game's opening minutes for no real strategic reason. The
        // requirement starts at Classical->Durg and Durg->Imperial.
        public static bool AppliesTo(AgeId currentAge)
        {
            return currentAge != AgeId.Ancient;
        }

        public static bool IsMet(FactionId faction)
        {
            return CompletedNonTownCenterBuildingCount(faction) >= RequiredBuildingCount;
        }

        private static int CompletedNonTownCenterBuildingCount(FactionId faction)
        {
            int count = 0;
            foreach (Building building in Building.All)
            {
                if (building is TownCenter)
                {
                    continue;
                }

                if (!building.TryGetComponent(out FactionMember member) || member.Faction != faction)
                {
                    continue;
                }

                if (building.TryGetComponent(out ConstructionSite site) && !site.IsComplete)
                {
                    continue;
                }

                count++;
            }

            return count;
        }
    }
}
