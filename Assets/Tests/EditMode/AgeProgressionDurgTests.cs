using NUnit.Framework;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Tests
{
    // Wave 1 item 5 (docs/IMPLEMENTATION_ROADMAP.md): AgeId.Durg now sits
    // between Classical and Imperial. These tests confirm the new age
    // slots correctly into both the enum-ordering-dependent AgeProgress
    // transitions and the CSV-driven AgeProfile lookup, rather than only
    // trusting the enum compiles.
    public class AgeProgressionDurgTests
    {
        [TearDown]
        public void TearDown()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Ancient);
        }

        [Test]
        public void NextAge_FromClassical_IsDurg()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);

            Assert.AreEqual(AgeId.Durg, AgeProgress.NextAge(FactionId.Player));
        }

        [Test]
        public void NextAge_FromDurg_IsImperial()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);

            Assert.AreEqual(AgeId.Imperial, AgeProgress.NextAge(FactionId.Player));
        }

        [Test]
        public void HasNextAge_FromDurg_IsTrue()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);

            Assert.IsTrue(AgeProgress.HasNextAge(FactionId.Player));
        }

        [Test]
        public void Advance_ToDurg_IsReflectedByCurrentAge()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);

            AgeProgress.Advance(FactionId.Player, AgeId.Durg);

            Assert.AreEqual(AgeId.Durg, AgeProgress.CurrentAge(FactionId.Player));
        }

        [Test]
        public void DurgProfile_IsBetweenClassicalAndImperial()
        {
            AgeProfile classical = AgeProfile.For(AgeId.Classical);
            AgeProfile durg = AgeProfile.For(AgeId.Durg);
            AgeProfile imperial = AgeProfile.For(AgeId.Imperial);

            Assert.Greater(durg.WoodCost, classical.WoodCost);
            Assert.Less(durg.WoodCost, imperial.WoodCost);
            Assert.Greater(durg.StoneCost, classical.StoneCost);
            Assert.Less(durg.StoneCost, imperial.StoneCost);
            Assert.Greater(durg.GatherRateMultiplier, classical.GatherRateMultiplier);
            Assert.Less(durg.GatherRateMultiplier, imperial.GatherRateMultiplier);
            Assert.Greater(durg.MaxHealthMultiplier, classical.MaxHealthMultiplier);
            Assert.Less(durg.MaxHealthMultiplier, imperial.MaxHealthMultiplier);
            Assert.Less(durg.TrainTimeMultiplier, classical.TrainTimeMultiplier);
            Assert.Greater(durg.TrainTimeMultiplier, imperial.TrainTimeMultiplier);
        }

        [Test]
        public void DurgProfile_HasNonEmptyDisplayName()
        {
            AgeProfile durg = AgeProfile.For(AgeId.Durg);

            Assert.AreEqual("Durg Age", durg.DisplayName);
        }
    }
}
