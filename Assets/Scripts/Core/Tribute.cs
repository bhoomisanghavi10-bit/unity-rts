using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Core
{
    // Item 4 (Diplomacy, docs/PARTIAL_ELEMENTS_FIX_PLAN.md): an AoE II-style
    // resource gift between factions. Deliberately independent of
    // DiplomacyRegistry's War/Allied state - user-confirmed to match real
    // AoE II's actual rule (tribute works regardless of stance, even to a
    // faction you're at war with), not gated behind an alliance check. Taxed
    // on the way through (TaxRate) as friction against using it as a
    // zero-cost way to move resources around, the same purpose Market's own
    // sell/buy spread serves.
    public static class Tribute
    {
        public const float TaxRate = 0.20f;

        // Returns false (a no-op - nothing charged, nothing credited) if
        // from == to, amount is non-positive, either faction has no
        // ResourceStockpile, or the sender can't afford the full amount.
        public static bool Send(FactionId from, FactionId to, ResourceType type, float amount)
        {
            if (from == to || amount <= 0f)
            {
                return false;
            }

            ResourceStockpile sender = ResourceStockpile.For(from);
            ResourceStockpile receiver = ResourceStockpile.For(to);
            if (sender == null || receiver == null || sender.GetTotal(type) < amount)
            {
                return false;
            }

            sender.Add(type, -amount);
            receiver.Add(type, amount * (1f - TaxRate));
            return true;
        }
    }
}
