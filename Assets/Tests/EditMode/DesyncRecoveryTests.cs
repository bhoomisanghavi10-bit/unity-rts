using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // AoE-Parity Phase 5 (resync-on-desync): proves DesyncRecovery.Apply
    // actually recovers a diverged local state to match a known-correct
    // snapshot, verified via StateHash reconverging - the concrete
    // recovery-side counterpart to CommandBusDeterminismTests' "same inputs
    // -> same state" proof. Deliberately single-process/synthetic: there is
    // no peer to detect a real desync against, and no transport to receive
    // a real snapshot over - see DesyncRecovery.cs's own doc comment for
    // what this does and doesn't prove.
    public class DesyncRecoveryTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            // Unit.All/Building.All are shared static lists across the
            // whole EditMode run - another test file's imperfect cleanup
            // can leave stale entries in them by the time this test runs.
            // WipeCurrentMatch (called via ApplySnapshotToRunningMatch)
            // iterates both and calls Destroy() on every entry, so any
            // stale leftovers would inflate the count of Editor-only
            // "Destroy may not be called from edit mode" errors below to
            // an unpredictable number - confirmed directly (14 occurrences
            // for what should have been exactly 2). Cleared here so this
            // test's own Destroy() count is deterministic; this only
            // forgets the stale entries, it doesn't destroy whatever
            // GameObjects they pointed at (already-orphaned garbage from a
            // finished test either way).
            Unit.All.Clear();
            Building.All.Clear();

            // ApplySnapshotToRunningMatch's WipeCurrentMatch calls Destroy()
            // (correct for real Play mode), which logs an Editor-only error
            // here ("Destroy may not be called from edit mode") even though
            // the call itself doesn't throw - same expected-not-a-regression
            // situation BuildingAttackerTests/GathererCombatResponseTests
            // already document for their own Editor-only logs.
            LogAssert.ignoreFailingMessages = true;
        }

        // Neither ignoreFailingMessages nor a custom ILogHandler filter
        // suppresses this specific Editor-only error in this Unity Test
        // Framework version - confirmed directly, both still fail as
        // "Unhandled log message" (the test framework's own log capture
        // sits ahead of Debug.unityLogger.logHandler, so replacing the
        // handler doesn't hide anything from it). LogAssert.Expect is the
        // only mechanism that actually works, but it needs an *exact*
        // count: SoldierFactory.Spawn's own weapon attachment
        // (WeaponAttachment.KeepOnlyFirstMesh) also calls Destroy() on
        // every extra renderer sub-mesh the sourced weapon model happens to
        // have, on top of WipeCurrentMatch's per-unit Destroy() - not
        // simply "1 per unit." Counts below were measured directly off a
        // real test run's captured console output, not guessed.
        private static void ExpectDestroyEditModeErrors(int exactCount)
        {
            for (int i = 0; i < exactCount; i++)
            {
                LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            }
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            foreach (GameObject go in _spawned)
            {
                if (go == null)
                {
                    continue;
                }
                // Same Unit.All-doesn't-fire-OnEnable/OnDisable-synchronously
                // gotcha CommandBusDeterminismTests/BuildingAttackerTests
                // already document.
                if (go.TryGetComponent(out Unit unit))
                {
                    Unit.All.Remove(unit);
                }
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        // SaveManager.Capture()/CaptureFaction reads ResourceStockpile.For(faction)
        // unconditionally for every AllFactions entry (Player/Enemy/Enemy2) -
        // every real instance is scene-authored via the Inspector, so a bare
        // EditMode test needs to create its own or Capture() throws a
        // NullReferenceException reading a null stockpile.
        private void CreateStockpile(FactionId faction)
        {
            var go = new GameObject("Stockpile_" + faction);
            go.AddComponent<ResourceStockpile>().Configure(faction);
            _spawned.Add(go);
        }

        // RestoreUnits spawns replacement units via real Factories
        // (SoldierFactory etc.), which AddComponent<Unit>() the same way
        // this test's own CreateUnit does - and hit the exact same
        // OnEnable-doesn't-fire-synchronously-in-EditMode gotcha, so they
        // don't appear in the custom Unit.All list on their own either.
        // Object.FindObjectsByType queries Unity's own live object graph
        // instead (unaffected by that gotcha), so it's used here to find
        // and register the newly-spawned ones - the same "register
        // directly" workaround CreateUnit already uses for its own units.
        // staleUnits are the pre-recovery units WipeCurrentMatch's Destroy()
        // call hasn't actually removed from the scene yet (also an EditMode-
        // only artifact - a real frame boundary would have cleared them) -
        // excluded here so they aren't mistaken for newly-restored ones.
        //
        // Sorted to match snapshot.units' own order (by position) before
        // adding to Unit.All, rather than whatever order FindObjectsByType
        // happens to return - StateHash.Compute() folds Unit.All in
        // iteration order, and a real Play-mode frame boundary would
        // naturally register these via OnEnable in RestoreUnits' own
        // spawn order (which matches snapshot.units' order); this restores
        // that same ordering guarantee that only breaks in the
        // FindObjectsByType workaround this method exists to work around -
        // confirmed by direct debugging (position/faction/health were
        // already exactly correct on every unit; only Unit.All's happened-
        // to-differ enumeration order was making the hash disagree).
        private void SyncUnitAllFromScene(MatchSaveData snapshot, params GameObject[] staleUnits)
        {
            var found = new List<Unit>(Object.FindObjectsByType<Unit>(FindObjectsSortMode.None));
            found.RemoveAll(u => System.Array.IndexOf(staleUnits, u.gameObject) >= 0);

            foreach (UnitSaveData saved in snapshot.units)
            {
                Unit match = found.Find(u => Vector3.Distance(u.transform.position, saved.position) < 0.01f);
                if (match == null)
                {
                    continue;
                }
                found.Remove(match);
                if (!Unit.All.Contains(match))
                {
                    Unit.All.Add(match);
                }
                if (!_spawned.Contains(match.gameObject))
                {
                    _spawned.Add(match.gameObject);
                }
            }
        }

        private GameObject CreateUnit(string name, Vector3 position, FactionId faction)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            Unit unit = go.AddComponent<Unit>();
            if (!Unit.All.Contains(unit))
            {
                Unit.All.Add(unit);
            }
            // No Builder component - Attackable defaults to UnitClass.Infantry,
            // which SaveManager.IdentifyUnitType maps to "Soldier", so
            // RestoreUnits below spawns a real SoldierFactory unit for it.
            go.AddComponent<Attackable>().Configure(30f);
            go.AddComponent<FactionMember>().Configure(faction);
            _spawned.Add(go);
            return go;
        }

        [Test]
        public void Apply_RecoversDivergedUnitState_StateHashReconverges()
        {
            CreateStockpile(FactionId.Player);
            CreateStockpile(FactionId.Enemy);
            CreateStockpile(FactionId.Enemy2);
            GameObject unitA = CreateUnit("A", new Vector3(0f, 0f, 0f), FactionId.Player);
            GameObject unitB = CreateUnit("B", new Vector3(10f, 0f, 5f), FactionId.Enemy);

            // The authoritative snapshot, captured while state is still
            // correct - mirrors what a real peer's own capture would be.
            MatchSaveData snapshot = SaveManager.Capture();
            uint baselineHash = StateHash.Compute();

            // Diverge: move a unit, same class of drift a real desync
            // (e.g. a missed/misordered command on one peer) would cause.
            unitA.transform.position += new Vector3(50f, 0f, 50f);
            uint perturbedHash = StateHash.Compute();
            Assert.AreNotEqual(baselineHash, perturbedHash,
                "Moving a unit must actually change StateHash, or this test's positive case below would be meaningless.");

            ExpectDestroyEditModeErrors(14); // 2 units x 7 (WipeCurrentMatch's own Destroy + WeaponAttachment's mesh trim per restored Soldier)
            DesyncRecovery.Apply(snapshot);

            // WipeCurrentMatch uses Destroy(), not DestroyImmediate() - in
            // EditMode that doesn't take effect synchronously, so the old
            // (pre-recovery) units are still in Unit.All at this exact
            // point even though they're pending destruction. Scrubbed
            // directly here, same convention as this test class's own
            // TearDown, so the hash below reflects only the newly-restored
            // units RestoreUnits just spawned - which is what a real
            // Play-mode frame boundary would do for free.
            Unit.All.Remove(unitA.GetComponent<Unit>());
            Unit.All.Remove(unitB.GetComponent<Unit>());
            SyncUnitAllFromScene(snapshot, unitA, unitB);

            uint recoveredHash = StateHash.Compute();
            Assert.AreEqual(baselineHash, recoveredHash,
                "After DesyncRecovery.Apply, StateHash must reconverge to the authoritative snapshot's value.");
        }

        [Test]
        public void Apply_RestoresExactUnitPositionsAndHealth_NotJustAMatchingHash()
        {
            // Independent of the hash check above - guards against a hash
            // collision masking a real bug (e.g. two units silently swapped).
            CreateStockpile(FactionId.Player);
            CreateStockpile(FactionId.Enemy);
            CreateStockpile(FactionId.Enemy2);
            GameObject unitA = CreateUnit("A", new Vector3(3f, 0f, 4f), FactionId.Player);
            // 1 from this TakeDamage's own VFX burst Destroy() (same Editor-
            // only gotcha BuildingAttackerTests documents) + 7 from later
            // restoring the 1 unit (WipeCurrentMatch's own Destroy +
            // WeaponAttachment's mesh trim) - queued upfront since
            // LogAssert.Expect just drains its queue in order as matching
            // messages arrive, regardless of what else happens in between.
            ExpectDestroyEditModeErrors(8);
            unitA.GetComponent<Attackable>().TakeDamage(10f);

            MatchSaveData snapshot = SaveManager.Capture();
            Vector3 expectedPosition = unitA.transform.position;
            float expectedHealth = unitA.GetComponent<Attackable>().Health;

            unitA.transform.position = Vector3.zero;

            DesyncRecovery.Apply(snapshot);

            Unit.All.Remove(unitA.GetComponent<Unit>());
            SyncUnitAllFromScene(snapshot, unitA);
            Unit restoredUnit = Unit.All.Count > 0 ? Unit.All[0] : null;

            Assert.IsNotNull(restoredUnit, "RestoreUnits should have spawned a replacement unit.");
            Assert.Less(Vector3.Distance(restoredUnit.transform.position, expectedPosition), 0.01f,
                "Restored unit's position should match the snapshot exactly.");
            Assert.AreEqual(expectedHealth, restoredUnit.GetComponent<Attackable>().Health, 0.01f,
                "Restored unit's health should match the snapshot exactly.");
        }
    }
}
