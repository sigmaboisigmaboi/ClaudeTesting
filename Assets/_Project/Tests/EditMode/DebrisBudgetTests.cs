using NUnit.Framework;
using TheDeep.Destruction;

namespace TheDeep.Tests.EditMode
{
    public class DebrisBudgetTests
    {
        [Test]
        public void Add_UnderCap_FreezesNothing()
        {
            var budget = new DebrisBudget<string>(maxActivePieces: 40);

            Assert.IsEmpty(budget.Add("wall", 12));
            Assert.IsEmpty(budget.Add("pillar", 3));
            Assert.AreEqual(15, budget.ActivePieces);
        }

        [Test]
        public void Add_OverCap_FreezesOldestGroupsFirst()
        {
            var budget = new DebrisBudget<string>(maxActivePieces: 20);
            budget.Add("first", 10);
            budget.Add("second", 8);

            var toFreeze = budget.Add("third", 10);

            CollectionAssert.AreEqual(new[] { "first" }, toFreeze);
            Assert.AreEqual(18, budget.ActivePieces);
            Assert.AreEqual(2, budget.ActiveGroups);
        }

        [Test]
        public void Remove_FreesItsPieces()
        {
            var budget = new DebrisBudget<string>(maxActivePieces: 20);
            budget.Add("wall", 12);

            budget.Remove("wall");

            Assert.AreEqual(0, budget.ActivePieces);
            Assert.IsEmpty(budget.Add("next", 20));
        }

        [Test]
        public void Add_GroupBiggerThanCap_IsAllowedOnItsOwn()
        {
            var budget = new DebrisBudget<string>(maxActivePieces: 10);
            budget.Add("small", 4);

            var toFreeze = budget.Add("huge", 15);

            CollectionAssert.AreEqual(new[] { "small" }, toFreeze);
            Assert.AreEqual(15, budget.ActivePieces);
        }
    }
}
