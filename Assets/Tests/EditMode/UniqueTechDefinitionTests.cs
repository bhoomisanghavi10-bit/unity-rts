using System;
using NUnit.Framework;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // Regression coverage for the KeyNotFoundException bug flagged from a
    // real live match: UniqueTechDefinition.For(CivilizationId) used to do
    // an unguarded Bonuses[id] lookup with entries only for Chola/
    // Vijayanagara/Rajput, throwing for Maurya/Maratha - which crashed
    // Barracks.UniqueTech every frame via BuildMenu.UpdateUniqueTechButton
    // whenever a Maurya or Maratha Barracks was selected. Asserts every
    // CivilizationId value resolves without throwing, not just the 3 that
    // happened to work before.
    public class UniqueTechDefinitionTests
    {
        [Test]
        public void For_DoesNotThrow_ForEveryCivilizationId()
        {
            foreach (CivilizationId id in Enum.GetValues(typeof(CivilizationId)))
            {
                Assert.DoesNotThrow(() => UniqueTechDefinition.For(id), $"UniqueTechDefinition.For({id}) threw.");
            }
        }

        [Test]
        public void For_Maurya_HasAgeUpResearchTimeDiscount()
        {
            UniqueTechDefinition tech = UniqueTechDefinition.For(CivilizationId.Maurya);
            Assert.Less(tech.AgeUpResearchTimeMultiplier, 1f);
        }

        [Test]
        public void For_Maratha_HasCavalryDamageTakenDiscount()
        {
            UniqueTechDefinition tech = UniqueTechDefinition.For(CivilizationId.Maratha);
            Assert.Less(tech.CavalryDamageTakenMultiplier, 1f);
        }

        [Test]
        public void For_NonOwningCivs_HaveNeutralMauryaMarathaBonuses()
        {
            UniqueTechDefinition chola = UniqueTechDefinition.For(CivilizationId.Chola);
            Assert.AreEqual(1f, chola.AgeUpResearchTimeMultiplier);
            Assert.AreEqual(1f, chola.CavalryDamageTakenMultiplier);
        }
    }
}
