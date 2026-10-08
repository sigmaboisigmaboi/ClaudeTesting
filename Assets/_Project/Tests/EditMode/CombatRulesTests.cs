using NUnit.Framework;
using TheDeep.Combat;
using UnityEngine;

namespace TheDeep.Tests.EditMode
{
    public class CombatRulesTests
    {
        [Test]
        public void ImpactDamage_BelowOrAtThreshold_IsZero()
        {
            Assert.AreEqual(0f, CombatRules.ImpactDamage(5f, 8f, 1.5f, 45f), 0.0001f);
            Assert.AreEqual(0f, CombatRules.ImpactDamage(8f, 8f, 1.5f, 45f), 0.0001f);
        }

        [Test]
        public void ImpactDamage_IsTheScaledAmountAboveTheThreshold()
        {
            // A light crate thrown standing still: about 15 strength.
            Assert.AreEqual(10.5f, CombatRules.ImpactDamage(15f, 8f, 1.5f, 45f), 0.0001f);
        }

        [Test]
        public void ImpactDamage_IsCapped()
        {
            Assert.AreEqual(45f, CombatRules.ImpactDamage(200f, 8f, 1.5f, 45f), 0.0001f);
        }

        [Test]
        public void ImpactDamage_InvalidInput_IsZero()
        {
            Assert.AreEqual(0f, CombatRules.ImpactDamage(float.NaN, 8f, 1.5f, 45f), 0.0001f);
            Assert.AreEqual(0f, CombatRules.ImpactDamage(50f, 8f, 0f, 45f), 0.0001f);
            Assert.AreEqual(0f, CombatRules.ImpactDamage(50f, 8f, 1.5f, 0f), 0.0001f);
        }

        [Test]
        public void StrikeConnects_InFrontAndInReach()
        {
            Assert.IsTrue(CombatRules.StrikeConnects(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 1.5f), 2.2f, 60f));
        }

        [Test]
        public void StrikeMisses_OutOfReach_BehindOrFarAbove()
        {
            Assert.IsFalse(CombatRules.StrikeConnects(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 2.5f), 2.2f, 60f));
            Assert.IsFalse(CombatRules.StrikeConnects(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -1f), 2.2f, 60f));
            Assert.IsFalse(CombatRules.StrikeConnects(Vector3.zero, Vector3.forward, new Vector3(0f, 2f, 1f), 2.2f, 60f));
        }

        [Test]
        public void StrikeConnects_OnlyWithinTheConeInFront()
        {
            // 45 degrees off to the side: inside a 60-degree cone. About 76 degrees: outside.
            Assert.IsTrue(CombatRules.StrikeConnects(Vector3.zero, Vector3.forward, new Vector3(1f, 0f, 1f), 2.2f, 60f));
            Assert.IsFalse(CombatRules.StrikeConnects(Vector3.zero, Vector3.forward, new Vector3(1.6f, 0f, 0.4f), 2.2f, 60f));
        }

        [Test]
        public void KnockbackDisplacement_IsHorizontal_FixedLength_AwayFromTheAttacker()
        {
            Vector3 push = CombatRules.KnockbackDisplacement(new Vector3(0f, 1f, 0f), new Vector3(3f, 0f, 4f), 2.5f, Vector3.forward);

            Assert.AreEqual(1.5f, push.x, 0.0001f);
            Assert.AreEqual(0f, push.y, 0.0001f);
            Assert.AreEqual(2f, push.z, 0.0001f);
        }

        [Test]
        public void KnockbackDisplacement_OnTopOfEachOther_UsesTheFallback()
        {
            Vector3 push = CombatRules.KnockbackDisplacement(Vector3.zero, Vector3.zero, 2.5f, new Vector3(-1f, 0f, 0f));

            Assert.AreEqual(-2.5f, push.x, 0.0001f);
            Assert.AreEqual(0f, push.z, 0.0001f);
        }

        [Test]
        public void IsLethalDrop_OnlyBeyondTheLimit()
        {
            Assert.IsTrue(CombatRules.IsLethalDrop(0f, -1.6f, 1.5f));
            Assert.IsFalse(CombatRules.IsLethalDrop(0f, -1.4f, 1.5f));
            Assert.IsFalse(CombatRules.IsLethalDrop(0f, 2f, 1.5f));
        }
    }
}
