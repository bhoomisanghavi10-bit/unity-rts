using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Match;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Wave 6 item 39 (Cheat codes): covers CheatCodes.Execute's real side
    // effects against ResourceStockpile/AgeProgress/FogOfWarManager/
    // MatchManager directly, same convention every other cheat/debug-style
    // hook in this project uses (e.g. KarmashalaTests' own CreateStockpile).
    // Spawn is deliberately NOT exercised here - EntitySpawner.SpawnUnit
    // hard-errors outside Play mode for pre-existing reasons unrelated to
    // this item (see the Scenario Editor heavy-path session 1 log entry);
    // it's covered instead by CheatCommandParserTests' own parse-only
    // coverage and needs live Play-mode verification, same as every other
    // EntitySpawner caller in this codebase.
    public class CheatCodesTests
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

            // Both are bare static registries (no per-test reset hook of
            // their own) - reset directly so one test's mutation can't leak
            // into the next, same reasoning UpgradeProgress.ResetForTests()
            // documents for its own tier state.
            AgeProgress.Initialize(FactionId.Player, AgeId.Ancient);
            MatchManager.ForceOutcome(MatchOutcome.Ongoing);
            Time.timeScale = 1f;
            if (FogOfWarManager.ToggleRevealAll())
            {
                FogOfWarManager.ToggleRevealAll();
            }
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private ResourceStockpile CreatePlayerStockpile()
        {
            return CreateGameObject("Stockpile").AddComponent<ResourceStockpile>();
        }

        [Test]
        public void Execute_ResourcesCommand_GrantsAllFourTypes()
        {
            ResourceStockpile stockpile = CreatePlayerStockpile();
            CheatCommand command = CheatCommandParser.Parse("resources 500");

            CheatCodes.Execute(command);

            Assert.AreEqual(500f, stockpile.GetTotal(ResourceType.Wood));
            Assert.AreEqual(500f, stockpile.GetTotal(ResourceType.Food));
            Assert.AreEqual(500f, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(500f, stockpile.GetTotal(ResourceType.Stone));
        }

        [Test]
        public void Execute_SingleResourceCommand_GrantsOnlyThatType()
        {
            ResourceStockpile stockpile = CreatePlayerStockpile();
            CheatCommand command = CheatCommandParser.Parse("wood 250");

            CheatCodes.Execute(command);

            Assert.AreEqual(250f, stockpile.GetTotal(ResourceType.Wood));
            Assert.AreEqual(0f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void Execute_ResourcesCommand_IsAdditiveAcrossCalls()
        {
            ResourceStockpile stockpile = CreatePlayerStockpile();
            CheatCodes.Execute(CheatCommandParser.Parse("gold 100"));
            CheatCodes.Execute(CheatCommandParser.Parse("gold 100"));

            Assert.AreEqual(200f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void Execute_ResourcesCommand_WithNoStockpileInScene_ReturnsMessageInsteadOfThrowing()
        {
            CheatCommand command = CheatCommandParser.Parse("wood 100");
            Assert.DoesNotThrow(() => CheatCodes.Execute(command));
        }

        [Test]
        public void Execute_AgeCommand_AdvancesPlayerAgeDirectly()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Ancient);
            CheatCodes.Execute(CheatCommandParser.Parse("age imperial"));

            Assert.AreEqual(AgeId.Imperial, AgeProgress.CurrentAge(FactionId.Player));
        }

        [Test]
        public void Execute_RevealCommand_TogglesFogOfWarStateEachCall()
        {
            string first = CheatCodes.Execute(CheatCommandParser.Parse("reveal"));
            Assert.AreEqual("Map revealed.", first);

            string second = CheatCodes.Execute(CheatCommandParser.Parse("reveal"));
            Assert.AreEqual("Fog of war restored.", second);
        }

        [Test]
        public void Execute_WinCommand_ForcesVictory()
        {
            CheatCodes.Execute(CheatCommandParser.Parse("win"));
            Assert.AreEqual(MatchOutcome.Victory, MatchManager.Outcome);
        }

        [Test]
        public void Execute_LoseCommand_ForcesDefeat()
        {
            CheatCodes.Execute(CheatCommandParser.Parse("lose"));
            Assert.AreEqual(MatchOutcome.Defeat, MatchManager.Outcome);
        }

        [Test]
        public void Execute_InvalidCommand_ReturnsErrorWithoutMutatingState()
        {
            ResourceStockpile stockpile = CreatePlayerStockpile();
            string result = CheatCodes.Execute(CheatCommandParser.Parse("frobnicate"));

            Assert.IsNotNull(result);
            Assert.AreEqual(0f, stockpile.GetTotal(ResourceType.Wood));
            Assert.AreEqual(MatchOutcome.Ongoing, MatchManager.Outcome);
        }
    }
}
