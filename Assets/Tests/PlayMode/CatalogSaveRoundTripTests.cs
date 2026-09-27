using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Multiplayer;

namespace KingdomsOfBharat.PlayModeTests
{
    // Reference round-trip fixture for the extended save schema (this
    // ticket): every entity/state point the ticket names - Worker,
    // Padati, Dhanurdhara, Town Center, Barracks, damaged health, partial
    // construction, a production queue, a research queue, researched
    // progression, non-default resources - built through real production
    // APIs on a live match, captured, wiped, and restored through the real
    // SaveManager.Capture/ApplySnapshotToRunningMatch pipeline (reflected
    // into since they're internal, same precedent
    // DefinitionCatalogSpawnTests.cs already established - this assembly
    // has no InternalsVisibleTo grant from Runtime).
    //
    // Every comparison below re-finds "the same" restored entity by its
    // STABLE RUNTIME NetworkId (this ticket's own requirement 3), not by
    // scanning for a plausible-looking match - and compares actual field
    // values (health, construction progress, training/research state,
    // tier, resources), not just Unit.All/Building.All counts.
    public class CatalogSaveRoundTripTests
    {
        private MethodInfo _captureMethod;
        private MethodInfo _applyMethod;

        [SetUp]
        public void SetUp()
        {
            _captureMethod = typeof(SaveManager).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic);
            _applyMethod = typeof(SaveManager).GetMethod("ApplySnapshotToRunningMatch", BindingFlags.Static | BindingFlags.NonPublic);
        }

