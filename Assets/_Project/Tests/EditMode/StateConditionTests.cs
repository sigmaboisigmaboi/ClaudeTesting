using System.Collections.Generic;
using NUnit.Framework;
using TheDeep.Consequences;
using TheDeep.Factions;
using TheDeep.State;

namespace TheDeep.Tests.EditMode
{
    public class StateConditionTests
    {
        static StateCondition.ReputationRequirement Band(Faction faction, ReputationBand min, ReputationBand max)
            => new StateCondition.ReputationRequirement { faction = faction, minBand = min, maxBand = max };

        [Test]
        public void EmptyCondition_IsAlwaysMet()
        {
            var condition = new StateCondition();

            Assert.IsTrue(condition.IsEmpty);
            Assert.IsTrue(condition.IsMet(new WorldState()));
        }

        [Test]
        public void RequiredFact_MetOnlyWhenPresent()
        {
            var condition = new StateCondition { requiredFacts = new List<string> { "outpost.span_closed" } };
            var state = new WorldState();

            Assert.IsFalse(condition.IsMet(state));
            state.Set("outpost.span_closed");
            Assert.IsTrue(condition.IsMet(state));
        }

        [Test]
        public void ForbiddenFact_FailsWhenPresent()
        {
            var condition = new StateCondition { forbiddenFacts = new List<string> { "outpost.span_closed" } };
            var state = new WorldState();

            Assert.IsTrue(condition.IsMet(state));
            state.Set("outpost.span_closed");
            Assert.IsFalse(condition.IsMet(state));
        }

        [Test]
        public void RequiredDestroyedId_MetOnlyWhenDestroyed()
        {
            var condition = new StateCondition { requiredDestroyedIds = new List<string> { "p2span.wall_alcove" } };
            var state = new WorldState();

            Assert.IsFalse(condition.IsMet(state));
            state.MarkDestroyed("p2span.wall_alcove");
            Assert.IsTrue(condition.IsMet(state));
        }

        [Test]
        public void ReputationRequirement_ChecksTheFactionsBand()
        {
            var hostileToConcord = new StateCondition
            {
                reputation = new List<StateCondition.ReputationRequirement> { Band(Faction.Concord, ReputationBand.Hostile, ReputationBand.Hostile) }
            };
            var state = new WorldState();

            Assert.IsFalse(hostileToConcord.IsMet(state));    // 0 = Neutral
            state.AddReputation(Faction.Concord, -40);
            Assert.IsTrue(hostileToConcord.IsMet(state));     // -40 = Hostile
            state.AddReputation(Faction.Hollowers, -100);
            Assert.IsTrue(hostileToConcord.IsMet(state));     // other factions don't matter
        }

        [Test]
        public void AllPartsMustHold()
        {
            var condition = new StateCondition
            {
                requiredFacts = new List<string> { "a" },
                reputation = new List<StateCondition.ReputationRequirement> { Band(Faction.Hollowers, ReputationBand.Friendly, ReputationBand.Friendly) }
            };
            var state = new WorldState();
            state.Set("a");

            Assert.IsFalse(condition.IsMet(state));
            state.AddReputation(Faction.Hollowers, 20);
            Assert.IsTrue(condition.IsMet(state));
        }
    }
}
