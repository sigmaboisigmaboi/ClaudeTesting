using NUnit.Framework;
using TheDeep.World;

namespace TheDeep.Tests.EditMode
{
    public class ImpactMathTests
    {
        [Test]
        public void Strength_GreaterMass_GivesGreaterStrengthAtSameSpeed()
        {
            float light = ImpactMath.Strength(mass: 5f, speed: 3f);
            float medium = ImpactMath.Strength(mass: 25f, speed: 3f);

            Assert.Greater(medium, light);
        }

        [Test]
        public void Strength_GreaterSpeed_GivesGreaterStrengthAtSameMass()
        {
            float slow = ImpactMath.Strength(mass: 5f, speed: 1f);
            float fast = ImpactMath.Strength(mass: 5f, speed: 4f);

            Assert.Greater(fast, slow);
        }

        [TestCase(0f, 3f)]
        [TestCase(5f, 0f)]
        [TestCase(-5f, 3f)]
        [TestCase(5f, -3f)]
        [TestCase(float.NaN, 3f)]
        [TestCase(5f, float.NaN)]
        public void Strength_ZeroOrInvalidInput_ReturnsZero(float mass, float speed)
        {
            Assert.AreEqual(0f, ImpactMath.Strength(mass, speed));
        }
    }
}