        [UnityTest]
        public IEnumerator CatalogReferenceFixture_RoundTripsLogicalStateAndStaysOperational()
        {
            LogAssert.ignoreFailingMessages = false;

            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            CivilizationSetup civilizationSetup = Object.FindFirstObjectByType<CivilizationSetup>();
            Assert.IsNotNull(civilizationSetup);
            civilizationSetup.BeginMatch(CivilizationId.Chola);
            yield return null;
            yield return null;

            const FactionId player = FactionId.Player;

            // --- Fixture setup, all via production APIs ---

            // Non-default resources: distinct per-type values so a
            // transposition bug (e.g. Wood/Gold swapped) would be caught.
            ResourceStockpile stockpile = ResourceStockpile.For(player);
            stockpile.SetTotal(ResourceType.Food, 410f);
            stockpile.SetTotal(ResourceType.Wood, 275f);
            stockpile.SetTotal(ResourceType.Gold, 610f);
            stockpile.SetTotal(ResourceType.Stone, 130f);

            // Deterministic age so every tier-research age gate below
            // passes regardless of Chola's own default starting age.
            AgeProgress.Initialize(player, AgeId.Imperial);

            // Researched progression: Padati's own tier ladder, advanced
            // once (Padati -> Senani) before anything using it spawns.
            InfantryLineProgress.AdvanceTier(player);
            int infantryTierBefore = InfantryLineProgress.Tier(player);
            Assert.Greater(infantryTierBefore, 0, "Sanity check: the tier actually advanced.");

            // Worker, damaged.
            GameObject workerGo = EntitySpawner.SpawnUnit(DefinitionCatalog.Worker, player, new Vector3(-40f, 1f, -35f));
            Attackable workerAttackable = workerGo.GetComponent<Attackable>();
            float workerDamagedHealth = workerAttackable.MaxHealth * 0.4f;
            workerAttackable.RestoreHealth(workerDamagedHealth);
            Assert.IsTrue(NetworkId.TryGetId(workerGo.GetComponent<Unit>(), out int workerId));

            // Padati and Dhanurdhara, spawned via their own explicit
            // catalog IDs (always base-tier, per DefinitionCatalog's own
            // contract - proven by DefinitionCatalogSpawnTests.cs).
            GameObject padatiGo = EntitySpawner.SpawnUnit(DefinitionCatalog.CholaPadati, player, new Vector3(-35f, 1f, -35f));
            Assert.IsTrue(NetworkId.TryGetId(padatiGo.GetComponent<Unit>(), out int padatiId));

            GameObject dhanurdharaGo = EntitySpawner.SpawnUnit(DefinitionCatalog.CholaDhanurdhara, player, new Vector3(-30f, 1f, -35f));
            Assert.IsTrue(NetworkId.TryGetId(dhanurdharaGo.GetComponent<Unit>(), out int dhanurdharaId));

            // Town Center with a production queue: a Worker mid-training.
            GameObject townCenterGo = EntitySpawner.SpawnBuilding(DefinitionCatalog.TownCenter, player, new Vector3(-15f, 1f, -35f));
            TownCenter townCenter = townCenterGo.GetComponent<TownCenter>();
            townCenter.RequestTrain();
            Assert.IsTrue(townCenter.IsTraining, "Sanity check: Worker training actually started.");
            Assert.IsTrue(NetworkId.TryGetId(townCenterGo.GetComponent<Building>(), out int townCenterId));

            // Barracks #1: partial construction (no training/research -
            // RequestTrain*/RequestResearch* both require IsComplete).
            GameObject barracksPartialGo = DefinitionCatalog.Default.Spawn(DefinitionCatalog.Barracks, new Vector3(0f, 1f, -35f), player, buildTime: 4f);
            ConstructionSite partialSite = barracksPartialGo.GetComponent<ConstructionSite>();
            partialSite.BeginBuilding();
            yield return new WaitForSeconds(2f);
            partialSite.StopBuilding();
            float partialProgressBefore = partialSite.Progress;
            Assert.Greater(partialProgressBefore, 0.05f, "Sanity check: construction actually advanced.");
            Assert.Less(partialProgressBefore, 0.95f, "Sanity check: construction is genuinely partial, not complete.");
            Assert.IsTrue(NetworkId.TryGetId(barracksPartialGo.GetComponent<Building>(), out int barracksPartialId));

            // Barracks #2: complete, with both a production queue (Padati
            // training) and a research queue (Infantry tier research) in
            // flight at once - the same "training and research don't
            // block each other" shape this project's own Barracks/
            // TownCenter classes already establish.
            GameObject barracksCompleteGo = EntitySpawner.SpawnBuilding(DefinitionCatalog.Barracks, player, new Vector3(15f, 1f, -35f));
            // Construction only progresses while a builder is actively
            // assigned (ConstructionSite.Update's own guard) - jump
            // straight to complete rather than simulating an assigned
            // Worker, since construction-in-progress is already covered by
            // barracksPartialGo above.
            barracksCompleteGo.GetComponent<ConstructionSite>().CompleteImmediately();
            Barracks barracksComplete = barracksCompleteGo.GetComponent<Barracks>();
            Assert.IsTrue(barracksComplete.IsComplete, "Sanity check: Barracks #2 finished its own quick construction.");
            barracksComplete.RequestTrain();
            barracksComplete.RequestResearchInfantryTier();
            Assert.IsTrue(barracksComplete.IsTraining, "Sanity check: Padati training actually started.");
            Assert.IsTrue(barracksComplete.IsResearchingInfantryTier, "Sanity check: Infantry tier research actually started.");
            Assert.IsTrue(NetworkId.TryGetId(barracksCompleteGo.GetComponent<Building>(), out int barracksCompleteId));

            // Snapshot resources right before capture, not the fixture's
            // own initial setup values above - RequestTrain() on both the
            // Town Center and Barracks #2 legitimately spent Food/Gold in
            // between, so the correct "before" baseline for the round-trip
            // comparison is whatever is actually left at capture time.
            float foodBefore = stockpile.GetTotal(ResourceType.Food);
            float woodBefore = stockpile.GetTotal(ResourceType.Wood);
            float goldBefore = stockpile.GetTotal(ResourceType.Gold);
            float stoneBefore = stockpile.GetTotal(ResourceType.Stone);

            // --- Capture ---
            var captured = (MatchSaveData)_captureMethod.Invoke(null, null);
            string json = JsonUtility.ToJson(captured);
            MatchSaveData data = JsonUtility.FromJson<MatchSaveData>(json);
            // SaveManager.CurrentSaveVersion is internal (no InternalsVisibleTo
            // grant to this assembly, same as Capture/ApplySnapshotToRunningMatch
            // above) - 1 is its known current value, covered independently
            // and exactly by SaveSchemaVersionTests.cs (EditMode, same
            // assembly as Runtime).
            Assert.AreEqual(1, data.version, "Capture() should stamp an explicit, nonzero schema version.");

            // --- Wipe and restore through the real, phased pipeline ---
            _applyMethod.Invoke(null, new object[] { data });
            yield return null;
            yield return null;

            // --- Requirement 8: compare LOGICAL state, not just counts -
            // re-find each entity by its preserved stable runtime id. ---

            Assert.IsTrue(NetworkId.TryResolveUnit(workerId, out Unit restoredWorker), "Worker's stable runtime id should resolve after restore.");
            Attackable restoredWorkerAttackable = restoredWorker.GetComponent<Attackable>();
            Assert.AreEqual(workerDamagedHealth, restoredWorkerAttackable.Health, 0.01f, "Damaged health should survive the round trip exactly.");

            Assert.IsTrue(NetworkId.TryResolveUnit(padatiId, out Unit restoredPadati), "Padati's stable runtime id should resolve after restore.");
            Assert.AreEqual(DefinitionCatalog.CholaPadati, restoredPadati.GetComponent<DefinitionId>().Value,
                "Restored Padati should still carry its stable DEFINITION id, not an inferred generic class name.");

            Assert.IsTrue(NetworkId.TryResolveUnit(dhanurdharaId, out Unit restoredDhanurdhara), "Dhanurdhara's stable runtime id should resolve after restore.");
            Assert.AreEqual(DefinitionCatalog.CholaDhanurdhara, restoredDhanurdhara.GetComponent<DefinitionId>().Value);

            Assert.IsTrue(NetworkId.TryResolveBuilding(townCenterId, out Building restoredTownCenterBuilding), "Town Center's stable runtime id should resolve after restore.");
            TownCenter restoredTownCenter = (TownCenter)restoredTownCenterBuilding;
            Assert.IsTrue(restoredTownCenter.IsTraining, "The Worker-training queue should still be in progress immediately after restore.");

            Assert.IsTrue(NetworkId.TryResolveBuilding(barracksPartialId, out Building restoredBarracksPartialBuilding), "The partially-built Barracks' stable runtime id should resolve after restore.");
            ConstructionSite restoredPartialSite = restoredBarracksPartialBuilding.GetComponent<ConstructionSite>();
            Assert.IsFalse(restoredPartialSite.IsComplete, "Partial construction should not have silently become complete.");
            Assert.AreEqual(partialProgressBefore, restoredPartialSite.Progress, 0.01f, "Partial construction progress should survive the round trip exactly.");

            Assert.IsTrue(NetworkId.TryResolveBuilding(barracksCompleteId, out Building restoredBarracksCompleteBuilding), "The complete Barracks' stable runtime id should resolve after restore.");
            Barracks restoredBarracksComplete = (Barracks)restoredBarracksCompleteBuilding;
            Assert.IsTrue(restoredBarracksComplete.IsTraining, "The Padati-training queue should still be in progress immediately after restore.");
            Assert.IsTrue(restoredBarracksComplete.IsResearchingInfantryTier, "The Infantry tier-research queue should still be in progress immediately after restore.");

            // Researched progression (faction-level, not tied to any one
            // entity) and non-default resources.
            Assert.AreEqual(infantryTierBefore, InfantryLineProgress.Tier(player), "The researched Infantry tier should survive the round trip.");
            Assert.AreEqual(foodBefore, stockpile.GetTotal(ResourceType.Food), 0.01f);
            Assert.AreEqual(woodBefore, stockpile.GetTotal(ResourceType.Wood), 0.01f);
            Assert.AreEqual(goldBefore, stockpile.GetTotal(ResourceType.Gold), 0.01f);
            Assert.AreEqual(stoneBefore, stockpile.GetTotal(ResourceType.Stone), 0.01f);

            // --- Requirement 9: continue the restored match briefly and
            // prove restored entities are genuinely operational, not just
            // present. A generous real-time wait so every queue (whatever
            // its exact remaining duration was at capture time) has long
            // since finished if - and only if - its countdown actually
            // resumed ticking from where it was saved. The longest of the
            // three is the Infantry tier research targeting Khandayata
            // (InfantryLineProgress's own table: researchTime 30s, no age/
            // civ multiplier applied to it) - 35s comfortably clears that
            // regardless of how much of it had already elapsed pre-capture.
            yield return new WaitForSeconds(35f);

            Assert.IsFalse(restoredTownCenter.IsTraining, "The restored Worker-training queue should have completed after resuming.");
            Assert.IsFalse(restoredBarracksComplete.IsTraining, "The restored Padati-training queue should have completed after resuming.");
            Assert.IsFalse(restoredBarracksComplete.IsResearchingInfantryTier, "The restored Infantry tier-research queue should have completed after resuming.");
            Assert.Greater(InfantryLineProgress.Tier(player), infantryTierBefore, "The Infantry tier should have advanced again once its resumed research finished.");
        }

