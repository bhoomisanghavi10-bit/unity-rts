using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Roadmap Section 1 (resource-specific drop-off buildings, Phase 3.1 of
    // the AoE-parity execution plan): Gatherer.FindNearestDropOff used to
    // hardcode "nearest TownCenter" - AcceptsDropOff is the routing rule
    // that replaces it, letting a worker route to whichever drop-off (a
    // resource-specific camp or the universal TownCenter) actually accepts
    // what it's carrying. Exercised directly against the static rule
    // rather than driving a full Gatherer state machine - see
    // AssemblyInfo.cs's InternalsVisibleTo grant.
    public class GathererDropOffTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
        }

        private T NewBuilding<T>() where T : Building
        {
            var go = new GameObject("Test" + typeof(T).Name);
            T building = go.AddComponent<T>();
            _spawned.Add(go);
            return building;
        }

        [TestCase(ResourceType.Wood)]
        [TestCase(ResourceType.Food)]
        [TestCase(ResourceType.Gold)]
        [TestCase(ResourceType.Stone)]
        public void TownCenter_AcceptsEveryResourceType(ResourceType type)
        {
            TownCenter townCenter = NewBuilding<TownCenter>();

            Assert.IsTrue(Gatherer.AcceptsDropOff(townCenter, type));
        }

        [Test]
        public void LumberCamp_AcceptsWood_RejectsOthers()
        {
            LumberCamp lumberCamp = NewBuilding<LumberCamp>();

            Assert.IsTrue(Gatherer.AcceptsDropOff(lumberCamp, ResourceType.Wood));
            Assert.IsFalse(Gatherer.AcceptsDropOff(lumberCamp, ResourceType.Food));
            Assert.IsFalse(Gatherer.AcceptsDropOff(lumberCamp, ResourceType.Gold));
            Assert.IsFalse(Gatherer.AcceptsDropOff(lumberCamp, ResourceType.Stone));
        }

        [Test]
        public void MiningCamp_AcceptsGoldAndStone_RejectsOthers()
        {
            MiningCamp miningCamp = NewBuilding<MiningCamp>();

            Assert.IsTrue(Gatherer.AcceptsDropOff(miningCamp, ResourceType.Gold));
            Assert.IsTrue(Gatherer.AcceptsDropOff(miningCamp, ResourceType.Stone));
            Assert.IsFalse(Gatherer.AcceptsDropOff(miningCamp, ResourceType.Wood));
            Assert.IsFalse(Gatherer.AcceptsDropOff(miningCamp, ResourceType.Food));
        }

        [Test]
        public void Mill_AcceptsFood_RejectsOthers()
        {
            Mill mill = NewBuilding<Mill>();

            Assert.IsTrue(Gatherer.AcceptsDropOff(mill, ResourceType.Food));
            Assert.IsFalse(Gatherer.AcceptsDropOff(mill, ResourceType.Wood));
            Assert.IsFalse(Gatherer.AcceptsDropOff(mill, ResourceType.Gold));
            Assert.IsFalse(Gatherer.AcceptsDropOff(mill, ResourceType.Stone));
        }

        [Test]
        public void UnrelatedBuilding_AcceptsNoResourceType()
        {
            House house = NewBuilding<House>();

            Assert.IsFalse(Gatherer.AcceptsDropOff(house, ResourceType.Wood));
            Assert.IsFalse(Gatherer.AcceptsDropOff(house, ResourceType.Food));
            Assert.IsFalse(Gatherer.AcceptsDropOff(house, ResourceType.Gold));
            Assert.IsFalse(Gatherer.AcceptsDropOff(house, ResourceType.Stone));
        }
    }
}
