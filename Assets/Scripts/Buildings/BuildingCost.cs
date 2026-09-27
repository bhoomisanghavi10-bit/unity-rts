using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Buildings
{
    // Records the exact resources actually deducted (after civ/tech
    // multipliers) when this building's foundation was placed, so a
    // canceled foundation can be refunded a real fraction of what was
    // really paid - the AoE-style rule that canceling an unfinished
    // building refunds its cost, in full if it hasn't taken damage and
    // proportionally less if it has (see ConstructionSite.CancelAndRefund).
    // Attached by BuildingPlacer.ExecuteBuild right after each *Factory.Place
    // call. Deliberately separate from Repairable's own cost model
    // (Repairable charges an ongoing per-HP rate, not a one-time refund of
    // the original spend - see its own comment on why it can't reuse this
    // for an exact figure) and from ProductionItem's per-unit cost (unit
    // training already has its own refund path via ProductionQueue.Cancel).
    public class BuildingCost : MonoBehaviour
    {
        private readonly List<ResourceType> _types = new List<ResourceType>();
        private readonly List<float> _amounts = new List<float>();

        public void Record(ResourceType type, float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            _types.Add(type);
            _amounts.Add(amount);
        }

        // Refunds `fraction` of everything recorded into the given
        // stockpile. Called once, right before the foundation is
        // destroyed by a cancel - not idempotent by design (a second call
        // would double-refund), which is fine since the caller always
        // destroys the GameObject in the same breath.
        public void Refund(ResourceStockpile stockpile, float fraction)
        {
            if (stockpile == null || fraction <= 0f)
            {
                return;
            }

            for (int i = 0; i < _types.Count; i++)
            {
                stockpile.Add(_types[i], _amounts[i] * fraction);
            }
        }
    }
}
