using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Progression
{
    // Wave 6 item 36: a post-match score readout, four weighted categories
    // modeled on AoE II's own Military/Economy/Technology/Society split -
    // not a byte-for-byte port of AoE II's real (undocumented) weights,
    // just the same shape, same as every other tier line's own "reuse the
    // closest existing precedent, not independently balanced" convention
    // in this project.
    //
    // Every category except kills/razings is computed live from the
    // existing Unit.All/Building.All/ResourceStockpile/AgeProgress/
    // UpgradeProgress/UniqueTechProgress registries - the project's own
    // established "recompute, don't incrementally track" convention (see
    // Population's own header comment) - so there's nothing here that can
    // drift out of sync with reality after a unit dies or a building is
    // destroyed. Kills/razings are the one genuine exception: a dead unit
    // can't be recounted after the fact, so those two are the only
    // incrementing counters in this file, credited from Attackable's own
    // death branch in TakeDamage.
    public static class ScoreProgress
    {
        private static readonly System.Collections.Generic.Dictionary<FactionId, int> Kills =
            new System.Collections.Generic.Dictionary<FactionId, int>();
        private static readonly System.Collections.Generic.Dictionary<FactionId, int> BuildingsRazed =
            new System.Collections.Generic.Dictionary<FactionId, int>();

        public readonly struct Breakdown
        {
            public readonly int Military;
            public readonly int Economy;
            public readonly int Technology;
            public readonly int Society;
            public int Total => Military + Economy + Technology + Society;

            public Breakdown(int military, int economy, int technology, int society)
            {
                Military = military;
                Economy = economy;
                Technology = technology;
                Society = society;
            }
        }

        // Called from Attackable.TakeDamage's death branch, crediting
        // whoever landed the killing blow (the attacker passed into
        // TakeDamage) rather than the victim's own faction. A no-op if the
        // attacker has no FactionMember (shouldn't happen for any real
        // combat unit, but matches this project's existing "guard, don't
        // assume" convention for cross-system hooks).
        public static void RecordKill(FactionId attackerFaction, bool victimWasBuilding)
        {
            if (victimWasBuilding)
            {
                BuildingsRazed[attackerFaction] = BuildingsRazedCount(attackerFaction) + 1;
            }
            else
            {
                Kills[attackerFaction] = KillCount(attackerFaction) + 1;
            }
        }

        public static int KillCount(FactionId faction)
        {
            return Kills.TryGetValue(faction, out int count) ? count : 0;
        }

        public static int BuildingsRazedCount(FactionId faction)
        {
            return BuildingsRazed.TryGetValue(faction, out int count) ? count : 0;
        }

        public static Breakdown Compute(FactionId faction)
        {
            return new Breakdown(
                Military(faction),
                Economy(faction),
                Technology(faction),
                Society(faction));
        }

        // Kills*3 + BuildingsRazed*5 + living military units*1. "Military"
        // excludes Workers (identified by Gatherer presence, the same
        // marker TownBell/IdleWorkerFinder already use) and Support-class
        // utility units (Vaidya/Purohita/Scout - non-combat by design),
        // since Worker itself is stamped UnitClass.Infantry too and can't
        // be told apart from a real Soldier by class alone.
        private static int Military(FactionId faction)
        {
            int livingMilitary = 0;
            foreach (Unit unit in Unit.All)
            {
                if (!unit.TryGetComponent(out FactionMember member) || member.Faction != faction)
                {
                    continue;
                }

                if (unit.TryGetComponent(out Gatherer _))
                {
                    continue;
                }

                if (unit.TryGetComponent(out Attackable attackable)
                    && attackable.Class != UnitClass.Building
                    && attackable.Class != UnitClass.Support)
                {
                    livingMilitary++;
                }
            }

            return KillCount(faction) * 3 + BuildingsRazedCount(faction) * 5 + livingMilitary;
        }

        // Current stockpile total (all 4 resource types) *0.05 + worker
        // count *3. Deliberately not a lifetime-gathered total - this
        // project has no single choke point every resource deposit
        // (Gatherer/Farm/LivestockWorker/Trader/BoatTrader/scripted
        // mission grants) already funnels through, so adding one would be
        // real new bookkeeping scope beyond a session-sized item; current
        // stockpile + worker count is a live, driftless proxy instead.
        private static int Economy(FactionId faction)
        {
            ResourceStockpile stockpile = ResourceStockpile.For(faction);
            float stockpileTotal = stockpile == null
                ? 0f
                : stockpile.GetTotal(ResourceType.Wood) + stockpile.GetTotal(ResourceType.Food)
                  + stockpile.GetTotal(ResourceType.Gold) + stockpile.GetTotal(ResourceType.Stone);

            int workers = 0;
            foreach (Unit unit in Unit.All)
            {
                if (unit.TryGetComponent(out FactionMember member) && member.Faction == faction
                    && unit.TryGetComponent(out Gatherer _))
                {
                    workers++;
                }
            }

            return Mathf.RoundToInt(stockpileTotal * 0.05f) + workers * 3;
        }

        // Age reached (Ancient=0..Imperial=3) *30, flat Attack/Armor tiers
        // (0-3 each) *15, unique tech researched ?50:0. Deliberately
        // doesn't also sum every per-unit-class tier line (Infantry/
        // Cavalry/Archer/... - 11 separate static classes with no shared
        // interface) - a real future refinement, not attempted this
        // session to keep this item session-sized.
        private static int Technology(FactionId faction)
        {
            int ageOrdinal = (int)AgeProgress.CurrentAge(faction);
            int upgradeTiers = UpgradeProgress.AttackTier(faction) + UpgradeProgress.ArmorTier(faction);
            int uniqueTechBonus = UniqueTechProgress.HasResearched(faction) ? 50 : 0;

            return ageOrdinal * 30 + upgradeTiers * 15 + uniqueTechBonus;
        }

        // Population *4 + complete buildings owned *3. AoE II's own
        // Society score covers Wonders/Relics/villagers/Castles - this
        // project has neither Wonders nor Relics yet (Wave 6 item 35 is
        // unbuilt), so population + building count are the closest
        // available proxies.
        private static int Society(FactionId faction)
        {
            int completeBuildings = 0;
            foreach (Building building in Building.All)
            {
                if (!building.TryGetComponent(out FactionMember member) || member.Faction != faction)
                {
                    continue;
                }

                bool complete = !building.TryGetComponent(out ConstructionSite site) || site.IsComplete;
                if (complete)
                {
                    completeBuildings++;
                }
            }

            return Population.Current(faction) * 4 + completeBuildings * 3;
        }

        // Per-match reset, called from CivilizationSetup.BeginMatchCore
        // alongside DiplomacyRegistry.Reset()/TeamColorBuildingTint.Reset()
        // - only Kills/BuildingsRazed need clearing here since Unit.All/
        // Building.All/ResourceStockpile/AgeProgress are all reset by
        // BeginMatchCore's own existing flow (scene reload + explicit
        // AgeProgress.Initialize calls). Note: UpgradeProgress/
        // UniqueTechProgress have no reset call anywhere in
        // CivilizationSetup either - a pre-existing gap, not introduced
        // here - so Technology's score can read stale tiers across two
        // matches started without an intervening domain reload; flagged,
        // not fixed, since touching that reset flow is out of this item's
        // scope.
        public static void Reset()
        {
            Kills.Clear();
            BuildingsRazed.Clear();
        }

        internal static void ResetForTests()
        {
            Reset();
        }
    }
}
