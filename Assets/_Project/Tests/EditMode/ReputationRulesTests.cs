using NUnit.Framework;
using TheDeep.Factions;

namespace TheDeep.Tests.EditMode
{
    public class ReputationRulesTests
    {
        [TestCase(-100, ReputationBand.Hostile)]
        [TestCase(-40, ReputationBand.Hostile)]
        [TestCase(-39, ReputationBand.Unfriendly)]
        [TestCase(-11, ReputationBand.Unfriendly)]
        [TestCase(-10, ReputationBand.Neutral)]
        [TestCase(0, ReputationBand.Neutral)]
        [TestCase(19, ReputationBand.Neutral)]
        [TestCase(20, ReputationBand.Friendly)]
        [TestCase(100, ReputationBand.Friendly)]
        public void BandFor_UsesTheDocumentedThresholds(int value, ReputationBand expected)
        {
            Assert.AreEqual(expected, ReputationRules.BandFor(value));
        }

        [Test]
        public void Clamp_KeepsValuesWithinMinusHundredToHundred()
        {
            Assert.AreEqual(100, ReputationRules.Clamp(250));
            Assert.AreEqual(-100, ReputationRules.Clamp(-250));
            Assert.AreEqual(15, ReputationRules.Clamp(15));
        }
    }
}
