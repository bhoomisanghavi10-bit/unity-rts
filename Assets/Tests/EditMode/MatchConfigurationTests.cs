using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // Prompt 11: the authoritative match configuration - deterministic
    // substreams, hashing, slot/civ resolution, lifecycle, rules that other
    // systems read from it, and the human/AI-neutral starting-force plan.
    public class MatchConfigurationTests
    {
        [TearDown]
        public void TearDown()
        {
            MatchConfiguration.End();
        }

        private static MatchConfiguration Make(int seed = 7, CivilizationId player = CivilizationId.Chola, CivilizationId ai = CivilizationId.Maratha, MapId map = MapId.RiverValley)
        {
            return MatchConfiguration.Create(map, player, ai, seed: seed);
        }

        [Test]
        public void Stream_SameSeedAndName_ReproducesSequence()
        {
            var a = Make(1234).Stream("resources");
            var b = Make(1234).Stream("resources");
            for (int i = 0; i < 50; i++)
            {
                Assert.AreEqual(a.Range(0f, 100f), b.Range(0f, 100f));
            }
        }

        [Test]
        public void Stream_DifferentSeedOrName_Varies()
        {
            Assert.AreNotEqual(Make(1).SeedFor("resources"), Make(2).SeedFor("resources"));
            Assert.AreNotEqual(Make(1).SeedFor("resources"), Make(1).SeedFor("wildlife"));
            Assert.GreaterOrEqual(Make(1).SeedFor("resources"), 0);
        }

        [Test]
        public void Streams_AreIndependent_DrawingOneNeverShiftsAnother()
        {
            MatchConfiguration c = Make(99);
            var resources = c.Stream("resources");
            for (int i = 0; i < 1000; i++) resources.NextFloat01();

            float afterHeavyUse = c.Stream("wildlife").NextFloat01();
            float fresh = Make(99).Stream("wildlife").NextFloat01();

            Assert.AreEqual(fresh, afterHeavyUse);
        }

        [Test]
        public void Hash_IdenticalConfigsMatch_AnyDifferenceChangesIt()
        {
            int baseline = Make().ComputeHash();
            Assert.AreEqual(baseline, Make().ComputeHash());
            Assert.AreNotEqual(baseline, Make(seed: 8).ComputeHash());
            Assert.AreNotEqual(baseline, Make(player: CivilizationId.Rajput).ComputeHash());
            Assert.AreNotEqual(baseline, Make(map: MapId.Highlands).ComputeHash());
            MatchConfiguration tweaked = Make();
            tweaked.PopulationBase = 30;
            Assert.AreNotEqual(baseline, tweaked.ComputeHash());
        }

        [Test]
        public void Create_DefaultSlots_PlayerHuman_EnemyAi_Enemy2Closed()
        {
            MatchConfiguration c = Make();

            Assert.AreEqual(SlotType.Human, c.SlotFor(FactionId.Player).Type);
            Assert.AreEqual(SlotType.AI, c.SlotFor(FactionId.Enemy).Type);
            Assert.AreEqual(SlotType.Closed, c.SlotFor(FactionId.Enemy2).Type);
            Assert.IsTrue(c.IsAi(FactionId.Enemy));
            Assert.IsFalse(c.IsPlayable(FactionId.Enemy2));
            Assert.AreEqual(FactionId.Player, c.LocalFaction);
            Assert.AreEqual(MatchConfiguration.CurrentSchemaVersion, c.SchemaVersion);
        }

        [Test]
        public void Create_ThirdFaction_OpensEnemy2AsAi_AndCivsAreDistinct()
        {
            MatchConfiguration c = MatchConfiguration.Create(MapId.RiverValley, CivilizationId.Vijayanagara, CivilizationId.Vijayanagara,
                thirdFaction: true, enemy2Civilization: CivilizationId.Vijayanagara, seed: 5);

            Assert.IsTrue(c.IsAi(FactionId.Enemy2));
            var civs = new System.Collections.Generic.HashSet<CivilizationId>
            {
                c.SlotFor(FactionId.Player).Civilization, c.SlotFor(FactionId.Enemy).Civilization, c.SlotFor(FactionId.Enemy2).Civilization,
            };
            Assert.AreEqual(3, civs.Count, "The player's pick stays; the other slots resolve to distinct civs.");
            Assert.AreEqual(CivilizationId.Vijayanagara, c.SlotFor(FactionId.Player).Civilization);
        }

        [Test]
        public void Create_HumanSecondSlot_IsHumanNotAi()
        {
            MatchConfiguration c = MatchConfiguration.Create(MapId.RiverValley, CivilizationId.Chola, CivilizationId.Maratha, seed: 1, secondSlotIsHuman: true);

            Assert.AreEqual(SlotType.Human, c.SlotFor(FactionId.Enemy).Type);
            Assert.IsFalse(c.IsAi(FactionId.Enemy));
        }

        [Test]
        public void Create_WithoutSeed_PicksFreshNonNegativeSeedEachTime()
        {
            MatchConfiguration a = MatchConfiguration.Create(MapId.RiverValley, CivilizationId.Chola, CivilizationId.Maratha);
            MatchConfiguration b = MatchConfiguration.Create(MapId.RiverValley, CivilizationId.Chola, CivilizationId.Maratha);

            Assert.GreaterOrEqual(a.Seed, 0);
            Assert.AreNotEqual(a.Seed, b.Seed, "Each unseeded match gets its own seed.");
        }

        [Test]
        public void BeginEnd_ReplacesConfigurationNeverMerges()
        {
            MatchConfiguration first = Make(1);
            MatchConfiguration second = Make(2, player: CivilizationId.Rajput);
            MatchConfiguration.Begin(first);
            MatchConfiguration.Begin(second);

            Assert.AreSame(second, MatchConfiguration.Current);
            Assert.AreEqual(2, MatchConfiguration.Current.Seed);
            MatchConfiguration.End();
            Assert.IsNull(MatchConfiguration.Current);
        }

        [Test]
        public void PopulationRules_ComeFromTheActiveConfiguration()
        {
            Assert.AreEqual(10, Population.Cap(FactionId.Enemy2));

            MatchConfiguration c = Make();
            c.PopulationBase = 25;
            MatchConfiguration.Begin(c);

            Assert.AreEqual(25, Population.Cap(FactionId.Enemy2));
            MatchConfiguration.End();
            Assert.AreEqual(10, Population.Cap(FactionId.Enemy2), "Nothing may leak after the match ends.");
        }

        [Test]
        public void StartingForcePlan_IsIdenticalRelativeToEveryMapStart()
        {
            MapDefinitionData map = MapRegistry.Get(MapId.SkirmishMedium);
            StartingForces.Plan player = StartingForces.PlanFor(StartingForces.StartFor(map, FactionId.Player));
            StartingForces.Plan enemy = StartingForces.PlanFor(StartingForces.StartFor(map, FactionId.Enemy));

            Assert.AreEqual(StartingForces.WorkerCount, player.Workers.Count);
            Assert.AreEqual(player.Workers.Count, enemy.Workers.Count);
            for (int i = 0; i < player.Workers.Count; i++)
            {
                Assert.AreEqual(player.Workers[i] - player.TownCenter, enemy.Workers[i] - enemy.TownCenter,
                    "Human and AI Workers sit at the same offsets around their own Town Center.");
            }
            Assert.AreEqual(map.PlayerTownCenter, player.TownCenter);
            Assert.AreEqual(map.EnemyTownCenter, enemy.TownCenter);
            Assert.AreEqual(map.Enemy2TownCenter, StartingForces.StartFor(map, FactionId.Enemy2));
        }

        [Test]
        public void StartingForcePlan_WorkersStartNextToTheirTownCenter_NotAtWorldOrigin()
        {
            MapDefinitionData map = MapRegistry.Get(MapId.SkirmishMedium);
            StartingForces.Plan plan = StartingForces.PlanFor(map.PlayerTownCenter);

            // Threshold was 6f, tuned when WorkerBehindTownCenter was -3
            // (flush against the TownCenter's own footprint edge). Bumped
            // to clear StartingForces.WorkerBehindTownCenter's own -6.5
            // fix (see that constant's comment - clears the low-poly
            // TownCenter models' real visual footprint, which the old -3
            // put workers underneath) while still asserting "near the
            // Town Center," not literally at the world origin.
            foreach (Vector3 w in plan.Workers)
            {
                Assert.Less(Vector3.Distance(w, plan.TownCenter), 8f);
            }
        }
    }
}
