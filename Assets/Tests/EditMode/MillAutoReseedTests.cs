using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // Farm mechanics correction against the pasted AoE II reference:
    // MillAutoReseedRegistry + Mill.RequestToggleAutoReseed - the
    // per-faction toggle simplification of "Farms can be automatically
    // reseeded if they are queued in the Mill (or equivalent)". Farm.Tick's
    // own consumption of this registry is covered by
    // FarmTests.Tick_MillAutoReseed_RestoresFoodWithoutAnyAssignedReseeder;
    // this file covers the registry and Mill's own request method directly.
    public class MillAutoReseedTests
    {
        private readonly System.Collections.Generic.List<GameObject> _spawned =
            new System.Collections.Generic.List<GameObject>();

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
            MillAutoReseedRegistry.Reset();
        }

        private Mill CreateMill(FactionId faction, bool complete = true)
        {
            var go = new GameObject("Mill");
            _spawned.Add(go);
            go.AddComponent<FactionMember>().Configure(faction);
            Mill mill = go.AddComponent<Mill>();
            if (!complete)
            {
                go.AddComponent<ConstructionSite>().Configure(10f);
            }
            return mill;
        }

        [Test]
        public void IsEnabled_DefaultsToFalse()
        {
            Assert.IsFalse(MillAutoReseedRegistry.IsEnabled(FactionId.Player));
        }

        [Test]
        public void Toggle_FlipsState_EachCall()
        {
            MillAutoReseedRegistry.Toggle(FactionId.Player);
            Assert.IsTrue(MillAutoReseedRegistry.IsEnabled(FactionId.Player));

            MillAutoReseedRegistry.Toggle(FactionId.Player);
            Assert.IsFalse(MillAutoReseedRegistry.IsEnabled(FactionId.Player));
        }

        [Test]
        public void Toggle_PerFaction_Independent()
        {
            MillAutoReseedRegistry.Toggle(FactionId.Player);
            Assert.IsTrue(MillAutoReseedRegistry.IsEnabled(FactionId.Player));
            Assert.IsFalse(MillAutoReseedRegistry.IsEnabled(FactionId.Enemy));
        }

        [Test]
        public void Mill_RequestToggleAutoReseed_TogglesTheFactionRegistry()
        {
            Mill mill = CreateMill(FactionId.Player);

            mill.RequestToggleAutoReseed();

            Assert.IsTrue(mill.AutoReseedEnabled);
            Assert.IsTrue(MillAutoReseedRegistry.IsEnabled(FactionId.Player));
        }

        [Test]
        public void Mill_RequestToggleAutoReseed_NoOp_WhileUnderConstruction()
        {
            Mill mill = CreateMill(FactionId.Player, complete: false);

            mill.RequestToggleAutoReseed();

            Assert.IsFalse(mill.AutoReseedEnabled);
        }
    }
}