        // Requirement 4's own justification for putting "match
        // configuration" (faction tier progression) BEFORE "entity
        // creation" is that a unit's stats bake in at spawn time from
        // whatever tier its faction has right then (InfantryLineProgress's
        // own "not retroactive" convention) - the round-trip test above
        // never actually exercises this, since Padati/Dhanurdhara there
        // are spawned via their explicit catalog IDs, which are always
        // base-tier BY DESIGN regardless of research (see
        // DefinitionCatalogSpawnTests.ExplicitBaseTier_DoesNotFollowResearch).
        // This test uses the LEGACY factory adapter instead (unitType
        // "Soldier", not a catalog ID) specifically because that one DOES
        // follow research tier - if the restore phases ever ran in the
        // wrong order (entities created before tiers restored), this is
        // the test that would actually catch it: the restored Soldier
        // would come back at tier-0 MaxHealth instead of the correct,
        // already-researched tier.
        [UnityTest]
        public IEnumerator RestoredLegacyTieredUnit_BakesInTheCorrectlyRestoredTier_ProvingPhaseOrdering()
        {
            LogAssert.ignoreFailingMessages = false;

            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            CivilizationSetup civilizationSetup = Object.FindFirstObjectByType<CivilizationSetup>();
            civilizationSetup.BeginMatch(CivilizationId.Chola);
            yield return null;
            yield return null;

            const FactionId player = FactionId.Player;
            InfantryLineProgress.AdvanceTier(player);
            int tierAtTrainingTime = InfantryLineProgress.Tier(player);

            // Legacy adapter, not DefinitionCatalog.CholaPadati - bakes in
            // whatever tier the faction has right now (confirmed via the
            // MaxHealth comparison below), and is identified/saved under
            // the legacy "Soldier" name (DefinitionId is null for a
            // tier-advanced legacy spawn - see
            // DefinitionCatalogSpawnTests.ExplicitBaseTier...).
            GameObject soldierGo = SoldierFactory.Spawn(new Vector3(-20f, 1f, -35f), player);
            Assert.IsNull(soldierGo.GetComponent<DefinitionId>());
            float tieredMaxHealthAtCapture = soldierGo.GetComponent<Attackable>().MaxHealth;
            Assert.IsTrue(NetworkId.TryGetId(soldierGo.GetComponent<Unit>(), out int soldierId));

            var captured = (MatchSaveData)_captureMethod.Invoke(null, null);
            MatchSaveData data = JsonUtility.FromJson<MatchSaveData>(JsonUtility.ToJson(captured));
            UnitSaveData savedSoldier = data.units.Find(u => u.networkId == soldierId);
            Assert.IsNotNull(savedSoldier);
            Assert.AreEqual("Soldier", savedSoldier.unitType, "Sanity check: captured under the legacy name, not a catalog id.");
            Assert.AreEqual(tierAtTrainingTime, data.factions.Find(f => f.faction == (int)player).infantryTier);

            _applyMethod.Invoke(null, new object[] { data });
            yield return null;
            yield return null;

            // The tier must already be restored (phase 1) by the time this
            // saved "Soldier" entry re-spawns through SoldierFactory
            // (phase 2) - if entity creation ran first, this would come
            // back at tier 0's (lower) MaxHealth instead.
            Assert.IsTrue(NetworkId.TryResolveUnit(soldierId, out Unit restoredSoldier));
            float restoredMaxHealth = restoredSoldier.GetComponent<Attackable>().MaxHealth;
            Assert.AreEqual(tieredMaxHealthAtCapture, restoredMaxHealth, 0.01f,
                "A restored legacy-adapter unit must bake in the already-restored tier, proving match configuration runs before entity creation.");
        }

