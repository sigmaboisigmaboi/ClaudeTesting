using NUnit.Framework;

namespace TheDeep.Tests.EditMode
{
    // Proves the Unity Test Framework can find, compile, and run our tests.
    // It deliberately tests nothing about the game.
    public class SanityTests
    {
        [Test]
        public void OnePlusOne_EqualsTwo()
        {
            Assert.AreEqual(2, 1 + 1);
        }
    }
}
