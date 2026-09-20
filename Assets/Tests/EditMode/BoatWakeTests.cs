using NUnit.Framework;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    public class BoatWakeTests
    {
        [Test]
        public void ScaleForLength_ScalesWithHullLength()
        {
            Assert.AreEqual(1f, BoatWake.ScaleForLength(3f), 0.001f);
            Assert.Greater(BoatWake.ScaleForLength(5f), BoatWake.ScaleForLength(3f));
        }

        [Test]
        public void ScaleForLength_IsClamped()
        {
            Assert.AreEqual(0.6f, BoatWake.ScaleForLength(0.1f), 0.001f);
            Assert.AreEqual(2f, BoatWake.ScaleForLength(100f), 0.001f);
        }
    }
}
