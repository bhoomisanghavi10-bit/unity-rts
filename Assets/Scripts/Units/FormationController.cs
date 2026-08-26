using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.Units
{
    // The runtime FormationController FormationDefinition.cs's own comment
    // asked for ("not included here... belongs in your existing unit-
    // control code"): arranges a selected group into an AoE2-style
    // composed formation - melee/high-armor units (FormationDefinition.
    // preferredFrontRow) walked first so GroupFormation's shape functions
    // place them nearer the front/edge, ranged/fragile units
    // (preferredBackRow) behind them - not the free-for-all clumping a
    // plain move order produces. Distinct from GroupFormation's own
    // simpler move-order *spreading* (which this sits on top of, sharing
    // the same unified FormationType shapes - see GroupFormation.cs's
    // comment on why there's only one FormationType now, not two).
    //
    // Line and Box are the two required shapes (per the request this was
    // scoped from): Line gets genuine multi-row placement (a front rank,
    // then a back rank behind it, each capped at unitsPerRow before
    // wrapping to a further rank) rather than one undifferentiated line -
    // the actual "front absorbs hits, back free-fires" AoE2 behavior only
    // exists if front/back categories land at different depths, not just
    // different left-right positions in the same rank. Box's hollow
    // perimeter already gives a near-side/far-side split for free by
    // walking front-row units first, so it reuses GroupFormation.GetOffset
    // directly rather than needing its own row logic. Every other
    // FormationType shape (Grid/Column/Staggered/Flank/Skirmish) falls
    // back to that same "front units get earlier indices" ordering through
    // GroupFormation.GetOffset - real behavior, not a stub, just without
    // Line's explicit multi-row depth separation.
    public class FormationController : MonoBehaviour
    {
        [SerializeField] private FormationDefinition formation;

        public FormationDefinition Formation
        {
            get => formation;
            set => formation = value;
        }

        // Returns each unit's target offset, relative to the group's move
        // destination - same contract as GroupFormation.GetOffset, just
        // per-unit instead of per-index, and category-aware. Units with no
        // Attackable (shouldn't happen for anything selectable, but this
        // is public API) or a UnitClass this formation's front/back lists
        // don't mention default to the back row - safer than exposing an
        // unrecognized unit type at the front line by default.
        public Dictionary<GameObject, Vector3> ComputeOffsets(IReadOnlyList<GameObject> units, Vector3 moveDirection)
        {
            var result = new Dictionary<GameObject, Vector3>();
            if (units == null || units.Count == 0)
            {
                return result;
            }

            float spacing = formation != null ? formation.unitSpacing : 1.5f;
            FormationType type = formation != null ? formation.type : FormationType.Line;
            int unitsPerRow = formation != null && formation.unitsPerRow > 0 ? formation.unitsPerRow : units.Count;

            List<GameObject> front = new List<GameObject>();
            List<GameObject> back = new List<GameObject>();
            SplitByRow(units, formation, front, back);

            if (type == FormationType.Line)
            {
                PlaceRanks(front, 0, spacing, unitsPerRow, moveDirection, result);
                int backRowStart = Mathf.CeilToInt((float)front.Count / unitsPerRow);
                PlaceRanks(back, backRowStart, spacing, unitsPerRow, moveDirection, result);
                return result;
            }

            // Every other shape: front-row units simply get the earlier
            // indices, so Box's perimeter walk (and every other shape's
            // own index->offset math) naturally puts them nearer the
            // front/edge without needing a bespoke row layout each.
            List<GameObject> ordered = new List<GameObject>(units.Count);
            ordered.AddRange(front);
            ordered.AddRange(back);
            for (int i = 0; i < ordered.Count; i++)
            {
                result[ordered[i]] = GroupFormation.GetOffset(type, i, ordered.Count, spacing, moveDirection);
            }
            return result;
        }

        private static void SplitByRow(IReadOnlyList<GameObject> units, FormationDefinition def, List<GameObject> front, List<GameObject> back)
        {
            foreach (GameObject go in units)
            {
                UnitCategory? category = CategoryOf(go);
                if (def != null && category.HasValue && def.preferredFrontRow.Contains(category.Value))
                {
                    front.Add(go);
                }
                else
                {
                    back.Add(go);
                }
            }
        }

        // Lays out one or more horizontal ranks of up to unitsPerRow units
        // each, starting at rank index `startRank` (so a back row's ranks
        // continue immediately behind the front row's own ranks rather
        // than overlapping at the same depth).
        private static void PlaceRanks(List<GameObject> units, int startRank, float spacing, int unitsPerRow, Vector3 moveDirection, Dictionary<GameObject, Vector3> result)
        {
            for (int i = 0; i < units.Count; i += unitsPerRow)
            {
                int rankIndex = startRank + (i / unitsPerRow);
                int rankSize = Mathf.Min(unitsPerRow, units.Count - i);
                for (int j = 0; j < rankSize; j++)
                {
                    result[units[i + j]] = GroupFormation.RankOffset(j, rankSize, spacing, moveDirection, rankIndex * spacing);
                }
            }
        }

        private static UnitCategory? CategoryOf(GameObject go)
        {
            Attackable attackable = go.GetComponent<Attackable>();
            return attackable == null ? null : (UnitCategory?)MapUnitClass(attackable.Class);
        }

        // UnitClass (Combat) and UnitCategory (the CSV-generated data
        // schema) are two separate enums for two separate reasons - see
        // CombatBonus's own note on why they were deliberately NOT unified
        // (CounterMatrix's untested RPS values would have silently
        // overridden a playtested balance pass). No such conflict here:
        // this is a plain lookup, not competing design data, so a simple
        // name-matching map is all that's needed. UnitCategory's extra
        // Support/Hero values have no UnitClass equivalent (no live unit
        // reports either today) and are simply never produced by this map.
        private static UnitCategory MapUnitClass(UnitClass unitClass) => unitClass switch
        {
            UnitClass.Infantry => UnitCategory.Infantry,
            UnitClass.Archer => UnitCategory.Archer,
            UnitClass.Cavalry => UnitCategory.Cavalry,
            UnitClass.Siege => UnitCategory.Siege,
            UnitClass.Building => UnitCategory.Building,
            UnitClass.Naval => UnitCategory.Naval,
            UnitClass.Spearman => UnitCategory.Spearman,
            _ => UnitCategory.Infantry,
        };
    }
}
