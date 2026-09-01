using System.Collections.Generic;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Core
{
    // AoE-parity Phase 4.2: which CombatResponse a civ's Workers default to
    // when attacked mid-gather. Not representable as a passiveBonuses
    // StatModifier (a categorical behavior choice, not a numeric stat), so a
    // hand-written per-civ lookup - same bespoke convention as TeamBonus/
    // RajputDefianceHook/BuildingPlacer.WoodMultiplierFor. Maratha's guerrilla
    // hit-and-run identity (Ganimi Kava - already reflected in its Cavalry
    // speed bonus and team bonus) extends here: its Workers flee rather than
    // fight back. Every other civ defaults to Fight, matching the pre-4.2
    // capability every Worker already had (WorkerFactory's own MeleeAttacker),
    // just now auto-triggered instead of requiring an explicit command.
    public static class WorkerCombatResponseDefaults
    {
        private static readonly Dictionary<CivilizationId, CombatResponse> Defaults = new Dictionary<CivilizationId, CombatResponse>
        {
            { CivilizationId.Chola, CombatResponse.Fight },
            { CivilizationId.Vijayanagara, CombatResponse.Fight },
            { CivilizationId.Rajput, CombatResponse.Fight },
            { CivilizationId.Maurya, CombatResponse.Fight },
            { CivilizationId.Maratha, CombatResponse.Flee },
        };

        public static CombatResponse For(CivilizationId civ)
        {
            return Defaults.TryGetValue(civ, out CombatResponse response) ? response : CombatResponse.Fight;
        }
    }
}
