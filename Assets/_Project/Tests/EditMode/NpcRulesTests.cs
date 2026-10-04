using NUnit.Framework;
using TheDeep.NPC;
using TheDeep.World;
using UnityEngine;

namespace TheDeep.Tests.EditMode
{
    public class NpcRulesTests
    {
        [TestCase(0, 2, 1)]
        [TestCase(1, 2, 0)]
        [TestCase(2, 3, 0)]
        [TestCase(0, 0, 0)]
        public void NextStop_LoopsAround(int current, int count, int expected)
        {
            Assert.AreEqual(expected, NpcRules.NextStop(current, count));
        }

        [Test]
        public void AwayDirection_IsHorizontal_AndPointsAwayFromTheSource()
        {
            Vector3 away = NpcRules.AwayDirection(new Vector3(0, 5, 0), new Vector3(3, 0, 4), Vector3.forward);

            Assert.AreEqual(0.6f, away.x, 0.0001f);
            Assert.AreEqual(0f, away.y, 0.0001f);
            Assert.AreEqual(0.8f, away.z, 0.0001f);
        }

        [Test]
        public void AwayDirection_OnTopOfTheSource_UsesTheFallback()
        {
            Vector3 away = NpcRules.AwayDirection(Vector3.zero, Vector3.zero, new Vector3(-2, 1, 0));

            Assert.AreEqual(-1f, away.x, 0.0001f);
            Assert.AreEqual(0f, away.z, 0.0001f);
        }

        [Test]
        public void ShoveDistance_ScalesWithStrength_AndIsCapped()
        {
            Assert.AreEqual(0.4f, NpcRules.ShoveDistance(10f, 0.04f, 1.5f), 0.0001f);
            Assert.AreEqual(1.5f, NpcRules.ShoveDistance(1000f, 0.04f, 1.5f), 0.0001f);
            Assert.AreEqual(0f, NpcRules.ShoveDistance(0f, 0.04f, 1.5f), 0.0001f);
            Assert.AreEqual(0f, NpcRules.ShoveDistance(float.NaN, 0.04f, 1.5f), 0.0001f);
        }

        [Test]
        public void Disturbance_ReachesOnlyWithinItsRadius()
        {
            var disturbance = new Disturbance(new Vector3(0, 0, 0), 8f);

            Assert.IsTrue(disturbance.Reaches(new Vector3(0, 0, 7.9f)));
            Assert.IsFalse(disturbance.Reaches(new Vector3(0, 0, 8.1f)));
            Assert.IsFalse(new Disturbance(Vector3.zero, 0f).Reaches(Vector3.zero));
        }

        [Test]
        public void Disturbance_IsUnderfoot_OnlyOnTopOfTheBrokenObject()
        {
            // Like the Span's bridge deck: 3 x 0.2 x 6.6, centered at (0, 0.1, 3).
            var deck = new Disturbance(new Vector3(0, 0.1f, 3), 8f, new Bounds(new Vector3(0, 0.1f, 3), new Vector3(3, 0.2f, 6.6f)));

            Assert.IsTrue(deck.IsUnderfoot(new Vector3(0.5f, 0f, 3f)));      // walking across it
            Assert.IsTrue(deck.IsUnderfoot(new Vector3(0f, 0.2f, 6f)));      // standing on top, far end
            Assert.IsFalse(deck.IsUnderfoot(new Vector3(0f, 0f, -2f)));      // on the ledge before it
            Assert.IsFalse(deck.IsUnderfoot(new Vector3(2f, 0f, 3f)));       // beside it (in the air)
            Assert.IsFalse(deck.IsUnderfoot(new Vector3(0f, -4f, 3f)));      // far below, on the chasm floor
            Assert.IsFalse(new Disturbance(Vector3.zero, 8f).IsUnderfoot(Vector3.zero)); // no footprint
        }
    }
}
