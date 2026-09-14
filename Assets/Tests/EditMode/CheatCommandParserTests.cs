using NUnit.Framework;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Wave 6 item 39 (Cheat codes): pure parser coverage, no scene/GameObject
    // dependency at all - the whole point of splitting CheatCommandParser out
    // from CheatCodes.cs's side-effecting execution.
    public class CheatCommandParserTests
    {
        [Test]
        public void Parse_EmptyInput_Fails()
        {
            Assert.IsFalse(CheatCommandParser.Parse("").IsValid);
            Assert.IsFalse(CheatCommandParser.Parse("   ").IsValid);
        }

        [Test]
        public void Parse_UnknownVerb_Fails()
        {
            CheatCommand command = CheatCommandParser.Parse("frobnicate");
            Assert.IsFalse(command.IsValid);
        }

        [Test]
        public void Parse_Help_ReturnsHelpKind()
        {
            CheatCommand command = CheatCommandParser.Parse("help");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual(CheatCommandKind.Help, command.Kind);
        }

        [Test]
        public void Parse_ResourcesWithAmount_GrantsAll()
        {
            CheatCommand command = CheatCommandParser.Parse("resources 500");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual(CheatCommandKind.Resources, command.Kind);
            Assert.IsTrue(command.GrantAllResources);
            Assert.AreEqual(500f, command.Amount);
        }

        [Test]
        public void Parse_ResourcesWithNoAmount_DefaultsToOneThousand()
        {
            CheatCommand command = CheatCommandParser.Parse("resources");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual(1000f, command.Amount);
        }

        [Test]
        public void Parse_SingleResourceKeyword_GrantsOnlyThatType()
        {
            CheatCommand command = CheatCommandParser.Parse("wood 250");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual(CheatCommandKind.Resources, command.Kind);
            Assert.IsFalse(command.GrantAllResources);
            Assert.AreEqual(ResourceType.Wood, command.Resource);
            Assert.AreEqual(250f, command.Amount);
        }

        [Test]
        public void Parse_ResourceWithNonNumericAmount_Fails()
        {
            CheatCommand command = CheatCommandParser.Parse("gold abc");
            Assert.IsFalse(command.IsValid);
        }

        [Test]
        public void Parse_AgeWithValidName_IsCaseInsensitive()
        {
            CheatCommand command = CheatCommandParser.Parse("age IMPERIAL");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual(CheatCommandKind.Age, command.Kind);
            Assert.AreEqual(AgeId.Imperial, command.Age);
        }

        [Test]
        public void Parse_AgeWithNoArgument_Fails()
        {
            Assert.IsFalse(CheatCommandParser.Parse("age").IsValid);
        }

        [Test]
        public void Parse_AgeWithUnknownName_Fails()
        {
            Assert.IsFalse(CheatCommandParser.Parse("age medieval").IsValid);
        }

        [Test]
        public void Parse_Reveal_ReturnsRevealKind()
        {
            Assert.AreEqual(CheatCommandKind.Reveal, CheatCommandParser.Parse("reveal").Kind);
        }

        [Test]
        public void Parse_SpawnWithValidUnitAndCount_Succeeds()
        {
            CheatCommand command = CheatCommandParser.Parse("spawn soldier 3");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual(CheatCommandKind.Spawn, command.Kind);
            Assert.AreEqual("Soldier", command.UnitType);
            Assert.AreEqual(3, command.Count);
        }

        [Test]
        public void Parse_SpawnWithNoCount_DefaultsToOne()
        {
            CheatCommand command = CheatCommandParser.Parse("spawn worker");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual(1, command.Count);
        }

        [Test]
        public void Parse_SpawnCount_ClampedToTwenty()
        {
            CheatCommand command = CheatCommandParser.Parse("spawn worker 999");
            Assert.IsTrue(command.IsValid);
            Assert.AreEqual(20, command.Count);
        }

        [Test]
        public void Parse_SpawnWithUnknownUnit_Fails()
        {
            Assert.IsFalse(CheatCommandParser.Parse("spawn dragon").IsValid);
        }

        [Test]
        public void Parse_SpawnWithNoUnitType_Fails()
        {
            Assert.IsFalse(CheatCommandParser.Parse("spawn").IsValid);
        }

        [Test]
        public void Parse_WinAndLose_ReturnExpectedKinds()
        {
            Assert.AreEqual(CheatCommandKind.Win, CheatCommandParser.Parse("win").Kind);
            Assert.AreEqual(CheatCommandKind.Lose, CheatCommandParser.Parse("lose").Kind);
        }

        [Test]
        public void Parse_IsCaseInsensitiveOnVerb()
        {
            Assert.AreEqual(CheatCommandKind.Win, CheatCommandParser.Parse("WIN").Kind);
        }
    }
}
