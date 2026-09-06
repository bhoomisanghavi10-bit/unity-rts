using System.Collections.Generic;
using NUnit.Framework;
using KingdomsOfBharat.UI;

namespace KingdomsOfBharat.Tests
{
    // Roadmap item 31: pure pagination math behind BuildMenu's command-panel
    // icon grid (BuildMenu.ComputeGridPage) - deliberately independent of
    // any MonoBehaviour/scene state so it's testable without a live Button
    // list, matching this project's own "internal static, unit-tested
    // directly" precedent (e.g. CommandBus.ExecuteTick).
    public class CommandGridLayoutTests
    {
        [Test]
        public void AllFit_OnePage_EverythingVisible()
        {
            bool[] flags = { true, false, true, true, false, true };

            (List<int> visible, int pageCount, int clampedPage) = BuildMenu.ComputeGridPage(flags, capacity: 4, requestedPage: 0);

            Assert.AreEqual(1, pageCount);
            Assert.AreEqual(0, clampedPage);
            CollectionAssert.AreEqual(new[] { 0, 2, 3, 5 }, visible);
        }

        [Test]
        public void MoreThanCapacity_SplitsIntoPages()
        {
            bool[] flags = new bool[10];
            for (int i = 0; i < flags.Length; i++)
            {
                flags[i] = true;
            }

            (List<int> page0, int pageCount0, int clamped0) = BuildMenu.ComputeGridPage(flags, capacity: 4, requestedPage: 0);
            (List<int> page1, int pageCount1, int clamped1) = BuildMenu.ComputeGridPage(flags, capacity: 4, requestedPage: 1);
            (List<int> page2, int pageCount2, int clamped2) = BuildMenu.ComputeGridPage(flags, capacity: 4, requestedPage: 2);

            Assert.AreEqual(3, pageCount0);
            Assert.AreEqual(3, pageCount1);
            Assert.AreEqual(3, pageCount2);
            Assert.AreEqual(0, clamped0);
            Assert.AreEqual(1, clamped1);
            Assert.AreEqual(2, clamped2);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, page0);
            CollectionAssert.AreEqual(new[] { 4, 5, 6, 7 }, page1);
            CollectionAssert.AreEqual(new[] { 8, 9 }, page2);
        }

        [Test]
        public void RequestedPageBeyondCount_ClampsToLastPage()
        {
            bool[] flags = { true, true, true, true, true };

            (List<int> visible, int pageCount, int clampedPage) = BuildMenu.ComputeGridPage(flags, capacity: 2, requestedPage: 99);

            Assert.AreEqual(3, pageCount);
            Assert.AreEqual(2, clampedPage);
            CollectionAssert.AreEqual(new[] { 4 }, visible);
        }

        [Test]
        public void NegativeRequestedPage_ClampsToZero()
        {
            bool[] flags = { true, true, true };

            (List<int> visible, int pageCount, int clampedPage) = BuildMenu.ComputeGridPage(flags, capacity: 2, requestedPage: -5);

            Assert.AreEqual(0, clampedPage);
            CollectionAssert.AreEqual(new[] { 0, 1 }, visible);
        }

        [Test]
        public void NoActiveButtons_OnePageEmpty()
        {
            bool[] flags = { false, false, false };

            (List<int> visible, int pageCount, int clampedPage) = BuildMenu.ComputeGridPage(flags, capacity: 4, requestedPage: 0);

            Assert.AreEqual(1, pageCount);
            Assert.AreEqual(0, clampedPage);
            Assert.AreEqual(0, visible.Count);
        }

        [Test]
        public void ExactMultipleOfCapacity_NoTrailingEmptyPage()
        {
            bool[] flags = new bool[8];
            for (int i = 0; i < flags.Length; i++)
            {
                flags[i] = true;
            }

            (_, int pageCount, _) = BuildMenu.ComputeGridPage(flags, capacity: 4, requestedPage: 0);

            Assert.AreEqual(2, pageCount);
        }
    }
}
