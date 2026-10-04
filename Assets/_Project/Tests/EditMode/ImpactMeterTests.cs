using NUnit.Framework;
using TheDeep.World;

namespace TheDeep.Tests.EditMode
{
    public class ImpactMeterTests
    {
        [Test]
        public void CalculateStrength_GreaterMass_GivesGreaterStrengthAtSameSpeed()
        {
            float light = ImpactMeter.CalculateStrength(mass: 5f, speed: 3f);
            float medium = ImpactMeter.CalculateStrength(mass: 25f, speed: 3f);

            Assert.Greater(medium, light);
        }

        [Test]
        public void CalculateStrength_GreaterSpeed_GivesGreaterStrengthAtSameMass()
        {
            float slow = ImpactMeter.CalculateStrength(mass: 5f, speed: 1f);
            float fast = ImpactMeter.CalculateStrength(mass: 5f, speed: 4f);

            Assert.Greater(fast, slow);
        }

        [TestCase(0f, 3f)]
        [TestCase(5f, 0f)]
        [TestCase(-5f, 3f)]
        [TestCase(5f, -3f)]
        [TestCase(float.NaN, 3f)]
        [TestCase(5f, float.NaN)]
        public void CalculateStrength_ZeroOrInvalidInput_ReturnsZero(float mass, float speed)
        {
            Assert.AreEqual(0f, ImpactMeter.CalculateStrength(mass, speed));
        }
    }
}
