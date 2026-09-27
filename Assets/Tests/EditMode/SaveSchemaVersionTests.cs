using System;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // Requirement 5/6 of the extended catalog save schema: unsupported
    // versions must produce a clear error, never be silently accepted or
    // silently downgraded - and backward compatibility (a save with no
    // version field at all) is only claimed where it's actually
    // implemented and tested, not just assumed. See SaveManager.
    // ValidateSaveVersion/CurrentSaveVersion and every new field's own
    // "-1/null = not present" sentinel default in SaveData.cs.
    public class SaveSchemaVersionTests
    {
        [Test]
        public void NewerVersionThanSupported_ThrowsClearError()
        {
            var data = new MatchSaveData { version = SaveManager.CurrentSaveVersion + 1 };

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => SaveManager.ValidateSaveVersion(data));
            StringAssert.Contains(data.version.ToString(), ex.Message);
            StringAssert.Contains(SaveManager.CurrentSaveVersion.ToString(), ex.Message);
        }

        [Test]
        public void CurrentVersion_DoesNotThrow()
        {
            var data = new MatchSaveData { version = SaveManager.CurrentSaveVersion };
            Assert.DoesNotThrow(() => SaveManager.ValidateSaveVersion(data));
        }

        [Test]
        public void NegativeVersion_ThrowsClearError()
        {
            var data = new MatchSaveData { version = -1 };
            StringAssert.Contains("unsupported", Assert.Throws<InvalidOperationException>(
                () => SaveManager.ValidateSaveVersion(data)).Message);
        }

        [Test]
        public void MissingVersionField_TreatedAsLegacy_DoesNotThrow()
        {
            // JsonUtility leaves an absent int field at its C# default (0)
            // rather than throwing - this is exactly what deserializing a
            // save written before the version field existed produces.
            var data = JsonUtility.FromJson<MatchSaveData>("{\"mapId\":0}");
            Assert.AreEqual(0, data.version);
            Assert.DoesNotThrow(() => SaveManager.ValidateSaveVersion(data));
        }

        [Test]
        public void Capture_StampsTheCurrentSchemaVersion()
        {
            // SaveManager.Capture is internal, but this EditMode test file
            // is in the same assembly as Runtime (see AssemblyInfo.cs'
            // InternalsVisibleTo grant) and can call it directly - no
            // reflection needed here, unlike CatalogSaveRoundTripTests.cs
            // in the separate PlayMode assembly.
            MatchSaveData data = SaveManager.Capture();
            Assert.AreEqual(SaveManager.CurrentSaveVersion, data.version);
        }

        [Test]
        public void LegacySentinelDefaults_MatchWhatAnUnversionedSaveActuallyDeserializesTo()
        {
            // Every new field this ticket added must default to its own
            // documented "not present" sentinel WITHOUT the JSON round
            // trip explicitly setting it - otherwise a real legacy save
            // (which never mentions these keys at all) wouldn't actually
            // hit the sentinel-guarded skip paths in SaveManager's restore
            // phases the way SaveData.cs's own comments claim.
            var faction = JsonUtility.FromJson<FactionSaveData>("{}");
            Assert.AreEqual(-1, faction.infantryTier);
            Assert.AreEqual(-1, faction.archerTier);

            var unit = JsonUtility.FromJson<UnitSaveData>("{}");
            Assert.AreEqual(-1, unit.networkId);

            var building = JsonUtility.FromJson<BuildingSaveData>("{}");
            Assert.AreEqual(-1, building.networkId);
            Assert.AreEqual(-1f, building.constructionProgress);
            Assert.IsNull(building.trainingDefinitionId);
            Assert.AreEqual(-1f, building.trainingRemaining);
            Assert.AreEqual(-1f, building.infantryTierResearchRemaining);
        }
    }
}