        // Requirement 6: backward compatibility is only claimed where it's
        // explicitly implemented and tested - this is that test for the
        // full restore PIPELINE, not just the isolated sentinel-default
        // values SaveSchemaVersionTests.cs (EditMode) already covers. Hand-
        // builds a MatchSaveData matching EXACTLY the pre-this-ticket shape
        // (no version, no networkId, no constructionProgress/training/
        // research/tier fields - every one of them absent, not just
        // zeroed, matching what JsonUtility.FromJson actually produces for
        // a real old save file) and drives it through the real, current
        // ApplySnapshotToRunningMatch to confirm it still restores cleanly.
        [UnityTest]
        public IEnumerator TrulyLegacyShapedSave_RestoresCorrectly_ThroughTheCurrentPipeline()
        {
            LogAssert.ignoreFailingMessages = false;

            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            CivilizationSetup civilizationSetup = Object.FindFirstObjectByType<CivilizationSetup>();
            civilizationSetup.BeginMatch(CivilizationId.Chola);
            yield return null;
            yield return null;

            // Built the way JsonUtility.FromJson<MatchSaveData>() would
            // actually produce it from an old save file's JSON text (which
            // never mentions any of this ticket's new field names at all) -
            // round-tripped through JSON for the same reason, not
            // constructed directly, so this exercises real deserialization
            // defaulting, not just C# field initializers.
            string legacyJson = "{\"mapId\":0,"
                + "\"factions\":[{\"faction\":0,\"civilization\":0,\"currentAge\":0,\"attackTier\":0,\"armorTier\":0,"
                + "\"classAttackTiers\":[],\"classArmorTiers\":[],"
                + "\"resources\":[{\"resourceType\":0,\"amount\":250},{\"resourceType\":1,\"amount\":180},{\"resourceType\":2,\"amount\":90},{\"resourceType\":3,\"amount\":40}]}],"
                + "\"units\":[{\"unitType\":\"Worker\",\"faction\":0,\"position\":{\"x\":-25,\"y\":1,\"z\":-35},\"health\":20,\"stance\":-1}],"
                + "\"buildings\":[{\"buildingType\":\"TownCenter\",\"faction\":0,\"position\":{\"x\":-45,\"y\":1,\"z\":-35},\"health\":500,\"isComplete\":true}],"
                + "\"alliances\":[]}";
            MatchSaveData legacyData = JsonUtility.FromJson<MatchSaveData>(legacyJson);

            // Confirm this really did deserialize to every new field's
            // documented "not present" sentinel, not just assume it -
            // otherwise this wouldn't actually be testing the legacy case.
            Assert.AreEqual(0, legacyData.version);
            Assert.AreEqual(-1, legacyData.units[0].networkId);
            Assert.AreEqual(-1, legacyData.buildings[0].networkId);
            Assert.AreEqual(-1f, legacyData.buildings[0].constructionProgress);
            Assert.IsNull(legacyData.buildings[0].trainingDefinitionId);
            Assert.AreEqual(-1, legacyData.factions[0].infantryTier);

            Assert.DoesNotThrow(() => _applyMethod.Invoke(null, new object[] { legacyData }));
            yield return null;
            yield return null;

            Unit restoredWorkerUnit = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None)
                .FirstOrDefault(u => u.TryGetComponent(out FactionMember fm) && fm.Faction == FactionId.Player
                    && u.TryGetComponent(out Attackable a) && Mathf.Approximately(a.Health, 20f));
            Assert.IsNotNull(restoredWorkerUnit, "The legacy-named Worker should still restore correctly.");

            Building restoredTownCenter = Object.FindObjectsByType<Building>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b is TownCenter && b.TryGetComponent(out FactionMember fm) && fm.Faction == FactionId.Player);
            Assert.IsNotNull(restoredTownCenter, "The legacy-named Town Center should still restore correctly.");
            Assert.IsTrue(restoredTownCenter.GetComponent<ConstructionSite>() == null
                || restoredTownCenter.GetComponent<ConstructionSite>().IsComplete,
                "isComplete=true (the only signal a legacy save carries) should still make it complete.");

            Assert.AreEqual(250f, ResourceStockpile.For(FactionId.Player).GetTotal(ResourceType.Food), 0.01f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            MapRegistry.Select(MapId.RiverValley);
        }
    }
}
