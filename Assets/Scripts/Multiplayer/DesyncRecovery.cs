using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Multiplayer
{
    // AoE-Parity Phase 5: the recovery half of resync-on-desync. StateHash
    // now computes a real, live value every simulated second (see
    // StateHash.Subscribe) - the piece a future transport would use to
    // detect that a peer's hash disagrees with the local one. This class is
    // what happens once that's detected: given an authoritative snapshot
    // (however it arrived - a peer's own MatchSaveData capture, sent over a
    // transport that doesn't exist yet), overwrite local dynamic state to
    // match it.
    //
    // What this proves (verified via EditMode tests replaying a diverge-
    // then-recover scenario, and live in Play mode against a real running
    // match): given a known-correct snapshot, applying it makes local state
    // - and therefore StateHash.Compute() - match that snapshot again.
    //
    // What this does NOT do, and can't yet: detect divergence itself (that
    // needs an actual peer to compare hashes against - there is none, single
    // process only), or receive a snapshot over a network (no transport
    // exists - see docs/AOE_PARITY_EXECUTION_PLAN.md Phase 5). Apply() takes
    // the snapshot as a plain in-memory parameter specifically so wiring in
    // a real transport later is just "deserialize the bytes it sends into a
    // MatchSaveData, call Apply" - no change needed here.
    public static class DesyncRecovery
    {
        public static void Apply(MatchSaveData authoritativeSnapshot)
        {
            SaveManager.ApplySnapshotToRunningMatch(authoritativeSnapshot);
        }
    }
}
