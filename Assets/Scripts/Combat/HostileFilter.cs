using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Combat
{
    // Shared faction-hostility check, extracted from BuildingAttacker's own
    // original IsHostile so MeleeAttacker's splash-damage scan (Roadmap
    // Section 1 Phase 2.3) doesn't duplicate the exact same comparison a
    // second time. Default-hostile if either side has no FactionMember -
    // same reasoning as before: an unfactioned Attackable (shouldn't
    // normally happen) is safer treated as fair game than silently immune.
    public static class HostileFilter
    {
        public static bool IsHostile(Attackable candidate, FactionMember selfFaction)
        {
            if (!candidate.TryGetComponent(out FactionMember candidateFaction))
            {
                return true;
            }

            if (selfFaction == null)
            {
                return true;
            }

            return candidateFaction.Faction != selfFaction.Faction;
        }
    }
}
