using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Item 4 (Diplomacy, docs/PARTIAL_ELEMENTS_FIX_PLAN.md). Exercises
    // Tribute.Send directly against real ResourceStockpile instances -
    // ResourceStockpile.Configure exists specifically so tests can create
    // one programmatically (see its own doc comment).
    public class TributeTests
    {
        private GameObject _playerGo;
        private GameObject _enemyGo;
        private ResourceStockpile _player;
        private ResourceStockpile _enemy;

        [SetUp]
        public void SetUp()
        {
            _playerGo = new GameObject("PlayerStockpile");
            _player = _playerGo.AddComponent<ResourceStockpile>();
            _player.Configure(FactionId.Player);

            _enemyGo = new GameObject("EnemyStockpile");
            _enemy = _enemyGo.AddComponent<ResourceStockpile>();
            _enemy.Configure(FactionId.Enemy);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_playerGo);
            Object.DestroyImmediate(_enemyGo);
            DiplomacyRegistry.Reset();
        }

        [Test]
        public void Send_DeductsFullAmountFromSender_CreditsTaxedAmountToReceiver()
        {
            _player.Add(ResourceType.Wood, 200f);

            bool result = Tribute.Send(FactionId.Player, FactionId.Enemy, ResourceType.Wood, 100f);

            Assert.IsTrue(result);
            Assert.AreEqual(100f, _player.GetTotal(ResourceType.Wood), 0.01f,
                "The sender must be charged the full requested amount, not the taxed amount.");
            Assert.AreEqual(80f, _enemy.GetTotal(ResourceType.Wood), 0.01f,
                "The receiver gets the amount minus Tribute.TaxRate (20%): 100 * 0.8 = 80.");
        }

        [Test]
        public void Send_InsufficientFunds_IsRejected_WithNoChangeToEitherSide()
        {
            _player.Add(ResourceType.Wood, 50f);

            bool result = Tribute.Send(FactionId.Player, FactionId.Enemy, ResourceType.Wood, 100f);

            Assert.IsFalse(result);
            Assert.AreEqual(50f, _player.GetTotal(ResourceType.Wood), 0.01f);
            Assert.AreEqual(0f, _enemy.GetTotal(ResourceType.Wood), 0.01f);
        }

        [Test]
        public void Send_ToSelf_IsRejected()
        {
            _player.Add(ResourceType.Wood, 200f);

            bool result = Tribute.Send(FactionId.Player, FactionId.Player, ResourceType.Wood, 100f);

            Assert.IsFalse(result);
            Assert.AreEqual(200f, _player.GetTotal(ResourceType.Wood), 0.01f,
                "A self-tribute must be a complete no-op, not even a wash after tax.");
        }

        [Test]
        public void Send_NonPositiveAmount_IsRejected()
        {
            _player.Add(ResourceType.Wood, 200f);

            Assert.IsFalse(Tribute.Send(FactionId.Player, FactionId.Enemy, ResourceType.Wood, 0f));
            Assert.IsFalse(Tribute.Send(FactionId.Player, FactionId.Enemy, ResourceType.Wood, -10f));
            Assert.AreEqual(200f, _player.GetTotal(ResourceType.Wood), 0.01f);
        }

        [Test]
        public void Send_SucceedsWhileAtWar_NotGatedByDiplomacyStance()
        {
            // User-confirmed design decision (see docs/SESSION_LOG.md 2026-09-03,
            // Item 4): Tribute matches real AoE II's rule and works
            // regardless of alliance state. Deliberately NOT calling
            // DiplomacyRegistry.SetAllied here - default state is War.
            Assert.IsFalse(DiplomacyRegistry.AreAllied(FactionId.Player, FactionId.Enemy),
                "Precondition: Player and Enemy must be at War (the default, unset state) for this test to prove anything.");
            _player.Add(ResourceType.Wood, 200f);

            bool result = Tribute.Send(FactionId.Player, FactionId.Enemy, ResourceType.Wood, 100f);

            Assert.IsTrue(result, "Tribute must succeed even while at war - no DiplomacyRegistry gate exists.");
        }
    }
}
