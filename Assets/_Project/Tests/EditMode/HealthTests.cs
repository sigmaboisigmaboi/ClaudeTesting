using NUnit.Framework;
using TheDeep.Combat;

namespace TheDeep.Tests.EditMode
{
    public class HealthTests
    {
        [Test]
        public void StartsFull_AndAlive()
        {
            var health = new Health(100f);

            Assert.AreEqual(100f, health.Max, 0.0001f);
            Assert.AreEqual(100f, health.Current, 0.0001f);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void TakeDamage_ReducesHealth_AndReturnsWhatWasTaken()
        {
            var health = new Health(100f);

            Assert.AreEqual(20f, health.TakeDamage(20f), 0.0001f);
            Assert.AreEqual(80f, health.Current, 0.0001f);
        }

        [Test]
        public void TakeDamage_NeverGoesBelowZero_AndZeroIsDead()
        {
            var health = new Health(30f);

            Assert.AreEqual(30f, health.TakeDamage(50f), 0.0001f);
            Assert.AreEqual(0f, health.Current, 0.0001f);
            Assert.IsTrue(health.IsDead);
        }

        [TestCase(0f)]
        [TestCase(-10f)]
        [TestCase(float.NaN)]
        public void TakeDamage_ZeroNegativeOrInvalid_DoesNothing(float amount)
        {
            var health = new Health(100f);

            Assert.AreEqual(0f, health.TakeDamage(amount), 0.0001f);
            Assert.AreEqual(100f, health.Current, 0.0001f);
        }

        [Test]
        public void NothingMoreHappens_OnceDead()
        {
            var health = new Health(10f);
            health.TakeDamage(10f);

            Assert.AreEqual(0f, health.TakeDamage(5f), 0.0001f);
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void Kill_IsInstantDeath()
        {
            var health = new Health(100f);

            health.Kill();

            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(0f, health.Current, 0.0001f);
        }

        [Test]
        public void InvalidMax_FallsBackToOne()
        {
            Assert.AreEqual(1f, new Health(0f).Max, 0.0001f);
            Assert.AreEqual(1f, new Health(-5f).Max, 0.0001f);
        }
    }
}
