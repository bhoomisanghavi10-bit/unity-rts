using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Wildlife;

namespace KingdomsOfBharat.Buildings
{
    // Wave 6 item 33: one-click "send every Worker home" convenience,
    // AoE II's own Town Bell. Purely a local UI convenience, not a real
    // networked order of its own - it just calls the same
    // GarrisonSeeker.GarrisonAt every manual right-click garrison order
    // already calls (see SelectionManager's hitGarrison branch), which is
    // itself already local-only/un-networked (each client resolves its own
    // units' garrisoning deterministically), so this needs no CommandBus/
    // NetMessage wiring - see CLAUDE.md's Town Bell session note.
    //
    // Deliberately no "ring again to send them back out" toggle (that's
    // Ungarrison's job, already wired per-building on BuildMenu) - the
    // roadmap item text specs one-click garrison-all, not an alarm mode.
    public static class TownBell
    {
        // Best-effort nearest-with-room assignment: reserves room locally
        // as each worker is assigned so a burst of workers doesn't all
        // pick the same closest TownCenter and overflow it, but doesn't
        // guarantee delivery - a worker whose only reachable TownCenter
        // fills up before it arrives simply stops without entering,
        // identical to what already happens for a manual garrison order
        // today (GarrisonSeeker.GarrisonAt has never retried on failure).
        public static int Ring(FactionId faction)
        {
            List<GarrisonPoint> townCenters = new List<GarrisonPoint>();
            foreach (Building building in Building.All)
            {
                if (building is TownCenter
                    && building.TryGetComponent(out FactionMember member)
                    && member.Faction == faction
                    && building.TryGetComponent(out GarrisonPoint garrisonPoint))
                {
                    townCenters.Add(garrisonPoint);
                }
            }

            if (townCenters.Count == 0)
            {
                return 0;
            }

            Dictionary<GarrisonPoint, int> reserved = new Dictionary<GarrisonPoint, int>();
            int sent = 0;

            foreach (Unit unit in Unit.All)
            {
                if (!unit.TryGetComponent(out Gatherer _)
                    || !unit.TryGetComponent(out GarrisonSeeker seeker)
                    || !unit.TryGetComponent(out FactionMember unitFaction)
                    || unitFaction.Faction != faction)
                {
                    continue;
                }

                GarrisonPoint nearest = NearestWithRoom(unit.transform.position, townCenters, reserved);
                if (nearest == null)
                {
                    continue;
                }

                // Same cancel list as SelectionManager's own manual
                // right-click garrison order (its hitGarrison branch) -
                // a worker mid-build/repair/farm/livestock/fight needs
                // every one of those stopped, not just its gather task,
                // or it'll keep doing that job instead of walking home.
                unit.TryGetComponent(out Gatherer gatherer);
                gatherer.CancelGather();
                if (unit.TryGetComponent(out Builder builder))
                {
                    builder.CancelBuild();
                }
                if (unit.TryGetComponent(out Repairer repairer))
                {
                    repairer.CancelRepair();
                }
                if (unit.TryGetComponent(out FarmWorker farmWorker))
                {
                    farmWorker.CancelWork();
                }
                if (unit.TryGetComponent(out LivestockWorker livestockWorker))
                {
                    livestockWorker.CancelWork();
                }
                if (unit.TryGetComponent(out MeleeAttacker attacker))
                {
                    attacker.CancelAttack();
                }
                seeker.GarrisonAt(nearest);
                reserved[nearest] = reserved.TryGetValue(nearest, out int count) ? count + 1 : 1;
                sent++;
            }

            return sent;
        }

        private static GarrisonPoint NearestWithRoom(
            Vector3 from, List<GarrisonPoint> candidates, Dictionary<GarrisonPoint, int> reserved)
        {
            GarrisonPoint best = null;
            float bestDistance = float.MaxValue;

            foreach (GarrisonPoint candidate in candidates)
            {
                int alreadyReserved = reserved.TryGetValue(candidate, out int count) ? count : 0;
                if (candidate.Count + alreadyReserved >= candidate.Capacity)
                {
                    continue;
                }

                float distance = Vector3.Distance(from, candidate.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }
    }
}
