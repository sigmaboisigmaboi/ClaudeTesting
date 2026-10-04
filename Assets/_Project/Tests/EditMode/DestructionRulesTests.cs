using NUnit.Framework;
using TheDeep.Destruction;

namespace TheDeep.Tests.EditMode
{
    public class DestructionRulesTests
    {
        [TestCase(3f, 5f)]
        [TestCase(5f, 5f)]
        [TestCase(0f, 5f)]
        public void DamageFromImpact_AtOrBelowThreshold_IsZero(float strength, float threshold)
        {
            Assert.AreEqual(0f, DestructionRules.DamageFromImpact(strength, threshold));
        }

        [Test]
        public void DamageFromImpact_AboveThreshold_IsTheAmountAbove()
        {
            Assert.AreEqual(15f, DestructionRules.DamageFromImpact(20f, 5f), 0.0001f);
        }

        [Test]
        public void DamageFromImpact_InvalidInput_IsZero()
        {
            Assert.AreEqual(0f, DestructionRules.DamageFromImpact(float.NaN, 5f));
            Assert.AreEqual(0f, DestructionRules.DamageFromImpact(20f, float.NaN));
        }

        [Test]
        public void ApplyDamage_ReducesIntegrity_AndNeverGoesBelowZero()
        {
            Assert.AreEqual(15f, DestructionRules.ApplyDamage(25f, 10f), 0.0001f);
            Assert.AreEqual(0f, DestructionRules.ApplyDamage(25f, 40f));
        }

        [Test]
        public void ApplyDamage_ZeroNegativeOrInvalidDamage_LeavesIntegrityUnchanged()
        {
            Assert.AreEqual(25f, DestructionRules.ApplyDamage(25f, 0f));
            Assert.AreEqual(25f, DestructionRules.ApplyDamage(25f, -5f));
            Assert.AreEqual(25f, DestructionRules.ApplyDamage(25f, float.NaN));
        }

        [Test]
        public void IsBroken_OnlyAtZeroIntegrity()
        {
            Assert.IsTrue(DestructionRules.IsBroken(0f));
            Assert.IsFalse(DestructionRules.IsBroken(0.5f));
        }

        [Test]
        public void ShouldCollapse_OnlyWhenItHasSupportsAndAllAreGone()
        {
            Assert.IsTrue(DestructionRules.ShouldCollapse(supportCount: 2, supportsStillStanding: 0));
            Assert.IsFalse(DestructionRules.ShouldCollapse(supportCount: 2, supportsStillStanding: 1));
            Assert.IsFalse(DestructionRules.ShouldCollapse(supportCount: 0, supportsStillStanding: 0));
        }
    }
}
